using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmptyFinanceAndSeedVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeedVersion",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeedVersion",
                table: "Settings");
        }
    }
}
