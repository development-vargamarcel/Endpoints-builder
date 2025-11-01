' ===================================
' CRUD OPERATIONS EXAMPLES
' Complete examples for Create, Read, Update, Delete operations
' Using the simplified, production-ready API
' ===================================

'---------------------------------------
' EXAMPLE 1: SIMPLE READ OPERATION
'---------------------------------------
' Flexible search with explicit field selection

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadAndTokenValidationError = DB.Global.ValidatePayloadAndToken(DB,CheckToken,"ReadUsers",ParsedPayload,StringPayload)
If PayloadAndTokenValidationError IsNot Nothing Then
    Return PayloadAndTokenValidationError
End If

' Define parameter conditions for flexible search
Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)

searchConditions.Add("UserId", DB.Global.CreateParameterCondition(
    "UserId",
    "UserId = :UserId",
    Nothing
))

searchConditions.Add("Email", DB.Global.CreateParameterCondition(
    "Email",
    "Email LIKE :Email",
    Nothing
))

searchConditions.Add("Department", DB.Global.CreateParameterCondition(
    "Department",
    "Department = :Department",
    Nothing
))

' Create read logic with EXPLICIT field selection (no SELECT *)
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name, Department, CreatedDate FROM Users {WHERE}",
    searchConditions
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,  ' No required parameters
    readLogic,
    "User search",
    ParsedPayload,
    StringPayload,
    False
)

' Example payloads:
' { "UserId": "123" }
' { "Email": "%@company.com" }
' { "Department": "Sales" }
' {}  -- Returns all records


'---------------------------------------
' EXAMPLE 2: INSERT OPERATION (NO UPDATES)
'---------------------------------------

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "InsertUser", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

' Define field mappings (JSON property -> SQL column)
Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name", "department"},
    New String() {"UserId", "Email", "Name", "Department"},
    New Boolean() {True, True, True, False},  ' userId, email, name are required
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

' Create write logic - insert only, no updates
Dim insertLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    fieldMappings,
    New String() {"UserId"},  ' Key field
    False  ' allowUpdates = False (insert only)
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"userId", "email", "name"}),
    insertLogic,
    "User inserted",
    ParsedPayload2,
    StringPayload2,
    False
)

' Payload: { "userId": "456", "email": "jane@example.com", "name": "Jane Doe" }
'
' Response on success:
' { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
'
' Response if exists:
' { "Result": "KO", "Reason": "Record already exists and updates are not allowed" }


'---------------------------------------
' EXAMPLE 3: UPSERT OPERATION (INSERT OR UPDATE)
'---------------------------------------

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "UpsertUser", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim upsertMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name", "department"},
    New String() {"UserId", "Email", "Name", "Department"},
    New Boolean() {True, True, False, False},
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

' Create upsert logic - insert or update
Dim upsertLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    upsertMappings,
    New String() {"UserId"},
    True  ' allowUpdates = True (upsert)
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"userId", "email"}),
    upsertLogic,
    "User upserted",
    ParsedPayload3,
    StringPayload3,
    False
)

' Payload: { "userId": "456", "email": "jane.updated@example.com", "name": "Jane Smith" }
'
' Response on insert:
' { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
'
' Response on update:
' { "Result": "OK", "Action": "UPDATED", "Message": "Record updated successfully" }


'---------------------------------------
' EXAMPLE 4: BATCH INSERT/UPDATE
'---------------------------------------

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "BatchUsers", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim batchMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name", "department"},
    New String() {"UserId", "Email", "Name", "Department"},
    New Boolean() {True, True, True, False},
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users",
    batchMappings,
    New String() {"UserId"},
    True  ' Allow updates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New String() {"Records"}),
    batchLogic,
    "Batch operation completed",
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
' EXAMPLE 5: READ WITH DATE RANGE
'---------------------------------------

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "DateRange", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

Dim dateConditions As New System.Collections.Generic.Dictionary(Of String, Object)

dateConditions.Add("startDate", DB.Global.CreateParameterCondition(
    "startDate",
    "CreatedDate >= :startDate",
    Nothing
))

dateConditions.Add("endDate", DB.Global.CreateParameterCondition(
    "endDate",
    "CreatedDate <= :endDate",
    Nothing
))

dateConditions.Add("status", DB.Global.CreateParameterCondition(
    "status",
    "Status = :status",
    "Status = 'Active'"  ' Default: only active if not specified
))

Dim dateRangeLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name, CreatedDate, Status FROM Users {WHERE} ORDER BY CreatedDate DESC",
    dateConditions,
    "Status = 'Active'"  ' Default WHERE clause
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    dateRangeLogic,
    "Date range search",
    ParsedPayload5,
    StringPayload5,
    False
)

' Payload: { "startDate": "2024-01-01", "endDate": "2024-12-31" }


'---------------------------------------
' EXAMPLE 6: SOFT DELETE
'---------------------------------------

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "SoftDelete", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim deleteMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "isDeleted", "deletedDate"},
    New String() {"UserId", "IsDeleted", "DeletedDate"},
    New Boolean() {True, True, False},
    New Object() {Nothing, Nothing, Nothing}
)

Dim deleteLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    deleteMappings,
    New String() {"UserId"},
    True  ' Allow updates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"userId", "isDeleted"}),
    deleteLogic,
    "User soft deleted",
    ParsedPayload6,
    StringPayload6,
    False
)

' Payload: { "userId": "456", "isDeleted": true, "deletedDate": "2025-01-15" }


'---------------------------------------
' EXAMPLE 7: COMPOSITE KEY
'---------------------------------------

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "OrderItems", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim compositeKeyMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"orderId", "productId", "quantity", "price"},
    New String() {"OrderId", "ProductId", "Quantity", "Price"},
    New Boolean() {True, True, True, False},
    New Object() {Nothing, Nothing, Nothing, 0}
)

Dim compositeKeyLogic = DB.Global.CreateBusinessLogicForWriting(
    "OrderItems",
    compositeKeyMappings,
    New String() {"OrderId", "ProductId"},  ' Composite key
    True
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"orderId", "productId", "quantity"}),
    compositeKeyLogic,
    "Order item saved",
    ParsedPayload7,
    StringPayload7,
    False
)

' Payload: { "orderId": "ORD-001", "productId": "PROD-123", "quantity": 5, "price": 99.99 }


'---------------------------------------
' EXAMPLE 8: CUSTOM UPDATE SQL
'---------------------------------------

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "LoginTracking", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

Dim loginMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId"},
    New String() {"UserId"},
    New Boolean() {True},
    New Object() {Nothing}
)

Dim loginLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    loginMappings,
    New String() {"UserId"},
    True,
    Nothing,  ' Default existence check
    "UPDATE Users SET LastLoginDate = GETDATE(), LoginCount = LoginCount + 1 WHERE UserId = :UserId",  ' Custom update
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"userId"}),
    loginLogic,
    "Login recorded",
    ParsedPayload8,
    StringPayload8,
    False
)

' Payload: { "userId": "123" }
' Updates LastLoginDate and increments LoginCount


'---------------------------------------
' EXAMPLE 9: MULTI-TABLE JOIN
'---------------------------------------

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, False, "UsersWithDept", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

Dim joinConditions As New System.Collections.Generic.Dictionary(Of String, Object)

joinConditions.Add("userId", DB.Global.CreateParameterCondition(
    "userId",
    "u.UserId = :userId",
    Nothing
))

joinConditions.Add("deptName", DB.Global.CreateParameterCondition(
    "deptName",
    "d.Name LIKE :deptName",
    Nothing
))

Dim joinLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT u.UserId, u.Name, u.Email, d.Name as DeptName, d.Location " &
    "FROM Users u INNER JOIN Departments d ON u.DepartmentId = d.DepartmentId {WHERE}",
    joinConditions
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    joinLogic,
    "Users with departments",
    ParsedPayload9,
    StringPayload9,
    False
)

' Payload: { "deptName": "Sales%" }


'---------------------------------------
' EXAMPLE 10: COUNT/AGGREGATE
'---------------------------------------

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, False, "CountUsers", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    Return PayloadError10
End If

Dim countConditions As New System.Collections.Generic.Dictionary(Of String, Object)

countConditions.Add("status", DB.Global.CreateParameterCondition(
    "status",
    "Status = :status",
    Nothing
))

Dim countLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT COUNT(*) as UserCount, Department FROM Users {WHERE} GROUP BY Department",
    countConditions
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    countLogic,
    "Count users",
    ParsedPayload10,
    StringPayload10,
    False
)

' Payload: { "status": "Active" }
' Response: { "Result": "OK", "Records": [{"UserCount": 25, "Department": "Sales"}] }
