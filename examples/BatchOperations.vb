' ===================================
' BATCH OPERATIONS EXAMPLES
' Handling multiple records in a single request
' ===================================

'---------------------------------------
' EXAMPLE 1: BASIC BATCH INSERT/UPDATE
'---------------------------------------
' Insert or update multiple records at once

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "BatchUsers", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Define fields
Dim allFields = New String() {"UserId", "Email", "Name", "Department", "Status"}
Dim keyFields = New String() {"UserId"}

' Create batch logic
Dim batchLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Users",
    allFields,
    keyFields,
    True  ' Allow updates
)

' Validator for batch operations
Dim batchValidator = DB.Global.CreateValidatorForBatch(
    New String() {"Records"}  ' Ensures Records array exists
)

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
'     { "UserId": "101", "Email": "user1@example.com", "Name": "User One", "Department": "IT" },
'     { "UserId": "102", "Email": "user2@example.com", "Name": "User Two", "Department": "Sales" },
'     { "UserId": "103", "Email": "user3@example.com", "Name": "User Three", "Department": "HR" }
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

'---------------------------------------
' EXAMPLE 2: BATCH INSERT ONLY (NO UPDATES)
'---------------------------------------
' Reject updates, only allow inserts

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchInsertOnly", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim batchInsertLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Products",
    New String() {"ProductId", "Name", "Price", "Category"},
    New String() {"ProductId"},
    False  ' allowUpdates = False
)

Dim batchValidator2 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    batchValidator2,
    batchInsertLogic,
    "Batch product insert",
    ParsedPayload2,
    StringPayload2,
    False
)

' Example payload:
' {
'   "Records": [
'     { "ProductId": "P001", "Name": "Widget A", "Price": 19.99, "Category": "Widgets" },
'     { "ProductId": "P002", "Name": "Widget B", "Price": 29.99, "Category": "Widgets" },
'     { "ProductId": "P001", "Name": "Widget A Updated", "Price": 24.99, "Category": "Widgets" }
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
' EXAMPLE 3: BATCH WITH ERROR HANDLING
'---------------------------------------
' Handle partial success - some records succeed, others fail

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchWithErrors", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim batchWithValidation = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Orders",
    New String() {"OrderId", "CustomerId", "Amount", "OrderDate"},
    New String() {"OrderId"},  ' OrderId is required
    True
)

Dim validator3 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator3,
    batchWithValidation,
    "Batch orders with validation",
    ParsedPayload3,
    StringPayload3,
    False
)

' Example payload with mixed valid/invalid records:
' {
'   "Records": [
'     { "OrderId": "O001", "CustomerId": "C123", "Amount": 100.00, "OrderDate": "2025-01-15" },
'     { "CustomerId": "C456", "Amount": 200.00, "OrderDate": "2025-01-16" },  // Missing OrderId
'     { "OrderId": "O002", "CustomerId": "C789", "Amount": 150.00, "OrderDate": "2025-01-17" },
'     { "OrderId": "O003", "Amount": 75.00, "OrderDate": "2025-01-18" }  // Missing CustomerId (if required)
'   ]
' }
'
' Response:
' {
'   "Result": "PARTIAL",
'   "Inserted": 2,
'   "Updated": 0,
'   "Errors": 2,
'   "ErrorDetails": [
'     "Record skipped - Missing required parameters: OrderId",
'     "Save error: CustomerId is required"
'   ],
'   "Message": "Processed 4 records: 2 inserted, 0 updated, 2 errors."
' }

'---------------------------------------
' EXAMPLE 4: BULK UPDATE EXISTING RECORDS
'---------------------------------------
' Update status of multiple existing records

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "BulkUpdate", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim bulkUpdateLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Orders",
    New String() {"OrderId", "Status", "UpdatedDate", "UpdatedBy"},
    New String() {"OrderId"},
    True  ' Allow updates
)

Dim validator4 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator4,
    bulkUpdateLogic,
    "Bulk status update",
    ParsedPayload4,
    StringPayload4,
    False
)

' Example payload - update status for multiple orders:
' {
'   "Records": [
'     { "OrderId": "O001", "Status": "Shipped", "UpdatedDate": "2025-01-20", "UpdatedBy": "admin" },
'     { "OrderId": "O002", "Status": "Shipped", "UpdatedDate": "2025-01-20", "UpdatedBy": "admin" },
'     { "OrderId": "O003", "Status": "Shipped", "UpdatedDate": "2025-01-20", "UpdatedBy": "admin" }
'   ]
' }
'
' If all orders exist, all will be updated. New orders will be inserted.

'---------------------------------------
' EXAMPLE 5: BATCH WITH SINGLE RECORD FALLBACK
'---------------------------------------
' If Records array is not provided, treat payload as single record

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "FlexibleBatch", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

' Batch handler automatically falls back to single record mode if no Records array
Dim flexibleLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Customers",
    New String() {"CustomerId", "Name", "Email", "Phone"},
    New String() {"CustomerId"},
    True
)

' Note: No batch validator - allows both single record and batch
Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"CustomerId"}),  ' Validate required fields for single record
    flexibleLogic,
    "Flexible customer upsert",
    ParsedPayload5,
    StringPayload5,
    False
)

' Single record payload:
' { "CustomerId": "C001", "Name": "John Doe", "Email": "john@example.com", "Phone": "555-0100" }
'
' Batch payload:
' {
'   "Records": [
'     { "CustomerId": "C001", "Name": "John Doe", "Email": "john@example.com", "Phone": "555-0100" },
'     { "CustomerId": "C002", "Name": "Jane Smith", "Email": "jane@example.com", "Phone": "555-0101" }
'   ]
' }
'
' Both formats work with the same endpoint!

'---------------------------------------
' EXAMPLE 6: IMPORT DATA FROM EXTERNAL SOURCE
'---------------------------------------
' Typical use case: importing CSV data converted to JSON

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "ImportEmployees", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim importLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Employees",
    New String() {"EmployeeId", "FirstName", "LastName", "Email", "Department", "HireDate"},
    New String() {"EmployeeId"},
    True  ' Update if employee exists
)

Dim validator6 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator6,
    importLogic,
    "Employee import",
    ParsedPayload6,
    StringPayload6,
    False
)

' Large batch payload (e.g., from CSV import):
' {
'   "Records": [
'     { "EmployeeId": "E001", "FirstName": "Alice", "LastName": "Anderson", "Email": "alice@company.com", "Department": "Engineering", "HireDate": "2024-01-15" },
'     { "EmployeeId": "E002", "FirstName": "Bob", "LastName": "Brown", "Email": "bob@company.com", "Department": "Sales", "HireDate": "2024-02-01" },
'     // ... hundreds more records
'   ]
' }
'
' Response shows exactly which records succeeded and which failed:
' {
'   "Result": "OK",
'   "Inserted": 150,
'   "Updated": 25,
'   "Errors": 5,
'   "ErrorDetails": [
'     "E123 - Save error: Invalid email format",
'     "E456 - Record skipped - Missing required parameters: Email",
'     // ...
'   ],
'   "Message": "Processed 180 records: 150 inserted, 25 updated, 5 errors."
' }

'---------------------------------------
' EXAMPLE 7: BATCH DELETE (SOFT DELETE)
'---------------------------------------
' Mark multiple records as deleted

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchDelete", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim batchDeleteLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Products",
    New String() {"ProductId", "IsDeleted", "DeletedDate", "DeletedBy"},
    New String() {"ProductId"},
    True  ' Must allow updates for soft delete
)

Dim validator7 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator7,
    batchDeleteLogic,
    "Batch soft delete",
    ParsedPayload7,
    StringPayload7,
    False
)

' Payload to soft-delete multiple products:
' {
'   "Records": [
'     { "ProductId": "P001", "IsDeleted": true, "DeletedDate": "2025-01-20", "DeletedBy": "admin" },
'     { "ProductId": "P002", "IsDeleted": true, "DeletedDate": "2025-01-20", "DeletedBy": "admin" },
'     { "ProductId": "P003", "IsDeleted": true, "DeletedDate": "2025-01-20", "DeletedBy": "admin" }
'   ]
' }

'---------------------------------------
' EXAMPLE 8: SYNCHRONIZATION PATTERN
'---------------------------------------
' Sync local data with external system

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "SyncProducts", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

Dim syncLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "ProductCatalog",
    New String() {"SKU", "Name", "Price", "Stock", "LastSyncDate", "ExternalId"},
    New String() {"SKU"},  ' SKU is the key
    True  ' Update existing products
)

Dim validator8 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator8,
    syncLogic,
    "Product catalog sync",
    ParsedPayload8,
    StringPayload8,
    False
)

' Sync payload from external system:
' {
'   "Records": [
'     { "SKU": "SKU-001", "Name": "Product A", "Price": 29.99, "Stock": 100, "LastSyncDate": "2025-01-20T10:30:00", "ExternalId": "EXT-123" },
'     { "SKU": "SKU-002", "Name": "Product B", "Price": 39.99, "Stock": 50, "LastSyncDate": "2025-01-20T10:30:00", "ExternalId": "EXT-124" },
'     // ...
'   ]
' }
'
' New products are inserted, existing products are updated with latest data

'---------------------------------------
' EXAMPLE 9: BATCH WITH COMPUTED FIELDS
'---------------------------------------
' Calculate fields before insertion

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchInvoices", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

' Note: Computed fields like TotalAmount would typically be calculated in the database
' or via stored procedures, but can be provided in the payload
Dim batchInvoiceLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Invoices",
    New String() {"InvoiceId", "CustomerId", "Subtotal", "TaxRate", "TaxAmount", "TotalAmount", "InvoiceDate"},
    New String() {"InvoiceId"},
    True
)

Dim validator9 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

Return DB.Global.ProcessActionLink(
    DB,
    validator9,
    batchInvoiceLogic,
    "Batch invoice creation",
    ParsedPayload9,
    StringPayload9,
    False
)

' Payload with pre-calculated totals:
' {
'   "Records": [
'     { "InvoiceId": "INV-001", "CustomerId": "C123", "Subtotal": 100.00, "TaxRate": 0.10, "TaxAmount": 10.00, "TotalAmount": 110.00, "InvoiceDate": "2025-01-20" },
'     { "InvoiceId": "INV-002", "CustomerId": "C456", "Subtotal": 200.00, "TaxRate": 0.10, "TaxAmount": 20.00, "TotalAmount": 220.00, "InvoiceDate": "2025-01-20" }
'   ]
' }

'---------------------------------------
' EXAMPLE 10: MONITORING BATCH RESULTS
'---------------------------------------
' Process batch and log detailed results

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, False, "MonitoredBatch", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    Return PayloadError10
End If

Dim monitoredLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "DataImports",
    New String() {"RecordId", "SourceSystem", "DataType", "DataValue", "ImportDate"},
    New String() {"RecordId"},
    True
)

Dim validator10 = DB.Global.CreateValidatorForBatch(New String() {"Records"})

' Process batch
Dim result = DB.Global.ProcessActionLink(
    DB,
    validator10,
    monitoredLogic,
    "Monitored data import",
    ParsedPayload10,
    StringPayload10,
    False
)

' Log the result for monitoring
DB.Global.LogCustom(DB, StringPayload10, result, "Data Import Batch Result: ")

Return result

' The response includes detailed statistics:
' {
'   "Result": "OK",  // or "PARTIAL" or "KO"
'   "ColumnsYouCanWriteTo": "RecordId,SourceSystem,DataType,DataValue,ImportDate",
'   "RequiredColumns": "RecordId",
'   "Inserted": 150,
'   "Updated": 25,
'   "Errors": 5,
'   "ErrorDetails": [
'     "REC-123 - Save error: DataValue exceeds maximum length",
'     "Record skipped - Missing required parameters: RecordId"
'   ],
'   "Message": "Processed 180 records: 150 inserted, 25 updated, 5 errors."
' }
'
' Use this information to:
' - Display success/failure metrics to users
' - Retry failed records
' - Alert administrators if error threshold exceeded
' - Audit trail for compliance

'---------------------------------------
' RESPONSE CODES EXPLANATION
'---------------------------------------
'
' Result: "OK"      - All records processed successfully
' Result: "PARTIAL" - Some records succeeded, some failed (check ErrorDetails)
' Result: "KO"      - Complete failure (e.g., invalid payload, all records failed)
'
' Always check:
' - Inserted: Number of new records created
' - Updated: Number of existing records modified
' - Errors: Number of failed operations
' - ErrorDetails: Array of specific error messages for failed records
'
' Best practices:
' 1. Always validate Records array exists using CreateValidatorForBatch
' 2. Set appropriate allowUpdates flag based on your use case
' 3. Handle partial success scenarios in your application
' 4. Log batch operations for audit trails
' 5. Consider implementing retry logic for failed records
' 6. For very large batches (1000+ records), consider chunking
