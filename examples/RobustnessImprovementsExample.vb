'================================================
' ROBUSTNESS IMPROVEMENTS EXAMPLE
' Demonstrates new security and robustness features
'================================================
' This example demonstrates:
' - SQL Identifier validation (prevents injection)
' - Batch size limits (prevents DoS)
' - Query prepending (for SET statements)
' - Array length validation
' - Overflow protection
' - Resource cleanup improvements
'================================================

'================================================
' EXAMPLE 1: SQL PREPEND FOR DATE FORMAT
' Use prependSQL to set date format before query execution
'================================================
Public Function Example1_DateFormatPrepend() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, False, "DateExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    ' Define search parameters
    Dim searchConditions = DB.Global.CreateParameterConditionsDictionary(
        New String() {"startDate", "endDate"},
        New String() {"OrderDate >= :startDate", "OrderDate <= :endDate"}
    )

    ' FEATURE: Prepend SQL to set date format
    ' This ensures consistent date parsing regardless of server settings
    Dim readLogic = DB.Global.CreateBusinessLogicForReading(
        "SELECT OrderId, OrderDate, CustomerName, TotalAmount FROM Orders {WHERE}",
        searchConditions,
        Nothing,                    ' defaultWhereClause
        Nothing,                    ' fieldMappings
        True,                       ' useForJsonPath
        False,                      ' includeExecutedSQL (hide in production)
        "SET DATEFORMAT ymd;"       ' prependSQL - NEW FEATURE!
    )

    Return DB.Global.ProcessActionLink(
        DB,
        Nothing,
        readLogic,
        "Orders filtered by date",
        ParsedPayload,
        StringPayload,
        False
    )
End Function

'================================================
' EXAMPLE 2: BATCH SIZE LIMITS
' Library now enforces maximum batch size (1000 records by default)
'================================================
Public Function Example2_BatchWithSizeLimit() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, True, "BatchExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    ' Define field mappings
    Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
        New String() {"productId", "productName", "price", "stock"},
        New String() {"ProductId", "ProductName", "Price", "Stock"},
        New Boolean() {True, True, True, False},           ' isRequired
        New Boolean() {True, False, False, False},          ' isPrimaryKey
        New Object() {Nothing, Nothing, Nothing, 0}         ' defaultValues
    )

    ' ROBUSTNESS: If payload contains > 1000 records, returns error automatically
    ' Error: "Batch size 1500 exceeds maximum allowed size of 1000. Please split into smaller batches."
    Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
        "Products",
        fieldMappings,
        Nothing,    ' keyFields extracted from IsPrimaryKey
        True        ' allowUpdates
    )

    Return DB.Global.ProcessActionLink(
        DB,
        DB.Global.CreateValidatorForBatch(New String() {"Records"}),
        batchLogic,
        "Batch product upsert with size limit",
        ParsedPayload,
        StringPayload,
        True
    )
End Function

'================================================
' EXAMPLE 3: VALIDATED FIELD MAPPINGS
' Library validates array lengths and detects duplicates
'================================================
Public Function Example3_ValidatedFieldMappings() As String
    Try
        ' ROBUSTNESS: This will throw exception if arrays have different lengths
        Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
            New String() {"userId", "email", "name"},
            New String() {"UserId", "Email", "Name"},
            New Boolean() {True, True, False},              ' isRequired (3 elements)
            New Boolean() {True, False, False},             ' isPrimaryKey (3 elements)
            New Object() {Nothing, Nothing, Nothing}        ' defaultValues (3 elements)
        )

        ' ❌ BAD EXAMPLE (would throw exception):
        ' Dim badMappings = DB.Global.CreateFieldMappingsDictionary(
        '     New String() {"userId", "email", "name"},
        '     New String() {"UserId", "Email"},             ' ❌ Only 2 elements - EXCEPTION!
        '     New Boolean() {True, True, False},
        '     New Boolean() {True, False, False},
        '     New Object() {Nothing, Nothing, Nothing}
        ' )
        ' Error: "Array length mismatch: jsonProps has 3 elements but sqlCols has 2 elements"

        ' ❌ DUPLICATE DETECTION (would throw exception):
        ' Dim duplicateMappings = DB.Global.CreateFieldMappingsDictionary(
        '     New String() {"userId", "userId", "name"},   ' ❌ Duplicate "userId" - EXCEPTION!
        '     New String() {"UserId", "UserId", "Name"},
        '     New Boolean() {True, True, False},
        '     New Boolean() {True, False, False},
        '     New Object() {Nothing, Nothing, Nothing}
        ' )
        ' Error: "Duplicate field mapping for 'userId' at index 1"

        Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
            "Users",
            fieldMappings
        )

        Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, True, "UserWrite", ParsedPayload, StringPayload)
        If validationResult IsNot Nothing Then
            Return validationResult
        End If

        Return DB.Global.ProcessActionLink(
            DB,
            DB.Global.CreateValidator(New String() {"userId", "email"}),
            writeLogic,
            "User write with validated mappings",
            ParsedPayload,
            StringPayload,
            True
        )
    Catch ex As ArgumentException
        ' Validation errors are caught at initialization
        Return DB.Global.CreateErrorResponse($"Configuration error: {ex.Message}")
    End Try
End Function

'================================================
' EXAMPLE 4: COMPOSITE KEY WITH SAFE DELIMITER
' Library now uses ASCII 31 (Unit Separator) to prevent key collisions
'================================================
Public Function Example4_CompositeKeysNoCollision() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, True, "CompositeKeyExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    ' Composite key scenario: OrderId + LineNumber
    Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
        New String() {"orderId", "lineNumber", "productId", "quantity"},
        New String() {"OrderId", "LineNumber", "ProductId", "Quantity"},
        New Boolean() {True, True, True, True},                     ' isRequired
        New Boolean() {True, True, False, False},                   ' isPrimaryKey (both orderId and lineNumber)
        New Object() {Nothing, Nothing, Nothing, Nothing}
    )

    ' ROBUSTNESS: Even if orderId contains pipe character "123|456",
    ' the library uses ASCII 31 delimiter to prevent collisions
    ' Old: "123|456|1" could be ["123", "456|1"] OR ["123|456", "1"] - AMBIGUOUS!
    ' New: "123|456␟1" is unambiguous (␟ = ASCII 31, never in data)

    Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
        "OrderLines",
        fieldMappings,
        Nothing,    ' keyFields extracted from IsPrimaryKey
        True        ' allowUpdates
    )

    Return DB.Global.ProcessActionLink(
        DB,
        DB.Global.CreateValidatorForBatch(New String() {"Records"}),
        batchLogic,
        "Order lines batch with safe composite keys",
        ParsedPayload,
        StringPayload,
        True
    )
End Function

'================================================
' EXAMPLE 5: INTEGER OVERFLOW PROTECTION
' Library validates integer ranges to prevent overflow exceptions
'================================================
Public Function Example5_OverflowProtection() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, False, "OverflowExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    ' Test payload scenarios:
    ' ✅ {"age": 25}                  -> Works (valid integer)
    ' ✅ {"age": 2147483647}          -> Works (max integer)
    ' ✅ {"age": -2147483648}         -> Works (min integer)
    ' ❌ {"age": 3000000000}          -> Returns False (overflow - silently handled)
    ' ❌ {"age": 3.5e10}              -> Returns False (overflow - silently handled)

    Dim ageResult = DB.Global.GetIntegerParameter(ParsedPayload, "age")
    If Not ageResult.Item1 Then
        Return DB.Global.CreateErrorResponse("Invalid age parameter (must be integer in valid range)")
    End If

    Dim age As Integer = ageResult.Item2

    ' Use age safely - no overflow exceptions possible
    Return Newtonsoft.Json.JsonConvert.SerializeObject(New With {
        .Result = "OK",
        .Age = age,
        .AgeGroup = If(age < 18, "Minor", If(age < 65, "Adult", "Senior"))
    })
End Function

'================================================
' EXAMPLE 6: CASE-INSENSITIVE {WHERE} PLACEHOLDER
' Library now handles {WHERE}, {where}, {Where}, etc.
'================================================
Public Function Example6_CaseInsensitivePlaceholder() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, False, "CaseExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    Dim searchConditions = DB.Global.CreateParameterConditionsDictionary(
        New String() {"status"},
        New String() {"Status = :status"}
    )

    ' ROBUSTNESS: All these work now (case-insensitive)
    ' "SELECT * FROM Orders {WHERE}"      ✅
    ' "SELECT * FROM Orders {where}"      ✅
    ' "SELECT * FROM Orders {Where}"      ✅
    ' "SELECT * FROM Orders {wHeRe}"      ✅

    Dim readLogic = DB.Global.CreateBusinessLogicForReading(
        "SELECT OrderId, Status FROM Orders {where}",  ' Lowercase works!
        searchConditions,
        Nothing,
        Nothing,
        False,
        True
    )

    Return DB.Global.ProcessActionLink(
        DB,
        Nothing,
        readLogic,
        "Case-insensitive WHERE placeholder",
        ParsedPayload,
        StringPayload,
        False
    )
End Function

'================================================
' EXAMPLE 7: SQL IDENTIFIER VALIDATION
' Library validates table and column names to prevent SQL injection
'================================================
Public Function Example7_SQLInjectionPrevention() As String
    Try
        ' ✅ VALID IDENTIFIERS:
        ' - "Users"
        ' - "User_Accounts"
        ' - "dbo.Users"
        ' - "[User Accounts]"

        ' ❌ INVALID IDENTIFIERS (throw exception):
        ' - "Users; DROP TABLE Users--"
        ' - "Users/*comment*/"
        ' - "123Users" (starts with number)
        ' - "" (empty)

        Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, True, "InjectionExample", ParsedPayload, StringPayload)
        If validationResult IsNot Nothing Then
            Return validationResult
        End If

        ' SECURITY: Table and column names are validated automatically
        ' This would throw exception if tableName contains SQL injection attempts
        Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
            New String() {"userId", "userName"},
            New String() {"UserId", "UserName"},    ' ✅ Valid column names
            New Boolean() {True, True},
            New Boolean() {True, False},
            New Object() {Nothing, Nothing}
        )

        ' This initialization validates all identifiers
        Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
            "Users",    ' ✅ Valid table name
            fieldMappings
        )

        ' ❌ BAD EXAMPLE (would throw exception):
        ' Dim badLogic = DB.Global.CreateBusinessLogicForWriting(
        '     "Users; DROP TABLE Users--",  ' ❌ SQL Injection attempt - EXCEPTION!
        '     fieldMappings
        ' )
        ' Error: "Invalid table name: 'Users; DROP TABLE Users--'. Identifiers must start with letter/underscore..."

        Return DB.Global.ProcessActionLink(
            DB,
            DB.Global.CreateValidator(New String() {"userId", "userName"}),
            writeLogic,
            "Write with SQL injection protection",
            ParsedPayload,
            StringPayload,
            True
        )
    Catch ex As ArgumentException
        ' SQL identifier validation errors
        Return DB.Global.CreateErrorResponse($"Security validation failed: {ex.Message}")
    End Try
End Function

'================================================
' EXAMPLE 8: KEY FIELD VALIDATION
' Library ensures all key fields are present before executing queries
'================================================
Public Function Example8_KeyFieldValidation() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, True, "KeyValidationExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
        New String() {"orderId", "customerId", "orderDate", "amount"},
        New String() {"OrderId", "CustomerId", "OrderDate", "Amount"},
        New Boolean() {True, True, True, True},             ' All required
        New Boolean() {True, False, False, False},          ' Only OrderId is primary key
        New Object() {Nothing, Nothing, Nothing, Nothing}
    )

    ' ROBUSTNESS: If payload is missing OrderId (primary key), returns error:
    ' "Key field 'OrderId' is missing from payload. All key fields are required for write operations."

    ' Test scenarios:
    ' ✅ {"orderId": 123, "customerId": 456, "orderDate": "2025-01-01", "amount": 100}  -> Works
    ' ❌ {"customerId": 456, "orderDate": "2025-01-01", "amount": 100}                  -> Error (missing orderId)

    Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
        "Orders",
        fieldMappings
    )

    Return DB.Global.ProcessActionLink(
        DB,
        DB.Global.CreateValidator(New String() {"orderId", "customerId", "orderDate", "amount"}),
        writeLogic,
        "Order write with key field validation",
        ParsedPayload,
        StringPayload,
        True
    )
End Function

'================================================
' EXAMPLE 9: DBNULL CONVERSION
' Library converts DBNull to Nothing for proper JSON serialization
'================================================
Public Function Example9_DBNullHandling() As String
    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, False, "DBNullExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    Dim searchConditions = DB.Global.CreateParameterConditionsDictionary(
        New String() {"userId"},
        New String() {"UserId = :userId"}
    )

    ' ROBUSTNESS: NULL values in database are now converted to null in JSON
    ' Old behavior: DBNull.Value in dictionary -> JSON serialization error or "DBNull"
    ' New behavior: DBNull.Value -> Nothing -> null in JSON ✅

    ' Result example with NULL MiddleName in database:
    ' {
    '   "Result": "OK",
    '   "Records": [
    '     {
    '       "UserId": 123,
    '       "FirstName": "John",
    '       "MiddleName": null,      <- Properly converted!
    '       "LastName": "Doe"
    '     }
    '   ]
    ' }

    Dim readLogic = DB.Global.CreateBusinessLogicForReading(
        "SELECT UserId, FirstName, MiddleName, LastName FROM Users {WHERE}",
        searchConditions,
        Nothing,
        Nothing,
        False,
        True
    )

    Return DB.Global.ProcessActionLink(
        DB,
        Nothing,
        readLogic,
        "User query with DBNull handling",
        ParsedPayload,
        StringPayload,
        False
    )
End Function

'================================================
' EXAMPLE 10: RESOURCE CLEANUP IMPROVEMENTS
' Library now uses try-finally for all database operations
'================================================
Public Function Example10_ResourceCleanup() As String
    ' ROBUSTNESS: All database operations now include proper cleanup
    '
    ' Old code (potential leak):
    '   Dim q As New QWTable()
    '   q.Active = True
    '   ' ... if exception occurs, q.Active = False and q.Dispose() are never called
    '   q.Active = False
    '
    ' New code (leak-proof):
    '   Dim q As New QWTable()
    '   Try
    '       q.Active = True
    '       ' ... process data
    '   Finally
    '       Try : q.Active = False : Catch : End Try
    '       Try : q.Dispose() : Catch : End Try
    '   End Try
    '
    ' This applies to:
    ' - ExecuteQueryToDictionary
    ' - ExecuteQueryToJSON
    ' - BulkExistenceCheck
    ' - All write operations

    Dim validationResult = DB.Global.ValidatePayloadAndToken(DB, False, "ResourceExample", ParsedPayload, StringPayload)
    If validationResult IsNot Nothing Then
        Return validationResult
    End If

    ' Even if this query throws exception, resources are cleaned up properly
    Dim searchConditions = DB.Global.CreateParameterConditionsDictionary(
        New String() {"invalidField"},
        New String() {"NonExistentColumn = :invalidField"}  ' This will cause error
    )

    Try
        Dim readLogic = DB.Global.CreateBusinessLogicForReading(
            "SELECT * FROM Users {WHERE}",
            searchConditions,
            Nothing,
            Nothing,
            False,
            True
        )

        Return DB.Global.ProcessActionLink(
            DB,
            Nothing,
            readLogic,
            "Query with resource cleanup",
            ParsedPayload,
            StringPayload,
            False
        )
    Catch ex As Exception
        ' Even with exception, all database connections are properly closed
        Return DB.Global.CreateErrorResponse($"Query failed but resources were cleaned up: {ex.Message}")
    End Try
End Function

'================================================
' SUMMARY OF ALL IMPROVEMENTS
'================================================
' ✅ SQL Injection Prevention: Table/column names validated
' ✅ Batch Size Limits: Maximum 1000 records (configurable)
' ✅ Query Prepending: Add SET statements before queries
' ✅ Array Length Validation: Parallel arrays must match
' ✅ Duplicate Detection: Field mappings checked for duplicates
' ✅ Integer Overflow Protection: Safe integer parsing
' ✅ Stream Seekability Check: Handles non-seekable streams
' ✅ DBNull Conversion: Proper JSON null handling
' ✅ Case-Insensitive Placeholders: {WHERE}, {where}, etc.
' ✅ Key Field Validation: Ensures all keys present
' ✅ Composite Key Safety: Uses ASCII 31 delimiter
' ✅ Resource Cleanup: Try-finally for all operations
' ✅ Parameter Name Collisions Fixed: Better bulk check naming
' ✅ Cache Race Conditions Fixed: Thread-safe cache management
' ✅ WHERE Placeholder Validation: Only one allowed
' ✅ LogMessage Fixed: Correct message in logs
