# API Reference

Complete reference for all functions in the Endpoint Library.

## Table of Contents

- [Core Processing](#core-processing)
- [Validation Functions](#validation-functions)
- [Factory Functions](#factory-functions)
- [Advanced Factory Functions](#advanced-factory-functions)
- [Utility Functions](#utility-functions)
- [Parameter Getters](#parameter-getters)
- [Helper Functions](#helper-functions)
- [Classes](#classes)

---

## Core Processing

### ProcessActionLink

Main function to process API requests with validation and business logic.

```vb
Function ProcessActionLink(
    ByVal database As Object,
    ByVal p_validator As Func(Of JObject, String),
    ByVal p_businessLogic As Func(Of Object, JObject, Object),
    Optional ByVal LogMessage As String = Nothing,
    Optional ByVal payload As JObject = Nothing,
    Optional ByVal StringPayload As String = "",
    Optional ByVal CheckForToken As Boolean = True
) As String
```

**Parameters:**
- `database`: Database connection object (typically `DB`)
- `p_validator`: Validation function (or `Nothing` for no validation)
- `p_businessLogic`: Business logic function to execute
- `LogMessage`: Optional message to log (if provided, logs request/response)
- `payload`: Parsed JSON payload (JObject)
- `StringPayload`: Raw JSON string
- `CheckForToken`: Whether to validate token (default: True)

**Returns:** JSON string with result

**Example:**
```vb
Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"UserId"}),
    DB.Global.CreateBusinessLogicForReading("Users", New String() {"UserId"}, Nothing, False),
    "User query",
    ParsedPayload,
    StringPayload,
    True
)
```

---

### ValidatePayloadAndToken

Validates JSON payload and optional token validation.

```vb
Function ValidatePayloadAndToken(
    DB As Object,
    Optional CheckForToken As Boolean = True,
    Optional loggerContext As String = "",
    Optional ByRef ParsedPayload As JObject = Nothing,
    Optional ByRef StringPayload As String = ""
) As Object
```

**Parameters:**
- `DB`: Database connection object
- `CheckForToken`: Enable token validation (default: True)
- `loggerContext`: Context string for error messages
- `ParsedPayload`: Output parameter - parsed JSON object
- `StringPayload`: Output parameter - raw JSON string

**Returns:** Error object if validation fails, `Nothing` if successful

**Example:**
```vb
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, True, "MyEndpoint", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If
```

---

## Validation Functions

### CreateValidator

Creates a validator for required parameters.

```vb
Function CreateValidator(
    requiredParams As String()
) As Func(Of JObject, String)
```

**Parameters:**
- `requiredParams`: Array of required parameter names (case-insensitive)

**Returns:** Validation function

**Example:**
```vb
Dim validator = DB.Global.CreateValidator(New String() {"UserId", "Email"})
```

---

### CreateValidatorForBatch

Creates a validator for batch operations requiring array parameters.

```vb
Function CreateValidatorForBatch(
    requiredArrayParams As String()
) As Func(Of JObject, String)
```

**Parameters:**
- `requiredArrayParams`: Array of required array parameter names

**Returns:** Validation function

**Example:**
```vb
Dim batchValidator = DB.Global.CreateValidatorForBatch(New String() {"Records"})
```

---

## Factory Functions

### CreateBusinessLogicForReading

Creates standard read logic for a table.

```vb
Function CreateBusinessLogicForReading(
    tableName As String,
    AllParametersList As String(),
    excludeFields As String(),
    Optional useLikeOperator As Boolean = True
) As Func(Of Object, JObject, Object)
```

**Parameters:**
- `tableName`: Name of the table to query
- `AllParametersList`: Array of field names that can be used for filtering
- `excludeFields`: Array of field names to exclude from response
- `useLikeOperator`: Use LIKE (True) or equals (False) for comparisons

**Returns:** Business logic function

**Example:**
```vb
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "Users",
    New String() {"UserId", "Email", "Name"},
    New String() {"Password", "PasswordHash"},
    True
)
```

**Generated SQL:**
```sql
-- With useLikeOperator = True
SELECT * FROM Users WHERE UserId LIKE :UserId AND Email LIKE :Email

-- With useLikeOperator = False
SELECT * FROM Users WHERE UserId = :UserId AND Email = :Email
```

---

### CreateBusinessLogicForWriting

Creates standard write logic (insert/update) for a table.

```vb
Function CreateBusinessLogicForWriting(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```

**Parameters:**
- `tableName`: Name of the table
- `AllParametersList`: Array of all field names that can be written
- `RequiredParametersList`: Array of required fields (used as primary key)
- `allowUpdates`: Allow updates to existing records (True) or insert only (False)

**Returns:** Business logic function

**Example:**
```vb
Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    New String() {"UserId", "Email", "Name", "Department"},
    New String() {"UserId"},
    True
)
```

**Behavior:**
- Checks if record exists based on `RequiredParametersList`
- If exists and `allowUpdates = True`: Updates non-key fields
- If exists and `allowUpdates = False`: Returns error
- If not exists: Inserts new record

---

### CreateBusinessLogicForWritingBatch

Creates batch write logic for multiple records.

```vb
Function CreateBusinessLogicForWritingBatch(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```

**Parameters:**
- Same as `CreateBusinessLogicForWriting`

**Returns:** Business logic function

**Example:**
```vb
Dim batchLogic = DB.Global.CreateBusinessLogicForWritingBatch(
    "Products",
    New String() {"ProductId", "Name", "Price"},
    New String() {"ProductId"},
    True
)
```

**Payload Format:**
```json
{
  "Records": [
    {"ProductId": "P001", "Name": "Product 1", "Price": 19.99},
    {"ProductId": "P002", "Name": "Product 2", "Price": 29.99}
  ]
}
```

**Response Format:**
```json
{
  "Result": "OK",
  "Inserted": 1,
  "Updated": 1,
  "Errors": 0,
  "ErrorDetails": [],
  "Message": "Processed 2 records: 1 inserted, 1 updated, 0 errors."
}
```

---

## Advanced Factory Functions

### CreateBusinessLogicForReading

Creates advanced read logic with custom SQL and parameter conditions.

```vb
Function CreateBusinessLogicForReading(
    baseSQL As String,
    parameterConditions As Dictionary(Of String, Object),
    Optional excludeFields As String() = Nothing,
    Optional defaultWhereClause As String = Nothing,
    Optional fieldMappings As Dictionary(Of String, FieldMapping) = Nothing
) As Func(Of Object, JObject, Object)
```

**Parameters:**
- `baseSQL`: Base SQL query. Use `{WHERE}` placeholder for dynamic WHERE clause
- `parameterConditions`: Dictionary of parameter conditions
- `excludeFields`: Fields to exclude from results
- `defaultWhereClause`: WHERE clause applied when no parameters provided
- `fieldMappings`: Optional field mappings (JSON to SQL)

**Returns:** Business logic function

**Example:**
```vb
Dim conditions As New Dictionary(Of String, Object)
conditions.Add("startDate", DB.Global.CreateParameterCondition(
    "startDate",
    "OrderDate >= :startDate",
    Nothing
))
conditions.Add("endDate", DB.Global.CreateParameterCondition(
    "endDate",
    "OrderDate <= :endDate",
    Nothing
))

Dim logic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Orders {WHERE} ORDER BY OrderDate DESC",
    conditions,
    Nothing,
    "OrderDate >= DATEADD(day, -30, GETDATE())",
    Nothing
)
```

**SQL Generation:**
- `{WHERE}` replaced with `WHERE` + generated conditions
- Conditions combined with `AND`
- Default clause used when no parameters provided

---

### CreateAdvancedBusinessLogicForWriting

Creates advanced write logic with field mappings and custom SQL.

```vb
Function CreateAdvancedBusinessLogicForWriting(
    tableName As String,
    fieldMappings As Dictionary(Of String, FieldMapping),
    keyFields As String(),
    allowUpdates As Boolean,
    Optional customExistenceCheckSQL As String = Nothing,
    Optional customUpdateSQL As String = Nothing,
    Optional customWhereClause As String = Nothing
) As Func(Of Object, JObject, Object)
```

**Parameters:**
- `tableName`: Table name
- `fieldMappings`: Dictionary mapping JSON properties to SQL columns
- `keyFields`: Array of key field names (SQL column names)
- `allowUpdates`: Allow updates to existing records
- `customExistenceCheckSQL`: Custom SQL to check if record exists
- `customUpdateSQL`: Custom UPDATE SQL statement
- `customWhereClause`: Custom WHERE clause for updates

**Returns:** Business logic function

**Example:**
```vb
Dim mappings As New Dictionary(Of String, FieldMapping)
mappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True, Nothing))
mappings.Add("email", DB.Global.CreateFieldMapping("email", "EMAIL_ADDRESS", True, Nothing))
mappings.Add("status", DB.Global.CreateFieldMapping("status", "STATUS", False, "ACTIVE"))

Dim logic = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "USERS",
    mappings,
    New String() {"USER_ID"},
    True,
    Nothing, Nothing, Nothing
)
```

---

## Utility Functions

### CreateParameterCondition

Creates a parameter condition object.

```vb
Function CreateParameterCondition(
    paramName As String,
    sqlWhenPresent As String,
    Optional sqlWhenAbsent As String = Nothing,
    Optional useParameter As Boolean = True,
    Optional defaultValue As Object = Nothing
) As ParameterCondition
```

**Parameters:**
- `paramName`: Parameter name (case-insensitive)
- `sqlWhenPresent`: SQL clause when parameter is provided
- `sqlWhenAbsent`: SQL clause when parameter is absent (or `Nothing` to skip)
- `useParameter`: Bind parameter value (True) or use literal (False)
- `defaultValue`: Default value if not provided

**Returns:** ParameterCondition object

**Example:**
```vb
Dim condition = DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    "Status IS NOT NULL",
    True,
    Nothing
)
```

---

### CreateFieldMapping

Creates a field mapping object.

```vb
Function CreateFieldMapping(
    jsonProp As String,
    sqlCol As String,
    Optional isRequired As Boolean = False,
    Optional defaultVal As Object = Nothing
) As FieldMapping
```

**Parameters:**
- `jsonProp`: JSON property name
- `sqlCol`: SQL column name
- `isRequired`: Whether field is required
- `defaultVal`: Default value if not provided

**Returns:** FieldMapping object

**Example:**
```vb
Dim mapping = DB.Global.CreateFieldMapping("userId", "USER_ID", True, Nothing)
```

---

### CreateParameterConditionsDictionary

Creates parameter conditions dictionary from arrays.

```vb
Function CreateParameterConditionsDictionary(
    paramNames As String(),
    sqlWhenPresentArray As String(),
    Optional sqlWhenAbsentArray As String() = Nothing,
    Optional useParameterArray As Boolean() = Nothing,
    Optional defaultValueArray As Object() = Nothing
) As Dictionary(Of String, Object)
```

**Parameters:**
- Parallel arrays for creating multiple conditions

**Returns:** Dictionary of parameter conditions

**Example:**
```vb
Dim dict = DB.Global.CreateParameterConditionsDictionary(
    New String() {"status", "priority"},
    New String() {"Status = :status", "Priority = :priority"},
    Nothing,
    Nothing,
    Nothing
)
```

---

### CreateFieldMappingsDictionary

Creates field mappings dictionary from arrays.

```vb
Function CreateFieldMappingsDictionary(
    jsonProps As String(),
    sqlCols As String(),
    Optional isRequiredArray As Boolean() = Nothing,
    Optional defaultValArray As Object() = Nothing
) As Dictionary(Of String, FieldMapping)
```

**Parameters:**
- Parallel arrays for creating multiple mappings

**Returns:** Dictionary of field mappings

**Example:**
```vb
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email"},
    New String() {"USER_ID", "EMAIL_ADDRESS"},
    New Boolean() {True, True},
    Nothing
)
```

---

## Parameter Getters

### GetStringParameter

Extracts string parameter from payload.

```vb
Function GetStringParameter(
    payload As JObject,
    paramName As String
) As Tuple(Of Boolean, String)
```

**Returns:** Tuple (found: Boolean, value: String)

**Example:**
```vb
Dim result = DB.Global.GetStringParameter(payload, "UserId")
If result.Item1 Then
    Dim userId As String = result.Item2
End If
```

---

### GetDateParameter

Extracts date parameter from payload.

```vb
Function GetDateParameter(
    payload As JObject,
    paramName As String
) As Tuple(Of Boolean, Date)
```

**Returns:** Tuple (found: Boolean, value: Date)

---

### GetIntegerParameter

Extracts integer parameter from payload.

```vb
Function GetIntegerParameter(
    payload As JObject,
    paramName As String
) As Tuple(Of Boolean, Integer)
```

**Returns:** Tuple (found: Boolean, value: Integer)

---

### GetObjectParameter

Extracts any parameter from payload (string, number, boolean, object, array).

```vb
Function GetObjectParameter(
    payload As JObject,
    paramName As String
) As Tuple(Of Boolean, Object)
```

**Returns:** Tuple (found: Boolean, value: Object)

---

### GetArrayParameter

Extracts array parameter from payload.

```vb
Function GetArrayParameter(
    payload As JObject,
    paramName As String
) As Tuple(Of Boolean, JArray)
```

**Returns:** Tuple (found: Boolean, value: JArray)

---

## Helper Functions

### GetDestinationIdentifier

Extracts DestinationIdentifier from payload for routing.

```vb
Function GetDestinationIdentifier(
    ByRef payload As JObject
) As Tuple(Of Boolean, String)
```

**Returns:** Tuple (found: Boolean, value: String)

**Example:**
```vb
Dim result = DB.Global.GetDestinationIdentifier(payload)
If result.Item1 Then
    Dim destinationId As String = result.Item2
    If destinationId = "user-read" Then
        ' Handle user read endpoint
    End If
End If
```

---

### CreateErrorResponse

Creates standardized error response.

```vb
Function CreateErrorResponse(
    reason As String
) As String
```

**Returns:** JSON error string

**Example:**
```vb
Return DB.Global.CreateErrorResponse("Invalid parameter: UserId is required")
```

**Output:**
```json
{
  "Result": "KO",
  "Reason": "Invalid parameter: UserId is required"
}
```

---

### LogCustom

Logs custom message with payload and result.

```vb
Sub LogCustom(
    database As Object,
    StringPayload As String,
    StringResult As String,
    Optional LogMessage As String = ""
)
```

**Example:**
```vb
DB.Global.LogCustom(DB, StringPayload, result, "User query executed")
```

---

### ParsePayload

Parses JSON from HTTP request.

```vb
Function ParsePayload(
    Optional ByRef PayloadString As String = Nothing,
    Optional ByRef ErrorMessage As String = Nothing
) As JObject
```

**Returns:** Parsed JObject or Nothing on error

**Example:**
```vb
Dim errorMsg As String = ""
Dim payloadStr As String = ""
Dim payload = DB.Global.ParsePayload(payloadStr, errorMsg)
If payload Is Nothing Then
    Return DB.Global.CreateErrorResponse(errorMsg)
End If
```

---

### ExecuteQueryToDictionary

Executes SQL query and returns results as list of dictionaries.

```vb
Function ExecuteQueryToDictionary(
    database As Object,
    sql As String,
    parameters As Dictionary(Of String, Object),
    excludeFields As String()
) As List(Of Dictionary(Of String, Object))
```

**Returns:** List of records (each record is a dictionary)

---

## Classes

### ParameterCondition

Defines SQL behavior based on parameter presence.

**Properties:**
- `ParameterName As String`
- `SQLWhenPresent As String`
- `SQLWhenAbsent As String`
- `UseParameter As Boolean`
- `DefaultValue As Object`

**Constructor:**
```vb
Public Sub New(
    paramName As String,
    sqlWhenPresent As String,
    Optional sqlWhenAbsent As String = Nothing,
    Optional useParameter As Boolean = True,
    Optional defaultValue As Object = Nothing
)
```

---

### FieldMapping

Maps JSON property to SQL column.

**Properties:**
- `JsonProperty As String`
- `SqlColumn As String`
- `IsRequired As Boolean`
- `DefaultValue As Object`

**Constructor:**
```vb
Public Sub New(
    jsonProp As String,
    sqlCol As String,
    Optional isRequired As Boolean = False,
    Optional defaultVal As Object = Nothing
)
```

---

## Response Formats

### Success Response (Read)

```json
{
  "Result": "OK",
  "ProvidedParameters": "userId,email",
  "ExecutedSQL": "SELECT * FROM Users WHERE UserId = :UserId",
  "Records": [
    {"UserId": "123", "Email": "user@example.com", "Name": "John Doe"}
  ]
}
```

### Success Response (Write)

```json
{
  "Result": "OK",
  "Action": "INSERTED",
  "Message": "Record inserted successfully"
}
```

### Error Response

```json
{
  "Result": "KO",
  "Reason": "Missing required parameter: UserId"
}
```

### Batch Response

```json
{
  "Result": "PARTIAL",
  "Inserted": 5,
  "Updated": 3,
  "Errors": 2,
  "ErrorDetails": [
    "Record 1: Missing required field",
    "Record 4: Duplicate key"
  ],
  "Message": "Processed 10 records: 5 inserted, 3 updated, 2 errors."
}
```

---

## Quick Reference

### Common Patterns

#### Simple Read
```vb
DB.Global.CreateBusinessLogicForReading(tableName, fields, excludeFields, useLike)
```

#### Simple Write
```vb
DB.Global.CreateBusinessLogicForWriting(tableName, allFields, keyFields, allowUpdates)
```

#### Advanced Read with Conditions
```vb
DB.Global.CreateBusinessLogicForReading(baseSQL, conditions, excludeFields, defaultWhere, mappings)
```

#### Batch Operations
```vb
DB.Global.CreateBusinessLogicForWritingBatch(tableName, allFields, keyFields, allowUpdates)
```

---

For more examples, see the `examples/` directory.
