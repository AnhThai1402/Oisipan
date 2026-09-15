using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStatusToBit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ xác nhận");

            migrationBuilder.AddColumn<string>(
                name: "RequestStatus",
                table: "CancellationReasons",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql("UPDATE [Orders] SET [OrderStatus] = [Status] WHERE [Status] IS NOT NULL AND LTRIM(RTRIM([Status])) <> ''");
            migrationBuilder.Sql("UPDATE [CancellationReasons] SET [RequestStatus] = [Status] WHERE [Status] IS NOT NULL AND LTRIM(RTRIM([Status])) <> ''");

            foreach (var table in StatusTables)
            {
                NormalizeStatusToBitText(migrationBuilder, table);
                AlterStatusToBit(migrationBuilder, table);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in StatusTables)
            {
                AlterStatusToString(migrationBuilder, table);
                NormalizeStatusToActiveText(migrationBuilder, table);
            }

            migrationBuilder.Sql("UPDATE [Orders] SET [Status] = [OrderStatus] WHERE [OrderStatus] IS NOT NULL AND LTRIM(RTRIM([OrderStatus])) <> ''");
            migrationBuilder.Sql("UPDATE [CancellationReasons] SET [Status] = [RequestStatus] WHERE [RequestStatus] IS NOT NULL AND LTRIM(RTRIM([RequestStatus])) <> ''");

            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "CancellationReasons");
        }

        private static readonly string[] StatusTables =
        {
            "Vouchers",
            "UserVouchers",
            "UserAddresses",
            "ProductVariantValues",
            "ProductVariants",
            "ProductValues",
            "Products",
            "ProductOptions",
            "Payments",
            "Orders",
            "OrderDetails",
            "NewsArticles",
            "InvoiceRecords",
            "Categories",
            "CancellationReasons",
            "Accounts"
        };

        private static void NormalizeStatusToBitText(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.Sql($@"
UPDATE [{table}]
SET [Status] = CASE
    WHEN LOWER(LTRIM(RTRIM([Status]))) IN ('inactive', 'false', '0') THEN '0'
    ELSE '1'
END");
        }

        private static void NormalizeStatusToActiveText(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.Sql($@"
UPDATE [{table}]
SET [Status] = CASE
    WHEN LOWER(LTRIM(RTRIM([Status]))) IN ('1', 'true') THEN 'Active'
    ELSE 'Inactive'
END");
        }

        private static void AlterStatusToBit(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Status",
                table: table,
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        private static void AlterStatusToString(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: table,
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
