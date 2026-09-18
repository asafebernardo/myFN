using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreditCardInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditCardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CycleStartExclusive = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StatementAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardInvoices_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditCardInvoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInvoices_CreditCardId_ClosingDate",
                table: "CreditCardInvoices",
                columns: new[] { "CreditCardId", "ClosingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardInvoices_UserId",
                table: "CreditCardInvoices",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardInvoices");
        }
    }
}
