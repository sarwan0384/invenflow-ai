using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvenFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVendorDeleteBehaviorToSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
