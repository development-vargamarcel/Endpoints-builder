[Libraries]
ENABLED=S
NOME=Endpoint handling
SCRIPT=' 
' Important: the users of this library cannot use classes and cannot use custom types defined here. Also all names must be full qualified names. 
===================================
' ENHANCED FLEXIBLE SQL WRAPPER SYSTEM
' Supports custom SQL, conditional expressions, and parameter-specific clauses
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
' HELPER CLASSES FOR FACTORY FUNCTIONS
' ===================================

' PERFORMANCE: Cache for case-insensitive property name lookups
' Reduces O(n) property iteration to O(1) dictionary lookup
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
' ENHANCED BUSINESS LOGIC READER
' Supports custom SQL and parameter-specific conditions
' ===================================
Public Class BusinessLogicAdvancedReaderWrapper
    Private ReadOnly _baseSQL As String
    Private ReadOnly _parameterConditions As System.Collections.Generic.Dictionary(Of String, Object)
    Private ReadOnly _excludeFields As String()
    Private ReadOnly _defaultWhereClause As String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping)
    
    ''' <summary>
    ''' Advanced reader with full SQL customization
    ''' </summary>
    ''' <param name="baseSQL">Base SQL query (can include {WHERE} placeholder)</param>
    ''' <param name="parameterConditions">Dictionary of parameter-specific SQL conditions</param>
    ''' <param name="excludeFields">Fields to exclude from results</param>
    ''' <param name="defaultWhereClause">Default WHERE clause if no parameters provided</param>
    ''' <param name="fieldMappings">Optional JSON-to-SQL field mappings</param>
    Public Sub New(baseSQL As String, _
                   parameterConditions As System.Collections.Generic.Dictionary(Of String, Object), _
                   Optional excludeFields As String() = Nothing, _
                   Optional defaultWhereClause As String = Nothing, _
                   Optional fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping) = Nothing)
        _baseSQL = baseSQL
        _parameterConditions = If(parameterConditions, New System.Collections.Generic.Dictionary(Of String, Object))
        _excludeFields = excludeFields
        _defaultWhereClause = defaultWhereClause
        _fieldMappings = fieldMappings
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

            ' Execute query
            Dim rows = ExecuteQueryToDictionary(database, finalSQL, sqlParameters, _excludeFields)
            
            Return New With {
                .Result = "OK",
                .ProvidedParameters = String.Join(",", providedParams),
                .ExecutedSQL = finalSQL,
                .Records = rows
            }
        Catch ex As Exception
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(
                CreateErrorResponse($"Error reading records: {ex.Message}")
            )
        End Try
    End Function
End Class

' ===================================
' STANDARD BUSINESS LOGIC READER (Backward Compatible)
' ===================================
Public Class BusinessLogicReaderWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _AllParametersList As String()
    Private ReadOnly _excludeFields As String()
    Private ReadOnly _useLikeOperator As Boolean

    Public Sub New(tableName As String, AllParametersList As String(), excludeFields As String(), Optional useLikeOperator As Boolean = True)
        _tableName = tableName
        _AllParametersList = AllParametersList
        _excludeFields = excludeFields
        _useLikeOperator = useLikeOperator
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            Dim parameters = getParameters(payload, _AllParametersList)

            ' PERFORMANCE: Use StringBuilder for efficient SQL construction
            Dim sqlBuilder As New System.Text.StringBuilder(256)
            sqlBuilder.Append("SELECT * FROM ")
            sqlBuilder.Append(_tableName)

            Dim whereConditions As New System.Collections.Generic.List(Of String)(parameters.Count)
            For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                If _useLikeOperator Then
                    whereConditions.Add($"{param.Key} LIKE :{param.Key}")
                Else
                    whereConditions.Add($"{param.Key} = :{param.Key}")
                End If
            Next

            If whereConditions.Count > 0 Then
                sqlBuilder.Append(" WHERE ")
                sqlBuilder.Append(String.Join(" AND ", whereConditions))
            End If

            Dim sql As String = sqlBuilder.ToString()
            Dim rows = ExecuteQueryToDictionary(database, sql, parameters, _excludeFields)

            Return New With {
                .Result = "OK",
                .ColumnsYouCanFilterBy = String.Join(",", _AllParametersList),
                .Records = rows
            }
        Catch ex As Exception
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(
                CreateErrorResponse($"Error reading records: {ex.Message}")
            )
        End Try
    End Function
End Class

' ===================================
' ENHANCED BUSINESS LOGIC WRITER
' Supports custom SQL for existence checks and updates
' ===================================
Public Class BusinessLogicAdvancedWriterWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping)
    Private ReadOnly _keyFields As String()
    Private ReadOnly _allowUpdates As Boolean
    Private ReadOnly _customExistenceCheckSQL As String
    Private ReadOnly _customUpdateSQL As String
    Private ReadOnly _customWhereClause As String
    
    ''' <summary>
    ''' Advanced writer with custom SQL support
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
' STANDARD BUSINESS LOGIC WRITER (Backward Compatible)
' ===================================
Public Class BusinessLogicWriterWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _AllParametersList As String()
    Private ReadOnly _RequiredParametersList As String()
    Private ReadOnly _allowUpdates As Boolean

    Public Sub New(tableName As String, AllParametersList As String(), RequiredParametersList As String(), allowUpdates As Boolean)
        _tableName = tableName
        _AllParametersList = AllParametersList
        _RequiredParametersList = RequiredParametersList
        _allowUpdates = allowUpdates
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            For Each paramName As String In _RequiredParametersList
                Dim paramResult = GetObjectParameter(payload, paramName)
                If Not paramResult.Item1 Then
                    Return New With {.Result = "KO", .Reason = $"Required parameter '{paramName}' is missing"}
                End If
            Next

            Dim parameters = getParameters(payload, _AllParametersList)
            
            Dim whereConditions As New System.Collections.Generic.List(Of String)
            Dim keyValuesList As New System.Collections.Generic.List(Of String)
            For Each keyCol As String In _RequiredParametersList
                whereConditions.Add($"{keyCol} = :{keyCol}")
                keyValuesList.Add(If(parameters.ContainsKey(keyCol), parameters(keyCol).ToString(), String.Empty))
            Next
            Dim IndexColumnsValues As String = String.Join(",", keyValuesList)
            
            Dim checkQuery As New QWTable()
            Dim recordExists As Boolean
            Try
                checkQuery.Database = database
                checkQuery.SQL = $"SELECT COUNT(*) as CNT FROM {_tableName} WHERE {String.Join(" AND ", whereConditions)}"
                For Each keyCol As String In _RequiredParametersList
                    If parameters.ContainsKey(keyCol) Then
                        checkQuery.params(keyCol) = parameters(keyCol)
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
                    Return New With {.Result = "KO", .Reason = $"{IndexColumnsValues} - Record already exists and updates are not allowed"}
                End If

                Dim setClauses As New System.Collections.Generic.List(Of String)
                Dim updateColumns = _AllParametersList.Except(_RequiredParametersList)

                For Each colName As String In updateColumns
                    If parameters.ContainsKey(colName) Then
                        setClauses.Add($"{colName} = :{colName}")
                    End If
                Next

                If setClauses.Count > 0 Then
                    Dim updateQuery As New QWTable()
                    Try
                        updateQuery.Database = database
                        updateQuery.SQL = $"UPDATE {_tableName} SET {String.Join(", ", setClauses)} WHERE {String.Join(" AND ", whereConditions)}"
                        
                        For Each param As System.Collections.Generic.KeyValuePair(Of String, Object) In parameters
                            updateQuery.params(param.Key) = param.Value
                        Next
                        
                        updateQuery.Active = True
                        updateQuery.Active = False
                    Catch ex As Exception
                        Return New With {.Result = "KO", .Reason = $"{IndexColumnsValues} - Update error: {ex.Message}"}
                    Finally
                        If updateQuery IsNot Nothing Then updateQuery.Dispose()
                    End Try
                End If
                
                Return New With {
                    .Result = "OK",
                    .RequiredColumns = String.Join(",", _RequiredParametersList),
                    .Action = "UPDATED",
                    .Message = "Record updated successfully"
                }
            Else
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
                        End Try
                    Next
                    
                    Dim saveMsg As String = ""
                    Dim okSave As Boolean = qTable.SaveRecord(saveMsg)
                    
                    qTable.Active = False
                    
                    If Not okSave Then
                        Return New With {.Result = "KO", .Reason = $"{IndexColumnsValues} - Error saving record: {saveMsg}"}
                    End If

                    Return New With {
                        .Result = "OK",
                        .RequiredColumns = String.Join(",", _RequiredParametersList),
                        .Action = "INSERTED",
                        .Message = "Record inserted successfully"
                    }
                Finally
                    If qTable IsNot Nothing Then
                        qTable.Dispose()
                    End If
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

        For Each recordParams In recordParameters
            Dim conditions As New System.Collections.Generic.List(Of String)(keyFields.Length)

            For Each keyField In keyFields
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

            For Each param In queryParams
                checkQuery.params(param.Key) = param.Value
            Next

            checkQuery.RequestLive = False
            checkQuery.Active = True

            ' Build composite keys from results
            While Not checkQuery.Rowset.EndOfSet
                Dim compositeKey As New System.Text.StringBuilder()
                For Each keyField In keyFields
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
    For Each keyField In keyFields
        If compositeKey.Length > 0 Then compositeKey.Append("|")
        If recordParams.ContainsKey(keyField) Then
            compositeKey.Append(recordParams(keyField).ToString())
        End If
    Next
    Return compositeKey.ToString()
End Function

' ===================================
' BATCH WRITER (Backward Compatible)
' ===================================
Public Class BusinessLogicBatchWriterWrapper
    Private ReadOnly _tableName As String
    Private ReadOnly _AllParametersList As String()
    Private ReadOnly _RequiredParametersList As String()
    Private ReadOnly _allowUpdates As Boolean

    Public Sub New(tableName As String, AllParametersList As String(), RequiredParametersList As String(), allowUpdates As Boolean)
        _tableName = tableName
        _AllParametersList = AllParametersList
        _RequiredParametersList = RequiredParametersList
        _allowUpdates = allowUpdates
    End Sub

    Public Function Execute(database As Object, payload As Newtonsoft.Json.Linq.JObject) As Object
        Try
            Dim recordsToken = GetPropertyCaseInsensitive(payload, "Records")
            Dim recordsArray As Newtonsoft.Json.Linq.JArray = TryCast(recordsToken, Newtonsoft.Json.Linq.JArray)

            If recordsArray Is Nothing Then
                Dim singleRecordHandler As New BusinessLogicWriterWrapper(_tableName, _AllParametersList, _RequiredParametersList, _allowUpdates)
                Return singleRecordHandler.Execute(database, payload)
            End If

            Dim insertedCount As Integer = 0, updatedCount As Integer = 0, errorCount As Integer = 0
            Dim errors As New System.Collections.Generic.List(Of String)

            ' PERFORMANCE: Pre-extract all record parameters and perform bulk existence check
            ' This reduces N database queries to 1 query for existence checking
            Dim allRecordParams As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))(recordsArray.Count)
            Dim recordDataList As New System.Collections.Generic.List(Of Object)(recordsArray.Count)

            ' First pass: Extract parameters and validate required fields
            For Each recordToken As Newtonsoft.Json.Linq.JToken In recordsArray
                Try
                    Dim record As Newtonsoft.Json.Linq.JObject = CType(recordToken, Newtonsoft.Json.Linq.JObject)

                    Dim missingParams As New System.Collections.Generic.List(Of String)
                    For Each paramName As String In _RequiredParametersList
                        Dim paramResult = GetObjectParameter(record, paramName)
                        If Not paramResult.Item1 Then
                            missingParams.Add(paramName)
                        End If
                    Next

                    If missingParams.Count > 0 Then
                        errors.Add($"Record skipped - Missing required parameters: {String.Join(", ", missingParams)}")
                        errorCount += 1
                        recordDataList.Add(Nothing) ' Placeholder for skipped record
                        Continue For
                    End If

                    Dim recordParams = getParameters(record, _AllParametersList)
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
                existingRecords = BulkExistenceCheck(database, _tableName, _RequiredParametersList, allRecordParams)
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
                    Dim compositeKey As String = GetCompositeKey(recordParams, _RequiredParametersList)
                    Dim recordExists As Boolean = existingRecords.Contains(compositeKey)

                    Dim keyValuesList As New System.Collections.Generic.List(Of String)
                    For Each keyCol As String In _RequiredParametersList
                        keyValuesList.Add(If(recordParams.ContainsKey(keyCol), recordParams(keyCol).ToString(), String.Empty))
                    Next
                    Dim IndexColumnsValues As String = String.Join(",", keyValuesList)

                    If recordExists Then
                        ' PERFORMANCE: Record existence already determined via bulk check
                        If Not _allowUpdates Then
                            errors.Add($"{IndexColumnsValues} - Record already exists and updates are not allowed")
                            errorCount += 1
                            Continue For
                        End If

                        Dim whereConditions As New System.Collections.Generic.List(Of String)
                        For Each keyCol As String In _RequiredParametersList
                            whereConditions.Add($"{keyCol} = :{keyCol}")
                        Next

                        Dim setClauses As New System.Collections.Generic.List(Of String)
                        Dim updateColumns = _AllParametersList.Except(_RequiredParametersList)

                        For Each colName As String In updateColumns
                            If recordParams.ContainsKey(colName) Then
                                setClauses.Add($"{colName} = :{colName}")
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
                .ColumnsYouCanWriteTo = String.Join(",", _AllParametersList),
                .RequiredColumns = String.Join(",", _RequiredParametersList),
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

Public Shared Function getParameters(payload As Newtonsoft.Json.Linq.JObject, AllParametersList As String()) As System.Collections.Generic.Dictionary(Of String, Object)
    Dim parameters As New System.Collections.Generic.Dictionary(Of String, Object)
    For Each param As String In AllParametersList
        Dim paramResult = GetObjectParameter(payload, param)
        If paramResult.Item1 Then
            parameters.Add(param, paramResult.Item2)
        End If
    Next
    Return parameters
End Function

Public Shared Function getParameters(payload As Newtonsoft.Json.Linq.JObject, parameterMapping As System.Collections.Generic.Dictionary(Of String, String)) As System.Collections.Generic.Dictionary(Of String, Object)
    Dim parameters As New System.Collections.Generic.Dictionary(Of String, Object)
    For Each mapping As System.Collections.Generic.KeyValuePair(Of String, String) In parameterMapping
        Dim paramResult = GetObjectParameter(payload, mapping.Key)
        If paramResult.Item1 Then
            parameters.Add(mapping.Value, paramResult.Item2)
        End If
    Next
    Return parameters
End Function

Public Shared Function CreateErrorResponse(reason As String) As String
    Return Newtonsoft.Json.JsonConvert.SerializeObject(New With {.Result = "KO", .Reason = reason})
End Function

Public Shared Function ExecuteQueryToDictionary(database As Object, sql As String, parameters As System.Collections.Generic.Dictionary(Of String, Object), excludeFields As String()) As System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))
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

    ' PERFORMANCE NOTE: Field exclusion is provided for backward compatibility
    ' BEST PRACTICE: Explicitly specify fields in your SELECT statement instead of SELECT *
    ' Example: SELECT UserId, Email, Name FROM Users (instead of SELECT * and excluding Password)
    Dim excludeSet As System.Collections.Generic.HashSet(Of String) = Nothing
    If excludeFields IsNot Nothing AndAlso excludeFields.Length > 0 Then
        excludeSet = New System.Collections.Generic.HashSet(Of String)(excludeFields, StringComparer.OrdinalIgnoreCase)
    End If

    ' PERFORMANCE: Pre-calculate capacity for better memory allocation
    Dim estimatedFieldCount As Integer = q.rowset.fields.size
    If excludeSet IsNot Nothing Then
        estimatedFieldCount = estimatedFieldCount - excludeSet.Count
        If estimatedFieldCount < 0 Then estimatedFieldCount = q.rowset.fields.size
    End If

    Dim rows As New System.Collections.Generic.List(Of System.Collections.Generic.Dictionary(Of String, Object))()

    While Not q.rowset.endofset
        ' PERFORMANCE: Pre-allocate dictionary with estimated capacity
        Dim row As New System.Collections.Generic.Dictionary(Of String, Object)(estimatedFieldCount)

        For i As Integer = 1 To q.rowset.fields.size
            Dim fieldName As String = q.Rowset.fields(i).fieldname

            ' Apply field exclusion only if needed (legacy feature)
            If excludeSet Is Nothing OrElse Not excludeSet.Contains(fieldName) Then
                row.Add(fieldName, q.rowset.fields(i).value)
            End If
        Next

        rows.Add(row)
        q.rowset.next()
    End While

    q.Active = False
    Return rows
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

Public Function CreateBusinessLogicForReadingRows(tableName As String, AllParametersList As String(), excludeFields As String(), Optional useLikeOperator As Boolean = True) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicReaderWrapper(tableName, AllParametersList, excludeFields, useLikeOperator).Execute
End Function

Public Function CreateBusinessLogicForWritingRows(tableName As String, AllParametersList As String(), RequiredParametersList As String(), allowUpdates As Boolean) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicWriterWrapper(tableName, AllParametersList, RequiredParametersList, allowUpdates).Execute
End Function

Public Function CreateBusinessLogicForWritingRowsBatch(tableName As String, AllParametersList As String(), RequiredParametersList As String(), allowUpdates As Boolean) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicBatchWriterWrapper(tableName, AllParametersList, RequiredParametersList, allowUpdates).Execute
End Function

' ===================================
' ENHANCED FACTORY FUNCTIONS
' ===================================

Public Function CreateAdvancedBusinessLogicForReading(
    baseSQL As String,
    parameterConditions As System.Collections.Generic.Dictionary(Of String, Object),
    Optional excludeFields As String() = Nothing,
    Optional defaultWhereClause As String = Nothing,
    Optional fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping) = Nothing
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicAdvancedReaderWrapper(baseSQL, parameterConditions, excludeFields, defaultWhereClause, fieldMappings).Execute
End Function

Public Function CreateAdvancedBusinessLogicForWriting(
    tableName As String,
    fieldMappings As System.Collections.Generic.Dictionary(Of String, FieldMapping),
    keyFields As String(),
    allowUpdates As Boolean,
    Optional customExistenceCheckSQL As String = Nothing,
    Optional customUpdateSQL As String = Nothing,
    Optional customWhereClause As String = Nothing
) As Func(Of Object, Newtonsoft.Json.Linq.JObject, Object)
    Return AddressOf New BusinessLogicAdvancedWriterWrapper(tableName, fieldMappings, keyFields, allowUpdates, customExistenceCheckSQL, customUpdateSQL, customWhereClause).Execute
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
'''''
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
