' ===================================
' FIELD MAPPING EXAMPLES
' Map JSON properties to SQL columns with validation
' ===================================

'---------------------------------------
' EXAMPLE 1: BASIC FIELD MAPPING
'---------------------------------------
' Map camelCase JSON to SNAKE_CASE SQL columns

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "MappedWrite", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Create field mappings dictionary
Dim fieldMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

' Map JSON properties to SQL columns
fieldMappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True, Nothing))
fieldMappings.Add("email", DB.Global.CreateFieldMapping("email", "EMAIL_ADDRESS", True, Nothing))
fieldMappings.Add("firstName", DB.Global.CreateFieldMapping("firstName", "FIRST_NAME", True, Nothing))
fieldMappings.Add("lastName", DB.Global.CreateFieldMapping("lastName", "LAST_NAME", True, Nothing))
fieldMappings.Add("phoneNumber", DB.Global.CreateFieldMapping("phoneNumber", "PHONE_NUMBER", False, Nothing))
fieldMappings.Add("department", DB.Global.CreateFieldMapping("department", "DEPT_CODE", False, "GENERAL"))

' Define key fields (using SQL column names)
Dim keyFields = New String() {"USER_ID"}

' Create advanced writer with field mappings
Dim mappedWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "USERS_TABLE",
    fieldMappings,
    keyFields,
    True,  ' Allow updates
    Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,  ' Validation handled by field mappings
    mappedWriter,
    "User with field mapping",
    ParsedPayload,
    StringPayload,
    False
)

' JSON Payload (camelCase):
' {
'   "userId": "U123",
'   "email": "john.doe@example.com",
'   "firstName": "John",
'   "lastName": "Doe",
'   "phoneNumber": "555-0100"
' }
'
' Automatically mapped to SQL columns:
' USER_ID = "U123"
' EMAIL_ADDRESS = "john.doe@example.com"
' FIRST_NAME = "John"
' LAST_NAME = "Doe"
' PHONE_NUMBER = "555-0100"
' DEPT_CODE = "GENERAL" (default value applied)

'---------------------------------------
' EXAMPLE 2: REQUIRED VS OPTIONAL FIELDS
'---------------------------------------
' Enforce required fields through field mappings

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "RequiredFields", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim mappings2 As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

' Required fields (isRequired = True)
mappings2.Add("orderId", DB.Global.CreateFieldMapping("orderId", "ORDER_ID", True, Nothing))
mappings2.Add("customerId", DB.Global.CreateFieldMapping("customerId", "CUSTOMER_ID", True, Nothing))
mappings2.Add("orderDate", DB.Global.CreateFieldMapping("orderDate", "ORDER_DATE", True, Nothing))

' Optional fields (isRequired = False)
mappings2.Add("notes", DB.Global.CreateFieldMapping("notes", "NOTES", False, Nothing))
mappings2.Add("shippingAddress", DB.Global.CreateFieldMapping("shippingAddress", "SHIPPING_ADDR", False, Nothing))
mappings2.Add("priority", DB.Global.CreateFieldMapping("priority", "PRIORITY_LEVEL", False, "NORMAL"))

Dim writer2 = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "ORDERS",
    mappings2,
    New String() {"ORDER_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, writer2, "Order with validation",
    ParsedPayload2, StringPayload2, False
)

' Valid payload:
' {
'   "orderId": "ORD-001",
'   "customerId": "C123",
'   "orderDate": "2025-01-20"
' }
' Response: Success (priority defaults to "NORMAL")
'
' Invalid payload:
' {
'   "orderId": "ORD-001",
'   "orderDate": "2025-01-20"
' }
' Response: { "Result": "KO", "Reason": "Missing required fields: customerId" }

'---------------------------------------
' EXAMPLE 3: DEFAULT VALUES
'---------------------------------------
' Apply default values for missing optional fields

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "DefaultValues", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim mappings3 As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

mappings3.Add("productId", DB.Global.CreateFieldMapping("productId", "PRODUCT_ID", True, Nothing))
mappings3.Add("name", DB.Global.CreateFieldMapping("name", "PRODUCT_NAME", True, Nothing))
mappings3.Add("price", DB.Global.CreateFieldMapping("price", "PRICE", True, Nothing))

' Fields with defaults
mappings3.Add("status", DB.Global.CreateFieldMapping("status", "STATUS", False, "ACTIVE"))
mappings3.Add("stockQuantity", DB.Global.CreateFieldMapping("stockQuantity", "STOCK_QTY", False, 0))
mappings3.Add("taxRate", DB.Global.CreateFieldMapping("taxRate", "TAX_RATE", False, 0.10))
mappings3.Add("isVisible", DB.Global.CreateFieldMapping("isVisible", "IS_VISIBLE", False, True))

Dim writer3 = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "PRODUCTS",
    mappings3,
    New String() {"PRODUCT_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, writer3, "Product with defaults",
    ParsedPayload3, StringPayload3, False
)

' Minimal payload:
' {
'   "productId": "P001",
'   "name": "Widget",
'   "price": 19.99
' }
'
' Database values after insert:
' PRODUCT_ID = "P001"
' PRODUCT_NAME = "Widget"
' PRICE = 19.99
' STATUS = "ACTIVE" (default)
' STOCK_QTY = 0 (default)
' TAX_RATE = 0.10 (default)
' IS_VISIBLE = True (default)

'---------------------------------------
' EXAMPLE 4: READING WITH FIELD MAPPINGS
'---------------------------------------
' Use field mappings in read operations for consistency

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "MappedRead", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

' Define mappings for search parameters
Dim readMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)
readMappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", False, Nothing))
readMappings.Add("email", DB.Global.CreateFieldMapping("email", "EMAIL_ADDRESS", False, Nothing))
readMappings.Add("department", DB.Global.CreateFieldMapping("department", "DEPT_CODE", False, Nothing))

' Create search conditions using SQL column names
Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)
searchConditions.Add("USER_ID", DB.Global.CreateParameterCondition("userId", "USER_ID = :USER_ID", Nothing))
searchConditions.Add("EMAIL_ADDRESS", DB.Global.CreateParameterCondition("email", "EMAIL_ADDRESS LIKE :EMAIL_ADDRESS", Nothing))
searchConditions.Add("DEPT_CODE", DB.Global.CreateParameterCondition("department", "DEPT_CODE = :DEPT_CODE", Nothing))

Dim reader4 = DB.Global.CreateAdvancedBusinessLogicForReading(
    "SELECT USER_ID, EMAIL_ADDRESS, FIRST_NAME, LAST_NAME, DEPT_CODE FROM USERS_TABLE {WHERE}",
    searchConditions,
    Nothing,
    Nothing,
    readMappings
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, reader4, "User search with mapping",
    ParsedPayload4, StringPayload4, False
)

' JSON Request (camelCase):
' {
'   "email": "%@company.com",
'   "department": "IT"
' }
'
' SQL Generated:
' SELECT USER_ID, EMAIL_ADDRESS, FIRST_NAME, LAST_NAME, DEPT_CODE
' FROM USERS_TABLE
' WHERE EMAIL_ADDRESS LIKE :EMAIL_ADDRESS AND DEPT_CODE = :DEPT_CODE

'---------------------------------------
' EXAMPLE 5: COMPLEX MAPPING SCENARIO
'---------------------------------------
' Map nested business logic to flat database structure

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "ComplexMapping", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

' Map from API-friendly names to legacy database columns
Dim legacyMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

' Identity fields
legacyMappings.Add("employeeId", DB.Global.CreateFieldMapping("employeeId", "EMP_NO", True, Nothing))
legacyMappings.Add("ssn", DB.Global.CreateFieldMapping("ssn", "SSN_TAX_ID", False, Nothing))

' Personal info
legacyMappings.Add("firstName", DB.Global.CreateFieldMapping("firstName", "F_NAME", True, Nothing))
legacyMappings.Add("lastName", DB.Global.CreateFieldMapping("lastName", "L_NAME", True, Nothing))
legacyMappings.Add("middleInitial", DB.Global.CreateFieldMapping("middleInitial", "M_INIT", False, Nothing))

' Contact info
legacyMappings.Add("primaryEmail", DB.Global.CreateFieldMapping("primaryEmail", "EMAIL_1", False, Nothing))
legacyMappings.Add("secondaryEmail", DB.Global.CreateFieldMapping("secondaryEmail", "EMAIL_2", False, Nothing))
legacyMappings.Add("mobilePhone", DB.Global.CreateFieldMapping("mobilePhone", "PHONE_MOB", False, Nothing))
legacyMappings.Add("workPhone", DB.Global.CreateFieldMapping("workPhone", "PHONE_WRK", False, Nothing))

' Employment info
legacyMappings.Add("hireDate", DB.Global.CreateFieldMapping("hireDate", "DT_HIRE", True, Nothing))
legacyMappings.Add("departmentCode", DB.Global.CreateFieldMapping("departmentCode", "DEPT_CD", True, Nothing))
legacyMappings.Add("jobTitle", DB.Global.CreateFieldMapping("jobTitle", "JOB_TTL", False, "ASSOCIATE"))
legacyMappings.Add("salary", DB.Global.CreateFieldMapping("salary", "ANNUAL_SAL", False, Nothing))
legacyMappings.Add("isActive", DB.Global.CreateFieldMapping("isActive", "ACTV_FLG", False, "Y"))

Dim legacyWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "EMP_MASTER",
    legacyMappings,
    New String() {"EMP_NO"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, legacyWriter, "Employee legacy mapping",
    ParsedPayload5, StringPayload5, False
)

' Modern API payload:
' {
'   "employeeId": "E12345",
'   "firstName": "Jane",
'   "lastName": "Smith",
'   "primaryEmail": "jane.smith@company.com",
'   "mobilePhone": "555-0100",
'   "hireDate": "2025-01-15",
'   "departmentCode": "ENG",
'   "jobTitle": "Software Engineer",
'   "salary": 95000
' }
'
' Mapped to legacy database:
' EMP_NO = "E12345"
' F_NAME = "Jane"
' L_NAME = "Smith"
' EMAIL_1 = "jane.smith@company.com"
' PHONE_MOB = "555-0100"
' DT_HIRE = "2025-01-15"
' DEPT_CD = "ENG"
' JOB_TTL = "Software Engineer"
' ANNUAL_SAL = 95000
' ACTV_FLG = "Y" (default)

'---------------------------------------
' EXAMPLE 6: DYNAMIC FIELD MAPPING
'---------------------------------------
' Build field mappings from configuration

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "DynamicMapping", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

' Use helper function to create mappings from arrays
Dim jsonProps = New String() {"customerId", "customerName", "contactEmail", "accountType", "creditLimit"}
Dim sqlCols = New String() {"CUST_ID", "CUST_NAME", "EMAIL_ADDR", "ACCT_TYPE", "CREDIT_LMT"}
Dim requiredFlags = New Boolean() {True, True, True, False, False}
Dim defaults = New Object() {Nothing, Nothing, Nothing, "STANDARD", 1000}

' Create dictionary using factory function
Dim dynamicMappings = DB.Global.CreateFieldMappingsDictionary(
    jsonProps,
    sqlCols,
    requiredFlags,
    defaults
)

Dim dynamicWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "CUSTOMERS",
    dynamicMappings,
    New String() {"CUST_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, dynamicWriter, "Customer with dynamic mapping",
    ParsedPayload6, StringPayload6, False
)

' This approach allows configuration-driven field mappings:
' - Load mappings from config file
' - Different mappings per tenant
' - Version-specific API mappings

'---------------------------------------
' EXAMPLE 7: VALIDATION WITH FIELD MAPPINGS
'---------------------------------------
' Combine field mappings with custom validation

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "ValidatedMapping", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim validatedMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

validatedMappings.Add("accountId", DB.Global.CreateFieldMapping("accountId", "ACCT_ID", True, Nothing))
validatedMappings.Add("accountType", DB.Global.CreateFieldMapping("accountType", "ACCT_TYPE", True, Nothing))
validatedMappings.Add("balance", DB.Global.CreateFieldMapping("balance", "BALANCE_AMT", True, Nothing))

' Create custom validator for additional checks beyond field mappings
Dim customValidator = DB.Global.CreateValidator(New String() {"accountId", "accountType"})

Dim validatedWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "ACCOUNTS",
    validatedMappings,
    New String() {"ACCT_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    customValidator,  ' Additional validation layer
    validatedWriter,
    "Validated account",
    ParsedPayload7,
    StringPayload7,
    False
)

' Field mappings check IsRequired
' Custom validator can add business logic validation
' Example: check accountType is in allowed list, balance is non-negative, etc.

'---------------------------------------
' EXAMPLE 8: EXCLUDE UNMAPPED FIELDS
'---------------------------------------
' Only process explicitly mapped fields, ignore others

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "StrictMapping", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

' Only these fields are processed
Dim strictMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)
strictMappings.Add("transactionId", DB.Global.CreateFieldMapping("transactionId", "TXN_ID", True, Nothing))
strictMappings.Add("amount", DB.Global.CreateFieldMapping("amount", "TXN_AMT", True, Nothing))
strictMappings.Add("currency", DB.Global.CreateFieldMapping("currency", "CURRENCY_CODE", False, "USD"))
strictMappings.Add("timestamp", DB.Global.CreateFieldMapping("timestamp", "TXN_TIMESTAMP", True, Nothing))

Dim strictWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "TRANSACTIONS",
    strictMappings,
    New String() {"TXN_ID"},
    False,  ' No updates - transactions are immutable
    Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, strictWriter, "Transaction insert",
    ParsedPayload8, StringPayload8, False
)

' Payload:
' {
'   "transactionId": "TXN-001",
'   "amount": 150.00,
'   "currency": "EUR",
'   "timestamp": "2025-01-20T10:30:00Z",
'   "internalField": "ignore this",
'   "debugInfo": "this too"
' }
'
' Only mapped fields are processed:
' - transactionId, amount, currency, timestamp: SAVED
' - internalField, debugInfo: IGNORED (not in mappings)

'---------------------------------------
' EXAMPLE 9: VERSIONED MAPPINGS
'---------------------------------------
' Support multiple API versions with different mappings

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, False, "VersionedAPI", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

' Check API version (assume it's in payload or header)
Dim apiVersionResult = DB.Global.GetStringParameter(ParsedPayload9, "apiVersion")
Dim apiVersion As String = If(apiVersionResult.Item1, apiVersionResult.Item2, "v1")

Dim versionedMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

If apiVersion = "v1" Then
    ' V1 API: Simple field names
    versionedMappings.Add("id", DB.Global.CreateFieldMapping("id", "USER_ID", True, Nothing))
    versionedMappings.Add("name", DB.Global.CreateFieldMapping("name", "USER_NAME", True, Nothing))
ElseIf apiVersion = "v2" Then
    ' V2 API: More detailed field names
    versionedMappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True, Nothing))
    versionedMappings.Add("fullName", DB.Global.CreateFieldMapping("fullName", "USER_NAME", True, Nothing))
    versionedMappings.Add("displayName", DB.Global.CreateFieldMapping("displayName", "DISPLAY_NAME", False, Nothing))
End If

Dim versionedWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "USERS",
    versionedMappings,
    New String() {"USER_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, versionedWriter, "Versioned user API",
    ParsedPayload9, StringPayload9, False
)

' V1 Payload: { "apiVersion": "v1", "id": "U123", "name": "John Doe" }
' V2 Payload: { "apiVersion": "v2", "userId": "U123", "fullName": "John Doe", "displayName": "John" }
' Both map to the same database structure

'---------------------------------------
' EXAMPLE 10: AUDIT TRAIL WITH MAPPINGS
'---------------------------------------
' Add audit fields automatically

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, False, "AuditedMapping", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    Return PayloadError10
End If

Dim auditMappings As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

' Business fields
auditMappings.Add("recordId", DB.Global.CreateFieldMapping("recordId", "RECORD_ID", True, Nothing))
auditMappings.Add("description", DB.Global.CreateFieldMapping("description", "DESCRIPTION", True, Nothing))
auditMappings.Add("status", DB.Global.CreateFieldMapping("status", "STATUS", False, "ACTIVE"))

' Audit fields with defaults
' Note: In production, these would typically be set by database triggers or application logic
auditMappings.Add("createdBy", DB.Global.CreateFieldMapping("createdBy", "CREATED_BY", False, "SYSTEM"))
auditMappings.Add("createdDate", DB.Global.CreateFieldMapping("createdDate", "CREATED_DT", False, DateTime.Now))
auditMappings.Add("modifiedBy", DB.Global.CreateFieldMapping("modifiedBy", "MODIFIED_BY", False, "SYSTEM"))
auditMappings.Add("modifiedDate", DB.Global.CreateFieldMapping("modifiedDate", "MODIFIED_DT", False, DateTime.Now))

Dim auditWriter = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "AUDITED_RECORDS",
    auditMappings,
    New String() {"RECORD_ID"},
    True, Nothing, Nothing, Nothing
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, auditWriter, "Audited record",
    ParsedPayload10, StringPayload10, False
)

' Minimal payload:
' {
'   "recordId": "REC-001",
'   "description": "Sample record"
' }
'
' Saved with audit fields:
' RECORD_ID = "REC-001"
' DESCRIPTION = "Sample record"
' STATUS = "ACTIVE"
' CREATED_BY = "SYSTEM"
' CREATED_DT = (current timestamp)
' MODIFIED_BY = "SYSTEM"
' MODIFIED_DT = (current timestamp)
'
' Best practice: Pass user info from authenticated session and set createdBy/modifiedBy accordingly
