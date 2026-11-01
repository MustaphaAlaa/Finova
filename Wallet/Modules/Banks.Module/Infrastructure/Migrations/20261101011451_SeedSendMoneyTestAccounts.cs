using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Banks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSendMoneyTestAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "banks",
                table: "BankAccounts",
                columns: new[] { "Id", "AccountNumber", "ApplicationUserId", "BankId", "CurrencyId" },
                values: new object[,]
                {
                    { new Guid("a68e3e38-0de1-4ad0-92c7-21b783702f81"), "TEST-ACME-0001", new Guid("f108f490-6007-49bf-8f5c-c2fd24d9d909"), new Guid("c9337615-fb2b-4be1-9da0-c9704d546bf6"), new Guid("00596123-1c69-4603-ac90-4bbe433621e9") },
                    { new Guid("be906bfd-7598-41dd-85c1-244b62fb2a36"), "TEST-PAYTECH-0001", new Guid("f108f490-6007-49bf-8f5c-c2fd24d9d909"), new Guid("4b42ad75-6775-44af-931f-f5fb3f38b75a"), new Guid("00596123-1c69-4603-ac90-4bbe433621e9") }
                });

            migrationBuilder.InsertData(
                schema: "banks",
                table: "BankAccountsBalances",
                columns: new[] { "Id", "Balance", "BankAccountId", "LastUpdate" },
                values: new object[,]
                {
                    { new Guid("4f3d9d40-43df-4d81-88f9-b293f7b85c22"), 12000.00m, new Guid("be906bfd-7598-41dd-85c1-244b62fb2a36"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("635b69d6-d1f6-4b9a-93e6-06e3a8a40577"), 25000.00m, new Guid("a68e3e38-0de1-4ad0-92c7-21b783702f81"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "banks",
                table: "BankAccountsBalances",
                keyColumn: "Id",
                keyValue: new Guid("4f3d9d40-43df-4d81-88f9-b293f7b85c22"));

            migrationBuilder.DeleteData(
                schema: "banks",
                table: "BankAccountsBalances",
                keyColumn: "Id",
                keyValue: new Guid("635b69d6-d1f6-4b9a-93e6-06e3a8a40577"));

            migrationBuilder.DeleteData(
                schema: "banks",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: new Guid("a68e3e38-0de1-4ad0-92c7-21b783702f81"));

            migrationBuilder.DeleteData(
                schema: "banks",
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: new Guid("be906bfd-7598-41dd-85c1-244b62fb2a36"));
        }
    }
}
