' ===================================
' CRUD OPERATIONS EXAMPLES
' Complete examples for Create, Read, Update, Delete operations
' ===================================

'---------------------------------------
' EXAMPLE 1: SIMPLE READ OPERATION
'---------------------------------------
' Payload: { "UserId": "123", "Email": "john%" }
' Response: Returns matching users

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadAndTokenValidationError = DB.Global.ValidatePayloadAndToken(DB,CheckToken,"ReadUsers",ParsedPayload,StringPayload)
If PayloadAndTokenValidationError IsNot Nothing Then
    Return PayloadAndTokenValidationError
End If

' Define searchable fields and excluded fields
Dim searchFields = New String() {"UserId", "Email", "Name", "Department"}
Dim excludeFields = New String() {"Password", "PasswordHash", "SSN"}

' Create read logic with LIKE operator for flexible search
Dim readLogic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",           ' Table name
    searchFields,      ' Fields that can be used for filtering
    excludeFields,     ' Fields to exclude from response
    True              ' Use LIKE operator (True) or equals (False)
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,          ' No validator - all parameters are optional for search
    readLogic,
    "User search",
    ParsedPayload,
    StringPayload,
    False
)

' Example payloads:
' { "UserId": "123" }                          - Exact search
' { "Email": "%@company.com" }                 - Pattern search
' { "Name": "John%", "Department": "Sales" }   - Multi-field search
' {}                                           - Returns all records

'---------------------------------------
' EXAMPLE 2: INSERT OPERATION (NO UPDATES)
'---------------------------------------
' Payload: { "UserId": "456", "Email": "jane@example.com", "Name": "Jane Doe" }

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "InsertUser", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

' Define all fields and required fields
Dim allFields = New String() {"UserId", "Email", "Name", "Department", "CreatedDate"}
Dim requiredFields = New String() {"UserId", "Email"}  ' Primary key fields

' Create write logic - insert only, no updates
Dim insertLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    allFields,
    requiredFields,
    False             ' allowUpdates = False (insert only)
)

' Add validator for required fields
Dim validator = DB.Global.CreateValidator(requiredFields)

Return DB.Global.ProcessActionLink(
    DB,
    validator,
    insertLogic,
    "User inserted",
    ParsedPayload2,
    StringPayload2,
    False
)

' Response on success:
' { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
'
' Response if exists:
' { "Result": "KO", "Reason": "456,jane@example.com - Record already exists and updates are not allowed" }

'---------------------------------------
' EXAMPLE 3: UPSERT OPERATION (INSERT OR UPDATE)
'---------------------------------------
' Payload: { "UserId": "456", "Email": "jane.updated@example.com", "Name": "Jane Smith", "Department": "Marketing" }

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "UpsertUser", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

' Same field definitions
Dim allFields3 = New String() {"UserId", "Email", "Name", "Department", "ModifiedDate"}
Dim requiredFields3 = New String() {"UserId"}

' Create upsert logic - insert or update
Dim upsertLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    allFields3,
    requiredFields3,
    True              ' allowUpdates = True (upsert)
)

Dim validator3 = DB.Global.CreateValidator(requiredFields3)

Return DB.Global.ProcessActionLink(
    DB,
    validator3,
    upsertLogic,
    "User upserted",
    ParsedPayload3,
    StringPayload3,
    False
)

' Response on insert:
' { "Result": "OK", "Action": "INSERTED", "RequiredColumns": "UserId", "Message": "Record inserted successfully" }
'
' Response on update:
' { "Result": "OK", "Action": "UPDATED", "RequiredColumns": "UserId", "Message": "Record updated successfully" }

'---------------------------------------
' EXAMPLE 4: UPDATE ONLY SPECIFIC FIELDS
'---------------------------------------
' Update only email and department, keeping other fields unchanged

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "UpdateUser", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

' Only updatable fields + key field
Dim updateableFields = New String() {"UserId", "Email", "Department", "ModifiedDate"}
Dim keyField = New String() {"UserId"}

Dim updateLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    updateableFields,
    keyField,
    True
)

Dim validator4 = DB.Global.CreateValidator(keyField)

Return DB.Global.ProcessActionLink(
    DB,
    validator4,
    updateLogic,
    "User updated",
    ParsedPayload4,
    StringPayload4,
    False
)

' Payload: { "UserId": "456", "Email": "new.email@example.com" }
' Only Email will be updated, other fields remain unchanged

'---------------------------------------
' EXAMPLE 5: SOFT DELETE OPERATION
'---------------------------------------
' Mark record as deleted without removing from database

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "DeleteUser", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

' Soft delete = update IsDeleted flag
Dim deleteFields = New String() {"UserId", "IsDeleted", "DeletedDate"}
Dim keyField5 = New String() {"UserId"}

Dim deleteLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    deleteFields,
    keyField5,
    True
)

Dim validator5 = DB.Global.CreateValidator(New String() {"UserId", "IsDeleted"})

Return DB.Global.ProcessActionLink(
    DB,
    validator5,
    deleteLogic,
    "User deleted (soft)",
    ParsedPayload5,
    StringPayload5,
    False
)

' Payload: { "UserId": "456", "IsDeleted": true, "DeletedDate": "2025-01-15" }

'---------------------------------------
' EXAMPLE 6: READ WITH EXACT MATCH (NO LIKE OPERATOR)
'---------------------------------------
' Use equals instead of LIKE for exact matching

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "ExactSearch", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim exactSearchLogic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"UserId", "Email", "Status"},
    New String() {"Password"},
    False             ' useLikeOperator = False (use = instead of LIKE)
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    exactSearchLogic,
    "Exact user search",
    ParsedPayload6,
    StringPayload6,
    False
)

' Payload: { "Status": "Active" }
' SQL: WHERE Status = 'Active' (not Status LIKE 'Active')

'---------------------------------------
' EXAMPLE 7: READ ACTIVE RECORDS ONLY
'---------------------------------------
' Filter out soft-deleted records

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "ActiveUsers", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

' Use advanced reader with custom base SQL
Dim searchConditions7 As New System.Collections.Generic.Dictionary(Of String, Object)

searchConditions7.Add("Email", DB.Global.CreateParameterCondition(
    "Email",
    "Email LIKE :Email",
    Nothing
))

searchConditions7.Add("Department", DB.Global.CreateParameterCondition(
    "Department",
    "Department = :Department",
    Nothing
))

Dim activeUsersLogic = DB.Global.CreateAdvancedBusinessLogicForReading(
    "SELECT * FROM Users WHERE IsDeleted = 0 {WHERE}",  ' Base query with {WHERE} placeholder
    searchConditions7,
    New String() {"Password"},
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    activeUsersLogic,
    "Active users search",
    ParsedPayload7,
    StringPayload7,
    False
)

' Payload: { "Department": "Sales" }
' SQL: SELECT * FROM Users WHERE IsDeleted = 0 AND Department = :Department

'---------------------------------------
' EXAMPLE 8: READ WITH REQUIRED PARAMETERS
'---------------------------------------
' Force specific parameters to be provided

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "UsersByDept", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

' Require Department parameter
Dim requiredValidator = DB.Global.CreateValidator(New String() {"Department"})

Dim readWithRequired = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"Department", "Status"},
    New String() {"Password"},
    False
)

Return DB.Global.ProcessActionLink(
    DB,
    requiredValidator,  ' Validates Department is present
    readWithRequired,
    "Users by department",
    ParsedPayload8,
    StringPayload8,
    False
)

' Valid payload: { "Department": "IT" }
' Invalid payload: {} - Returns error: "Parameter Department not specified"

'---------------------------------------
' EXAMPLE 9: MULTI-TABLE JOIN READ
'---------------------------------------
' Read from multiple tables with JOIN

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, False, "UsersWithDept", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

Dim joinConditions As New System.Collections.Generic.Dictionary(Of String, Object)

joinConditions.Add("UserId", DB.Global.CreateParameterCondition(
    "UserId",
    "u.UserId = :UserId",
    Nothing
))

joinConditions.Add("DepartmentName", DB.Global.CreateParameterCondition(
    "DepartmentName",
    "d.Name LIKE :DepartmentName",
    Nothing
))

Dim joinLogic = DB.Global.CreateAdvancedBusinessLogicForReading(
    "SELECT u.UserId, u.Name, u.Email, d.Name as DepartmentName, d.Location " &
    "FROM Users u " &
    "INNER JOIN Departments d ON u.DepartmentId = d.DepartmentId " &
    "{WHERE}",
    joinConditions,
    Nothing,
    Nothing,
    Nothing
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

' Payload: { "DepartmentName": "Sales%" }
' Returns users with their department information

'---------------------------------------
' EXAMPLE 10: COUNT RECORDS
'---------------------------------------
' Get record count without retrieving all data

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, False, "CountUsers", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    Return PayloadError10
End If

Dim countConditions As New System.Collections.Generic.Dictionary(Of String, Object)

countConditions.Add("Department", DB.Global.CreateParameterCondition(
    "Department",
    "Department = :Department",
    Nothing
))

countConditions.Add("Status", DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    "Status IS NOT NULL"  ' Default condition when Status not provided
))

Dim countLogic = DB.Global.CreateAdvancedBusinessLogicForReading(
    "SELECT COUNT(*) as UserCount, Department FROM Users {WHERE} GROUP BY Department",
    countConditions,
    Nothing,
    Nothing,
    Nothing
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

' Payload: { "Status": "Active" }
' Response: { "Result": "OK", "Records": [{"UserCount": 25, "Department": "Sales"}, {"UserCount": 15, "Department": "IT"}] }
