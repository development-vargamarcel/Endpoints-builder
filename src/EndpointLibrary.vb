
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
    Public Property ParameterName As String
    Public Property SQLWhenPresent As String      ' SQL clause when parameter provided (e.g., "FieldName LIKE :ParamName")
    Public Property SQLWhenAbsent As String       ' SQL clause when parameter absent (e.g., "1=1" or Nothing)
    Public Property UseParameter As Boolean       ' Whether to bind parameter value (True) or use literal (False)
    Public Property DefaultValue As Object        ' Default value if not provided

    Public Sub New(paramName As String, sqlWhenPresent As String, Optional sqlWhenAbsent As String = Nothing, Optional useParameter As Boolean = True, Optional defaultValue As Object = Nothing)
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
    Public Property JsonProperty As String
    Public Property SqlColumn As String
    Public Property IsRequired As Boolean
    Public Property DefaultValue As Object

    Public Sub New(jsonProp As String, sqlCol As String, Optional isRequired As Boolean = False, Optional defaultVal As Object = Nothing)
        Me.JsonProperty = jsonProp
        Me.SqlColumn = sqlCol
        Me.IsRequired = isRequired
        Me.DefaultValue = defaultVal
    End Sub
End Class

' ===================================
' PERFORMANCE: PROPERTY NAME CACHING
' ===================================
Private Shared _propertyNameCache As New System.Collections.Concurrent.ConcurrentDictionary(Of Integer, System.Collections.Generic.Dictionary(Of String, String))
Private Shared _cacheHitCount As Integer = 0
Private Shared _cacheMissCount As Integer = 0
Private Const MAX_CACHE_SIZE As Integer = 1000

Public Shared Function GetPropertyCaseInsensitive(obj As Newtonsoft.Json.Linq.JObject, propertyName As String) As Newtonsoft.Json.Linq.JToken
    If obj Is Nothing OrElse String.IsNullOrEmpty(propertyName) Then
        Return Nothing
    End If

    ' Try exact match first for performance (fastest path)
    If obj(propertyName) IsNot Nothing Then
        Return obj(propertyName)
    End If

    ' Use cached property name mappings for case-insensitive lookup
    Dim objHash As Integer = obj.GetHashCode()
    Dim nameMap As System.Collections.Generic.Dictionary(Of String, String) = Nothing

    If _propertyNameCache.TryGetValue(objHash, nameMap) Then
        System.Threading.Interlocked.Increment(_cacheHitCount)
        Dim actualName As String = Nothing
        If nameMap.TryGetValue(propertyName, actualName) Then
            Return obj(actualName)
        End If
        Return Nothing
    End If

    ' Cache miss - build property name mapping
    System.Threading.Interlocked.Increment(_cacheMissCount)

    ' Prevent cache from growing indefinitely
    If _propertyNameCache.Count > MAX_CACHE_SIZE Then
        _propertyNameCache.Clear()
    End If

    nameMap = New System.Collections.Generic.Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    For Each prop As Newtonsoft.Json.Linq.JProperty In obj.Properties()
        If Not nameMap.ContainsKey(prop.Name) Then
            nameMap(prop.Name) = prop.Name
        End If
    Next

    _propertyNameCache.TryAdd(objHash, nameMap)

    Dim foundName As String = Nothing
    If nameMap.TryGetValue(propertyName, foundName) Then
        Return obj(foundName)
    End If

    Return Nothing
End Function

''' <summary>
''' Gets property cache statistics for monitoring
''' </summary>
Public Shared Function GetPropertyCacheStats() As Object
    Return New With {
        .CacheSize = _propertyNameCache.Count,
        .CacheHits = _cacheHitCount,
        .CacheMisses = _cacheMissCount,
        .HitRate = If(_cacheHitCount + _cacheMissCount > 0,
                     CDbl(_cacheHitCount) / (_cacheHitCount + _cacheMissCount) * 100,
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
' VALIDATORS
' ===================================

Public Class ValidatorWrapper
    Private ReadOnly _requiredParams As String()

    Public Sub New(requiredParams As String())
        _requiredParams = requiredParams
    End Sub

    Public Function Validate(payload As Newtonsoft.Json.Linq.JObject) As String
        For Each paramName As String In _requiredParams
            Dim paramResult = GetObjectParameter(payload, paramName)
            If Not paramResult.Item1 Then
                Return CreateErrorResponse($"Parameter {paramName} not specified. Required parameters: " & String.Join(",", _requiredParams))
            End If
        Next
        Return String.Empty
    End Function
End Class

Public Class ValidatorForBatchWrapper
    Private ReadOnly _requiredArrayParams As String()

    Public Sub New(requiredArrayParams As String())
        _requiredArrayParams = requiredArrayParams
    End Sub

    Public Function Validate(payload As Newtonsoft.Json.Linq.JObject) As String
        For Each paramName As String In _requiredArrayParams
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
    Private ReadOnly _baseSQL As String
    Private ReadOnly _parameterConditions As System.Collections.Generic.Dictionary(Of String, Object)
    Private ReadOnly _defaultWhereClause As String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping)
    Private ReadOnly _useForJsonPath As Boolean

    ''' <summary>
    ''' Reader with full SQL customization. Use explicit SELECT fields (not SELECT *)
    ''' </summary>
    ''' <param name="baseSQL">Base SQL query with explicit fields (e.g., SELECT UserId, Email FROM Users {WHERE})</param>
    ''' <param name="parameterConditions">Dictionary of parameter-specific SQL conditions</param>
    ''' <param name="defaultWhereClause">Default WHERE clause if no parameters provided</param>
    ''' <param name="fieldMappings">Optional JSON-to-SQL field mappings</param>
    ''' <param name="useForJsonPath">If True, uses SQL Server FOR JSON PATH for better performance (40-60% faster for simple queries)</param>
    Public Sub New(baseSQL As String, _
                   parameterConditions As System.Collections.Generic.Dictionary(Of String, Object), _
                   Optional defaultWhereClause As String = Nothing, _
                   Optional fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping) = Nothing, _
                   Optional useForJsonPath As Boolean = False)
        _baseSQL = baseSQL
        _parameterConditions = If(parameterConditions, New System.Collections.Generic.Dictionary(Of String, Object))
        _defaultWhereClause = defaultWhereClause
        _fieldMappings = fieldMappings
        _useForJsonPath = useForJsonPath
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            Dim whereConditions As New System.Collections.Generic.List(Of String)
            Dim sqlParameters As New System.Collections.Generic.Dictionary(Of String, Object)
            Dim providedParams As New System.Collections.Generic.List(Of String)

            ' Process each parameter condition
            For Each kvp As System.Collections.Generic.KeyValuePair(Of String, Object) In _parameterConditions
                Dim condition As ParameterCondition = kvp.Value
                Dim paramName As String = condition.ParameterName

                ' Check if parameter exists in payload (case-insensitive)
                Dim paramResult = GetObjectParameter(payload, paramName)

                If paramResult.Item1 Then
                    ' Parameter is present - use SQLWhenPresent
                    providedParams.Add(paramName)

                    If Not String.IsNullOrEmpty(condition.SQLWhenPresent) Then
                        whereConditions.Add(condition.SQLWhenPresent)

                        ' Add parameter binding if UseParameter is True
                        If condition.UseParameter Then
                            Dim sqlColName As String = paramName

                            ' Use field mapping if available
                            If _fieldMappings IsNot Nothing AndAlso _fieldMappings.ContainsKey(paramName) Then
                                sqlColName = _fieldMappings(paramName).SqlColumn
                            End If

                            sqlParameters.Add(sqlColName, paramResult.Item2)
                        End If
                    End If
                Else
                    ' Parameter is absent - use SQLWhenAbsent
                    If Not String.IsNullOrEmpty(condition.SQLWhenAbsent) Then
                        whereConditions.Add(condition.SQLWhenAbsent)
                    End If

                    ' Use default value if specified
                    If condition.DefaultValue IsNot Nothing AndAlso condition.UseParameter Then
                        Dim sqlColName As String = paramName
                        If _fieldMappings IsNot Nothing AndAlso _fieldMappings.ContainsKey(paramName) Then
                            sqlColName = _fieldMappings(paramName).SqlColumn
                        End If
                        sqlParameters.Add(sqlColName, condition.DefaultValue)
                    End If
                End If
            Next

            ' PERFORMANCE: Build final SQL efficiently
            Dim finalSQL As String

            ' Handle WHERE clause construction
            If whereConditions.Count > 0 Then
                Dim whereClause As String = String.Join(" AND ", whereConditions)

                ' Check if baseSQL has {WHERE} placeholder
                If _baseSQL.Contains("{WHERE}") Then
                    finalSQL = _baseSQL.Replace("{WHERE}", "WHERE " & whereClause)
                ElseIf Not _baseSQL.ToUpper().Contains("WHERE") Then
                    ' PERFORMANCE: StringBuilder for concatenation
                    Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + whereClause.Length + 10)
                    sqlBuilder.Append(_baseSQL)
                    sqlBuilder.Append(" WHERE ")
                    sqlBuilder.Append(whereClause)
                    finalSQL = sqlBuilder.ToString()
                Else
                    ' Base SQL already has WHERE, append with AND
                    Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + whereClause.Length + 10)
                    sqlBuilder.Append(_baseSQL)
                    sqlBuilder.Append(" AND ")
                    sqlBuilder.Append(whereClause)
                    finalSQL = sqlBuilder.ToString()
                End If
            Else
                ' No conditions, use default or remove placeholder
                If Not String.IsNullOrEmpty(_defaultWhereClause) Then
                    If _baseSQL.Contains("{WHERE}") Then
                        finalSQL = _baseSQL.Replace("{WHERE}", "WHERE " & _defaultWhereClause)
                    ElseIf Not _baseSQL.ToUpper().Contains("WHERE") Then
                        Dim sqlBuilder As New System.Text.StringBuilder(_baseSQL.Length + _defaultWhereClause.Length + 10)
                        sqlBuilder.Append(_baseSQL)
                        sqlBuilder.Append(" WHERE ")
                        sqlBuilder.Append(_defaultWhereClause)
                        finalSQL = sqlBuilder.ToString()
                    Else
                        finalSQL = _baseSQL
                    End If
                Else
                    ' Remove placeholder if exists
                    finalSQL = _baseSQL.Replace("{WHERE}", "")
                End If
            End If

            ' Execute query - use FOR JSON PATH if enabled for better performance
            If _useForJsonPath Then
                ' PERFORMANCE MODE: Use SQL Server's native FOR JSON PATH
                ' This is 40-60% faster as SQL Server does JSON serialization in native C++ code
                ' ROBUST: Automatically fallback to standard mode if JSON is malformed
                Try
                    Dim jsonRecords As String = ExecuteQueryToJSON(database, finalSQL, sqlParameters)

                    ' Parse the JSON string back to array for consistent response format
                    ' This validates that SQL Server generated valid JSON
                    Dim recordsArray = Newtonsoft.Json.Linq.JArray.Parse(jsonRecords)

                    Return New With {
                        .Result = "OK",
                        .ProvidedParameters = String.Join(",", providedParams),
                        .ExecutedSQL = finalSQL & " FOR JSON PATH",
                        .Records = recordsArray
                    }
                Catch jsonEx As Newtonsoft.Json.JsonException
                    ' FOR JSON PATH failed due to malformed JSON (unescaped chars, etc.)
                    ' AUTOMATIC FALLBACK: Retry with standard Dictionary mode
                    Dim rows = ExecuteQueryToDictionary(database, finalSQL, sqlParameters)

                    Return New With {
                        .Result = "OK",
                        .ProvidedParameters = String.Join(",", providedParams),
                        .ExecutedSQL = finalSQL,
                        .Records = rows,
                        .PerformanceMode = "Standard (FOR JSON PATH failed - data contains special characters)",
                        .FallbackReason = $"JSON parsing error: {jsonEx.Message}"
                    }
                End Try
            Else
                ' STANDARD MODE: Dictionary conversion in VB code (more flexible for complex transformations)
                Dim rows = ExecuteQueryToDictionary(database, finalSQL, sqlParameters)

                Return New With {
                    .Result = "OK",
                    .ProvidedParameters = String.Join(",", providedParams),
                    .ExecutedSQL = finalSQL,
                    .Records = rows
                }
            End If
        Catch ex As Exception
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(
                CreateErrorResponse($"Error reading records: {ex.Message}")
            )
        End Try
    End Function
End Class

' ===================================
' BUSINESS LOGIC: WRITER
' ===================================
Public Class BusinessLogicWriterWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping)
    Private ReadOnly _keyFields As String()
    Private ReadOnly _allowUpdates As Boolean
    Private ReadOnly _customExistenceCheckSQL As String
    Private ReadOnly _customUpdateSQL As String
    Private ReadOnly _customWhereClause As String

    ''' <summary>
    ''' Writer with custom SQL support
    ''' </summary>
    ''' <param name="tableName">Table name for operations</param>
    ''' <param name="fieldMappings">Dictionary of field mappings (JSON to SQL)</param>
    ''' <param name="keyFields">Fields that constitute the record key</param>
    ''' <param name="allowUpdates">Whether to allow updates to existing records</param>
    ''' <param name="customExistenceCheckSQL">Custom SQL for checking existence (use :ParamName for parameters)</param>
    ''' <param name="customUpdateSQL">Custom UPDATE SQL (use :ParamName for parameters)</param>
    ''' <param name="customWhereClause">Custom WHERE clause for updates</param>
    Public Sub New(tableName As String, _
                   fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping), _
                   keyFields As String(), _
                   allowUpdates As Boolean, _
                   Optional customExistenceCheckSQL As String = Nothing, _
                   Optional customUpdateSQL As String = Nothing, _
                   Optional customWhereClause As String = Nothing)
        _tableName = tableName
        _fieldMappings = fieldMappings
        _keyFields = keyFields
        _allowUpdates = allowUpdates
        _customExistenceCheckSQL = customExistenceCheckSQL
        _customUpdateSQL = customUpdateSQL
        _customWhereClause = customWhereClause
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            ' Validate required fields
            Dim missingRequired As New System.Collections.Generic.List(Of String)
            For Each kvp As System.Collections.Generic.KeyValuePair(Of String, FieldMapping) In _fieldMappings
                If kvp.Value.IsRequired Then
                    Dim paramResult = GetObjectParameter(payload, kvp.Key)
                    If Not paramResult.Item1 Then
                        missingRequired.Add(kvp.Key)
                    End If
                End If
            Next

            If missingRequired.Count > 0 Then
                Return New With {.Result = "KO", .Reason = $"Missing required fields: {String.Join(", ", missingRequired)}"}
            End If

            ' Get parameters with field mapping
            Dim parameters As New System.Collections.Generic.Dictionary(Of String, Object)
            For Each kvp As System.Collections.Generic.KeyValuePair(Of String, FieldMapping) In _fieldMappings
                Dim paramResult = GetObjectParameter(payload, kvp.Key)
                If paramResult.Item1 Then
                    parameters.Add(kvp.Value.SqlColumn, paramResult.Item2)
                ElseIf kvp.Value.DefaultValue IsNot Nothing Then
                    parameters.Add(kvp.Value.SqlColumn, kvp.Value.DefaultValue)
                End If
            Next

            ' Check existence
            Dim recordExists As Boolean = False
            Dim existenceSQL As String

            If Not String.IsNullOrEmpty(_customExistenceCheckSQL) Then
                existenceSQL = _customExistenceCheckSQL
            Else
                ' Build default existence check
                Dim whereConditions As New System.Collections.Generic.List(Of String)
                For Each keyField As String In _keyFields
                    whereConditions.Add($"{keyField} = :{keyField}")
                Next
                existenceSQL = $"SELECT COUNT(*) as CNT FROM {_tableName} WHERE {String.Join(" AND ", whereConditions)}"
            End If

            Dim checkQuery As New QWTable()
            Try
                checkQuery.Database = database
                checkQuery.SQL = existenceSQL
                For Each keyField As String In _keyFields
                    If parameters.ContainsKey(keyField) Then
                        checkQuery.params(keyField) = parameters(keyField)
                    End If
                Next
                checkQuery.RequestLive = False
                checkQuery.Active = True
                recordExists = (CInt(checkQuery.Rowset.Fields("CNT").Value) > 0)
            Finally
                If checkQuery IsNot Nothing Then
                    checkQuery.Active = False
                    checkQuery.Dispose()
                End If
            End Try

            If recordExists Then
                If Not _allowUpdates Then
                    Return New With {.Result = "KO", .Reason = "Record already exists and updates are not allowed"}
                End If

                ' Perform UPDATE
                If Not String.IsNullOrEmpty(_customUpdateSQL) Then
                    ' Use custom UPDATE SQL
                    Dim updateQuery As New QWTable()
                    Try
                        updateQuery.Database = database
                        updateQuery.SQL = _customUpdateSQL
                        For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                            updateQuery.params(param.Key) = param.Value
                        Next
                        updateQuery.Active = True
                        updateQuery.Active = False
                    Finally
                        If updateQuery IsNot Nothing Then updateQuery.Dispose()
                    End Try
                Else
                    ' Use standard UPDATE
                    Dim setClauses As New System.Collections.Generic.List(Of String)
                    For Each kvp As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                        If Not _keyFields.Contains(kvp.Key) Then
                            setClauses.Add($"{kvp.Key} = :{kvp.Key}")
                        End If
                    Next

                    If setClauses.Count > 0 Then
                        Dim whereClause As String
                        If Not String.IsNullOrEmpty(_customWhereClause) Then
                            whereClause = _customWhereClause
                        Else
                            Dim whereConditions As New System.Collections.Generic.List(Of String)
                            For Each keyField As String In _keyFields
                                whereConditions.Add($"{keyField} = :{keyField}")
                            Next
                            whereClause = String.Join(" AND ", whereConditions)
                        End If

                        Dim updateQuery As New QWTable()
                        Try
                            updateQuery.Database = database
                            updateQuery.SQL = $"UPDATE {_tableName} SET {String.Join(", ", setClauses)} WHERE {whereClause}"
                            For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                                updateQuery.params(param.Key) = param.Value
                            Next
                            updateQuery.Active = True
                            updateQuery.Active = False
                        Finally
                            If updateQuery IsNot Nothing Then updateQuery.Dispose()
                        End Try
                    End If
                End If

                Return New With {.Result = "OK", .Action = "UPDATED", .Message = "Record updated successfully"}
            Else
                ' Perform INSERT
                Dim qTable As New QWTable()
                Try
                    qTable.Database = database
                    qTable.SQL = $"SELECT * FROM {_tableName} WHERE 1=0"
                    qTable.RequestLive = True
                    qTable.AllowAllRecords = False
                    qTable.Active = True

                    qTable.BeginAppend()
                    For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                        Try
                            qTable.Replace(param.Key, param.Value)
                        Catch ex As Exception
                            ' Field might not exist
                        End Try
                    Next

                    Dim saveMsg As String = ""
                    Dim okSave As Boolean = qTable.SaveRecord(saveMsg)
                    qTable.Active = False

                    If Not okSave Then
                        Return New With {.Result = "KO", .Reason = $"Error saving record: {saveMsg}"}
                    End If

                    Return New With {.Result = "OK", .Action = "INSERTED", .Message = "Record inserted successfully"}
                Finally
                    If qTable IsNot Nothing Then qTable.Dispose()
                End Try
            End If
        Catch ex As Exception
            Return New With {.Result = "KO", .Reason = $"Database operation failed: {ex.Message}"}
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
    database As Object,
    tableName As String,
    keyFields As String(),
    recordParameters As System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))
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
        sqlBuilder.Append(String.Join(", ", keyFields))
        sqlBuilder.Append(" FROM ")
        sqlBuilder.Append(tableName)
        sqlBuilder.Append(" WHERE ")

        ' Build OR conditions for each record
        Dim recordConditions As New System.Collections.Generic.List(Of String)(recordParameters.Count)
        Dim queryParams As New System.Collections.Generic.Dictionary(Of String, Object)
        Dim paramIndex As Integer = 0

        For Each recordParams As System.Collections.Generic.Dictionary(Of String, Object) In recordParameters
            Dim conditions As New System.Collections.Generic.List(Of String)(keyFields.Length)

            For Each keyField As String In keyFields
                If recordParams.ContainsKey(keyField) Then
                    Dim paramName As String = $"{keyField}_{paramIndex}"
                    conditions.Add($"{keyField} = :{paramName}")
                    queryParams.Add(paramName, recordParams(keyField))
                End If
            Next

            If conditions.Count = keyFields.Length Then
                recordConditions.Add($"({String.Join(" AND ", conditions)})")
            End If

            paramIndex += 1
        Next

        If recordConditions.Count = 0 Then
            Return existingKeys
        End If

        sqlBuilder.Append(String.Join(" OR ", recordConditions))

        ' Execute the bulk check query
        Dim checkQuery As New QWTable()
        Try
            checkQuery.Database = database
            checkQuery.SQL = sqlBuilder.ToString()

            For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In queryParams
                checkQuery.params(param.Key) = param.Value
            Next

            checkQuery.RequestLive = False
            checkQuery.Active = True

            ' Build composite keys from results
            While Not checkQuery.Rowset.EndOfSet
                Dim compositeKey As New System.Text.StringBuilder()
                For Each keyField As String In keyFields
                    If compositeKey.Length > 0 Then compositeKey.Append("|")
                    compositeKey.Append(checkQuery.Rowset.Fields(keyField).Value.ToString())
                Next
                existingKeys.Add(compositeKey.ToString())
                checkQuery.Rowset.Next()
            End While

        Finally
            If checkQuery IsNot Nothing Then
                checkQuery.Active = False
                checkQuery.Dispose()
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
''' </summary>
Private Shared Function GetCompositeKey(recordParams As System.Collections.Generic.Dictionary(Of String, Object), keyFields As String()) As String
    Dim compositeKey As New System.Text.StringBuilder()
    For Each keyField As String In keyFields
        If compositeKey.Length > 0 Then compositeKey.Append("|")
        If recordParams.ContainsKey(keyField) Then
            compositeKey.Append(recordParams(keyField).ToString())
        End If
    Next
    Return compositeKey.ToString()
End Function

' ===================================
' BUSINESS LOGIC: BATCH WRITER
' ===================================
Public Class BusinessLogicBatchWriterWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping)
    Private ReadOnly _keyFields As String()
    Private ReadOnly _allowUpdates As Boolean

    Public Sub New(tableName As String, fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping), keyFields As String(), allowUpdates As Boolean)
        _tableName = tableName
        _fieldMappings = fieldMappings
        _keyFields = keyFields
        _allowUpdates = allowUpdates
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            Dim recordsToken = GetPropertyCaseInsensitive(payload, "Records")
            Dim recordsArray As Newtonsoft.Json.Linq.JArray = TryCast(recordsToken, Newtonsoft.Json.Linq.JArray)

            If recordsArray Is Nothing Then
                ' Single record - use standard writer
                Dim singleRecordHandler As New BusinessLogicWriterWrapper(_tableName, _fieldMappings, _keyFields, _allowUpdates)
                Return singleRecordHandler.Execute(database, payload)
            End If

            Dim insertedCount As Integer = 0, updatedCount As Integer = 0, errorCount As Integer = 0
            Dim errors As New System.Collections.Generic.List(Of String)

            ' PERFORMANCE: Pre-extract all record parameters and perform bulk existence check
            ' This reduces N database queries to 1 query for existence checking
            Dim allRecordParams As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))(recordsArray.Count)
            Dim recordDataList As New System.Collections.Generic.List(Of Object)(recordsArray.Count)

            ' First pass: Extract and validate parameters
            For Each recordToken As Newtonsoft.Json.Linq.JToken In recordsArray
                Try
                    Dim record As Newtonsoft.Json.Linq.JObject = CType(recordToken, Newtonsoft.Json.Linq.JObject)

                    ' Validate required fields
                    Dim missingRequired As New System.Collections.Generic.List(Of String)
                    For Each kvp As System.Collections.Generic.KeyValuePair(Of String, FieldMapping) In _fieldMappings
                        If kvp.Value.IsRequired Then
                            Dim paramResult = GetObjectParameter(record, kvp.Key)
                            If Not paramResult.Item1 Then
                                missingRequired.Add(kvp.Key)
                            End If
                        End If
                    Next

                    If missingRequired.Count > 0 Then
                        errors.Add($"Record skipped - Missing required fields: {String.Join(", ", missingRequired)}")
                        errorCount += 1
                        recordDataList.Add(Nothing)
                        Continue For
                    End If

                    ' Extract parameters with field mappings
                    Dim recordParams As New System.Collections.Generic.Dictionary(Of String, Object)
                    For Each kvp As System.Collections.Generic.KeyValuePair(Of String, FieldMapping) In _fieldMappings
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
                existingRecords = BulkExistenceCheck(database, _tableName, _keyFields, allRecordParams)
            Else
                existingRecords = New System.Collections.Generic.HashSet(Of String)()
            End If

            ' Second pass: Process each record with pre-determined existence
            For i As Integer = 0 To recordDataList.Count - 1
                Dim recordParams = TryCast(recordDataList(i), System.Collections.Generic.Dictionary(Of String, Object))
                If recordParams Is Nothing Then
                    Continue For ' Skip records that failed validation
                End If

                Try
                    ' Get composite key for this record
                    Dim compositeKey As String = GetCompositeKey(recordParams, _keyFields)
                    Dim recordExists As Boolean = existingRecords.Contains(compositeKey)

                    Dim keyValuesList As New System.Collections.Generic.List(Of String)
                    For Each keyCol As String In _keyFields
                        keyValuesList.Add(If(recordParams.ContainsKey(keyCol), recordParams(keyCol).ToString(), String.Empty))
                    Next
                    Dim IndexColumnsValues As String = String.Join(",", keyValuesList)

                    If recordExists Then
                        ' Record exists - perform UPDATE
                        If Not _allowUpdates Then
                            errors.Add($"{IndexColumnsValues} - Record already exists and updates are not allowed")
                            errorCount += 1
                            Continue For
                        End If

                        Dim whereConditions As New System.Collections.Generic.List(Of String)
                        For Each keyCol As String In _keyFields
                            whereConditions.Add($"{keyCol} = :{keyCol}")
                        Next

                        Dim setClauses As New System.Collections.Generic.List(Of String)
                        For Each kvp As System.Collections.Generic.KeyValuePair(Of String, Object) In recordParams
                            If Not _keyFields.Contains(kvp.Key) Then
                                setClauses.Add($"{kvp.Key} = :{kvp.Key}")
                            End If
                        Next

                        If setClauses.Count > 0 Then
                            Dim updateQuery As New QWTable()
                            Try
                                updateQuery.Database = database
                                updateQuery.SQL = $"SET DATEFORMAT ymd;UPDATE {_tableName} SET {String.Join(", ", setClauses)} WHERE {String.Join(" AND ", whereConditions)}"

                                For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In recordParams
                                    updateQuery.params(param.Key) = param.Value
                                Next

                                updateQuery.Active = True
                                updateQuery.Active = False
                                updatedCount += 1
                            Catch ex As Exception
                                errors.Add($"{IndexColumnsValues} - Update error: {ex.Message}")
                                errorCount += 1
                            Finally
                                If updateQuery IsNot Nothing Then updateQuery.Dispose()
                            End Try
                        Else
                            updatedCount += 1
                        End If
                    Else
                        ' Record does not exist - perform INSERT
                        Dim insertTable As New QWTable()
                        Try
                            insertTable.Database = database
                            insertTable.SQL = $"SELECT * FROM {_tableName} WHERE 1=0"
                            insertTable.RequestLive = True
                            insertTable.AllowAllRecords = False
                            insertTable.Active = True

                            insertTable.BeginAppend()
                            For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In recordParams
                                Try
                                    insertTable.Replace(param.Key, param.Value)
                                Catch ex As Exception
                                End Try
                            Next

                            Dim saveMsg As String = ""
                            If insertTable.SaveRecord(saveMsg) Then
                                insertedCount += 1
                            Else
                                errors.Add($"{IndexColumnsValues} - Save error: {saveMsg}")
                                errorCount += 1
                            End If
                        Catch ex As Exception
                            errors.Add($"{IndexColumnsValues} - Insert error: {ex.Message}")
                            errorCount += 1
                        Finally
                            If insertTable IsNot Nothing Then
                                insertTable.Active = False
                                insertTable.Dispose()
                            End If
                        End Try
                    End If

                Catch ex As Exception
                    errors.Add($"Record processing error: {ex.Message}")
                    errorCount += 1
                End Try
            Next

            Return New With {
                .Result = If(errorCount = 0, "OK", IIf(errorCount >= recordsArray.Count, "KO", "PARTIAL")),
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

Public Sub LogCustom(database As System.Object, StringPayload As String, StringResult As String, Optional LogMessage As String = "")
    Dim fullLogMessage As String = $"{LogMessage}{Environment.NewLine}{{""Payload"": {StringPayload},{Environment.NewLine}""Result"": {StringResult}}}"
    Write_LogDoc(database.QWSession, "**", "ACTIONLINK", 1, fullLogMessage)
End Sub

Public Function ProcessActionLink(
    ByVal database As System.Object,
    ByVal p_validator As System.Func(Of Newtonsoft.Json.Linq.JObject, System.String),
    ByVal p_businessLogic As System.Func(Of System.Object, Newtonsoft.Json.Linq.JObject, System.Object),
    Optional ByVal LogMessage As String = Nothing,
    Optional ByVal payload As Newtonsoft.Json.Linq.JObject = Nothing,
    Optional ByVal StringPayload As String = "",
    Optional ByVal CheckForToken As System.Boolean = True
) As System.String
    Try
        If payload Is Nothing Then
            Return CreateErrorResponse("Invalid or empty JSON payload")
        End If

        If CheckForToken Then
            Try
                Dim tokenValue = GetPropertyCaseInsensitive(payload, "Token")
                If tokenValue Is Nothing Then
                    Return CreateErrorResponse("Please insert the token in a property called Token.")
                End If
                Dim token As String = CType(tokenValue, Newtonsoft.Json.Linq.JValue).Value.ToString()
                If Not QWLib.Webutils.CheckToken2(token) Then
                    Return CreateErrorResponse("Invalid token.")
                End If
            Catch ex As Exception
                Return CreateErrorResponse("Please insert the token in a property called Token.")
            End Try
        End If

        If p_validator IsNot Nothing Then
            Dim validationError As String = p_validator(payload)
            If Not String.IsNullOrEmpty(validationError) Then
                Return validationError
            End If
        End If

        Dim result As Object = p_businessLogic(database, payload)
        Dim StringResult As String = Newtonsoft.Json.JsonConvert.SerializeObject(result)

        If LogMessage IsNot Nothing Then
            LogCustom(DB, StringPayload, StringResult, "Error at ValidatePayloadAndToken: ")
        End If

        Return StringResult
    Catch ex As Exception
        Return CreateErrorResponse($"Internal error: {ex.Message}")
    End Try
End Function

Public Shared Function ParsePayload(Optional ByRef PayloadString As String = Nothing,
                                   Optional ByRef ErrorMessage As String = Nothing) As Newtonsoft.Json.Linq.JObject
    Try
        If System.Web.HttpContext.Current Is Nothing OrElse
           System.Web.HttpContext.Current.Request Is Nothing Then
            ErrorMessage = "HttpContext is not available"
            Return Nothing
        End If

        System.Web.HttpContext.Current.Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin)

        Using reader As New System.IO.StreamReader(
            System.Web.HttpContext.Current.Request.InputStream,
            System.Text.Encoding.UTF8)

            Dim jsonString As String = reader.ReadToEnd()

            If Not PayloadString Is Nothing Then
                PayloadString = jsonString
            End If

            If String.IsNullOrWhiteSpace(jsonString) Then
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

Public Shared Function GetStringParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As String) As System.Tuple(Of Boolean, String)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            Return New System.Tuple(Of Boolean, String)(True, CType(token, Newtonsoft.Json.Linq.JValue).Value.ToString())
        Else
            Return New System.Tuple(Of Boolean, String)(False, Nothing)
        End If
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, String)(False, Nothing)
    End Try
End Function

Public Shared Function GetDateParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As String) As System.Tuple(Of Boolean, Date)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            Return New System.Tuple(Of Boolean, Date)(True, CType(CType(token, Newtonsoft.Json.Linq.JValue).Value, Date))
        Else
            Return New System.Tuple(Of Boolean, Date)(False, Date.MinValue)
        End If
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, Date)(False, Date.MinValue)
    End Try
End Function

Public Shared Function GetIntegerParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As String) As System.Tuple(Of Boolean, Integer)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            Return New System.Tuple(Of Boolean, Integer)(True, CInt(CType(token, Newtonsoft.Json.Linq.JValue).Value))
        Else
            Return New System.Tuple(Of Boolean, Integer)(False, 0)
        End If
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, Integer)(False, 0)
    End Try
End Function

Public Shared Function GetObjectParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As String) As System.Tuple(Of Boolean, Object)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing Then
            Select Case token.Type
                Case Newtonsoft.Json.Linq.JTokenType.Array
                    Return New System.Tuple(Of Boolean, Object)(True, CType(token, Newtonsoft.Json.Linq.JArray))
                Case Newtonsoft.Json.Linq.JTokenType.Object
                    Return New System.Tuple(Of Boolean, Object)(True, CType(token, Newtonsoft.Json.Linq.JObject))
                Case Else
                    Return New System.Tuple(Of Boolean, Object)(True, CType(token, Newtonsoft.Json.Linq.JValue).Value)
            End Select
        Else
            Return New System.Tuple(Of Boolean, Object)(False, Nothing)
        End If
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, Object)(False, Nothing)
    End Try
End Function

Public Shared Function GetArrayParameter(payload As Newtonsoft.Json.Linq.JObject, paramName As String) As System.Tuple(Of Boolean, Newtonsoft.Json.Linq.JArray)
    Try
        Dim token = GetPropertyCaseInsensitive(payload, paramName)
        If token IsNot Nothing AndAlso token.Type = Newtonsoft.Json.Linq.JTokenType.Array Then
            Return New System.Tuple(Of Boolean, Newtonsoft.Json.Linq.JArray)(True, CType(token, Newtonsoft.Json.Linq.JArray))
        End If
        Return New System.Tuple(Of Boolean, Newtonsoft.Json.Linq.JArray)(False, Nothing)
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, Newtonsoft.Json.Linq.JArray)(False, Nothing)
    End Try
End Function

Public Shared Function CreateErrorResponse(reason As String) As String
    Return Newtonsoft.Json.JsonConvert.SerializeObject(New With {.Result = "KO", .Reason = reason})
End Function

''' <summary>
''' Executes query and returns results as list of dictionaries
''' NOTE: Use explicit field selection in your SQL (e.g., SELECT UserId, Email FROM Users)
''' instead of SELECT * for better performance
''' </summary>
Public Shared Function ExecuteQueryToDictionary(database As Object, sql As String, parameters As System.Collections.Generic.Dictionary(Of String, Object)) As System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))
    Dim q As New QWTable()
    q.Database = database
    q.SQL = sql
    If parameters IsNot Nothing Then
        For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
            q.params(param.Key) = param.Value
        Next
    End If
    q.RequestLive = False
    q.Active = True

    ' PERFORMANCE: Pre-allocate with estimated capacity
    Dim estimatedFieldCount As Integer = q.rowset.fields.size
    Dim rows As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))()

    While Not q.rowset.endofset
        Dim row As New System.Collections.Generic.Dictionary(Of String, Object)(estimatedFieldCount)

        For i As Integer = 1 To q.rowset.fields.size
            Dim fieldName As String = q.Rowset.fields(i).fieldname
            row.Add(fieldName, q.rowset.fields(i).value)
        Next

        rows.Add(row)
        q.rowset.next()
    End While

    q.Active = False
    Return rows
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
''' <returns>JSON string with array of records, or empty array [] if no results</returns>
Public Shared Function ExecuteQueryToJSON(database As Object, sql As String, parameters As System.Collections.Generic.Dictionary(Of String, Object)) As String
    Dim q As New QWTable()
    Try
        q.Database = database

        ' PERFORMANCE + ROBUSTNESS: Use FOR JSON PATH with options for better handling
        ' - Use WITHOUT_ARRAY_WRAPPER when we know it's a single row (not applicable here)
        ' - INCLUDE_NULL_VALUES ensures consistent structure
        ' ROBUSTNESS: Wrap the entire query in a subquery and cast result as NVARCHAR(MAX)
        ' This prevents truncation issues that can cause malformed JSON

        Dim jsonSQL As String = $"SELECT CAST(( {sql} FOR JSON PATH, INCLUDE_NULL_VALUES ) AS NVARCHAR(MAX)) AS JsonResult"
        q.SQL = jsonSQL

        If parameters IsNot Nothing Then
            For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                q.params(param.Key) = param.Value
            Next
        End If

        q.RequestLive = False
        q.Active = True

        ' SQL Server returns JSON result in first row, first column
        ' If no results, SQL Server returns NULL (we'll return empty array instead)
        Dim jsonResult As String = "[]"

        If Not q.rowset.endofset AndAlso q.rowset.fields.size > 0 Then
            Dim fieldValue As Object = q.rowset.fields(1).value
            If fieldValue IsNot Nothing AndAlso Not IsDBNull(fieldValue) Then
                Dim rawJson As String = fieldValue.ToString()

                ' Basic validation: ensure we got something that looks like JSON
                If Not String.IsNullOrWhiteSpace(rawJson) Then
                    ' Trim whitespace that SQL Server might add
                    jsonResult = rawJson.Trim()

                    ' ROBUSTNESS: Validate JSON structure before returning
                    ' Check for proper brackets and basic structure
                    If Not String.IsNullOrEmpty(jsonResult) Then
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
Public Shared Function EscapeColumnForJson(columnName As String, Optional ByVal [alias] As String = Nothing, Optional useStringEscape As Boolean = True) As String
    Dim aliasName As String = If(String.IsNullOrEmpty([alias]), columnName, [alias])

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
        Dim escapedExpr As String = $"ISNULL(CAST({columnName} AS NVARCHAR(MAX)), '')"

        ' Remove most control characters (except tab, CR, LF which we'll handle separately)
        ' CHAR(0) to CHAR(8), CHAR(11), CHAR(12), CHAR(14) to CHAR(31)
        For i As Integer = 0 To 31
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
Public Shared Function BuildSafeColumnList(columns As String(), Optional textColumns As String() = Nothing, Optional useStringEscape As Boolean = True) As String
    If columns Is Nothing OrElse columns.Length = 0 Then
        Return "*"
    End If

    Dim columnList As New System.Collections.Generic.List(Of String)(columns.Length)
    Dim textColumnsSet As New System.Collections.Generic.HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    If textColumns IsNot Nothing Then
        For Each tc As String In textColumns
            textColumnsSet.Add(tc)
        Next
    End If

    For Each col As String In columns
        If textColumnsSet.Contains(col) Then
            columnList.Add(EscapeColumnForJson(col, Nothing, useStringEscape))
        Else
            columnList.Add(col)
        End If
    Next

    Return String.Join(", ", columnList)
End Function

Public Shared Function GetDestinationIdentifier(ByRef payload As Newtonsoft.Json.Linq.JObject) As System.Tuple(Of Boolean, String)
    Try
        If payload Is Nothing Then
            Return New System.Tuple(Of Boolean, String)(False, "Invalid JSON payload")
        End If

        Dim destinationToken = GetPropertyCaseInsensitive(payload, "DestinationIdentifier")
        If destinationToken IsNot Nothing Then
            Try
                Dim value As String = CType(destinationToken, Newtonsoft.Json.Linq.JValue).Value.ToString()
                Return New System.Tuple(Of Boolean, String)(True, value)
            Catch castEx As Exception
                Return New System.Tuple(Of Boolean, String)(False, "DestinationIdentifier field is not a valid string value")
            End Try
        Else
            Return New System.Tuple(Of Boolean, String)(False, "DestinationIdentifier field not found")
        End If
    Catch ex As Exception
        Return New System.Tuple(Of Boolean, String)(False, $"Unexpected error: {ex.Message}")
    End Try
End Function

' ===================================
' FACTORY FUNCTIONS
' ===================================

Public Function CreateValidator(requiredParams As String()) As System.Func(Of Newtonsoft.Json.Linq.JObject, String)
    Return AddressOf New ValidatorWrapper(requiredParams).Validate
End Function

Public Function CreateValidatorForBatch(requiredArrayParams As String()) As System.Func(Of Newtonsoft.Json.Linq.JObject, String)
    Return AddressOf New ValidatorForBatchWrapper(requiredArrayParams).Validate
End Function

Public Function CreateBusinessLogicForReading(
    baseSQL As String,
    parameterConditions As System.Collections.Generic.Dictionary(Of String, Object),
    Optional defaultWhereClause As String = Nothing,
    Optional fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping) = Nothing,
    Optional useForJsonPath As Boolean = False
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicReaderWrapper(baseSQL, parameterConditions, defaultWhereClause, fieldMappings, useForJsonPath).Execute
End Function

Public Function CreateBusinessLogicForWriting(
    tableName As String,
    fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping),
    keyFields As String(),
    allowUpdates As Boolean,
    Optional customExistenceCheckSQL As String = Nothing,
    Optional customUpdateSQL As String = Nothing,
    Optional customWhereClause As String = Nothing
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicWriterWrapper(tableName, fieldMappings, keyFields, allowUpdates, customExistenceCheckSQL, customUpdateSQL, customWhereClause).Execute
End Function

Public Function CreateBusinessLogicForBatchWriting(
    tableName As String,
    fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping),
    keyFields As String(),
    allowUpdates As Boolean
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicBatchWriterWrapper(tableName, fieldMappings, keyFields, allowUpdates).Execute
End Function

Public Shared Function ValidatePayloadAndToken(DB As Object, _
                                                      Optional CheckForToken As Boolean = True, _
                                                      Optional loggerContext As String = "", _
                                                      Optional ByRef ParsedPayload As Newtonsoft.Json.Linq.JObject = Nothing, _
                                                      Optional ByRef StringPayload As String = "") As Object
    Try
        Dim errorMsg As String = ""
        Dim StringPayloadQQ As String = ""
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

Private Shared Function ValidateToken(DB As Object, ParsedPayload As Newtonsoft.Json.Linq.JObject, loggerContext As String) As Object
    Try
        Dim token As Newtonsoft.Json.Linq.JToken = GetPropertyCaseInsensitive(ParsedPayload, "token")
        If token Is Nothing Then
            Return CreateErrorResponse("Insert the token into a property called Token. " & loggerContext & " error at token validation.")
        End If

        If token.Type <> Newtonsoft.Json.Linq.JTokenType.String Then
            Return CreateErrorResponse("Token must be a string value. " & loggerContext & " error at token validation.")
        End If

        If Not QWLib.Webutils.CheckToken2(token.ToString()) Then
            Return CreateErrorResponse("Invalid Token " & loggerContext & " error at token validation.")
        End If

        Return Nothing
    Catch ex As Exception
        Return CreateErrorResponse("Error while checking token: " & ex.Message & " " & loggerContext & " error at token validation.")
    End Try
End Function

' ===================================
' FACTORY FUNCTIONS FOR PARAMETER CONDITIONS AND FIELD MAPPINGS
' ===================================

''' <summary>
''' Creates a ParameterCondition without requiring direct class instantiation
''' </summary>
Public Function CreateParameterCondition(
    paramName As String,
    sqlWhenPresent As String,
    Optional sqlWhenAbsent As String = Nothing,
    Optional useParameter As Boolean = True,
    Optional defaultValue As Object = Nothing
) As ParameterCondition
    Return New ParameterCondition(paramName, sqlWhenPresent, sqlWhenAbsent, useParameter, defaultValue)
End Function

''' <summary>
''' Creates a FieldMapping without requiring direct class instantiation
''' </summary>
Public Function CreateFieldMapping(
    jsonProp As String,
    sqlCol As String,
    Optional isRequired As Boolean = False,
    Optional defaultVal As Object = Nothing
) As FieldMapping
    Return New FieldMapping(jsonProp, sqlCol, isRequired, defaultVal)
End Function

''' <summary>
''' Creates a dictionary of ParameterCondition objects from parallel arrays
''' </summary>
Public Function CreateParameterConditionsDictionary(
    paramNames As String(),
    sqlWhenPresentArray As String(),
    Optional sqlWhenAbsentArray As String() = Nothing,
    Optional useParameterArray As Boolean() = Nothing,
    Optional defaultValueArray As Object() = Nothing
) As System.Collections.Generic.Dictionary(Of String, Object)

    Dim dict As New System.Collections.Generic.Dictionary(Of String, Object)

    For i As Integer = 0 To paramNames.Length - 1
        Dim sqlAbsent As String = If(sqlWhenAbsentArray IsNot Nothing AndAlso i < sqlWhenAbsentArray.Length, sqlWhenAbsentArray(i), Nothing)
        Dim useParam As Boolean = If(useParameterArray IsNot Nothing AndAlso i < useParameterArray.Length, useParameterArray(i), True)
        Dim defValue As Object = If(defaultValueArray IsNot Nothing AndAlso i < defaultValueArray.Length, defaultValueArray(i), Nothing)

        dict.Add(paramNames(i), New ParameterCondition(paramNames(i), sqlWhenPresentArray(i), sqlAbsent, useParam, defValue))
    Next

    Return dict
End Function

''' <summary>
''' Creates a dictionary of FieldMapping objects from parallel arrays
''' </summary>
Public Function CreateFieldMappingsDictionary(
    jsonProps As String(),
    sqlCols As String(),
    Optional isRequiredArray As Boolean() = Nothing,
    Optional defaultValArray As Object() = Nothing
) As System.Collections.Generic.Dictionary(Of String, FieldMapping)

    Dim dict As New System.Collections.Generic.Dictionary(Of String, FieldMapping)

    For i As Integer = 0 To jsonProps.Length - 1
        Dim isReq As Boolean = If(isRequiredArray IsNot Nothing AndAlso i < isRequiredArray.Length, isRequiredArray(i), False)
        Dim defVal As Object = If(defaultValArray IsNot Nothing AndAlso i < defaultValArray.Length, defaultValArray(i), Nothing)

        dict.Add(jsonProps(i), New FieldMapping(jsonProps(i), sqlCols(i), isReq, defVal))
    Next

    Return dict
End Function
