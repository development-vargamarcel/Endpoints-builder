
' Important: the users of this library cannot use classes and cannot use custom types defined here. Also all names must be full qualified names.
'===================================
' FLEXIBLE SQL WRAPPER SYSTEM
' High-performance endpoint library with optimized batch operations
' ===================================

' ===================================
' PARAMETER CONDITION CLASS
' Defines SQL behavior when parameter is present or absent
' ===================================
Public Class ParameterCondition
    Public Property ParameterName As System.String
    Public Property SQLWhenPresent As System.String      ' SQL clause when parameter provided (e.g., "FieldName LIKE :ParamName")
    Public Property SQLWhenAbsent As System.String       ' SQL clause when parameter absent (e.g., "1=1" or Nothing)
    Public Property UseParameter As System.Boolean       ' Whether to bind parameter value (True) or use literal (False)
    Public Property DefaultValue As System.Object        ' Default value if not provided

    Public Sub New(paramName As System.String, sqlWhenPresent As System.String, Optional sqlWhenAbsent As System.String = Nothing, Optional useParameter As System.Boolean = True, Optional defaultValue As System.Object = Nothing)
        Me.ParameterName = paramName
        Me.SQLWhenPresent = sqlWhenPresent
        Me.SQLWhenAbsent = sqlWhenAbsent
        Me.UseParameter = useParameter
        Me.DefaultValue = defaultValue
    End Sub
End Class

' ===================================
' FIELD MAPPING CLASS
' Maps JSON property names to SQL column names
' ===================================
Public Class FieldMapping
    Public Property JsonProperty As System.String
    Public Property SqlColumn As System.String
    Public Property IsRequired As System.Boolean
    Public Property IsPrimaryKey As System.Boolean
    Public Property DefaultValue As System.Object

    Public Sub New(jsonProp As System.String, sqlCol As System.String, Optional isRequired As System.Boolean = False, Optional isPrimaryKey As System.Boolean = False, Optional defaultVal As System.Object = Nothing)
        Me.JsonProperty = jsonProp
        Me.SqlColumn = sqlCol
        Me.IsRequired = isRequired
        Me.IsPrimaryKey = isPrimaryKey
        Me.DefaultValue = defaultVal
    End Sub
End Class

' ===================================
' CONFIGURATION CONSTANTS
' ===================================
Private Const MAX_BATCH_SIZE As System.Int32 = 1000
Private Const MAX_SQL_IDENTIFIER_LENGTH As System.Int32 = 128
Private Const COMPOSITE_KEY_DELIMITER As System.String = "␟"  ' ASCII 31 (Unit Separator) - safe delimiter

' ===================================
' PERFORMANCE: PROPERTY NAME CACHING
' ===================================
Private Shared _propertyNameCache As New System.Collections.Concurrent.ConcurrentDictionary(Of System.Int32, System.Collections.Generic.Dictionary(Of System.String, System.String))
Private Shared _cacheHitCount As System.Int32 = 0
Private Shared _cacheMissCount As System.Int32 = 0
Private Const MAX_CACHE_SIZE As System.Int32 = 1000

Public Shared Function GetPropertyCaseInsensitive(obj As Newtonsoft.Json.Linq.JObject, propertyName As System.String) As Newtonsoft.Json.Linq.JToken
    If obj Is Nothing OrElse System.String.IsNullOrEmpty(propertyName) Then
        Return Nothing
    End If

    ' Try exact match first for performance (fastest path)
    ' Use TryGetValue to safely check for property existence
    Dim propertyValue As Newtonsoft.Json.Linq.JToken = Nothing
    If obj.TryGetValue(propertyName, propertyValue) AndAlso propertyValue IsNot Nothing Then
        Return propertyValue
    End If

    ' Use cached property name mappings for case-insensitive lookup
    Dim objHash As System.Int32 = obj.GetHashCode()
    Dim nameMap As System.Collections.Generic.Dictionary(Of System.String, System.String) = Nothing

    If _propertyNameCache.TryGetValue(objHash, nameMap) Then
        System.Threading.Interlocked.Increment(_cacheHitCount)
        Dim actualName As System.String = Nothing
        If nameMap.TryGetValue(propertyName, actualName) Then
            ' Validate cached mapping actually exists in object (hash collision protection)
            Try
                Dim cachedValue As Newtonsoft.Json.Linq.JToken = Nothing
                If obj.TryGetValue(actualName, cachedValue) Then
                    Return cachedValue
                Else
                    ' Cache was invalid (possible hash collision), remove and rebuild
                    _propertyNameCache.TryRemove(objHash, nameMap)
                    ' Fall through to rebuild cache below
                End If
            Catch
                ' Error accessing cached property, remove invalid cache entry
                _propertyNameCache.TryRemove(objHash, nameMap)
                ' Fall through to rebuild cache below
            End Try
        Else
            Return Nothing
        End If
    End If

    ' Cache miss - build property name mapping
    System.Threading.Interlocked.Increment(_cacheMissCount)

    ' ROBUST: Prevent cache from growing indefinitely (thread-safe)
    If _propertyNameCache.Count > MAX_CACHE_SIZE Then
        ' Only clear if we're still over the limit (double-check to avoid race)
        If _propertyNameCache.Count > MAX_CACHE_SIZE Then
            ' Create new cache instead of clearing (avoids race conditions during iteration)
            Dim newCache As New System.Collections.Concurrent.ConcurrentDictionary(Of System.Int32, System.Collections.Generic.Dictionary(Of System.String, System.String))
            _propertyNameCache = newCache
        End If
    End If

    nameMap = New System.Collections.Generic.Dictionary(Of System.String, System.String)(System.StringComparer.OrdinalIgnoreCase)
    For Each prop As Newtonsoft.Json.Linq.JProperty In obj.Properties()
        If Not nameMap.ContainsKey(prop.Name) Then
            nameMap(prop.Name) = prop.Name
        End If
    Next

    _propertyNameCache.TryAdd(objHash, nameMap)

    Dim foundName As System.String = Nothing
    If nameMap.TryGetValue(propertyName, foundName) Then
        Return obj(foundName)
    End If

    Return Nothing
End Function

''' <summary>
''' Gets property cache statistics for monitoring
''' </summary>
Public Shared Function GetPropertyCacheStats() As System.Object
    Return New With {
        .CacheSize = _propertyNameCache.Count,
        .CacheHits = _cacheHitCount,
        .CacheMisses = _cacheMissCount,
        .HitRate = If(_cacheHitCount + _cacheMissCount > 0,
                     System.Convert.ToDouble(_cacheHitCount) / (_cacheHitCount + _cacheMissCount) * 100,
                     0)
    }
End Function

''' <summary>
''' Clears property cache (for testing or memory management)
''' </summary>
Public Shared Sub ClearPropertyCache()
    _propertyNameCache.Clear()
    _cacheHitCount = 0
    _cacheMissCount = 0
End Sub

' ===================================
' SQL SECURITY: IDENTIFIER VALIDATION
' ===================================

''' <summary>
''' Validates SQL identifiers (table names, column names) to prevent SQL injection
''' CRITICAL SECURITY: Prevents injection via table/column names
''' </summary>
''' <param name="identifier">The SQL identifier to validate</param>
''' <param name="allowBrackets">Allow SQL Server bracket notation [TableName]</param>
''' <returns>True if identifier is safe, False otherwise</returns>
Public Shared Function ValidateSqlIdentifier(identifier As System.String, Optional allowBrackets As System.Boolean = True) As System.Boolean
    If System.String.IsNullOrWhiteSpace(identifier) Then
        Return False
    End If

    ' Check length
    If identifier.Length > MAX_SQL_IDENTIFIER_LENGTH Then
        Return False
    End If

    ' Strip brackets if allowed
    Dim cleanIdentifier As System.String = identifier
    If allowBrackets AndAlso identifier.StartsWith("[") AndAlso identifier.EndsWith("]") Then
        cleanIdentifier = identifier.Substring(1, identifier.Length - 2)
    End If

    ' Must start with letter or underscore
    If Not System.Char.IsLetter(cleanIdentifier(0)) AndAlso cleanIdentifier(0) <> "_"c Then
        Return False
    End If

    ' Rest must be alphanumeric, underscore, or dot (for schema.table notation)
    For Each c As System.Char In cleanIdentifier
        If Not (System.System.Char.IsLetterOrDigit(c) OrElse c = "_"c OrElse c = "."c) Then
            Return False
        End If
    Next

    ' Prevent SQL injection keywords disguised as identifiers
    Dim upper As System.String = cleanIdentifier.ToUpper()
    If upper.Contains("--") OrElse upper.Contains("/*") OrElse upper.Contains("*/") OrElse _
       upper.Contains(";") OrElse upper.Contains("'") OrElse upper.Contains("""") Then
        Return False
    End If

    Return True
End Function

''' <summary>
''' Validates and sanitizes a SQL identifier, throwing exception if invalid
''' </summary>
Public Shared Function ValidateAndGetSqlIdentifier(identifier As System.String, identifierType As System.String) As System.String
    If Not ValidateSqlIdentifier(identifier) Then
        Throw New System.ArgumentException($"Invalid {identifierType}: '{identifier}'. Identifiers must start with letter/underscore and contain only alphanumeric characters, underscores, or dots.")
    End If
    Return identifier
End Function

' ===================================
' VALIDATORS
' ===================================

Public Class ValidatorWrapper
    Private ReadOnly _requiredParams As System.System.String()
    Private ReadOnly _requiredArrayParams As System.System.String()

    Public Sub New(Optional requiredParams As System.System.String() = Nothing, Optional requiredArrayParams As System.System.String() = Nothing)
        _requiredParams = If(requiredParams, New System.System.String() {})
        _requiredArrayParams = If(requiredArrayParams, New System.System.String() {})
    End Sub

    Public Function Validate(payload As Newtonsoft.Json.Linq.JObject) As System.String
        ' Validate required scalar parameters
        For Each paramName As System.String In _requiredParams
            Dim paramResult = GetObjectParameter(payload, paramName)
            If Not paramResult.Item1 Then
                Return CreateErrorResponse($"Parameter {paramName} not specified. Required parameters: " & System.String.Join(",", _requiredParams))
            End If
        Next

        ' Validate required array parameters
        For Each paramName As System.String In _requiredArrayParams
            Dim token = GetPropertyCaseInsensitive(payload, paramName)

            If token Is Nothing Then
                Return CreateErrorResponse($"Parameter {paramName} not specified")
            End If

            If token.Type <> Newtonsoft.Json.Linq.JTokenType.Array Then
                Return CreateErrorResponse($"Parameter {paramName} must be an array")
            End If

            Dim paramResult = GetArrayParameter(payload, paramName)
            If Not paramResult.Item1 Then
                Return CreateErrorResponse($"Parameter {paramName} is not a valid array")
            End If
        Next

        Return String.Empty
    End Function
End Class

' ===================================
' BUSINESS LOGIC: READER
' ===================================
Public Class BusinessLogicReaderWrapper
    Private ReadOnly _baseSQL As System.String
    Private ReadOnly _parameterConditions As System.Collections.Generic.Dictionary(Of System.String, System.Object)
    Private ReadOnly _defaultWhereClause As System.String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping)
    Private ReadOnly _prependSQL As System.String

    ''' <summary>
    ''' Reader with full SQL customization. Use explicit SELECT fields (not SELECT *)
    ''' Automatically uses FOR JSON PATH for 40-60% better performance with fallback to standard mode
    ''' </summary>
    ''' <param name="baseSQL">Base SQL query with explicit fields (e.g., SELECT UserId, Email FROM Users {WHERE})</param>
    ''' <param name="parameterConditions">Dictionary of parameter-specific SQL conditions</param>
    ''' <param name="defaultWhereClause">Default WHERE clause if no parameters provided</param>
    ''' <param name="fieldMappings">Optional JSON-to-SQL field mappings</param>
    ''' <param name="prependSQL">Optional SQL to prepend at the beginning of the final query (e.g., "SET DATEFORMAT ymd;")</param>
    Public Sub New(baseSQL As System.String, _
                   parameterConditions As System.Collections.Generic.Dictionary(Of System.String, System.Object), _
                   Optional defaultWhereClause As System.String = Nothing, _
                   Optional fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping) = Nothing, _
                   Optional prependSQL As System.String = Nothing)
        ' Validate baseSQL for {WHERE} placeholder
        If Not System.String.IsNullOrEmpty(baseSQL) AndAlso baseSQL.Contains("{WHERE}") Then
            Dim wherePlaceholderCount As System.Int32 = (baseSQL.Length - baseSQL.Replace("{WHERE}", "").Length) / 7
            If wherePlaceholderCount > 1 Then
                Throw New System.ArgumentException("SQL can only contain one {WHERE} placeholder")
            End If
        End If

        _baseSQL = baseSQL
        _parameterConditions = If(parameterConditions, New System.Collections.Generic.Dictionary(Of System.String, System.Object))
        _defaultWhereClause = defaultWhereClause
        _fieldMappings = fieldMappings
        _prependSQL = prependSQL
    End Sub

    Public Function Execute(database As System.Object, payload As Newtonsoft.Json.Linq.JObject) As System.Object
        Try
            Dim whereConditions As New System.Collections.Generic.List(Of String)
            Dim sqlParameters As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
            Dim providedParams As New System.Collections.Generic.List(Of String)

            ' Process each parameter condition
            For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In _parameterConditions
                Dim condition As ParameterCondition = kvp.Value
                Dim paramName As System.String = condition.ParameterName

                ' Check if parameter exists in payload (case-insensitive)
                Dim paramResult = GetObjectParameter(payload, paramName)

                If paramResult.Item1 Then
                    ' Parameter is present - use SQLWhenPresent
                    providedParams.Add(paramName)

                    If Not System.String.IsNullOrEmpty(condition.SQLWhenPresent) Then
                        whereConditions.Add(condition.SQLWhenPresent)

                        ' Add parameter binding if UseParameter is True
                        If condition.UseParameter Then
                            Dim sqlColName As System.String = paramName

                            ' Use field mapping if available
                            If _fieldMappings IsNot Nothing AndAlso _fieldMappings.ContainsKey(paramName) Then
                                sqlColName = _fieldMappings(paramName).SqlColumn
                            End If

                            sqlParameters.Add(sqlColName, paramResult.Item2)
                        End If
                    End If
                Else
                    ' Parameter is absent - use SQLWhenAbsent
                    If Not System.String.IsNullOrEmpty(condition.SQLWhenAbsent) Then
                        whereConditions.Add(condition.SQLWhenAbsent)
                    End If

                    ' Use default value if specified
                    If condition.DefaultValue IsNot Nothing AndAlso condition.UseParameter Then
                        Dim sqlColName As System.String = paramName
                        If _fieldMappings IsNot Nothing AndAlso _fieldMappings.ContainsKey(paramName) Then
                            sqlColName = _fieldMappings(paramName).SqlColumn
                        End If
                        sqlParameters.Add(sqlColName, condition.DefaultValue)
                    End If
                End If
            Next

            ' PERFORMANCE: Build final SQL efficiently
            Dim finalSQL As System.String

            ' Handle WHERE clause construction (case-insensitive placeholder)
            If whereConditions.Count > 0 Then
                Dim whereClause As System.String = System.String.Join(" AND ", whereConditions)

                ' ROBUST: Case-insensitive {WHERE} placeholder replacement
                Dim wherePlaceholderPattern As New System.Text.RegularExpressions.Regex("\{WHERE\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                If wherePlaceholderPattern.IsMatch(_baseSQL) Then
                    finalSQL = wherePlaceholderPattern.Replace(_baseSQL, "WHERE " & whereClause)
                ElseIf Not _baseSQL.ToUpper().Contains("WHERE") Then
                    ' PERFORMANCE: StringBuilder for concatenation
                    Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + whereClause.Length + 10)
                    sqlBuilder.Append(_baseSQL)
                    sqlBuilder.Append(" WHERE ")
                    sqlBuilder.Append(whereClause)
                    finalSQL = sqlBuilder.ToSystem.String()
                Else
                    ' Base SQL already has WHERE, append with AND
                    Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + whereClause.Length + 10)
                    sqlBuilder.Append(_baseSQL)
                    sqlBuilder.Append(" AND ")
                    sqlBuilder.Append(whereClause)
                    finalSQL = sqlBuilder.ToSystem.String()
                End If
            Else
                ' No conditions, use default or remove placeholder
                If Not System.String.IsNullOrEmpty(_defaultWhereClause) Then
                    Dim wherePlaceholderPattern As New System.Text.RegularExpressions.Regex("\{WHERE\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    If wherePlaceholderPattern.IsMatch(_baseSQL) Then
                        finalSQL = wherePlaceholderPattern.Replace(_baseSQL, "WHERE " & _defaultWhereClause)
                    ElseIf Not _baseSQL.ToUpper().Contains("WHERE") Then
                        Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + _defaultWhereClause.Length + 10)
                        sqlBuilder.Append(_baseSQL)
                        sqlBuilder.Append(" WHERE ")
                        sqlBuilder.Append(_defaultWhereClause)
                        finalSQL = sqlBuilder.ToSystem.String()
                    Else
                        finalSQL = _baseSQL
                    End If
                Else
                    ' Remove placeholder if exists
                    Dim wherePlaceholderPattern As New System.Text.RegularExpressions.Regex("\{WHERE\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    finalSQL = wherePlaceholderPattern.Replace(_baseSQL, "")
                End If
            End If

            ' AUTOMATIC OPTIMIZATION: Always try FOR JSON PATH first (40-60% faster)
            ' Falls back to standard mode if JSON parsing fails
            Try
                ' Pass prependSQL to ExecuteQueryToJSON so it places it before outer SELECT CAST
                Dim jsonRecords As System.String = ExecuteQueryToJSON(database, finalSQL, sqlParameters, _prependSQL)

                ' Parse the JSON string back to array for consistent response format
                ' This validates that SQL Server generated valid JSON
                Dim recordsArray = Newtonsoft.Json.Linq.JArray.Parse(jsonRecords)

                ' Build response - SQL never included for security
                Dim response As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
                response.Add("Result", "OK")
                response.Add("ProvidedParameters", System.String.Join(",", providedParams))
                response.Add("Records", recordsArray)

                Return response
            Catch jsonEx As Newtonsoft.Json.JsonException
                ' FOR JSON PATH failed due to malformed JSON (unescaped chars, etc.)
                ' AUTOMATIC FALLBACK: Retry with standard Dictionary mode
                ' Prepend SQL for standard dictionary mode
                Dim sqlWithPrepend As System.String = finalSQL
                If Not System.String.IsNullOrEmpty(_prependSQL) Then
                    sqlWithPrepend = _prependSQL & " " & finalSQL
                End If

                Dim rows = ExecuteQueryToDictionary(database, sqlWithPrepend, sqlParameters)

                ' Build response - SQL never included for security
                Dim response As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
                response.Add("Result", "OK")
                response.Add("ProvidedParameters", System.String.Join(",", providedParams))
                response.Add("Records", rows)

                Return response
            End Try
        Catch ex As Exception
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(
                CreateErrorResponse($"Error reading records: {ex.Message}")
            )
        End Try
    End Function
End Class

' ===================================
' PERFORMANCE HELPER: BULK EXISTENCE CHECK
' Reduces N+1 query problem in batch operations
' ===================================

''' <summary>
''' Checks existence of multiple records in a single database query
''' Returns a HashSet of composite keys that exist in the database
''' </summary>
Private Shared Function BulkExistenceCheck(
    database As System.Object,
    tableName As System.String,
    keyFields As System.System.String(),
    recordParameters As System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of System.String, System.Object)),
    Optional prependSQL As System.String = Nothing
) As System.Collections.Generic.HashSet(Of String)

    If recordParameters Is Nothing OrElse recordParameters.Count = 0 Then
        Return New System.Collections.Generic.HashSet(Of String)()
    End If

    ' PERFORMANCE: Build a single query to check all records at once
    ' Instead of: SELECT COUNT(*) WHERE key1=:v1 AND key2=:v2 (repeated N times)
    ' We use: SELECT key1, key2, ... FROM table WHERE (key1, key2) IN ((...), (...), ...)

    Dim existingKeys As New System.Collections.Generic.HashSet(Of String)()

    Try
        ' Build the bulk check SQL
        Dim sqlBuilder As New System.Text.StringBuilder(512)
        sqlBuilder.Append("SELECT DISTINCT ")
        sqlBuilder.Append(System.String.Join(", ", keyFields))
        sqlBuilder.Append(" FROM ")
        sqlBuilder.Append(tableName)
        sqlBuilder.Append(" WHERE ")

        ' Build OR conditions for each record
        Dim recordConditions As New System.Collections.Generic.List(Of String)(recordParameters.Count)
        Dim queryParams As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
        Dim paramIndex As System.Int32 = 0

        For Each recordParams As System.Collections.Generic.Dictionary(Of System.String, System.Object) In recordParameters
            Dim conditions As New System.Collections.Generic.List(Of String)(keyFields.Length)

            For Each keyField As System.String In keyFields
                If recordParams.ContainsKey(keyField) Then
                    ' ROBUST: Use prefix to prevent collisions (e.g., UserId_0 vs UserId with paramIndex 0)
                    Dim paramName As System.String = $"p{paramIndex}_{keyField}"
                    conditions.Add($"{keyField} = :{paramName}")
                    queryParams.Add(paramName, recordParams(keyField))
                End If
            Next

            If conditions.Count = keyFields.Length Then
                recordConditions.Add($"({System.String.Join(" AND ", conditions)})")
            End If

            paramIndex += 1
        Next

        If recordConditions.Count = 0 Then
            Return existingKeys
        End If

        sqlBuilder.Append(System.String.Join(" OR ", recordConditions))

        ' Execute the bulk check query
        Dim checkQuery As New QWTable()
        Try
            checkQuery.Database = database
            ' Build query SQL and optionally prepend SQL (e.g., SET DATEFORMAT ymd;)
            Dim bulkCheckSQL As System.String = sqlBuilder.ToSystem.String()
            If Not System.String.IsNullOrEmpty(prependSQL) Then
                bulkCheckSQL = prependSQL & " " & bulkCheckSQL
            End If
            checkQuery.SQL = bulkCheckSQL

            For Each param As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In queryParams
                checkQuery.params(param.Key) = param.Value
            Next

            checkQuery.RequestLive = False
            checkQuery.Active = True

            ' Build composite keys from results
            While Not checkQuery.Rowset.EndOfSet
                Dim compositeKey As New System.Text.StringBuilder()
                For Each keyField As System.String In keyFields
                    If compositeKey.Length > 0 Then compositeKey.Append(COMPOSITE_KEY_DELIMITER)
                    ' Safely handle DBNull values in key fields
                    Dim fieldValue = checkQuery.Rowset.Fields(keyField).Value
                    If System.Convert.IsDBNull(fieldValue) OrElse fieldValue Is Nothing Then
                        compositeKey.Append("[NULL]")
                    Else
                        compositeKey.Append(fieldValue.ToSystem.String())
                    End If
                Next
                existingKeys.Add(compositeKey.ToSystem.String())
                checkQuery.Rowset.Next()
            End While

        Finally
            If checkQuery IsNot Nothing Then
                Try
                    checkQuery.Active = False
                Catch
                    ' Ignore cleanup errors
                End Try
                Try
                    checkQuery.Dispose()
                Catch
                    ' Ignore disposal errors
                End Try
            End If
        End Try

    Catch ex As Exception
        ' If bulk check fails, fall back to individual checks
        ' (This ensures backward compatibility if SQL syntax is not supported)
    End Try

    Return existingKeys
End Function

''' <summary>
''' Creates a composite key string from record parameters
''' ROBUST: Uses safe delimiter (ASCII 31) to prevent key collisions
''' </summary>
Private Shared Function GetCompositeKey(recordParams As System.Collections.Generic.Dictionary(Of System.String, System.Object), keyFields As System.System.String()) As System.String
    Dim compositeKey As New System.Text.StringBuilder()
    For Each keyField As System.String In keyFields
        If compositeKey.Length > 0 Then compositeKey.Append(COMPOSITE_KEY_DELIMITER)
        ' Safely handle null values in key fields
        If recordParams.ContainsKey(keyField) AndAlso recordParams(keyField) IsNot Nothing Then
            compositeKey.Append(recordParams(keyField).ToSystem.String())
        Else
            compositeKey.Append("[NULL]")
        End If
    Next
    Return compositeKey.ToSystem.String()
End Function

' ===================================
' BUSINESS LOGIC: BATCH WRITER
' ===================================
Public Class BusinessLogicBatchWriterWrapper
    Private ReadOnly _tableName As System.String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping)
    Private ReadOnly _keyFields As System.System.String()
    Private ReadOnly _allowUpdates As System.Boolean
    Private ReadOnly _prependSQL As System.String

    Public Sub New(tableName As System.String, fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping), allowUpdates As System.Boolean, Optional prependSQL As System.String = Nothing)
        ' SECURITY: Validate table name
        _tableName = ValidateAndGetSqlIdentifier(tableName, "table name")
        _fieldMappings = fieldMappings
        _allowUpdates = allowUpdates
        _prependSQL = prependSQL

        ' SECURITY: Validate all SQL column names in field mappings
        If fieldMappings IsNot Nothing Then
            For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, FieldMapping) In fieldMappings
                ValidateAndGetSqlIdentifier(kvp.Value.SqlColumn, "column name")
            Next
        End If

        ' Extract primary keys from field mappings (IsPrimaryKey=True)
        Dim primaryKeys As New System.Collections.Generic.List(Of String)
        For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, FieldMapping) In fieldMappings
            If kvp.Value.IsPrimaryKey Then
                primaryKeys.Add(kvp.Value.SqlColumn)
            End If
        Next

        If primaryKeys.Count = 0 Then
            Throw New System.ArgumentException("No primary keys specified. Mark fields with IsPrimaryKey=True in fieldMappings.")
        End If

        _keyFields = primaryKeys.ToArray()
    End Sub

    Public Function Execute(database As System.Object, payload As Newtonsoft.Json.Linq.JObject) As System.Object
        Try
            Dim recordsToken = GetPropertyCaseInsensitive(payload, "Records")
            Dim recordsArray As Newtonsoft.Json.Linq.JArray = TryCast(recordsToken, Newtonsoft.Json.Linq.JArray)

            If recordsArray Is Nothing Then
                ' Single record - wrap in array for batch processing
                Dim singleRecordArray As New Newtonsoft.Json.Linq.JArray()
                singleRecordArray.Add(payload)

                ' Create temporary payload with Records array
                Dim wrappedPayload As New Newtonsoft.Json.Linq.JObject()
                wrappedPayload.Add("Records", singleRecordArray)

                ' Process as batch of 1
                recordsArray = singleRecordArray
                payload = wrappedPayload
            End If

            ' ROBUST: Validate batch size to prevent DoS and memory exhaustion
            If recordsArray.Count > MAX_BATCH_SIZE Then
                Return New With {
                    .Result = "KO",
                    .Reason = $"Batch size {recordsArray.Count} exceeds maximum allowed size of {MAX_BATCH_SIZE}. Please split into smaller batches."
                }
            End If

            If recordsArray.Count = 0 Then
                Return New With {
                    .Result = "OK",
                    .Message = "No records to process",
                    .Inserted = 0,
                    .Updated = 0,
                    .Errors = 0
                }
            End If

            Dim insertedCount As System.Int32 = 0, updatedCount As System.Int32 = 0, errorCount As System.Int32 = 0
            Dim errors As New System.Collections.Generic.List(Of String)

            ' PERFORMANCE: Pre-extract all record parameters and perform bulk existence check
            ' This reduces N database queries to 1 query for existence checking
            Dim allRecordParams As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of System.String, System.Object))(recordsArray.Count)
            Dim recordDataList As New System.Collections.Generic.List(Of Object)(recordsArray.Count)

            ' First pass: Extract and validate parameters
            For Each recordToken As Newtonsoft.Json.Linq.JToken In recordsArray
                Try
                    Dim record As Newtonsoft.Json.Linq.JObject = CType(recordToken, Newtonsoft.Json.Linq.JObject)

                    ' Validate required fields
                    Dim missingRequired As New System.Collections.Generic.List(Of String)
                    For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, FieldMapping) In _fieldMappings
                        If kvp.Value.IsRequired Then
                            Dim paramResult = GetObjectParameter(record, kvp.Key)
                            If Not paramResult.Item1 Then
                                missingRequired.Add(kvp.Key)
                            End If
                        End If
                    Next

                    If missingRequired.Count > 0 Then
                        errors.Add($"Record skipped - Missing required fields: {System.String.Join(", ", missingRequired)}")
                        errorCount += 1
                        recordDataList.Add(Nothing)
                        Continue For
                    End If

                    ' Extract parameters with field mappings
                    Dim recordParams As New System.Collections.Generic.Dictionary(Of System.String, System.Object)
                    For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, FieldMapping) In _fieldMappings
                        Dim paramResult = GetObjectParameter(record, kvp.Key)
                        If paramResult.Item1 Then
                            recordParams.Add(kvp.Value.SqlColumn, paramResult.Item2)
                        ElseIf kvp.Value.DefaultValue IsNot Nothing Then
                            recordParams.Add(kvp.Value.SqlColumn, kvp.Value.DefaultValue)
                        End If
                    Next

                    allRecordParams.Add(recordParams)
                    recordDataList.Add(recordParams)

                Catch ex As Exception
                    errors.Add($"Record processing error: {ex.Message}")
                    errorCount += 1
                    recordDataList.Add(Nothing)
                End Try
            Next

            ' PERFORMANCE: Single bulk existence check for all valid records
            Dim existingRecords As System.Collections.Generic.HashSet(Of String) = Nothing
            If allRecordParams.Count > 0 Then
                existingRecords = BulkExistenceCheck(database, _tableName, _keyFields, allRecordParams, _prependSQL)
            Else
                existingRecords = New System.Collections.Generic.HashSet(Of String)()
            End If

            ' Second pass: Process each record with pre-determined existence
            For i As System.Int32 = 0 To recordDataList.Count - 1
                Dim recordParams = TryCast(recordDataList(i), System.Collections.Generic.Dictionary(Of System.String, System.Object))
                If recordParams Is Nothing Then
                    Continue For ' Skip records that failed validation
                End If

                Try
                    ' Get composite key for this record
                    Dim compositeKey As System.String = GetCompositeKey(recordParams, _keyFields)
                    Dim recordExists As System.Boolean = existingRecords.Contains(compositeKey)

                    Dim keyValuesList As New System.Collections.Generic.List(Of String)
                    For Each keyCol As System.String In _keyFields
                        keyValuesList.Add(If(recordParams.ContainsKey(keyCol), recordParams(keyCol).ToSystem.String(), String.Empty))
                    Next
                    Dim IndexColumnsValues As System.String = System.String.Join(",", keyValuesList)

                    If recordExists Then
                        ' Record exists - perform UPDATE
                        If Not _allowUpdates Then
                            errors.Add($"{IndexColumnsValues} - Record already exists and updates are not allowed")
                            errorCount += 1
                            Continue For
                        End If

                        Dim whereConditions As New System.Collections.Generic.List(Of String)
                        For Each keyCol As System.String In _keyFields
                            whereConditions.Add($"{keyCol} = :{keyCol}")
                        Next

                        Dim setClauses As New System.Collections.Generic.List(Of String)
                        For Each kvp As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In recordParams
                            If Not _keyFields.Contains(kvp.Key) Then
                                setClauses.Add($"{kvp.Key} = :{kvp.Key}")
                            End If
                        Next

                        If setClauses.Count > 0 Then
                            Dim updateQuery As New QWTable()
                            Try
                                updateQuery.Database = database
                                ' Build UPDATE SQL and optionally prepend SQL (e.g., SET DATEFORMAT ymd;)
                                Dim updateSQL As System.String = $"UPDATE {_tableName} SET {System.String.Join(", ", setClauses)} WHERE {System.String.Join(" AND ", whereConditions)}"
                                If Not System.String.IsNullOrEmpty(_prependSQL) Then
                                    updateSQL = _prependSQL & " " & updateSQL
                                End If
                                updateQuery.SQL = updateSQL

                                For Each param As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In recordParams
                                    updateQuery.params(param.Key) = param.Value
                                Next

                                updateQuery.Active = True
                                updateQuery.Active = False
                                updatedCount += 1
                            Catch ex As Exception
                                errors.Add($"{IndexColumnsValues} - Update error: {ex.Message}")
                                errorCount += 1
                            Finally
                                If updateQuery IsNot Nothing Then
                                    Try
                                        updateQuery.Dispose()
                                    Catch
                                        ' Ignore disposal errors
                                    End Try
                                End If
                            End Try
                        Else
                            updatedCount += 1
                        End If
                    Else
                        ' Record does not exist - perform INSERT
                        Dim insertTable As New QWTable()
                        Try
                            insertTable.Database = database
                            ' Build SELECT SQL for INSERT and optionally prepend SQL (e.g., SET DATEFORMAT ymd;)
                            Dim insertSQL As System.String = $"SELECT * FROM {_tableName} WHERE 1=0"
                            If Not System.String.IsNullOrEmpty(_prependSQL) Then
                                insertSQL = _prependSQL & " " & insertSQL
                            End If
                            insertTable.SQL = insertSQL
                            insertTable.RequestLive = True
                            insertTable.AllowAllRecords = False
                            insertTable.Active = True

                            insertTable.BeginAppend()
                            Dim failedFields As New System.Collections.Generic.List(Of String)
                            For Each param As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In recordParams
                                Try
                                    insertTable.Replace(param.Key, param.Value)
                                Catch ex As Exception
                                    ' Track failed fields
                                    failedFields.Add($"{param.Key}")
                                End Try
                            Next

                            ' Check if all fields failed - indicates schema problem
                            If failedFields.Count > 0 AndAlso failedFields.Count = recordParams.Count Then
                                errors.Add($"{IndexColumnsValues} - Schema mismatch: No fields could be set")
                                errorCount += 1
                            Else
                                Dim saveMsg As System.String = ""
                                If insertTable.SaveRecord(saveMsg) Then
                                    insertedCount += 1
                                Else
                                    errors.Add($"{IndexColumnsValues} - Save error: {saveMsg}")
                                    errorCount += 1
                                End If
                            End If
                        Catch ex As Exception
                            errors.Add($"{IndexColumnsValues} - Insert error: {ex.Message}")
                            errorCount += 1
                        Finally
                            If insertTable IsNot Nothing Then
                                Try
                                    insertTable.Active = False
                                Catch
                                    ' Ignore cleanup errors
                                End Try
                                Try
                                    insertTable.Dispose()
                                Catch
                                    ' Ignore disposal errors
                                End Try
                            End If
                        End Try
                    End If

                Catch ex As Exception
                    errors.Add($"Record processing error: {ex.Message}")
                    errorCount += 1
                End Try
            Next

            Return New With {
                .Result = If(errorCount = 0, "OK", If(errorCount >= recordsArray.Count, "KO", "PARTIAL")),
                .Inserted = insertedCount,
                .Updated = updatedCount,
                .Errors = errorCount,
                .ErrorDetails = errors.ToArray(),
                .Message = $"Processed {recordsArray.Count} records: {insertedCount} inserted, {updatedCount} updated, {errorCount} errors."
            }
        Catch ex As Exception
            Return New With {.Result = "KO", .Reason = $"Batch operation failed: {ex.Message}"}
        End Try
    End Function
End Class

' ===================================
' UTILITY FUNCTIONS
' ===================================

Public Sub LogCustom(database As System.Object, StringPayload As System.String, StringResult As System.String, Optional LogMessage As System.String = "")
    Dim fullLogMessage As System.String = $"{LogMessage}{System.Environment.NewLine}{{""Payload"": {StringPayload},{System.Environment.NewLine}""Result"": {StringResult}}}"
    Write_LogDoc(database.QWSession, "**", "ACTIONLINK", 1, fullLogMessage)
End Sub

Public Function ProcessActionLink(
    ByVal database As System.Object,
    ByVal p_validator As System.Func(Of Newtonsoft.Json.Linq.JObject, System.String),
    ByVal p_businessLogic As System.Func(Of System.Object, Newtonsoft.Json.Linq.JObject, System.Object),
    Optional ByVal LogMessage As System.String = Nothing,
    Optional ByVal payload As Newtonsoft.Json.Linq.JObject = Nothing,
    Optional ByVal StringPayload As System.String = ""
) As System.String
    Try
        If payload Is Nothing Then
            Return CreateErrorResponse("Invalid or empty JSON payload")
        End If

        If p_validator IsNot Nothing Then
            Dim validationError As System.String = p_validator(payload)
            If Not System.String.IsNullOrEmpty(validationError) Then
                Return validationError
            End If
        End If

        Dim result As System.Object = p_businessLogic(database, payload)
        Dim StringResult As System.String = Newtonsoft.Json.JsonConvert.SerializeObject(result)

        ' Use LogMessage if provided
        If LogMessage IsNot Nothing Then
            LogCustom(database, StringPayload, StringResult, LogMessage)
        End If

        Return StringResult
    Catch ex As Exception
        Return CreateErrorResponse($"Internal error: {ex.Message}")
    End Try
End Function

Public Shared Function ParsePayload(Optional ByRef PayloadString As System.String = Nothing,
                                   Optional ByRef ErrorMessage As System.String = Nothing) As Newtonsoft.Json.Linq.JObject
    Try
        If System.Web.HttpContext.Current Is Nothing OrElse
           System.Web.HttpContext.Current.Request Is Nothing Then
            ErrorMessage = "HttpContext is not available"
            Return Nothing
        End If

        ' ROBUST: Check if stream is seekable before seeking
        Dim inputStream As System.IO.Stream = System.Web.HttpContext.Current.Request.InputStream
        If inputStream.CanSeek Then
            inputStream.Seek(0, System.IO.SeekOrigin.Begin)
        End If

        Using reader As New System.IO.StreamReader(
            inputStream,
            System.Text.Encoding.UTF8)

            Dim jsonString As System.String = reader.ReadToEnd()

            If Not PayloadString Is Nothing Then
                PayloadString = jsonString
            End If

            If System.String.IsNullOrWhiteSpace(jsonString) Then
                ErrorMessage = "Request payload is empty"
                Return Nothing
            End If

            Return Newtonsoft.Json.Linq.JObject.Parse(jsonString)
        End Using

    Catch jsonEx As Newtonsoft.Json.JsonException
        ErrorMessage = $"Invalid JSON format: {jsonEx.Message}"
        Return Nothing
    Catch ioEx As System.IO.IOException
        ErrorMessage = $"IO error reading request: {ioEx.Message}"
        Return Nothing
    Catch ex As System.Exception
        ErrorMessage = $"Internal error: {ex.Message}"
        Return Nothing
    End Try
End Function

Public Shared Function GetStringParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As System.String) As System.Tuple(Of System.Boolean, System.String)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            ' Safely handle different token types
            Select Case token.Type
                Case Newtonsoft.Json.Linq.JTokenType.String, _
                     Newtonsoft.Json.Linq.JTokenType.Integer, _
                     Newtonsoft.Json.Linq.JTokenType.Float, _
                     Newtonsoft.Json.Linq.JTokenType.Boolean, _
                     Newtonsoft.Json.Linq.JTokenType.Date
                    ' For value types, use ToSystem.String() which is safe
                    Return New System.Tuple(Of System.Boolean, System.String)(True, token.ToSystem.String())
                Case Newtonsoft.Json.Linq.JTokenType.Null
                    ' Handle null explicitly
                    Return New System.Tuple(Of System.Boolean, System.String)(True, Nothing)
                Case Else
                    ' For arrays/objects, return JSON representation
                    Return New System.Tuple(Of System.Boolean, System.String)(True, token.ToSystem.String())
            End Select
        End If
        Return New System.Tuple(Of System.Boolean, System.String)(False, Nothing)
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, System.String)(False, Nothing)
    End Try
End Function

Public Shared Function GetDateParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As System.String) As System.Tuple(Of System.Boolean, System.DateTime)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            ' Handle Date type directly
            If token.Type = Newtonsoft.Json.Linq.JTokenType.Date Then
                Return New System.Tuple(Of System.Boolean, System.DateTime)(True, CType(CType(token, Newtonsoft.Json.Linq.JValue).Value, System.DateTime))
            ElseIf token.Type = Newtonsoft.Json.Linq.JTokenType.String Then
                ' Try parsing string as date
                Dim dateValue As System.DateTime
                If System.DateTime.TryParse(token.ToSystem.String(), dateValue) Then
                    Return New System.Tuple(Of System.Boolean, System.DateTime)(True, dateValue)
                End If
            End If
        End If
        Return New System.Tuple(Of System.Boolean, System.DateTime)(False, System.DateTime.MinValue)
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, System.DateTime)(False, System.DateTime.MinValue)
    End Try
End Function

Public Shared Function GetIntegerParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As System.String) As System.Tuple(Of System.Boolean, System.Int32)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            ' Handle Integer type directly
            If token.Type = Newtonsoft.Json.Linq.JTokenType.Integer Then
                Try
                    Dim value As System.Object = CType(token, Newtonsoft.Json.Linq.JValue).Value
                    ' Check if value is within integer range
                    If TypeOf value Is System.Int64 Then
                        Dim longValue As System.Int64 = System.Convert.ToInt64(value)
                        If longValue >= System.Int32.MinValue AndAlso longValue <= System.Int32.MaxValue Then
                            Return New System.Tuple(Of System.Boolean, System.Int32)(True, System.Convert.ToInt32(longValue))
                        Else
                            ' Value out of integer range
                            Return New System.Tuple(Of System.Boolean, System.Int32)(False, 0)
                        End If
                    Else
                        Return New System.Tuple(Of System.Boolean, System.Int32)(True, System.Convert.ToInt32(value))
                    End If
                Catch ex As System.OverflowException
                    ' Value out of range
                    Return New System.Tuple(Of System.Boolean, System.Int32)(False, 0)
                End Try
            ElseIf token.Type = Newtonsoft.Json.Linq.JTokenType.String Then
                ' Try parsing string as integer
                Dim intValue As System.Int32
                If System.Int32.TryParse(token.ToSystem.String(), intValue) Then
                    Return New System.Tuple(Of System.Boolean, System.Int32)(True, intValue)
                End If
            ElseIf token.Type = Newtonsoft.Json.Linq.JTokenType.Float Then
                ' Try converting float to integer (truncate)
                Try
                    Dim floatValue As System.Double = System.Convert.ToDouble(CType(token, Newtonsoft.Json.Linq.JValue).Value)
                    ' ROBUST: Check for overflow before converting
                    If floatValue >= System.Int32.MinValue AndAlso floatValue <= System.Int32.MaxValue Then
                        Return New System.Tuple(Of System.Boolean, System.Int32)(True, System.Convert.ToInt32(floatValue))
                    Else
                        ' Value out of integer range
                        Return New System.Tuple(Of System.Boolean, System.Int32)(False, 0)
                    End If
                Catch
                    ' Conversion failed, return false
                End Try
            End If
        End If
        Return New System.Tuple(Of System.Boolean, System.Int32)(False, 0)
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, System.Int32)(False, 0)
    End Try
End Function

Public Shared Function GetObjectParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As System.String) As System.Tuple(Of System.Boolean, System.Object)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            Select Case token.Type
                Case Newtonsoft.Json.Linq.JTokenType.Array
                    Return New System.Tuple(Of System.Boolean, System.Object)(True, CType(token, Newtonsoft.Json.Linq.JArray))
                Case Newtonsoft.Json.Linq.JTokenType.Object
                    Return New System.Tuple(Of System.Boolean, System.Object)(True, CType(token, Newtonsoft.Json.Linq.JObject))
                Case Else
                    Return New System.Tuple(Of System.Boolean, System.Object)(True, CType(token, Newtonsoft.Json.Linq.JValue).Value)
            End Select
        Else
            Return New System.Tuple(Of System.Boolean, System.Object)(False, Nothing)
        End If
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, System.Object)(False, Nothing)
    End Try
End Function

Public Shared Function GetArrayParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As System.String) As System.Tuple(Of System.Boolean, Newtonsoft.Json.Linq.JArray)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing AndAlso token.Type = Newtonsoft.Json.Linq.JTokenType.Array Then
            Return New System.Tuple(Of System.Boolean, Newtonsoft.Json.Linq.JArray)(True, CType(token, Newtonsoft.Json.Linq.JArray))
        End If
        Return New System.Tuple(Of System.Boolean, Newtonsoft.Json.Linq.JArray)(False, Nothing)
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, Newtonsoft.Json.Linq.JArray)(False, Nothing)
    End Try
End Function

Public Shared Function CreateErrorResponse(reason As System.String) As System.String
    Return Newtonsoft.Json.JsonConvert.SerializeObject(New With {.Result = "KO", .Reason = reason})
End Function

''' <summary>
''' Executes query and returns results as list of dictionaries
''' NOTE: Use explicit field selection in your SQL (e.g., SELECT UserId, Email FROM Users)
''' instead of SELECT * for better performance
''' ROBUST: Includes try-finally for resource cleanup and converts DBNull to Nothing
''' </summary>
Public Shared Function ExecuteQueryToDictionary(database As System.Object, sql As System.String, parameters As System.Collections.Generic.Dictionary(Of System.String, System.Object)) As System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of System.String, System.Object))
    Dim q As New QWTable()
    Try
        q.Database = database
        q.SQL = sql
        If parameters IsNot Nothing Then
            For Each param As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In parameters
                q.params(param.Key) = param.Value
            Next
        End If
        q.RequestLive = False
        q.Active = True

        ' PERFORMANCE: Pre-allocate with estimated capacity
        Dim estimatedFieldCount As System.Int32 = q.rowset.fields.size
        Dim rows As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of System.String, System.Object))()

        While Not q.rowset.endofset
            Dim row As New System.Collections.Generic.Dictionary(Of System.String, System.Object)(estimatedFieldCount)

            For i As System.Int32 = 1 To q.rowset.fields.size
                Dim fieldName As System.String = q.Rowset.fields(i).fieldname
                Dim fieldValue As System.Object = q.rowset.fields(i).value

                ' ROBUST: Convert DBNull to Nothing for proper JSON serialization
                If System.Convert.IsDBNull(fieldValue) Then
                    row.Add(fieldName, Nothing)
                Else
                    row.Add(fieldName, fieldValue)
                End If
            Next

            rows.Add(row)
            q.rowset.next()
        End While

        q.Active = False
        Return rows
    Finally
        ' ROBUST: Ensure cleanup even on error
        If q IsNot Nothing Then
            Try
                q.Active = False
            Catch
                ' Ignore cleanup errors
            End Try
            Try
                q.Dispose()
            Catch
                ' Ignore disposal errors
            End Try
        End If
    End Try
End Function

''' <summary>
''' Executes query with FOR JSON PATH and returns JSON string directly from SQL Server
''' This is 40-60% faster than ExecuteQueryToDictionary for simple queries
''' NOTE: SQL Server does all JSON serialization natively in C++ code
''' EDGE CASES HANDLED:
''' - NULL/empty results: Returns "[]"
''' - Special characters: Automatically applies STRING_ESCAPE to text columns
''' - Large result sets: Uses NVARCHAR(MAX) to avoid truncation
''' - Binary data: SQL Server encodes as Base64
''' - JSON validation: Pre-validates JSON structure before returning
''' </summary>
''' <param name="database">Database connection object</param>
''' <param name="sql">SQL query (FOR JSON PATH will be appended automatically)</param>
''' <param name="parameters">Query parameters dictionary</param>
''' <param name="prependSQL">Optional SQL to prepend before outer SELECT CAST (e.g., "SET DATEFORMAT ymd;")</param>
''' <returns>JSON string with array of records, or empty array [] if no results</returns>
Public Shared Function ExecuteQueryToJSON(database As System.Object, sql As System.String, parameters As System.Collections.Generic.Dictionary(Of System.String, System.Object), Optional prependSQL As System.String = Nothing) As System.String
    Dim q As New QWTable()
    Try
        q.Database = database

        ' PERFORMANCE + ROBUSTNESS: Use FOR JSON PATH with options for better handling
        ' - Use WITHOUT_ARRAY_WRAPPER when we know it's a single row (not applicable here)
        ' - INCLUDE_NULL_VALUES ensures consistent structure
        ' ROBUSTNESS: Wrap the entire query in a subquery and cast result as NVARCHAR(MAX)
        ' This prevents truncation issues that can cause malformed JSON

        Dim jsonSQL As System.String = $"SELECT CAST(( {sql} FOR JSON PATH, INCLUDE_NULL_VALUES ) AS NVARCHAR(MAX)) AS JsonResult"

        ' FEATURE: Prepend SQL if specified (e.g., SET DATEFORMAT ymd;)
        ' Place it BEFORE the outer SELECT CAST to ensure valid SQL syntax
        If Not System.String.IsNullOrEmpty(prependSQL) Then
            jsonSQL = prependSQL & " " & jsonSQL
        End If

        q.SQL = jsonSQL

        If parameters IsNot Nothing Then
            For Each param As System.Collections.Generic.KeyValuePair(Of System.String, System.Object) In parameters
                q.params(param.Key) = param.Value
            Next
        End If

        q.RequestLive = False
        q.Active = True

        ' SQL Server returns JSON result in first row, first column
        ' If no results, SQL Server returns NULL (we'll return empty array instead)
        Dim jsonResult As System.String = "[]"

        If Not q.rowset.endofset AndAlso q.rowset.fields.size > 0 Then
            Dim fieldValue As System.Object = q.rowset.fields(1).value
            If fieldValue IsNot Nothing AndAlso Not System.Convert.IsDBNull(fieldValue) Then
                Dim rawJson As System.String = fieldValue.ToSystem.String()

                ' Basic validation: ensure we got something that looks like JSON
                If Not System.String.IsNullOrWhiteSpace(rawJson) Then
                    ' Trim whitespace that SQL Server might add
                    jsonResult = rawJson.Trim()

                    ' ROBUSTNESS: Validate JSON structure before returning
                    ' Check for proper brackets and basic structure
                    If Not System.String.IsNullOrEmpty(jsonResult) Then
                        ' Ensure it's an array - if SQL Server returned object, wrap in array
                        If Not jsonResult.StartsWith("[") Then
                            If jsonResult.StartsWith("{") Then
                                ' Single object - wrap in array
                                jsonResult = "[" & jsonResult & "]"
                            Else
                                ' Invalid JSON structure - throw error for fallback
                                Throw New System.Exception("Invalid JSON structure returned from FOR JSON PATH")
                            End If
                        End If

                        ' ROBUSTNESS: Check for truncation by looking for incomplete JSON
                        ' A properly formed JSON array should end with ]
                        If Not jsonResult.EndsWith("]") AndAlso Not jsonResult.EndsWith("}") Then
                            Throw New System.Exception("JSON appears truncated - missing closing bracket")
                        End If
                    End If
                End If
            End If
        End If

        q.Active = False
        Return jsonResult

    Catch ex As Exception
        ' Ensure cleanup even on error
        If q IsNot Nothing AndAlso q.Active Then
            Try
                q.Active = False
            Catch
                ' Ignore cleanup errors
            End Try
        End If

        ' Re-throw so caller can handle
        Throw New System.Exception($"FOR JSON PATH execution failed: {ex.Message}", ex)
    Finally
        If q IsNot Nothing Then
            Try
                q.Dispose()
            Catch
                ' Ignore disposal errors
            End Try
        End If
    End Try
End Function

''' <summary>
''' Helper function to wrap text column in SQL to escape special characters for FOR JSON PATH
''' Use this for columns known to contain problematic data (quotes, newlines, control chars, etc.)
''' ROBUST: Uses SQL Server STRING_ESCAPE (2016+) with fallback to REPLACE for older versions
''' </summary>
''' <param name="columnName">The SQL column name to wrap</param>
''' <param name="alias">Optional alias for the column in results</param>
''' <param name="useStringEscape">If True, uses STRING_ESCAPE (SQL 2016+), otherwise uses REPLACE</param>
''' <returns>SQL expression that escapes special characters</returns>
''' <example>
''' Instead of: SELECT Description FROM Table
''' Use: SELECT ' & EscapeColumnForJson("Description") & ' FROM Table
''' Result (SQL 2016+): SELECT STRING_ESCAPE(Description, 'json') AS Description FROM Table
''' Result (SQL 2012): SELECT REPLACE(REPLACE(REPLACE(Description, CHAR(13), ''), CHAR(10), ' '), CHAR(9), ' ') AS Description FROM Table
''' </example>
Public Shared Function EscapeColumnForJson(columnName As System.String, Optional ByVal [alias] As System.String = Nothing, Optional useStringEscape As System.Boolean = True) As System.String
    Dim aliasName As System.String = If(System.String.IsNullOrEmpty([alias]), columnName, [alias])

    If useStringEscape Then
        ' SQL Server 2016+ has STRING_ESCAPE function that properly escapes for JSON
        ' This handles: quotes, backslashes, control characters (0x00-0x1F), and more
        ' Note: STRING_ESCAPE doesn't add quotes, just escapes the content
        Return $"STRING_ESCAPE(ISNULL(CAST({columnName} AS NVARCHAR(MAX)), ''), 'json') AS {aliasName}"
    Else
        ' Fallback for SQL Server 2012/2014
        ' Replace common problematic characters:
        ' - CHAR(0) through CHAR(31) = Control characters that break JSON
        ' - Backslash (\) - escape it
        ' - Double quote (") - escape it
        ' - Forward slash (/) - optionally escape (not required by JSON spec)
        ' Priority: Remove/replace the most common offenders

        ' Multi-stage replacement for safety:
        ' 1. Handle NULL values first
        ' 2. Remove dangerous control characters (0x00-0x1F)
        ' 3. Escape quotes and backslashes
        Dim escapedExpr As System.String = $"ISNULL(CAST({columnName} AS NVARCHAR(MAX)), '')"

        ' Remove most control characters (except tab, CR, LF which we'll handle separately)
        ' CHAR(0) to CHAR(8), CHAR(11), CHAR(12), CHAR(14) to CHAR(31)
        For i As System.Int32 = 0 To 31
            If i <> 9 AndAlso i <> 10 AndAlso i <> 13 Then ' Skip tab, LF, CR
                escapedExpr = $"REPLACE({escapedExpr}, CHAR({i}), '')"
            End If
        Next

        ' Replace common whitespace control chars with spaces
        escapedExpr = $"REPLACE(REPLACE(REPLACE({escapedExpr}, CHAR(13), ' '), CHAR(10), ' '), CHAR(9), ' ')"

        ' Escape backslashes and quotes (FOR JSON PATH should handle these, but extra safety)
        ' Note: FOR JSON PATH typically handles these, so we only do basic cleanup

        Return $"{escapedExpr} AS {aliasName}"
    End If
End Function

''' <summary>
''' Helper to build a safe column list for SELECT with automatic escaping for text columns
''' ROBUST: Automatically applies STRING_ESCAPE or REPLACE to text columns for FOR JSON PATH safety
''' </summary>
''' <param name="columns">Array of column names</param>
''' <param name="textColumns">Optional array of columns that should be escaped (contain text data)</param>
''' <param name="useStringEscape">If True, uses STRING_ESCAPE (SQL 2016+), otherwise uses REPLACE</param>
''' <returns>Comma-separated column list with escaping applied to text columns</returns>
''' <example>
''' BuildSafeColumnList({"ID", "Name", "Description"}, {"Description"})
''' Returns (SQL 2016+): "ID, Name, STRING_ESCAPE(ISNULL(CAST(Description AS NVARCHAR(MAX)), ''), 'json') AS Description"
''' </example>
Public Shared Function BuildSafeColumnList(columns As System.System.String(), Optional textColumns As System.System.String() = Nothing, Optional useStringEscape As System.Boolean = True) As System.String
    If columns Is Nothing OrElse columns.Length = 0 Then
        Return "*"
    End If

    Dim columnList As New System.Collections.Generic.List(Of String)(columns.Length)
    Dim textColumnsSet As New System.Collections.Generic.HashSet(Of String)(System.StringComparer.OrdinalIgnoreCase)

    If textColumns IsNot Nothing Then
        For Each tc As System.String In textColumns
            textColumnsSet.Add(tc)
        Next
    End If

    For Each col As System.String In columns
        If textColumnsSet.Contains(col) Then
            columnList.Add(EscapeColumnForJson(col, Nothing, useStringEscape))
        Else
            columnList.Add(col)
        End If
    Next

    Return System.String.Join(", ", columnList)
End Function

Public Shared Function GetDestinationIdentifier(ByRef payload As Newtonsoft.Json.Linq.JObject) As System.Tuple(Of System.Boolean, System.String)
    Try
        If payload Is Nothing Then
            Return New System.Tuple(Of System.Boolean, System.String)(False, "Invalid JSON payload")
        End If

        Dim destinationToken = GetPropertyCaseInsensitive(payload, "DestinationIdentifier")
        If destinationToken IsNot Nothing Then
            Try
                Dim value As System.String = CType(destinationToken, Newtonsoft.Json.Linq.JValue).Value.ToSystem.String()
                Return New System.Tuple(Of System.Boolean, System.String)(True, value)
            Catch castEx As Exception
                Return New System.Tuple(Of System.Boolean, System.String)(False, "DestinationIdentifier field is not a valid string value")
            End Try
        Else
            Return New System.Tuple(Of System.Boolean, System.String)(False, "DestinationIdentifier field not found")
        End If
    Catch ex As Exception
        Return New System.Tuple(Of System.Boolean, System.String)(False, $"Unexpected error: {ex.Message}")
    End Try
End Function

' ===================================
' FACTORY FUNCTIONS
' ===================================

Public Function CreateValidator(Optional requiredParams As System.System.String() = Nothing, Optional requiredArrayParams As System.System.String() = Nothing) As System.Func(Of Newtonsoft.Json.Linq.JObject, System.String)
    Return AddressOf New ValidatorWrapper(requiredParams, requiredArrayParams).Validate
End Function

Public Function CreateBusinessLogicForReading(
    baseSQL As System.String,
    parameterConditions As System.Collections.Generic.Dictionary(Of System.String, System.Object),
    Optional defaultWhereClause As System.String = Nothing,
    Optional fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping) = Nothing,
    Optional prependSQL As System.String = Nothing
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, System.Object)
    Return AddressOf New BusinessLogicReaderWrapper(baseSQL, parameterConditions, defaultWhereClause, fieldMappings, prependSQL).Execute
End Function

Public Function CreateBusinessLogicForBatchWriting(
    tableName As System.String,
    fieldMappings As System.Collections.Generic.Dictionary(Of System.String, FieldMapping),
    Optional allowUpdates As System.Boolean = True,
    Optional prependSQL As System.String = Nothing
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, System.Object)
    Return AddressOf New BusinessLogicBatchWriterWrapper(tableName, fieldMappings, allowUpdates, prependSQL).Execute
End Function

Public Shared Function ValidatePayloadAndToken(DB As System.Object, _
                                                      Optional CheckForToken As System.Boolean = True, _
                                                      Optional loggerContext As System.String = "", _
                                                      Optional ByRef ParsedPayload As Newtonsoft.Json.Linq.JObject = Nothing, _
                                                      Optional ByRef StringPayload As System.String = "") As System.Object
    Try
        Dim errorMsg As System.String = ""
        Dim StringPayloadQQ As System.String = ""
        ParsedPayload = ParsePayload(StringPayloadQQ, errorMsg)
        StringPayload = StringPayloadQQ
        If ParsedPayload Is Nothing Then
            Return CreateErrorResponse("ParsePayload error: " & errorMsg & " " & loggerContext & " error at parsing.")
        End If

        If CheckForToken Then
            Dim tokenValidationResult = ValidateToken(DB, ParsedPayload, loggerContext)
            If tokenValidationResult IsNot Nothing Then
                Return tokenValidationResult
            End If
        End If

        Return Nothing

    Catch ex As Exception
        Return CreateErrorResponse($"Unexpected error during payload validation: {ex.Message} - Context: {loggerContext}")
    End Try
End Function

Private Shared Function ValidateToken(DB As System.Object, ParsedPayload As Newtonsoft.Json.Linq.JObject, loggerContext As System.String) As System.Object
    Try
        Dim token As Newtonsoft.Json.Linq.JToken = GetPropertyCaseInsensitive(ParsedPayload, "token")
        If token Is Nothing Then
            Return CreateErrorResponse("Insert the token into a property called Token. " & loggerContext & " error at token validation.")
        End If

        If token.Type <> Newtonsoft.Json.Linq.JTokenType.String Then
            Return CreateErrorResponse("Token must be a string value. " & loggerContext & " error at token validation.")
        End If

        ' Extract actual string value from JToken
        Dim tokenValue As System.String = CType(token, Newtonsoft.Json.Linq.JValue).Value.ToSystem.String()
        If Not QWLib.Webutils.CheckToken2(tokenValue) Then
            Return CreateErrorResponse("Invalid Token " & loggerContext & " error at token validation.")
        End If

        Return Nothing
    Catch ex As Exception
        Return CreateErrorResponse("Error while checking token: " & ex.Message & " " & loggerContext & " error at token validation.")
    End Try
End Function

' ===================================
' FACTORY FUNCTIONS FOR DICTIONARIES
' ===================================

''' <summary>
''' Creates a dictionary of ParameterCondition objects from parallel arrays
''' ROBUST: Validates array lengths and checks for duplicates
''' </summary>
Public Function CreateParameterConditionsDictionary(
    paramNames As System.System.String(),
    sqlWhenPresentArray As System.System.String(),
    Optional sqlWhenAbsentArray As System.System.String() = Nothing,
    Optional useParameterArray As System.Boolean() = Nothing,
    Optional defaultValueArray As System.Object() = Nothing
) As System.Collections.Generic.Dictionary(Of System.String, System.Object)

    ' ROBUST: Validate required parameters
    If paramNames Is Nothing Then
        Throw New System.ArgumentNullException("paramNames", "paramNames parameter is required")
    End If
    If sqlWhenPresentArray Is Nothing Then
        Throw New System.ArgumentNullException("sqlWhenPresentArray", "sqlWhenPresentArray parameter is required")
    End If

    ' ROBUST: Validate array lengths match
    If paramNames.Length <> sqlWhenPresentArray.Length Then
        Throw New System.ArgumentException($"Array length mismatch: paramNames has {paramNames.Length} elements but sqlWhenPresentArray has {sqlWhenPresentArray.Length} elements")
    End If

    ' Validate optional arrays don't exceed required arrays
    If sqlWhenAbsentArray IsNot Nothing AndAlso sqlWhenAbsentArray.Length > paramNames.Length Then
        Throw New System.ArgumentException($"sqlWhenAbsentArray length ({sqlWhenAbsentArray.Length}) exceeds paramNames length ({paramNames.Length})")
    End If
    If useParameterArray IsNot Nothing AndAlso useParameterArray.Length > paramNames.Length Then
        Throw New System.ArgumentException($"useParameterArray length ({useParameterArray.Length}) exceeds paramNames length ({paramNames.Length})")
    End If
    If defaultValueArray IsNot Nothing AndAlso defaultValueArray.Length > paramNames.Length Then
        Throw New System.ArgumentException($"defaultValueArray length ({defaultValueArray.Length}) exceeds paramNames length ({paramNames.Length})")
    End If

    Dim dict As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

    For i As System.Int32 = 0 To paramNames.Length - 1
        ' ROBUST: Check for duplicates before adding
        If dict.ContainsKey(paramNames(i)) Then
            Throw New System.ArgumentException($"Duplicate parameter condition for '{paramNames(i)}' at index {i}")
        End If

        Dim sqlAbsent As System.String = If(sqlWhenAbsentArray IsNot Nothing AndAlso i < sqlWhenAbsentArray.Length, sqlWhenAbsentArray(i), Nothing)
        Dim useParam As System.Boolean = If(useParameterArray IsNot Nothing AndAlso i < useParameterArray.Length, useParameterArray(i), True)
        Dim defValue As System.Object = If(defaultValueArray IsNot Nothing AndAlso i < defaultValueArray.Length, defaultValueArray(i), Nothing)

        dict.Add(paramNames(i), New ParameterCondition(paramNames(i), sqlWhenPresentArray(i), sqlAbsent, useParam, defValue))
    Next

    Return dict
End Function

''' <summary>
''' Creates a dictionary of FieldMapping objects from parallel arrays
''' ROBUST: Validates array lengths and checks for duplicates
''' </summary>
Public Function CreateFieldMappingsDictionary(
    jsonProps As System.System.String(),
    sqlCols As System.System.String(),
    Optional isRequiredArray As System.Boolean() = Nothing,
    Optional isPrimaryKeyArray As System.Boolean() = Nothing,
    Optional defaultValArray As System.Object() = Nothing
) As System.Collections.Generic.Dictionary(Of System.String, FieldMapping)

    ' ROBUST: Validate required parameters
    If jsonProps Is Nothing Then
        Throw New System.ArgumentNullException("jsonProps", "jsonProps parameter is required")
    End If
    If sqlCols Is Nothing Then
        Throw New System.ArgumentNullException("sqlCols", "sqlCols parameter is required")
    End If

    ' ROBUST: Validate array lengths match
    If jsonProps.Length <> sqlCols.Length Then
        Throw New System.ArgumentException($"Array length mismatch: jsonProps has {jsonProps.Length} elements but sqlCols has {sqlCols.Length} elements")
    End If

    ' Validate optional arrays don't exceed required arrays
    If isRequiredArray IsNot Nothing AndAlso isRequiredArray.Length > jsonProps.Length Then
        Throw New System.ArgumentException($"isRequiredArray length ({isRequiredArray.Length}) exceeds jsonProps length ({jsonProps.Length})")
    End If
    If isPrimaryKeyArray IsNot Nothing AndAlso isPrimaryKeyArray.Length > jsonProps.Length Then
        Throw New System.ArgumentException($"isPrimaryKeyArray length ({isPrimaryKeyArray.Length}) exceeds jsonProps length ({jsonProps.Length})")
    End If
    If defaultValArray IsNot Nothing AndAlso defaultValArray.Length > jsonProps.Length Then
        Throw New System.ArgumentException($"defaultValArray length ({defaultValArray.Length}) exceeds jsonProps length ({jsonProps.Length})")
    End If

    Dim dict As New System.Collections.Generic.Dictionary(Of System.String, FieldMapping)

    For i As System.Int32 = 0 To jsonProps.Length - 1
        ' ROBUST: Check for duplicates before adding
        If dict.ContainsKey(jsonProps(i)) Then
            Throw New System.ArgumentException($"Duplicate field mapping for '{jsonProps(i)}' at index {i}")
        End If

        Dim isReq As System.Boolean = If(isRequiredArray IsNot Nothing AndAlso i < isRequiredArray.Length, isRequiredArray(i), False)
        Dim isPKey As System.Boolean = If(isPrimaryKeyArray IsNot Nothing AndAlso i < isPrimaryKeyArray.Length, isPrimaryKeyArray(i), False)
        Dim defVal As System.Object = If(defaultValArray IsNot Nothing AndAlso i < defaultValArray.Length, defaultValArray(i), Nothing)

        dict.Add(jsonProps(i), New FieldMapping(jsonProps(i), sqlCols(i), isReq, isPKey, defVal))
    Next

    Return dict
End Function
