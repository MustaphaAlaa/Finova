# Finova — High-Throughput Digital Wallet Platform

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL 17](https://img.shields.io/badge/PostgreSQL-17-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis 7.4](https://img.shields.io/badge/Redis-7.4-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ 4](https://img.shields.io/badge/RabbitMQ-4.0-FF6600?logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![CI Build & Test](https://github.com/MustaphaAlaa/Finova/actions/workflows/finova-wallet-ci.yml/badge.svg)](https://github.com/MustaphaAlaa/Finova/actions)
[![Tests Passing](https://img.shields.io/badge/Tests-152%20Passed-brightgreen)](Wallet/Tests/UnitTesting)

> **Educational & Engineering Reference**: Finova is an enterprise-grade digital wallet backend engineering showcase built on .NET 10. It is engineered to solve the complex real-world challenges of high-volume financial webhook ingestion, cryptographic multi-tenant bank authentication, asynchronous outbox decoupling, zero-allocation transaction parsing, and two-tier idempotency.

---

## 1. Project Overview

In a digital wallet ecosystem, banking partners notify the platform when funds are deposited or moved. Unlike consumer webhooks where payloads are small and occasional, commercial banking webhooks present rigorous distributed systems problems:

- **Bursty, High-Volume Payloads**: A single webhook delivery can batch up to **1,000 transactions**, each containing encoded transaction lines. Synchronous HTTP request processing would tie up web server threads, create cascading timeouts, and degrade database responsiveness.
- **Untrusted Public Surface**: Webhook endpoints must be publicly exposed to the internet so banking partners can reach them. Without robust, low-overhead cryptographic authentication, malicious actors can flood the platform or forge transactions.
- **Duplicate & Out-of-Order Deliveries**: Banking networks operate on "at-least-once" delivery semantics. Network retries, timeout reconnections, or transient bank failures frequently deliver duplicate webhooks or individual transactions repeatedly.
- **Variable Wire Formats**: Different banks transmit transaction logs using proprietary delimited string formats rather than uniform JSON bodies.
- **Reliability Under Load**: Processing failures must never drop incoming webhook notifications or corrupt user balances.

Finova addresses these engineering challenges with a **Modular Monolith architecture** paired with an **Outbox Pattern**, **PostgreSQL binary COPY bulk ingestion**, **System.Threading.Channels bounded processing**, and **Redis + Database-level multi-tier idempotency**.

---

## 2. Engineering Challenges & Architectural Decisions

Building Finova was initially estimated to take a few days for a conventional CRUD wallet. However, the project developed over approximately a month as deep domain exploration, failure mode analysis, and performance tuning surfaced critical engineering trade-offs:

### 2.1 Domain Scope & Avoiding Overengineering
During initial iterations, effort was diverted into constructing a full standalone Bank Simulator project with simulated account holders, ledger state machines, and auxiliary banking services. It became clear that simulating the entire banking partner ecosystem distracted from the core mission: building an ultra-resilient, high-throughput digital wallet ingestion and processing engine. Development was intentionally refocused on the main wallet application, keeping complexity strictly aligned with business requirements.

### 2.2 Public Webhook Security: Shared Secrets vs. Rotating URLs
A public webhook endpoint cannot rely on network perimeter security. A concept considered during design was generating rotating webhook URLs per bank. However, rotating URLs creates operational overhead (coordinating rotation schedules, handling flight transitions, synchronization bugs) without providing cryptographic integrity over the payload.

Finova instead implements **HMAC-SHA256 signature verification**:
- Every contracted bank possesses a secure shared secret configured in the application.
- The bank transmits an HMAC signature (`x-{BankCode}-Webhook-Signature`) and a Unix timestamp header (`x-{BankCode}-Webhook-Timestamp`).
- The application validates that the timestamp falls within a **-3 minute to +5 minute tolerance window** to prevent replay attacks.
- The request body is streamed in **80 KB chunks** directly through `IncrementalHash.CreateHMAC` without allocating the entire payload in memory.
- The computed hash is compared to the incoming signature using constant-time evaluation (`CryptographicOperations.FixedTimeEquals`) to eliminate timing attacks.

### 2.3 Early Validation via Action Filters Before Persistence
Deserializing and persisting untrusted 3 MB payloads containing 1,000 transactions before authentication wastes CPU cycles and database I/O. Finova utilizes ASP.NET Core Action and Authorization Filters:
1. `AuthorizeBankTransactionWebhookAttribute`: Verifies route parameters, headers, timestamp skew, and HMAC signatures.
2. `IdempotencyAttribute`: Checks Redis for in-flight or already completed deliveries.
3. `WebhookDeliveryValidator`: Enforces payload schema, validates bank contract status, and checks for database delivery existence before queuing.

### 2.4 The Outbox Pattern & Payload-Size-Aware Batching
Synchronously processing 1,000 transactions inside an HTTP POST request would risk client HTTP timeouts (banks typically enforce strict 3–5 second timeouts). Finova decouples reception from execution:
1. **HTTP Ingestion**: Validates the webhook and writes the raw payload as `jsonb` into `banks."InboundWebhookMessages"` with status `Pending` (`0`). Returns an immediate `200 OK` to the bank.
2. **Batch Claiming**: A background worker claims batches using PostgreSQL `FOR UPDATE SKIP LOCKED`.
3. **Payload-Size-Aware Claiming**: Conventional fixed-row batching (e.g., "claim 100 messages") causes memory spikes when 100 messages each contain 1,000 transactions (100,000 transactions in memory), or underutilizes workers when messages contain only 1 transaction. Finova calculates `PayloadSizeBytes` upon ingestion and uses a SQL window function (`SUM("PayloadSizeBytes") OVER (...)`) to cap claimed batches around a target cumulative payload size (e.g., 10 MB or 1,500 rows), guaranteeing that even single oversized messages are claimed safely.
4. **Publishing & Bounded In-Memory Pipeline**: The worker publishes claimed messages via MassTransit over RabbitMQ. The consumer queues work items into a bounded `System.Threading.Channels.Channel` drained by 8 parallel readers.
5. **Two-Phase Binary Staging & Flushing**: Validated transactions are parsed and written using PostgreSQL's binary `COPY ... FROM STDIN` into temporary staging tables (`bank_transaction_stage`), which are then batch-flushed to `BankTransactions` by a dedicated flusher background service.

### 2.5 Two-Tier Idempotency (Redis + PostgreSQL)
To guarantee at-most-once financial side effects:
- **Tier 1 (Redis Action Filter)**: Checks `idempotency:{bankCode}:{idempotencyKey}`. If a request is currently being processed, it returns an immediate `409 Conflict`. If already completed, it returns the cached response.
- **Tier 2 (PostgreSQL Constraints)**:
  - `banks."InboundWebhookMessages"` enforces a unique index on `WebhookDeliveryId`.
  - `transactions."bank_transaction_stage"` enforces a composite unique index on `(TransactionId, Reference)` with `ON CONFLICT DO NOTHING`.
  - `transactions."BankTransactions"` enforces a composite unique index on `(TransactionId, Reference)`.

### 2.6 Zero-Allocation Span Parsing via Bank-Specific Factories
Banks send non-standard string formats (e.g., `Date,Amount#Ref#Key/Val/Key/Val` or `Amount//Ref//Date`). Finova implements a **Factory Pattern** backed by .NET Keyed Services. Inside the parsers, string splitting and substring operations are replaced with `ReadOnlySpan<char>` and `Span.Split('/')`, parsing `DateTime` and `decimal` directly from memory slices without allocating heap strings.

---

## 3. Measured Performance & Load Testing Results

The architecture was validated under high-stress batch conditions:

| Metric | Measured Value | Testing Context & Details |
| :--- | :--- | :--- |
| **Total Ingested Transactions** | **~1,000,000 transactions** | 1,000 HTTP POST requests, each containing 1,000 transactions in the payload array |
| **End-to-End Processing Time** | **< 2 minutes** (< 120 seconds) | From initial HTTP request submission to final persistence in the `BankTransactions` table |
| **Effective Throughput** | **> 8,300+ transactions / sec** | Sustained end-to-end throughput across validation, parsing, staging, and flushing |
| **Batch Flushing Batch Size** | **32,000 rows / batch** | Drained continuously by `BankTransactionStageFlusherWorker` |
| **Data Integrity Verification** | **0 duplicate transactions** | Verified via SQL counts (`Queries.sql`) with duplicate rejection tracking |

> [!NOTE]
> *Load Testing Tool*: The custom load generator used to produce these benchmark results is being prepared for inclusion in this repository in an upcoming release.

---

## 4. Key Features

- **Modular Monolith**: Strict architectural boundaries separating [Banks.Module](Wallet/Modules/Banks.Module), [Transactions.Module](Wallet/Modules/Transactions.Module), and [Identity.Module](Wallet/Modules/Identity.Module), communicating solely through explicit contracts and asynchronous messages.
- **Cryptographic Webhook Authentication**: Bank-specific HMAC-SHA256 signature verification with Unix timestamp drift tolerance (-3 min / +5 min) and constant-time equality checks.
- **Streaming Action Filter Validation**: Rejects malformed bodies, untrusted banks, and invalid signatures before hitting controller business logic or consuming database I/O.
- **Two-Tier Idempotency**: Redis distributed lock/cache with HTTP 409 Conflict detection combined with database-level unique constraints.
- **Payload-Size-Aware Outbox Ingestion**: Background message claiming using window functions (`SUM() OVER`) and `FOR UPDATE SKIP LOCKED` to balance worker memory and throughput.
- **MassTransit & RabbitMQ Asynchronous Pipeline**: Decoupled message distribution with retry policies and Dead Letter Queuing capability.
- **System.Threading.Channels Processing**: Bounded channel queueing with 8 concurrent reader workers to maximize multi-core CPU utilization.
- **High-Performance PostgreSQL Staging & Bulk COPY**: Direct binary streaming import (`NpgsqlBinaryImporter`) into temporary tables followed by set-based conflict-resolving inserts.
- **Zero-Allocation Bank String Parsers**: Memory-efficient parsing using `ReadOnlySpan<char>` for Acme and PayTech formats.
- **Payment Request XML Generation**: Outbound money transfer service generating schema-compliant banking XML documents (`PaymentRequestMessage`).
- **Comprehensive Unit Testing**: 152 automated unit tests across all domain, application, and worker layers.

---

## 5. Technology Stack

| Category | Technologies & Tools |
| :--- | :--- |
| **Runtime & Language** | .NET 10, C# 13/14, ASP.NET Core Web API, Microsoft Extensions Hosting |
| **Architecture** | Modular Monolith, Domain-Driven Design (DDD), Outbox Pattern, Channel Producer-Consumer |
| **Data Access & Storage** | PostgreSQL 17, Entity Framework Core 10, Npgsql 10 (Binary COPY, `NpgsqlDataSource`) |
| **Caching & In-Memory** | Redis 7.4-alpine, StackExchange.Redis, `System.Threading.Channels` |
| **Messaging & Queues** | RabbitMQ 4 (Management Alpine), MassTransit 8.5.12 |
| **Security & Cryptography** | HMAC-SHA256 (`IncrementalHash`), `CryptographicOperations.FixedTimeEquals`, ASP.NET Core Identity |
| **Containerization** | Docker, Docker Compose (Separate Development and Production profiles) |
| **Testing** | xUnit 2.9, Moq 4.21, FluentAssertions 6.12, AutoFixture 4.18, Coverlet |
| **CI/CD** | GitHub Actions (Matrix build, automated testing, Docker Compose validation, image publishing) |

---

## 6. Architecture at a Glance

The solution is divided into two primary runtime hosts and three domain modules:
  
For comprehensive architectural design, module interfaces, and sequence flows, refer to:
- [Architecture Overview](docs/architecture/overview.md)
- [Modular Monolith Architecture](docs/architecture/modular-monolith.md)
- [Module Catalog & Responsibilities](docs/architecture/modules.md)
- [Dependency Rules](docs/architecture/dependency-rules.md)
- [Database Design & Schema Isolation](docs/architecture/database-design.md)
- [Background Processing](docs/architecture/background-processing.md)

---

## 7. Prerequisites

Before running the application locally or in containers, ensure you have:

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`dotnet --version` >= 10.0.100)
- [Docker Engine & Docker Compose v2](https://docs.docker.com/engine/install/) (`docker compose version` >= 2.20)
- [Entity Framework Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)
- `curl` or HTTP client for webhook simulation testing

---

## 8. Getting Started

### 8.1 Clone the Repository
```bash
git clone https://github.com/MustaphaAlaa/Finova.git
cd Finova
```

### 8.2 Environment Configuration
Finova provides configuration via `.env` and `appsettings.{Environment}.json`. Copy or verify your local `.env`:
```bash
cp .env.example .env # or verify existing .env
```

Ensure the following variables are specified:
```env
# PostgreSQL Credentials
POSTGRES_USER=finovaAdmin
POSTGRES_PASSWORD=your_secure_postgres_password
POSTGRES_DB=FinovaDb

# RabbitMQ Credentials
RABBITMQ_DEFAULT_USER=finova
RABBITMQ_DEFAULT_PASS=your_secure_rabbitmq_password
```
  
### 8.3 Starting Infrastructure via Docker Compose
To launch PostgreSQL 17, Redis 7.4, RedisInsight, and RabbitMQ:

```bash
docker compose up -d finova-database redis redis-insight rabbitmq
```

Verify service health:
```bash
docker compose ps
```

### 8.4 Applying Database Migrations
Database migrations are automatically applied on application startup via `MigrateFinovaDatabasesAsync()`. To apply them manually from the CLI:

```bash
# Apply Banks Module migrations
dotnet ef database update --project Wallet/Modules/Banks.Module/Infrastructure --startup-project Wallet/Host/Finova.API --context BanksDbContext

# Apply Transactions Module migrations
dotnet ef database update --project Wallet/Modules/Transactions.Module/Infrastructure --startup-project Wallet/Host/Finova.API --context TransactionsDbContext

# Apply Identity Module migrations
dotnet ef database update --project Wallet/Modules/Identity.Module/Infrastructure --startup-project Wallet/Host/Finova.API --context IdentityModuleDbContext
```

### 8.5 Running the Solution Locally
To run the Web API and Outbox Worker side-by-side:

**Terminal 1 — API Host:**
```bash
cd Wallet/Host/Finova.API
dotnet run
```
*API will listen at `http://localhost:8080` (or `http://localhost:5074` depending on launch profile).*

**Terminal 2 — Outbox Worker Host:**
```bash
cd Wallet/Host/Finova.Workers/OutboxWorker
dotnet run
```

### 8.6 Running Full Stack in Docker
To run both the application services and infrastructure containers together:

```bash
docker compose up --build -d
```

Service endpoints:
- **Finova Web API**: `http://localhost:8080`
- **Health Check**: `http://localhost:8080/health`
- **RabbitMQ Management UI**: `http://localhost:15672` (User: `finova`, Pass: configured in `.env`)
- **RedisInsight UI**: `http://localhost:5540`
- **PostgreSQL**: `localhost:6800` (mapped from container `5432`)

---

## 9. Running Tests

The solution contains 152 automated unit tests across all layers.

### Run All Tests
```bash
dotnet test Wallet/Finova.sln --configuration Release
```

### Run Tests Per Project
```bash
# Banks Module Tests (84 tests)
dotnet test Wallet/Tests/UnitTesting/Banks.Module.UnitTests/

# Transactions Module Tests (49 tests)
dotnet test Wallet/Tests/UnitTesting/Transactions.Module.UnitTests/

# Outbox Worker Tests (8 tests)
dotnet test Wallet/Tests/UnitTesting/OutboxWorker.UnitTests/

# Shared Utilities Tests (10 tests)
dotnet test Wallet/Tests/UnitTesting/Shared.UnitTests/

# Identity Module Tests (1 test)
dotnet test Wallet/Tests/UnitTesting/Identity.Module.UnitTests/
```

For the test suite organization, see [Unit Tests](Wallet/Tests/UnitTesting/) and [Integration Tests](Wallet/Tests/Integrations/).

---

## 10. Documentation Index

The `docs/` directory contains architecture documentation and workflow guides:

### Architecture
- [Architecture Overview](docs/architecture/overview.md) — System boundaries, components, and topology.
- [Modular Monolith Architecture](docs/architecture/modular-monolith.md) — Rationale, benefits, and trade-offs.
- [Module Catalog](docs/architecture/modules.md) — Detailed breakdown of Banks, Transactions, Identity, and Shared.
- [Dependency Rules](docs/architecture/dependency-rules.md) — Enforcement of boundaries and layer flow.
- [Database Design](docs/architecture/database-design.md) — Schema isolation, table definitions, and indexes.
- [Background Processing](docs/architecture/background-processing.md) — Outbox worker, MassTransit, channels, and stage flusher.

### Workflows
- [Webhook Ingestion](docs/workflows/webhook-ingestion.md) — End-to-end lifecycle of an incoming bank webhook.
- [Webhook Authentication](docs/workflows/webhook-authentication.md) — HMAC-SHA256 verification and replay protection.
- [Outbox Processing](docs/workflows/outbox-processing.md) — Payload-size-aware claiming and concurrency.
- [Transaction Processing](docs/workflows/transaction-processing.md) — Consumption, validation, staging, and flushing.
- [Idempotency](docs/workflows/idempotency.md) — Redis distributed lock and database constraints.
- [Transaction Parsing](docs/workflows/transaction-parsing.md) — Factory pattern and zero-allocation span parsing.


