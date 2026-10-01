using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestsManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrioritySortIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Requests_Priority_CreatedAt",
                table: "Requests",
                columns: new[] { "Priority", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Priority_CreatedAt",
                table: "Requests");
        }
    }
}
