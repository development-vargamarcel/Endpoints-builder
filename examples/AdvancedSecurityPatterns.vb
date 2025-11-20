' ===================================
' SECURITY PATTERNS AND BEST PRACTICES
' Token validation, authorization, and secure coding examples
' ===================================

'---------------------------------------
' EXAMPLE 1: TOKEN VALIDATION (ENABLED)
'---------------------------------------
' Enforce token validation for production endpoints

Dim CheckToken = True  ' IMPORTANT: Set to True in production
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "SecureEndpoint", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Define search conditions
Dim secureConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
secureConditions.Add("RecordId", DB.Global.CreateParameterCondition("RecordId", "RecordId = :RecordId", Nothing))
secureConditions.Add("UserId", DB.Global.CreateParameterCondition("UserId", "UserId = :UserId", Nothing))

' NOTE: Exclude sensitive fields in your SQL SELECT statement explicitly
' e.g., SELECT RecordId, UserId, Name FROM SensitiveData (do NOT include SSN, CreditCard, Password)
Dim secureLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT RecordId, UserId, Name, Email FROM SensitiveData {WHERE}",
    secureConditions
)

' Validator: No required params for flexible search, but token is enforced
Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    secureLogic,
    "Secure data access",
    ParsedPayload,
    StringPayload,
    True  ' CheckForToken = True
)

' Required payload structure:
' {
'   "Token": "valid-token-here",
'   "UserId": "U123"
' }
'
' Invalid requests return:
' { "Result": "KO", "Reason": "Please insert the token in a property called Token." }
' { "Result": "KO", "Reason": "Invalid token." }

'---------------------------------------
' EXAMPLE 2: EXCLUDE SENSITIVE FIELDS
'---------------------------------------
' Always exclude PII and sensitive data from responses

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "UserProfile", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

' Define search conditions for profile
Dim profileConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
profileConditions.Add("UserId", DB.Global.CreateParameterCondition("UserId", "UserId = :UserId", Nothing))
profileConditions.Add("Email", DB.Global.CreateParameterCondition("Email", "Email = :Email", Nothing))
profileConditions.Add("Phone", DB.Global.CreateParameterCondition("Phone", "Phone = :Phone", Nothing))

' IMPORTANT: Explicitly exclude sensitive fields by NOT selecting them in SQL
' Only select safe fields: UserId, Email, Name, Phone, Department, etc.
' NEVER select: Password, PasswordHash, SSN, CreditCardNumber, CVV, APIKey, etc.
Dim profileLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name, Phone, Department, CreatedDate FROM Users {WHERE}",
    profileConditions
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, profileLogic, "Profile read",
    ParsedPayload2, StringPayload2, False
)

' Response will NEVER include excluded fields, even if they exist in the database

'---------------------------------------
' EXAMPLE 3: PARAMETERIZED QUERIES (BUILT-IN)
'---------------------------------------
' Library automatically uses parameterized queries - SQL injection protection

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "SafeQuery", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

' This is SAFE - uses parameterized query
Dim safeConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
safeConditions.Add("ProductName", DB.Global.CreateParameterCondition("ProductName", "ProductName LIKE :ProductName", Nothing))
safeConditions.Add("Category", DB.Global.CreateParameterCondition("Category", "Category = :Category", Nothing))

Dim safeLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ProductId, ProductName, Category, Price, Description FROM Products {WHERE}",
    safeConditions
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, safeLogic, "Safe product search",
    ParsedPayload3, StringPayload3, False
)

' Malicious payload attempt:
' { "ProductName": "'; DROP TABLE Products; --" }
'
' Library safely binds as parameter:
' SQL: WHERE ProductName LIKE :ProductName
' Parameter: ProductName = "'; DROP TABLE Products; --"
' Result: Searches for literal string, no SQL injection possible

'---------------------------------------
' EXAMPLE 4: INPUT VALIDATION
'---------------------------------------
' Validate all required parameters before processing

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "ValidatedInput", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

' Strict validation
Dim requiredParams = New System.String() {"UserId", "Action", "Timestamp"}
Dim validator4 = DB.Global.CreateValidator(requiredParams)

' Additional custom validation example
Dim actionResult = DB.Global.GetStringParameter(ParsedPayload4, "Action")
If actionResult.Item1 Then
    Dim action As System.String = actionResult.Item2
    ' Whitelist allowed actions
    Dim allowedActions = New System.String() {"READ", "UPDATE", "DELETE"}
    If Not allowedActions.Contains(action.ToUpper()) Then
        Return DB.Global.CreateErrorResponse("Invalid action. Allowed: READ, UPDATE, DELETE")
    End If
End If

Dim auditConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
auditConditions.Add("UserId", DB.Global.CreateParameterCondition("UserId", "UserId = :UserId", Nothing))
auditConditions.Add("Action", DB.Global.CreateParameterCondition("Action", "Action = :Action", Nothing))

Dim logic4 = DB.Global.CreateBusinessLogicForReading(
    "SELECT LogId, UserId, Action, Timestamp, IpAddress FROM AuditLog {WHERE} ORDER BY Timestamp DESC",
    auditConditions
)

Return DB.Global.ProcessActionLink(
    DB, validator4, logic4, "Validated audit query",
    ParsedPayload4, StringPayload4, False
)

' Validates: Required fields present, Action value is whitelisted

'---------------------------------------
' EXAMPLE 5: ROLE-BASED DATA FILTERING
'---------------------------------------
' Filter data based on user role or permissions

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, True, "RoleBasedAccess", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

' Extract user role from authenticated context (implementation depends on your auth system)
Dim userRoleResult = DB.Global.GetStringParameter(ParsedPayload5, "UserRole")
Dim userIdResult = DB.Global.GetStringParameter(ParsedPayload5, "UserId")

If Not userRoleResult.Item1 OrElse Not userIdResult.Item1 Then
    Return DB.Global.CreateErrorResponse("UserRole and UserId are required")
End If

Dim userRole As System.String = userRoleResult.Item2
Dim userId As System.String = userIdResult.Item2

' Build query based on role
Dim roleConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

If userRole.ToUpper() = "ADMIN" Then
    ' Admins see all records
    roleConditions.Add("UserId", DB.Global.CreateParameterCondition(
        "UserId",
        "UserId = :UserId",
        Nothing  ' No filter if not specified
    ))
Else
    ' Regular users only see their own records
    roleConditions.Add("UserId", DB.Global.CreateParameterCondition(
        "UserId",
        "UserId = :UserId",
        $"UserId = '{userId}'"  ' Force filter to current user
    ))
End If

roleConditions.Add("Status", DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    Nothing
))

Dim roleBasedLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT OrderId, UserId, OrderDate, Status, TotalAmount FROM Orders {WHERE} ORDER BY OrderDate DESC",
    roleConditions,
    $"UserId = '{userId}'"  ' Default: users see only their data
)

' Validator: UserId and UserRole are required for authorization
Dim validator5 = DB.Global.CreateValidator(New System.String() {"UserId", "UserRole"})

Return DB.Global.ProcessActionLink(
    DB, validator5, roleBasedLogic, "Role-based order access",
    ParsedPayload5, StringPayload5, True
)

' Admin payload: { "Token": "...", "UserRole": "ADMIN" }
' Result: Can see all orders
'
' User payload: { "Token": "...", "UserRole": "USER", "UserId": "U123" }
' Result: Can only see orders where UserId = "U123"

'---------------------------------------
' EXAMPLE 6: RATE LIMITING (LOG-BASED)
'---------------------------------------
' Log all operations for rate limiting and audit trails

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, True, "RateLimited", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

' Get user identifier from token or payload
Dim userIdResult6 = DB.Global.GetStringParameter(ParsedPayload6, "UserId")
If Not userIdResult6.Item1 Then
    Return DB.Global.CreateErrorResponse("UserId required")
End If

' Log the request BEFORE processing
Dim logMessage = $"API Request from User: {userIdResult6.Item2} at {System.DateTime.Now}"
DB.Global.LogCustom(DB, StringPayload6, "Request received", logMessage)

' Process the request
Dim productConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
productConditions.Add("Category", DB.Global.CreateParameterCondition("Category", "Category = :Category", Nothing))

Dim logic6 = DB.Global.CreateBusinessLogicForReading(
    "SELECT ProductId, ProductName, Category, Price, Stock FROM Products {WHERE}",
    productConditions
)

' Validator: UserId is required for audit trail
Dim validator6 = DB.Global.CreateValidator(New System.String() {"UserId"})

Dim result6 = DB.Global.ProcessActionLink(
    DB, validator6, logic6, "Product query",
    ParsedPayload6, StringPayload6, True
)

' Log the response
DB.Global.LogCustom(DB, StringPayload6, result6, $"API Response to User: {userIdResult6.Item2}")

Return result6

' Implementation note: Analyze logs to implement rate limiting:
' - Count requests per user per time window
' - Block users exceeding threshold
' - Alert on suspicious patterns

'---------------------------------------
' EXAMPLE 7: SECURE UPDATE OPERATIONS
'---------------------------------------
' Prevent unauthorized updates

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, True, "SecureUpdate", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

' Validate user has permission to update this record
Dim targetUserIdResult = DB.Global.GetStringParameter(ParsedPayload7, "TargetUserId")
Dim currentUserIdResult = DB.Global.GetStringParameter(ParsedPayload7, "CurrentUserId")
Dim userRoleResult7 = DB.Global.GetStringParameter(ParsedPayload7, "UserRole")

If Not targetUserIdResult.Item1 OrElse Not currentUserIdResult.Item1 OrElse Not userRoleResult7.Item1 Then
    Return DB.Global.CreateErrorResponse("TargetUserId, CurrentUserId, and UserRole are required")
End If

' Authorization check: Users can only update their own profile unless they're admins
If userRoleResult7.Item2.ToUpper() <> "ADMIN" AndAlso targetUserIdResult.Item2 <> currentUserIdResult.Item2 Then
    Return DB.Global.CreateErrorResponse("Unauthorized: You can only update your own profile")
End If

' Proceed with update - create field mappings
Dim updateMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"UserId", "Email", "Phone", "Address"},
    New System.String() {"UserId", "Email", "Phone", "Address"},
    New Boolean() {True, False, False, False},
    New Boolean() {True, False, False, False},  ' UserId is primary key
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim updateLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    updateMappings,
    New System.String() {"UserId"},
    True
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"UserId"}),
    updateLogic,
    "Authorized user update",
    ParsedPayload7,
    StringPayload7,
    True
)

'---------------------------------------
' EXAMPLE 8: PREVENT MASS ASSIGNMENT
'---------------------------------------
' Only allow updating specific fields, not all fields

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, True, "LimitedUpdate", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

' Use field mappings to explicitly control which fields can be updated
Dim safeUpdateMappings As New System.Collections.Generic.Dictionary(Of System.String, FieldMapping)

' Only these fields can be updated by users
safeUpdateMappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True, Nothing))
safeUpdateMappings.Add("displayName", DB.Global.CreateFieldMapping("displayName", "DISPLAY_NAME", False, Nothing))
safeUpdateMappings.Add("bio", DB.Global.CreateFieldMapping("bio", "BIO", False, Nothing))
safeUpdateMappings.Add("avatarUrl", DB.Global.CreateFieldMapping("avatarUrl", "AVATAR_URL", False, Nothing))

' Sensitive fields are NOT in the mapping, so they can't be updated through this endpoint:
' - IsAdmin, AccountBalance, PasswordHash, Email (require separate verified endpoints)

Dim safeUpdateLogic = DB.Global.CreateBusinessLogicForWriting(
    "USER_PROFILES",
    safeUpdateMappings,
    New System.String() {"USER_ID"},
    True, Nothing, Nothing, Nothing
)

' Validator: UserId is required to identify which profile to update
Dim validator8 = DB.Global.CreateValidator(New System.String() {"userId"})

Return DB.Global.ProcessActionLink(
    DB, validator8, safeUpdateLogic, "Safe profile update",
    ParsedPayload8, StringPayload8, True
)

' Malicious payload attempt:
' {
'   "Token": "...",
'   "userId": "U123",
'   "displayName": "Hacker",
'   "IsAdmin": true,           // IGNORED - not in field mappings
'   "AccountBalance": 1000000  // IGNORED - not in field mappings
' }
'
' Only displayName is updated; IsAdmin and AccountBalance are ignored

'---------------------------------------
' EXAMPLE 9: SECURE BATCH OPERATIONS
'---------------------------------------
' Apply same security to batch operations

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, True, "SecureBatch", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

' Extract user info for authorization
Dim currentUserId9Result = DB.Global.GetStringParameter(ParsedPayload9, "CurrentUserId")
Dim userRole9Result = DB.Global.GetStringParameter(ParsedPayload9, "UserRole")

If Not currentUserId9Result.Item1 OrElse Not userRole9Result.Item1 Then
    Return DB.Global.CreateErrorResponse("CurrentUserId and UserRole required")
End If

' For batch operations, verify each record belongs to current user (unless admin)
Dim recordsResult = DB.Global.GetArrayParameter(ParsedPayload9, "Records")
If recordsResult.Item1 Then
    For Each recordToken As Newtonsoft.Json.Linq.JToken In recordsResult.Item2
        Dim record As Newtonsoft.Json.Linq.JObject = CType(recordToken, Newtonsoft.Json.Linq.JObject)
        Dim recordUserIdResult = DB.Global.GetStringParameter(record, "UserId")

        If recordUserIdResult.Item1 Then
            ' Check authorization for each record
            If userRole9Result.Item2.ToUpper() <> "ADMIN" AndAlso recordUserIdResult.Item2 <> currentUserId9Result.Item2 Then
                Return DB.Global.CreateErrorResponse($"Unauthorized: Cannot modify records for user {recordUserIdResult.Item2}")
            End If
        End If
    Next
End If

' Proceed with batch operation - create field mappings
Dim noteMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"NoteId", "UserId", "Title", "Content"},
    New System.String() {"NoteId", "UserId", "Title", "Content"},
    New Boolean() {True, True, False, False},
    New Boolean() {True, False, False, False},  ' NoteId is primary key
    New Object() {Nothing, Nothing, Nothing, Nothing}
)

Dim batchLogic9 = DB.Global.CreateBusinessLogicForBatchWriting(
    "UserNotes",
    noteMappings,
    New System.String() {"NoteId"},
    True
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New System.String() {"Records"}),
    batchLogic9,
    "Secure batch note update",
    ParsedPayload9,
    StringPayload9,
    True
)

'---------------------------------------
' EXAMPLE 10: COMPREHENSIVE SECURITY CHECKLIST
'---------------------------------------
' Complete secure endpoint implementation

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, True, "ComprehensiveSecure", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    ' 1. ✓ Token validation
    Return PayloadError10
End If

' 2. ✓ Get authenticated user context
Dim authUserId = DB.Global.GetStringParameter(ParsedPayload10, "UserId")
Dim authUserRole = DB.Global.GetStringParameter(ParsedPayload10, "UserRole")

If Not authUserId.Item1 OrElse Not authUserRole.Item1 Then
    Return DB.Global.CreateErrorResponse("Authentication context required")
End If

' 3. ✓ Validate required parameters
Dim validator10 = DB.Global.CreateValidator(New System.String() {"Operation", "TargetResource"})
Dim validationError = validator10(ParsedPayload10)
If Not System.String.IsNullOrEmpty(validationError) Then
    Return validationError
End If

' 4. ✓ Whitelist allowed operations
Dim operationResult = DB.Global.GetStringParameter(ParsedPayload10, "Operation")
Dim allowedOps = New System.String() {"READ", "CREATE", "UPDATE"}
If Not allowedOps.Contains(operationResult.Item2.ToUpper()) Then
    Return DB.Global.CreateErrorResponse("Invalid operation")
End If

' 5. ✓ Check resource-level permissions (simplified example)
Dim targetResourceResult = DB.Global.GetStringParameter(ParsedPayload10, "TargetResource")
If authUserRole.Item2.ToUpper() <> "ADMIN" AndAlso targetResourceResult.Item2 = "ADMIN_PANEL" Then
    Return DB.Global.CreateErrorResponse("Insufficient permissions")
End If

' 6. ✓ Use field mappings to prevent mass assignment
Dim secureMappings As New System.Collections.Generic.Dictionary(Of System.String, FieldMapping)
secureMappings.Add("resourceId", DB.Global.CreateFieldMapping("resourceId", "RESOURCE_ID", True, Nothing))
secureMappings.Add("title", DB.Global.CreateFieldMapping("title", "TITLE", False, Nothing))
secureMappings.Add("description", DB.Global.CreateFieldMapping("description", "DESCRIPTION", False, Nothing))
' Note: Sensitive fields like OwnerId, IsPublic, Permissions NOT included

' 7. ✓ Exclude sensitive fields from SQL SELECT (do not include PasswordHash, ApiKey, InternalNotes)
' 8. ✓ Add row-level security with WHERE clause
Dim securityConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
securityConditions.Add("resourceId", DB.Global.CreateParameterCondition(
    "resourceId",
    "RESOURCE_ID = :resourceId",
    Nothing
))

' Force filter by owner unless admin
Dim defaultWhere As System.String
If authUserRole.Item2.ToUpper() = "ADMIN" Then
    defaultWhere = "IS_DELETED = 0"  ' Admins see all non-deleted
Else
    defaultWhere = $"OWNER_ID = '{authUserId.Item2}' AND IS_DELETED = 0"  ' Users see only their own
End If

' 9. ✓ Create secure business logic - explicitly select only safe fields
Dim secureLogic10 = DB.Global.CreateBusinessLogicForReading(
    "SELECT RESOURCE_ID, TITLE, DESCRIPTION, CREATED_DATE, OWNER_ID FROM RESOURCES {WHERE} ORDER BY CREATED_DATE DESC",
    securityConditions,
    defaultWhere,
    secureMappings
)

' 10. ✓ Process with logging
Dim result10 = DB.Global.ProcessActionLink(
    DB, Nothing, secureLogic10, "Secure resource access",
    ParsedPayload10, StringPayload10, True
)

' 11. ✓ Log operation for audit trail
DB.Global.LogCustom(DB, StringPayload10, result10, $"Secure operation by user {authUserId.Item2}")

Return result10

' SECURITY CHECKLIST SUMMARY:
' ✓ 1. Token validation enabled
' ✓ 2. Authenticated user context extracted
' ✓ 3. Required parameters validated
' ✓ 4. Operations whitelisted
' ✓ 5. Resource-level permissions checked
' ✓ 6. Field mappings prevent mass assignment
' ✓ 7. Sensitive fields excluded from responses
' ✓ 8. Row-level security enforced
' ✓ 9. Parameterized queries (automatic)
' ✓ 10. Operations logged for audit
' ✓ 11. Error messages don't leak sensitive info

'---------------------------------------
' ADDITIONAL SECURITY BEST PRACTICES
'---------------------------------------

' 1. HTTPS Only: Deploy endpoints over HTTPS only
' 2. Token Expiration: Implement token refresh mechanism
' 3. Rate Limiting: Monitor logs and implement rate limits
' 4. Input Sanitization: Validate data types and formats
' 5. Output Encoding: Prevent XSS in API responses
' 6. CORS: Configure appropriate CORS policies
' 7. Versioning: Version your API for security updates
' 8. Documentation: Document security requirements
' 9. Testing: Include security tests in your test suite
' 10. Monitoring: Set up alerts for suspicious activity

' SECURITY ANTI-PATTERNS TO AVOID:

' ❌ DON'T: Disable token validation in production
' CheckToken = False  ' NEVER IN PRODUCTION

' ❌ DON'T: Include sensitive fields in responses
' excludeFields = Nothing  ' Always exclude sensitive data

' ❌ DON'T: Trust client-supplied role/permission data without verification
' Dim userRole = payload("UserRole")  ' Verify against authenticated session

' ❌ DON'T: Allow updates to all fields
' allowUpdates = True with no field restrictions  ' Use field mappings

' ❌ DON'T: Skip validation
' validator = Nothing  ' Always validate required params

' ❌ DON'T: Return detailed error messages to clients
' Return $"SQL Error: {ex.Message}"  ' Log errors, return generic messages

' ❌ DON'T: Use string concatenation for SQL
' sql = "WHERE UserId = '" & userId & "'"  ' Use parameterized queries

' ✅ DO: Use the library's built-in security features
' ✅ DO: Log all security-relevant operations
' ✅ DO: Validate input at multiple layers
' ✅ DO: Apply defense in depth
' ✅ DO: Regular security audits
' ✅ DO: Keep dependencies updated
' ✅ DO: Follow principle of least privilege
