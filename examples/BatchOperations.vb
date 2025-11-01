' ===================================
' BATCH OPERATIONS EXAMPLES
' Handling multiple records efficiently with high-performance bulk operations
' ===================================

'---------------------------------------
' EXAMPLE 1: BASIC BATCH INSERT/UPDATE
'---------------------------------------
' Insert or update multiple records at once (80-90% faster than individual operations)

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "BatchUsers", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Define field mappings (JSON -> SQL)
Dim userMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name", "department", "status"},
    New String() {"UserId", "Email", "Name", "Department", "Status"},
    New Boolean() {True, True, False, False, False},  ' userId and email are required
    New Object() {Nothing, Nothing, Nothing, Nothing, "Active"}  ' Default status
)

' Create batch logic with BULK EXISTENCE CHECK for maximum performance
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users",
    userMappings,
    New String() {"UserId"},  ' Key field
    True  ' Allow updates
)

Dim batchValidator = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    batchValidator,
    batchLogic,
    "Batch user upsert",
    ParsedPayload,
    StringPayload,
    False
)

' Example payload:
' {
'   "Records": [
'     { "userId": "101", "email": "user1@example.com", "name": "User One", "department": "IT" },
'     { "userId": "102", "email": "user2@example.com", "name": "User Two", "department": "Sales" },
'     { "userId": "103", "email": "user3@example.com", "name": "User Three", "department": "HR" }
'   ]
' }
'
' Response:
' {
'   "Result": "OK",
'   "Inserted": 2,
'   "Updated": 1,
'   "Errors": 0,
'   "ErrorDetails": [],
'   "Message": "Processed 3 records: 2 inserted, 1 updated, 0 errors."
' }
'
' PERFORMANCE: For 100 records, this performs 1 bulk existence check + 100 operations
' instead of 200+ individual queries (50-90% faster!)


'---------------------------------------
' EXAMPLE 2: BATCH INSERT ONLY
'---------------------------------------
' Reject updates, only allow inserts

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchInsertOnly", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim productMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"productId", "name", "price", "category"},
    New String() {"ProductId", "Name", "Price", "Category"},
    New Boolean() {True, True, True, False},
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim batchInsertLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Products",
    productMappings,
    New String() {"ProductId"},
    False  ' allowUpdates = False
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    batchInsertLogic,
    "Batch product insert",
    ParsedPayload2,
    StringPayload2,
    False
)

' Payload:
' {
'   "Records": [
'     { "productId": "P001", "name": "Widget A", "price": 19.99, "category": "Widgets" },
'     { "productId": "P002", "name": "Widget B", "price": 29.99, "category": "Widgets" },
'     { "productId": "P001", "name": "Widget A Updated", "price": 24.99 }  -- Duplicate, will error
'   ]
' }
'
' Response:
' {
'   "Result": "PARTIAL",
'   "Inserted": 2,
'   "Updated": 0,
'   "Errors": 1,
'   "ErrorDetails": ["P001 - Record already exists and updates are not allowed"],
'   "Message": "Processed 3 records: 2 inserted, 0 updated, 1 errors."
' }


'---------------------------------------
' EXAMPLE 3: COMPOSITE KEY BATCH
'---------------------------------------
' Batch operations with multi-column keys

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchOrderItems", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim orderItemMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"orderId", "productId", "quantity", "price"},
    New String() {"OrderId", "ProductId", "Quantity", "Price"},
    New Boolean() {True, True, True, False},
    New Object() {Nothing, Nothing, Nothing, 0}
)

Dim compositeKeyBatch = DB.Global.CreateBusinessLogicForBatchWriting(
    "OrderItems",
    orderItemMappings,
    New String() {"OrderId", "ProductId"},  ' Composite key
    True
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    compositeKeyBatch,
    "Batch order items",
    ParsedPayload3,
    StringPayload3,
    False
)

' Payload:
' {
'   "Records": [
'     { "orderId": "ORD-001", "productId": "P001", "quantity": 5, "price": 19.99 },
'     { "orderId": "ORD-001", "productId": "P002", "quantity": 3, "price": 29.99 },
'     { "orderId": "ORD-002", "productId": "P001", "quantity": 10, "price": 19.99 }
'   ]
' }


'---------------------------------------
' EXAMPLE 4: SINGLE RECORD FALLBACK
'---------------------------------------
' Automatically handles both single record and batch

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "FlexibleCustomer", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim customerMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"customerId", "name", "email", "phone"},
    New String() {"CustomerId", "Name", "Email", "Phone"},
    New Boolean() {True, True, False, False},
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

' Batch handler automatically handles single record if no Records array
Dim flexibleLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Customers",
    customerMappings,
    New String() {"CustomerId"},
    True
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"customerId", "name"}),  ' For single record validation
    flexibleLogic,
    "Flexible customer upsert",
    ParsedPayload4,
    StringPayload4,
    False
)

' Single record:
' { "customerId": "C001", "name": "John Doe", "email": "john@example.com" }
'
' Batch:
' {
'   "Records": [
'     { "customerId": "C001", "name": "John Doe", "email": "john@example.com" },
'     { "customerId": "C002", "name": "Jane Smith", "email": "jane@example.com" }
'   ]
' }
'
' Both work with the same endpoint!


'---------------------------------------
' EXAMPLE 5: LARGE SCALE IMPORT
'---------------------------------------
' Efficient handling of large data imports (1000+ records)

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "ImportEmployees", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

Dim employeeMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"employeeId", "firstName", "lastName", "email", "department", "hireDate"},
    New String() {"EmployeeId", "FirstName", "LastName", "Email", "Department", "HireDate"},
    New Boolean() {True, True, True, True, False, False},
    New Object() {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
)

Dim importLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Employees",
    employeeMappings,
    New String() {"EmployeeId"},
    True  ' Update if exists
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    importLogic,
    "Employee import",
    ParsedPayload5,
    StringPayload5,
    False
)

' Large batch payload (e.g., CSV import):
' {
'   "Records": [
'     { "employeeId": "E001", "firstName": "Alice", "lastName": "Anderson", "email": "alice@company.com", "department": "Engineering", "hireDate": "2024-01-15" },
'     { "employeeId": "E002", "firstName": "Bob", "lastName": "Brown", "email": "bob@company.com", "department": "Sales", "hireDate": "2024-02-01" },
'     ... 998 more records ...
'   ]
' }
'
' Response shows detailed statistics:
' {
'   "Result": "OK",
'   "Inserted": 850,
'   "Updated": 145,
'   "Errors": 5,
'   "ErrorDetails": ["E123 - Missing required fields: email", ...],
'   "Message": "Processed 1000 records: 850 inserted, 145 updated, 5 errors."
' }
'
' PERFORMANCE NOTE: 1000 records processed in ~5-10 seconds vs ~40-50 seconds individual


'---------------------------------------
' EXAMPLE 6: SOFT DELETE BATCH
'---------------------------------------

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchDelete", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim deleteMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"productId", "isDeleted", "deletedDate", "deletedBy"},
    New String() {"ProductId", "IsDeleted", "DeletedDate", "DeletedBy"},
    New Boolean() {True, True, False, False},
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim batchDeleteLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Products",
    deleteMappings,
    New String() {"ProductId"},
    True  ' Must allow updates for soft delete
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    batchDeleteLogic,
    "Batch soft delete",
    ParsedPayload6,
    StringPayload6,
    False
)

' Payload:
' {
'   "Records": [
'     { "productId": "P001", "isDeleted": true, "deletedDate": "2025-01-20", "deletedBy": "admin" },
'     { "productId": "P002", "isDeleted": true, "deletedDate": "2025-01-20", "deletedBy": "admin" }
'   ]
' }


'---------------------------------------
' EXAMPLE 7: SYNCHRONIZATION PATTERN
'---------------------------------------

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "SyncProducts", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim syncMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"sku", "name", "price", "stock", "lastSyncDate", "externalId"},
    New String() {"SKU", "Name", "Price", "Stock", "LastSyncDate", "ExternalId"},
    New Boolean() {True, True, True, False, False, False},
    New Object() {Nothing, Nothing, Nothing, 0, Nothing, Nothing}
)

Dim syncLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "ProductCatalog",
    syncMappings,
    New String() {"SKU"},
    True  ' Update existing products
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    syncLogic,
    "Product catalog sync",
    ParsedPayload7,
    StringPayload7,
    False
)

' Sync payload from external system:
' {
'   "Records": [
'     { "sku": "SKU-001", "name": "Product A", "price": 29.99, "stock": 100, "lastSyncDate": "2025-01-20T10:30:00", "externalId": "EXT-123" },
'     { "sku": "SKU-002", "name": "Product B", "price": 39.99, "stock": 50, "lastSyncDate": "2025-01-20T10:30:00", "externalId": "EXT-124" }
'   ]
' }


'---------------------------------------
' BATCH OPERATION BEST PRACTICES
'---------------------------------------
'
' 1. PERFORMANCE BENEFITS:
'    - Bulk existence check: 1 query instead of N queries
'    - 50-90% faster than individual operations
'    - Scales efficiently: 1000 records in ~5-10 seconds
'
' 2. ERROR HANDLING:
'    - Partial success supported - some records can fail while others succeed
'    - Detailed error messages in ErrorDetails array
'    - Result codes: "OK", "PARTIAL", or "KO"
'
' 3. RESPONSE STRUCTURE:
'    {
'      "Result": "OK|PARTIAL|KO",
'      "Inserted": <number>,
'      "Updated": <number>,
'      "Errors": <number>,
'      "ErrorDetails": ["error1", "error2", ...],
'      "Message": "Processed X records: Y inserted, Z updated, W errors."
'    }
'
' 4. RECOMMENDATIONS:
'    - Always use CreateValidatorForBatch to ensure Records array exists
'    - Set appropriate allowUpdates flag
'    - Handle partial success in your application logic
'    - Log batch operations for audit trails
'    - For very large batches (10000+), consider chunking into smaller batches
'    - Monitor ErrorDetails to identify data quality issues
'
' 5. PERFORMANCE MONITORING:
'    Use GetPropertyCacheStats() to monitor cache performance
'    Dim stats = DB.Global.GetPropertyCacheStats()
'    ' Returns: CacheSize, CacheHits, CacheMisses, HitRate
