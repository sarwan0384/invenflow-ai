using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvenFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorInventoryRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "InboundDocumentId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InboundDocumentId",
                table: "InventoryItems",
                column: "InboundDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_VendorId",
                table: "InventoryItems",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_InboundDocuments_InboundDocumentId",
                table: "InventoryItems",
                column: "InboundDocumentId",
                principalTable: "InboundDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_InboundDocuments_InboundDocumentId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_InboundDocumentId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_VendorId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "InboundDocumentId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "InventoryItems");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundDocuments_Vendors_VendorId",
                table: "InboundDocuments",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }
    }
}
