using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserVouchers",
                columns: table => new
                {
                    UserVoucherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedInOrderId = table.Column<int>(type: "int", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVouchers", x => x.UserVoucherId);
                    table.ForeignKey(
                        name: "FK_UserVouchers_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVouchers_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserId",
                table: "UserVouchers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_VoucherId",
                table: "UserVouchers",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVouchers");
        }
    }
}
