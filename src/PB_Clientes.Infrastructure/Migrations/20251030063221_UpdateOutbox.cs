using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PB_Clientes.Infrastructure.src.PB_Clientes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Baggage",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpanId",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "OutboxMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Baggage",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "SpanId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "OutboxMessages");
        }
    }
}
