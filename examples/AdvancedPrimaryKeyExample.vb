' ===================================
' PRIMARY KEY DECLARATION EXAMPLES
' Demonstrates the enhanced writing logic with primary key declaration in field mappings
' ===================================

'---------------------------------------
' EXAMPLE 1: DECLARE PRIMARY KEY IN FIELD MAPPINGS (NEW FEATURE)
' Instead of passing keyFields as a separate array, mark fields with IsPrimaryKey=True
'---------------------------------------

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadAndTokenValidationError = DB.Global.ValidatePayloadAndToken(DB,CheckToken,"UpsertUser",ParsedPayload,StringPayload)
If PayloadAndTokenValidationError IsNot Nothing Then
    Return PayloadAndTokenValidationError
End If

' Define field mappings with primary key declaration
' CreateFieldMappingsDictionary parameters:
' - jsonProps: JSON property names
' - sqlCols: SQL column names
' - isRequiredArray: Which fields are required (for validation)
' - isPrimaryKeyArray: Which fields are primary keys (for existence checking)
' - defaultValArray: Default values for fields
Dim fieldMappingsWithPK = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"userId", "email", "name", "department"},
    New System.String() {"UserId", "Email", "Name", "Department"},
    New Boolean() {True, True, False, False},     ' userId and email are required
    New Boolean() {True, False, False, False},    ' Only userId is primary key
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

' Create upsert logic WITHOUT passing keyFields parameter
' The library will automatically extract primary keys from field mappings
Dim upsertLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    fieldMappingsWithPK
    ' Note: No keyFields parameter needed!
    ' allowUpdates defaults to True for upsert behavior
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"userId", "email"}),
    upsertLogic,
    "User upserted using primary key from field mappings",
    ParsedPayload,
    StringPayload,
    False
)

' Payload: { "userId": "123", "email": "john@example.com", "name": "John Doe" }
'
' How it works:
' 1. The library extracts primary keys from field mappings (userId has IsPrimaryKey=True)
' 2. Checks if record with UserId=123 exists using: SELECT COUNT(*) FROM Users WHERE UserId = :UserId
' 3. If exists: UPDATE the record (only non-key fields)
' 4. If not exists: INSERT new record
'
' Response on insert: { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
' Response on update: { "Result": "OK", "Action": "UPDATED", "Message": "Record updated successfully" }


'---------------------------------------
' EXAMPLE 2: COMPOSITE PRIMARY KEY DECLARATION
' Mark multiple fields as primary keys
'---------------------------------------

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "OrderItems", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

' Composite key: both orderId and productId are primary keys
Dim compositePKMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"orderId", "productId", "quantity", "price"},
    New System.String() {"OrderId", "ProductId", "Quantity", "Price"},
    New Boolean() {True, True, True, False},      ' orderId, productId, quantity are required
    New Boolean() {True, True, False, False},     ' orderId AND productId are primary keys
    New Object() {Nothing, Nothing, Nothing, 0}
)

' Create upsert logic - will use composite key (OrderId + ProductId) for existence check
Dim compositeKeyLogic = DB.Global.CreateBusinessLogicForWriting(
    "OrderItems",
    compositePKMappings
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"orderId", "productId", "quantity"}),
    compositeKeyLogic,
    "Order item saved using composite key",
    ParsedPayload2,
    StringPayload2,
    False
)

' Payload: { "orderId": "ORD-001", "productId": "PROD-123", "quantity": 5, "price": 99.99 }
'
' Existence check uses: SELECT COUNT(*) FROM OrderItems WHERE OrderId = :OrderId AND ProductId = :ProductId
' Only primary key fields are used to determine if record exists!


'---------------------------------------
' EXAMPLE 3: INSERT-ONLY WITH PRIMARY KEY DECLARATION
' Set allowUpdates=False to prevent updates
'---------------------------------------

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "InsertUser", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim insertOnlyMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"userId", "email", "name"},
    New System.String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, True},      ' All fields required
    New Boolean() {True, False, False},    ' userId is primary key
    New Object() {Nothing, Nothing, Nothing}
)

' Set allowUpdates=False for insert-only behavior
Dim insertOnlyLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    insertOnlyMappings,
    Nothing,      ' keyFields = Nothing (extract from mappings)
    False         ' allowUpdates = False (insert only)
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"userId", "email", "name"}),
    insertOnlyLogic,
    "User inserted (updates not allowed)",
    ParsedPayload3,
    StringPayload3,
    False
)

' Payload: { "userId": "456", "email": "jane@example.com", "name": "Jane Doe" }
'
' Response on insert: { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
' Response if exists: { "Result": "KO", "Reason": "Record already exists and updates are not allowed" }


'---------------------------------------
' EXAMPLE 4: BATCH OPERATIONS WITH PRIMARY KEY DECLARATION
'---------------------------------------

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchUsers", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim batchMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"userId", "email", "name", "department"},
    New System.String() {"UserId", "Email", "Name", "Department"},
    New Boolean() {True, True, True, False},
    New Boolean() {True, False, False, False},    ' userId is primary key
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

' Create batch logic without keyFields parameter
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users",
    batchMappings
    ' Note: No keyFields needed - extracted from field mappings!
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New System.String() {"Records"}),
    batchLogic,
    "Batch operation completed with primary key declaration",
    ParsedPayload4,
    StringPayload4,
    False
)

' Payload:
' {
'   "Records": [
'     {"userId": "1", "email": "user1@example.com", "name": "User One"},
'     {"userId": "2", "email": "user2@example.com", "name": "User Two"},
'     {"userId": "3", "email": "user3@example.com", "name": "User Three"}
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
' EXAMPLE 5: BACKWARD COMPATIBILITY
' You can still pass keyFields as a parameter if you prefer
'---------------------------------------

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "BackwardCompat", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

' Old style: no IsPrimaryKey in field mappings
Dim oldStyleMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"userId", "email", "name"},
    New System.String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, False},
    Nothing,  ' No IsPrimaryKey array - will use explicit keyFields parameter below
    New Object() {Nothing, Nothing, Nothing}
)

' Old style: explicitly pass keyFields parameter
Dim oldStyleLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    oldStyleMappings,
    New System.String() {"UserId"},    ' Explicitly specify key fields (old way)
    True                        ' allowUpdates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"userId", "email"}),
    oldStyleLogic,
    "User upserted (backward compatible approach)",
    ParsedPayload5,
    StringPayload5,
    False
)

' This example shows that the old way still works!
' You can migrate gradually to the new approach.


'---------------------------------------
' EXAMPLE 6: REQUIRED FIELDS vs PRIMARY KEYS
' Demonstrates the difference between required fields and primary keys
'---------------------------------------

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "RequiredVsPK", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

' Configuration:
' - userId: Primary key AND required
' - email: Required but NOT primary key
' - name: Neither required nor primary key
Dim clarifyingMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"userId", "email", "name", "phone"},
    New System.String() {"UserId", "Email", "Name", "Phone"},
    New Boolean() {True, True, False, False},     ' userId and email are required
    New Boolean() {True, False, False, False},    ' Only userId is primary key
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim clarifyingLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    clarifyingMappings
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"userId", "email"}),
    clarifyingLogic,
    "User saved",
    ParsedPayload6,
    StringPayload6,
    False
)

' Valid payload: { "userId": "789", "email": "test@example.com" }
' - Passes validation (both required fields present)
' - Existence check uses ONLY userId (the primary key)
'
' Invalid payload: { "userId": "789" }  (missing email)
' - Fails validation with: "Missing required fields: email"
'
' KEY INSIGHT:
' - IsRequired: Controls field validation (must be present in payload)
' - IsPrimaryKey: Controls existence checking (used in WHERE clause)
' - A field can be required without being a primary key
' - A field can be a primary key without being required (not recommended but possible)
