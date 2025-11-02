' ===================================================================================================
' LIBRARY OPTIONS EXAMPLE
' ===================================================================================================
' This example demonstrates how to use library options to control what is returned in responses.
'
' FEATURES DEMONSTRATED:
' 1. includeExecutedSQL option - Control whether the executed SQL query is returned in the response
' 2. Default behavior (backward compatibility) - ExecutedSQL is included by default
' 3. Disabled option - ExecutedSQL is not included when set to False
'
' USE CASES:
' - Production environments where you don't want to expose SQL queries to clients
' - Reducing response payload size when SQL is not needed
' - Security considerations - hiding database schema information
' - Debug/development mode where SQL visibility is helpful (default behavior)
' ===================================================================================================

[ActionLinks]
ACTIONID=0651ac2a-5bef-456b-b94a-a824711030ba
DATABASE=QW_ICIM
ICALENDAR=0
JSON=1
NOME=LibraryOptions_Example
SCRIPT=dim CheckToken = false
Dim StringPayload = "" : Dim ParsedPayload
dim PayloadAndTokenValidationError = DB.Global.ValidatePayloadAndToken(DB,CheckToken,"",ParsedPayload,StringPayload)
if PayloadAndTokenValidationError IsNot Nothing Then
    DB.Global.LogCustom(DB,StringPayload,PayloadAndTokenValidationError,"Error at ValidatePayloadAndToken: ")
    return PayloadAndTokenValidationError
end if
Dim DestinationIdentifierInfo = DB.Global.GetDestinationIdentifier(ParsedPayload)

If DestinationIdentifierInfo.Item1 Then
    Dim destinationId As String = DestinationIdentifierInfo.Item2

    ' ===================================================================================================
    ' EXAMPLE 1: DEFAULT BEHAVIOR (BACKWARD COMPATIBLE)
    ' ===================================================================================================
    ' By default, includeExecutedSQL is True, so the SQL query is included in the response
    ' This maintains backward compatibility with existing code
    ' ===================================================================================================

    If destinationId = "users-search-with-sql" Then
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

        ' DEFAULT: includeExecutedSQL is not specified, defaults to True
        Dim businessLogic = DB.Global.CreateBusinessLogicForReading(
            "SELECT UserId, Email, Name, CreatedDate FROM Users {WHERE} ORDER BY CreatedDate DESC",
            searchConditions,
            Nothing,      ' defaultWhereClause
            Nothing,      ' fieldMappings
            True          ' useForJsonPath (performance optimization)
            ' includeExecutedSQL defaults to True (not specified)
        )

        Return DB.Global.ProcessActionLink(DB,
            Nothing,        ' No validator required
            businessLogic,
            "User search with SQL included in response",
            ParsedPayload, StringPayload, False)

        ' EXPECTED RESPONSE:
        ' {
        '     "Result": "OK",
        '     "ProvidedParameters": "UserId",
        '     "ExecutedSQL": "SELECT UserId, Email, Name, CreatedDate FROM Users WHERE UserId = :UserId ORDER BY CreatedDate DESC FOR JSON PATH",
        '     "Records": [...]
        ' }


    ' ===================================================================================================
    ' EXAMPLE 2: EXCLUDE SQL FROM RESPONSE (PRODUCTION MODE)
    ' ===================================================================================================
    ' Set includeExecutedSQL to False to exclude the SQL query from the response
    ' This is useful for:
    ' - Production environments (security)
    ' - Reducing response size
    ' - Hiding database schema information
    ' ===================================================================================================

    ElseIf destinationId = "users-search-no-sql" Then
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

        ' EXCLUDE SQL: Set includeExecutedSQL to False
        Dim businessLogic = DB.Global.CreateBusinessLogicForReading(
            "SELECT UserId, Email, Name, CreatedDate FROM Users {WHERE} ORDER BY CreatedDate DESC",
            searchConditions,
            Nothing,      ' defaultWhereClause
            Nothing,      ' fieldMappings
            True,         ' useForJsonPath (performance optimization)
            False         ' includeExecutedSQL = False (SQL will NOT be in response)
        )

        Return DB.Global.ProcessActionLink(DB,
            Nothing,        ' No validator required
            businessLogic,
            "User search without SQL in response",
            ParsedPayload, StringPayload, False)

        ' EXPECTED RESPONSE (no ExecutedSQL field):
        ' {
        '     "Result": "OK",
        '     "ProvidedParameters": "UserId",
        '     "Records": [...]
        ' }


    ' ===================================================================================================
    ' EXAMPLE 3: COMPLEX QUERY WITH SQL INCLUDED (DEBUG MODE)
    ' ===================================================================================================
    ' In development/debug mode, including SQL helps troubleshoot issues
    ' ===================================================================================================

    ElseIf destinationId = "complex-search-debug" Then
        Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)

        searchConditions.Add("startDate", DB.Global.CreateParameterCondition(
            "startDate",
            "CreatedDate >= :startDate",
            Nothing
        ))

        searchConditions.Add("endDate", DB.Global.CreateParameterCondition(
            "endDate",
            "CreatedDate <= :endDate",
            Nothing
        ))

        searchConditions.Add("status", DB.Global.CreateParameterCondition(
            "status",
            "Status = :status",
            Nothing
        ))

        searchConditions.Add("minAmount", DB.Global.CreateParameterCondition(
            "minAmount",
            "Amount >= :minAmount",
            Nothing
        ))

        ' DEBUG MODE: includeExecutedSQL explicitly set to True
        Dim businessLogic = DB.Global.CreateBusinessLogicForReading(
            "SELECT OrderId, UserId, Amount, Status, CreatedDate FROM Orders {WHERE} ORDER BY CreatedDate DESC",
            searchConditions,
            "Status = 'ACTIVE'",  ' Default where clause
            Nothing,              ' fieldMappings
            True,                 ' useForJsonPath
            True                  ' includeExecutedSQL = True (explicitly for debugging)
        )

        Return DB.Global.ProcessActionLink(DB,
            Nothing,
            businessLogic,
            "Complex search with SQL for debugging",
            ParsedPayload, StringPayload, False)

        ' EXPECTED RESPONSE (with detailed SQL for debugging):
        ' {
        '     "Result": "OK",
        '     "ProvidedParameters": "startDate,endDate,status",
        '     "ExecutedSQL": "SELECT OrderId, UserId, Amount, Status, CreatedDate FROM Orders WHERE CreatedDate >= :startDate AND CreatedDate <= :endDate AND Status = :status ORDER BY CreatedDate DESC FOR JSON PATH",
        '     "Records": [...]
        ' }


    ' ===================================================================================================
    ' EXAMPLE 4: STANDARD MODE (NO FOR JSON PATH) WITH SQL OPTION
    ' ===================================================================================================
    ' The includeExecutedSQL option works with both performance modes
    ' ===================================================================================================

    ElseIf destinationId = "standard-mode-no-sql" Then
        Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)

        searchConditions.Add("CategoryId", DB.Global.CreateParameterCondition(
            "CategoryId",
            "CategoryId = :CategoryId",
            Nothing
        ))

        ' Standard mode (useForJsonPath = False) with includeExecutedSQL = False
        Dim businessLogic = DB.Global.CreateBusinessLogicForReading(
            "SELECT ProductId, ProductName, CategoryId, Price FROM Products {WHERE} ORDER BY ProductName",
            searchConditions,
            Nothing,      ' defaultWhereClause
            Nothing,      ' fieldMappings
            False,        ' useForJsonPath = False (standard mode)
            False         ' includeExecutedSQL = False
        )

        Return DB.Global.ProcessActionLink(DB,
            Nothing,
            businessLogic,
            "Standard mode without SQL in response",
            ParsedPayload, StringPayload, False)

        ' EXPECTED RESPONSE (standard mode, no SQL):
        ' {
        '     "Result": "OK",
        '     "ProvidedParameters": "CategoryId",
        '     "Records": [...]
        ' }

    Else
        Return DB.Global.CreateErrorResponse("'" & destinationId & "' is not a valid DestinationIdentifier. Valid options: users-search-with-sql, users-search-no-sql, complex-search-debug, standard-mode-no-sql")
    End If
Else
    Dim errorMessage As String = DestinationIdentifierInfo.Item2
    Return DB.Global.CreateErrorResponse(errorMessage)
End If


' ===================================================================================================
' DEMO PAYLOADS
' ===================================================================================================

' PAYLOAD 1: Search with SQL included (default behavior)
' {
'     "DestinationIdentifier": "users-search-with-sql",
'     "UserId": "12345"
' }

' PAYLOAD 2: Search without SQL in response (production mode)
' {
'     "DestinationIdentifier": "users-search-no-sql",
'     "UserId": "12345",
'     "Email": "%@example.com"
' }

' PAYLOAD 3: Complex search with SQL for debugging
' {
'     "DestinationIdentifier": "complex-search-debug",
'     "startDate": "2025-01-01",
'     "endDate": "2025-12-31",
'     "status": "ACTIVE",
'     "minAmount": 100.00
' }

' PAYLOAD 4: Standard mode without SQL
' {
'     "DestinationIdentifier": "standard-mode-no-sql",
'     "CategoryId": "CAT001"
' }


' ===================================================================================================
' BEST PRACTICES
' ===================================================================================================
'
' 1. DEVELOPMENT/DEBUG:
'    - Use includeExecutedSQL = True (or don't specify, it's the default)
'    - Helps troubleshoot query issues
'    - Verify correct WHERE clause construction
'
' 2. PRODUCTION:
'    - Consider setting includeExecutedSQL = False
'    - Reduces response payload size
'    - Hides database schema from clients
'    - Security best practice (information disclosure prevention)
'
' 3. API DOCUMENTATION:
'    - Document whether SQL is included in responses
'    - If disabled, provide alternative debugging methods
'    - Consider environment-based configuration
'
' 4. BACKWARD COMPATIBILITY:
'    - Default is True, so existing code continues to work
'    - No breaking changes for existing implementations
'    - Opt-in to hide SQL by explicitly setting False
'
' ===================================================================================================
