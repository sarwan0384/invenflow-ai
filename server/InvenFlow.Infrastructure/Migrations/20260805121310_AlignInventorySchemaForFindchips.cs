using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvenFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignInventorySchemaForFindchips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_InboundDocuments_InboundDocumentId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Vendors_VendorId",
                table: "InventoryItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.RenameTable(
                name: "InventoryItems",
                newName: "inventory_items");

            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "inventory_items",
                newName: "sku");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "inventory_items",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "inventory_items",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "inventory_items",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "inventory_items",
                newName: "vendor_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "inventory_items",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "inventory_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "inventory_items",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "QuantityOnHand",
                table: "inventory_items",
                newName: "quantity_on_hand");

            migrationBuilder.RenameColumn(
                name: "InboundDocumentId",
                table: "inventory_items",
                newName: "inbound_document_id");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_VendorId",
                table: "inventory_items",
                newName: "IX_inventory_items_vendor_id");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_TenantId_Sku",
                table: "inventory_items",
                newName: "IX_inventory_items_tenant_id_sku");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_InboundDocumentId",
                table: "inventory_items",
                newName: "IX_inventory_items_inbound_document_id");

            migrationBuilder.AlterColumn<string>(
                name: "sku",
                table: "inventory_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "inventory_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "inventory_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "container_type",
                table: "inventory_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "inventory_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "disti_sku",
                table: "inventory_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                table: "inventory_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "min_qty",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "mpn",
                table: "inventory_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "price_tiers_json",
                table: "inventory_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "region",
                table: "inventory_items",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "stock",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_inventory_items",
                table: "inventory_items",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_tenant_id_mpn",
                table: "inventory_items",
                columns: new[] { "tenant_id", "mpn" });

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_items_InboundDocuments_inbound_document_id",
                table: "inventory_items",
                column: "inbound_document_id",
                principalTable: "InboundDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_items_Vendors_vendor_id",
                table: "inventory_items",
                column: "vendor_id",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inventory_items_InboundDocuments_inbound_document_id",
                table: "inventory_items");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_items_Vendors_vendor_id",
                table: "inventory_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inventory_items",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_tenant_id_mpn",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "container_type",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "description",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "disti_sku",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "min_qty",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "mpn",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "price_tiers_json",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "region",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "stock",
                table: "inventory_items");

            migrationBuilder.RenameTable(
                name: "inventory_items",
                newName: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "sku",
                table: "InventoryItems",
                newName: "Sku");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "InventoryItems",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "InventoryItems",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "InventoryItems",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vendor_id",
                table: "InventoryItems",
                newName: "VendorId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "InventoryItems",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "InventoryItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "InventoryItems",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "quantity_on_hand",
                table: "InventoryItems",
                newName: "QuantityOnHand");

            migrationBuilder.RenameColumn(
                name: "inbound_document_id",
                table: "InventoryItems",
                newName: "InboundDocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_inventory_items_vendor_id",
                table: "InventoryItems",
                newName: "IX_InventoryItems_VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_inventory_items_tenant_id_sku",
                table: "InventoryItems",
                newName: "IX_InventoryItems_TenantId_Sku");

            migrationBuilder.RenameIndex(
                name: "IX_inventory_items_inbound_document_id",
                table: "InventoryItems",
                newName: "IX_InventoryItems_InboundDocumentId");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "InventoryItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                column: "Id");

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
                onDelete: ReferentialAction.SetNull);
        }
    }
}
