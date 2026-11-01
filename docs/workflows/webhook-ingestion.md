# Webhook Ingestion Workflow

## 1. Overview

The Webhook Ingestion workflow represents the primary entry point for funds arriving into the Finova digital wallet platform. It is engineered to satisfy two conflicting requirements:
- **Fast, Non-Blocking Edge Response**: Returning a response to external banks within milliseconds to prevent HTTP gateway timeouts.
- **Strict Verification & Durability**: Authenticating callers cryptographically, enforcing idempotency, validating payloads, and guaranteeing durability before acknowledging receipt.

---

## 2. Ingestion Lifecycle Flowchart

```mermaid
flowchart TD
    Start(["Incoming HTTP POST /api/bank-webhook/{bankCode}/transaction"]) --> RouteCheck{"Extract bankCode from Route"}
    RouteCheck -->|Missing / Invalid| R400["400 Bad Request: Missing bank code"]

    RouteCheck --> AuthCheck{"AuthorizeBankTransactionWebhook<br/>Header & Timestamp Check"}
    AuthCheck -->|Missing Headers / Clock Skew| R400Auth["400 Bad Request: Invalid headers/timestamp"]

    AuthCheck --> HMAC{"Compute HMAC-SHA256<br/>(80KB Streaming Chunk Hash)"}
    HMAC -->|Signature Mismatch| R401["401 Unauthorized: Invalid Webhook Key"]

    HMAC --> IdempCheck{"Idempotency Filter<br/>(Redis: idempotency:{bank}:{key})"}
    IdempCheck -->|In-Progress (Status 102)| R409["409 Conflict: Concurrent delivery"]
    IdempCheck -->|Already Completed| R200Cache["200 OK: Cached Response"]

    IdempCheck -->|New Request| SetRedis["Set Redis Key = in-progress (TTL 3m)"]
    SetRedis --> ValPayload{"WebhookDeliveryValidator<br/>Check Empty, Bank Contract, DB Duplicates"}
    ValPayload -->|Delivery ID Exists in DB| R400Dup["400 Bad Request: Already sent to us"]
    ValPayload -->|Bank Not Contracted| R400Contract["400 Bad Request: Not contracted"]

    ValPayload --> Outbox["BankServices.WriteToOutboxAsync<br/>Calculate Payload Byte Size"]
    Outbox --> DBInsert[("INSERT INTO banks.InboundWebhookMessages<br/>Status = Pending (0), Payload = JSONB")]
    DBInsert --> CacheResponse["Update Redis Key with 200 OK Value"]
    CacheResponse --> R200["200 OK: 'Transaction received'"]
```

---

## 3. Step-by-Step Lifecycle Description

### Step 1: Partner Bank Submits Webhook
The partner bank sends an HTTP POST request to:
`POST /api/bank-webhook/{bankCode}/transaction`
with headers:
- `x-{BankCode}-Webhook-Signature`: The hexadecimal HMAC-SHA256 hash.
- `x-{BankCode}-Webhook-Timestamp`: Unix epoch timestamp in seconds.
- `x-Idempotency-key`: Unique delivery/client key.
- `Content-Type: application/json`

Payload structure:
```json
{
  "webhookDeliveryId": "a1b0c0dc-1111-0110-111d-1f1111567891",
  "bankCode": 1,
  "payload": [
    {
      "transactionId": "12a85f64-3333-4562-b3fc-2c113f66afa6",
      "webhookDeliveryId": "a1b0c0dc-1111-0110-111d-1f1111567891",
      "source": "a1b2c3d4-e5f6-7890-1234-56789abcdef0",
      "destination": "f47ac10b-58cc-4372-a567-0e02b2c3d4e5",
      "currency": "USD",
      "eventType": "DEPOSIT_SUCCESS",
      "accountNumber": "****1234",
      "amount": 1500.00,
      "timestamp": "2026-10-23T14:32:00Z",
      "transaction": "1500.00//REF9482//202610231432"
    }
  ]
}
```

### Step 2: Bank Route Identification
In [RequestUtilities.cs](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Shared/Services/RequestUtilities.cs), the route parameter `{bankCode}` is retrieved. If empty or invalid, the pipeline halts with `400 Bad Request`.

### Step 3: Webhook Cryptographic Authentication
[BankTransactionWebhookAuthorizationAuthorizationFilter](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Modules/Banks.Module/Application/Services/WebhookFilter/BankTransactionWebhookAuthorizationAuthorizationFilter.cs) executes:
1. Validates that `x-{BankCode}-Webhook-Signature` and `x-{BankCode}-Webhook-Timestamp` are present.
2. Checks clock drift: Rejects if the timestamp is more than **3 minutes behind** or **5 minutes ahead** of current server UTC time.
3. Retrieves the bank's configured secret key from `WebhooksHeaders:{BankCode}:SecretKey`.
4. Streams the request body in 80 KB chunks through `IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, secretKey)`.
5. Compares expected and received signatures with `CryptographicOperations.FixedTimeEquals`. If signatures do not match, returns `401 Unauthorized`.

*(See [Webhook Authentication](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/docs/workflows/webhook-authentication.md) for complete details).*

### Step 4: Distributed Idempotency Check (Redis)
[IdempotencyAttribute](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Modules/Banks.Module/Presentation/Filters/IdempotencyAttribute.cs) inspects Redis for key:
`idempotency:{bankCode}:{idempotencyKey}`
- **Cache Hit (Status 102 - Processing)**: Another request with the same idempotency key is currently running. Returns `409 Conflict`.
- **Cache Hit (Completed 2xx Response)**: Returns the cached response object immediately.
- **Cache Miss**: Atomically sets `idempotency:{bankCode}:{idempotencyKey}` with status `102 (Processing)` and TTL = 3 minutes (`When.NotExists`).

### Step 5: Webhook Delivery Validation
[WebhookDeliveryValidator](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Modules/Banks.Module/Application/Services/WebhookFilter/WebhookDeliveryValidator.cs) performs domain validation:
1. `WebhookDeliveryId` must not be `Guid.Empty`.
2. `Payload` must contain at least 1 element.
3. `BankCode` must match an active contracted bank in the `banks."Banks"` table (`Contracted == true`).
4. Checks PostgreSQL: Ensures `WebhookDeliveryId` does not already exist in `banks."InboundWebhookMessages"` via `ExistsByDeliveryIdAsync`.

### Step 6: Payload Size Calculation & Outbox Persistence
In [BankServices.WriteToOutboxAsync](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Modules/Banks.Module/Application/Services/BankServices.cs#L20):
1. Calculates exact UTF-8 byte size of the raw JSON string:
   ```csharp
   var jsonStr = request.Payload.RootElement.GetRawText();
   long payloadSizeBytes = Encoding.UTF8.GetByteCount(jsonStr);
   ```
2. Instantiates [InboundWebhookMessage](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/Wallet/Modules/Banks.Module/Contracts/Entities/InboundWebhookMessage.cs):
   - `WebhookDeliveryId = request.WebhookDeliveryId`
   - `BankId = bank.Id`
   - `BankCode = request.BankCode`
   - `Payload = request.Payload` (persisted as native PostgreSQL `jsonb`)
   - `PayloadSizeBytes = payloadSizeBytes`
   - `ReceivedAtUtc = DateTime.UtcNow`
   - `Status = InboundTransactionWebhookOutboxMessageStatus.Pending` (0)
3. Persists to database: `await outboxWriter.CreateAsync(msg, ct); await outboxWriter.SaveChangesAsync(ct);`.

### Step 7: Response Caching and Return
- The `IdempotencyAttribute` updates Redis key with the final `200 OK` response payload.
- Returns HTTP `200 OK`:
  ```json
  {
    "isSuccess": true,
    "value": "Transaction received",
    "error": null
  }
  ```

---

## 4. Failure Scenarios and Recovery

| Failure Mode | Detection Point | System Behavior | Data Impact |
| :--- | :--- | :--- | :--- |
| **Invalid Signature** | `BankWebhookAuthorizationService` | Aborts pipeline; returns `401 Unauthorized` | Zero database rows created. Request discarded. |
| **Tampered / Stale Timestamp** | `WebhookRequestAuthenticator` | Returns `400 Bad Request` ("Timestamp outside allowed window") | Zero database rows created. Replay attack blocked. |
| **Duplicate Delivery (In-flight)**| `IdempotencyAttribute` | Detects Redis status `102`; returns `409 Conflict` | Prevents concurrent duplicate database insertion. |
| **Duplicate Delivery (Retried)** | `WebhookDeliveryValidator` / PostgreSQL Unique Index | Detects existing `WebhookDeliveryId`; returns `400 Bad Request` | Prevented by unique index `IX_InboundWebhookMessages_WebhookDeliveryId`. |
| **Redis Server Outage** | StackExchange.Redis | Connection multiplexer logs warning; if Redis fails, requests fail gracefully to 500 or proceed depending on policy. | DB-level constraints protect against duplicates even without Redis. |
| **Database Connection Failure** | EF Core / Npgsql | Global exception handler catches error; returns `500 Internal Server Error` | Partner bank receives 500 and will retry delivery later. |

---

## 5. Related Documentation

- [Webhook Authentication Workflow](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/docs/workflows/webhook-authentication.md)
- [Outbox Processing Workflow](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/docs/workflows/outbox-processing.md)
- [Idempotency Strategy](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/docs/workflows/idempotency.md)
- [ADR-0002: Outbox Pattern](file:///Users/mostafa/IamBack/dotnet-csharp/Finova/docs/decisions/0002-outbox-pattern.md)
