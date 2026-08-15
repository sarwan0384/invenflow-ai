# Refactor Domain Model, EF Core Relationships, and CRUD Logic

## Steps

- [x] Step 1: Update `Vendor.cs` - Add navigation collections for InboundDocuments and InventoryItems
- [x] Step 2: Update `InboundDocument.cs` - Add InventoryItems navigation collection
- [x] Step 3: Update `InventoryItem.cs` - Add VendorId, Vendor, InboundDocumentId, InboundDocument
- [x] Step 4: Update `AppDbContext.cs` - Configure Fluent API relationships with proper delete behaviors
- [x] Step 5: Update `VendorsController.cs` - Refactor Delete with try/catch (DbUpdateException)
- [x] Step 6: Update `InboundDocumentsController.cs` - Refactor Delete with try/catch
- [x] Step 7: Update `InventoryController.cs` - Preserve TenantId, VendorId, InboundDocumentId on Create/Update
- [x] Step 8: Update `SyncController.cs` - Populate VendorId on new InventoryItems
- [x] Step 9: Run `dotnet build` to verify everything compiles
- [x] Step 10: Run `dotnet ef migrations add AddVendorInventoryRelationships`

All steps complete! ✅
