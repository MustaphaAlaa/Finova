using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transaction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class transactions_init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transactions");

            migrationBuilder.CreateTable(
                name: "bank_transaction_stage",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StagedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_transaction_stage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankTransactions",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageProcessingRecords",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageProcessingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransacitonProcessingRecords",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransacitonProcessingRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bank_transaction_stage_StagedAtUtc",
                schema: "transactions",
                table: "bank_transaction_stage",
                column: "StagedAtUtc",
                filter: "\"InsertedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bank_transaction_stage_TransactionId_Reference",
                schema: "transactions",
                table: "bank_transaction_stage",
                columns: new[] { "TransactionId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_TransactionId_Reference",
                schema: "transactions",
                table: "BankTransactions",
                columns: new[] { "TransactionId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageProcessingRecords_MessageId",
                schema: "transactions",
                table: "MessageProcessingRecords",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransacitonProcessingRecords_TransactionId",
                schema: "transactions",
                table: "TransacitonProcessingRecords",
                column: "TransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_transaction_stage",
                schema: "transactions");

            migrationBuilder.DropTable(
                name: "BankTransactions",
                schema: "transactions");

            migrationBuilder.DropTable(
                name: "MessageProcessingRecords",
                schema: "transactions");

            migrationBuilder.DropTable(
                name: "TransacitonProcessingRecords",
                schema: "transactions");
        }
    }
}
