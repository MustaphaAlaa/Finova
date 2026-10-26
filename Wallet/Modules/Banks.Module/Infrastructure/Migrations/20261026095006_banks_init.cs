using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Banks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class banks_init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "banks");

            migrationBuilder.CreateTable(
                name: "Banks",
                schema: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Contracted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundWebhookMessages",
                schema: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookDeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundWebhookMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                schema: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Banks_BankId",
                        column: x => x.BankId,
                        principalSchema: "banks",
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "banks",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankAccountsBalances",
                schema: "banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountsBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountsBalances_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalSchema: "banks",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "banks",
                table: "Banks",
                columns: new[] { "Id", "Contracted", "Name" },
                values: new object[,]
                {
                    { new Guid("0eb2b1cc-6144-4bf9-a5f0-27b937b94e22"), false, "GothamPayments" },
                    { new Guid("4b42ad75-6775-44af-931f-f5fb3f38b75a"), true, "PayTech" },
                    { new Guid("c9337615-fb2b-4be1-9da0-c9704d546bf6"), true, "Acme" },
                    { new Guid("d872cb88-d531-4268-b2c0-b140d3cd8b6f"), false, "BanqueTech" }
                });

            migrationBuilder.InsertData(
                schema: "banks",
                table: "Currencies",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("00596123-1c69-4603-ac90-4bbe433621e9"), "EGP", "Egyptian Pound" },
                    { new Guid("66bbf057-896c-4223-a8cb-915599fa3003"), "GBP", "British Pound Sterling" },
                    { new Guid("66deb98a-534f-4a76-9cc3-17c60b54fc94"), "SAR", "Saudi Riyal" },
                    { new Guid("704ed0f7-ab61-444f-96ee-d249eab626e7"), "AED", "UAE Dirham" },
                    { new Guid("ba922e41-9a9c-422d-8810-e634372e7075"), "EURO", "Euro" },
                    { new Guid("cce9fc73-d6f7-4604-bb0b-6358a65e2053"), "USD", "US Dollar" },
                    { new Guid("d85f635f-2b96-44b8-b624-ba409c82eb34"), "KWD", "Kuwaiti Dinar" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_ApplicationUserId",
                schema: "banks",
                table: "BankAccounts",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BankId_AccountNumber_CurrencyId",
                schema: "banks",
                table: "BankAccounts",
                columns: new[] { "BankId", "AccountNumber", "CurrencyId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CurrencyId",
                schema: "banks",
                table: "BankAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountsBalances_BankAccountId",
                schema: "banks",
                table: "BankAccountsBalances",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookMessages_ReceivedAtUtc_Id",
                schema: "banks",
                table: "InboundWebhookMessages",
                columns: new[] { "ReceivedAtUtc", "Id" },
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountsBalances",
                schema: "banks");

            migrationBuilder.DropTable(
                name: "InboundWebhookMessages",
                schema: "banks");

            migrationBuilder.DropTable(
                name: "BankAccounts",
                schema: "banks");

            migrationBuilder.DropTable(
                name: "Banks",
                schema: "banks");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "banks");
        }
    }
}
