using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class idx_WebhookDeliveryId_table_InboundWebhookMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookMessages_WebhookDeliveryId",
                schema: "banks",
                table: "InboundWebhookMessages",
                column: "WebhookDeliveryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboundWebhookMessages_WebhookDeliveryId",
                schema: "banks",
                table: "InboundWebhookMessages");
        }
    }
}
