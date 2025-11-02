''' <summary>
''' Complete Test Endpoint Examples using the testepoint table
''' Demonstrates all major features of the Endpoint Library with comprehensive test scenarios
''' </summary>
''' <remarks>
''' Prerequisites:
''' 1. Run testepoint_setup.sql to create the table and insert test data
''' 2. Ensure connection string is configured properly
''' 3. Ensure token validation is configured (or disabled for testing)
''' </remarks>

Imports System.Data.SqlClient
Imports Newtonsoft.Json.Linq

Public Class TestEndpointExamples

#Region "1. Basic Read Operations"

    ''' <summary>
    ''' Example 1.1: Simple read - Get all active endpoints
    ''' </summary>
    Public Function GetActiveEndpoints(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Description,
                Status,
                Priority,
                CreatedBy,
                CreatedDate,
                RequestCount,
                ErrorCount,
                AvgResponseTime
            FROM testepoint
            WHERE IsActive = 1
              AND IsDeleted = 0
            ORDER BY Priority DESC, EndpointName
        ", Nothing, token)
    End Function

    ''' <summary>
    ''' Example 1.2: Read with parameters - Search by endpoint type
    ''' </summary>
    Public Function GetEndpointsByType(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim paramConditions As New List(Of ParameterCondition) From {
            New ParameterCondition("EndpointType", "AND EndpointType = @EndpointType")
        }

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Description,
                Status,
                IsActive,
                Priority,
                RequestCount,
                ErrorCount
            FROM testepoint
            WHERE IsDeleted = 0
            /*PARAMETER_CONDITIONS*/
            ORDER BY EndpointName
        ", paramConditions, token)
    End Function

    ''' <summary>
    ''' Example 1.3: Read with LIKE search - Search endpoint name
    ''' </summary>
    Public Function SearchEndpointByName(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim paramConditions As New List(Of ParameterCondition) From {
            New ParameterCondition("EndpointName", "AND EndpointName LIKE '%' + @EndpointName + '%'")
        }

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Description,
                Status,
                CreatedBy,
                CreatedDate
            FROM testepoint
            WHERE IsDeleted = 0
            /*PARAMETER_CONDITIONS*/
            ORDER BY EndpointName
        ", paramConditions, token)
    End Function

    ''' <summary>
    ''' Example 1.4: Read with field exclusion - Hide sensitive data
    ''' </summary>
    Public Function GetEndpointsWithoutSecrets(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim excludedFields As New List(Of String) From {
            "ApiKey",
            "SecretToken"
        }

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT * FROM testepoint
            WHERE IsActive = 1 AND IsDeleted = 0
            ORDER BY Priority DESC
        ", Nothing, token, excludedFields)
    End Function

    ''' <summary>
    ''' Example 1.5: Read with multiple conditions - Filter by owner and status
    ''' </summary>
    Public Function GetEndpointsByOwnerAndStatus(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim paramConditions As New List(Of ParameterCondition) From {
            New ParameterCondition("OwnerId", "AND OwnerId = @OwnerId"),
            New ParameterCondition("Status", "AND Status = @Status")
        }

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Status,
                Priority,
                RequestCount,
                ErrorCount,
                AvgResponseTime,
                CreatedDate,
                LastModifiedDate
            FROM testepoint
            WHERE IsDeleted = 0
            /*PARAMETER_CONDITIONS*/
            ORDER BY Priority DESC, CreatedDate DESC
        ", paramConditions, token)
    End Function

#End Region

#Region "2. Advanced Read Operations"

    ''' <summary>
    ''' Example 2.1: Read with date range - Get endpoints created in a date range
    ''' </summary>
    Public Function GetEndpointsByDateRange(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim paramConditions As New List(Of ParameterCondition) From {
            New ParameterCondition("StartDate", "AND CreatedDate >= @StartDate"),
            New ParameterCondition("EndDate", "AND CreatedDate <= @EndDate")
        }

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Status,
                CreatedDate,
                CreatedBy,
                RequestCount
            FROM testepoint
            WHERE IsDeleted = 0
            /*PARAMETER_CONDITIONS*/
            ORDER BY CreatedDate DESC
        ", paramConditions, token)
    End Function

    ''' <summary>
    ''' Example 2.2: Read with aggregates - Get endpoint statistics by type
    ''' </summary>
    Public Function GetEndpointStatsByType(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointType,
                COUNT(*) as TotalCount,
                SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) as ActiveCount,
                SUM(RequestCount) as TotalRequests,
                SUM(ErrorCount) as TotalErrors,
                AVG(AvgResponseTime) as AvgResponseTime,
                MIN(CreatedDate) as FirstCreated,
                MAX(CreatedDate) as LastCreated
            FROM testepoint
            WHERE IsDeleted = 0
            GROUP BY EndpointType
            ORDER BY TotalRequests DESC
        ", Nothing, token)
    End Function

    ''' <summary>
    ''' Example 2.3: Read with performance metrics - Get top performing endpoints
    ''' </summary>
    Public Function GetTopPerformingEndpoints(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim paramConditions As New List(Of ParameterCondition) From {
            New ParameterCondition("TopN", "")  ' Used in TOP clause
        }

        ' Get TopN parameter or default to 10
        Dim topN As Integer = 10
        If json("TopN") IsNot Nothing Then
            topN = CInt(json("TopN"))
        End If

        Return ReadAdvanced(json, connectionString, "testepoint", $"
            SELECT TOP {topN}
                EndpointId,
                EndpointName,
                EndpointType,
                RequestCount,
                ErrorCount,
                AvgResponseTime,
                CAST(ErrorCount AS FLOAT) / NULLIF(RequestCount, 0) * 100 as ErrorRate,
                Priority
            FROM testepoint
            WHERE IsActive = 1
              AND IsDeleted = 0
              AND RequestCount > 0
            ORDER BY RequestCount DESC, AvgResponseTime ASC
        ", paramConditions, token)
    End Function

    ''' <summary>
    ''' Example 2.4: Read with complex filtering - Get endpoints needing attention
    ''' </summary>
    Public Function GetEndpointsNeedingAttention(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Status,
                ErrorCount,
                RequestCount,
                CAST(ErrorCount AS FLOAT) / NULLIF(RequestCount, 0) * 100 as ErrorRate,
                AvgResponseTime,
                LastModifiedDate,
                CASE
                    WHEN CAST(ErrorCount AS FLOAT) / NULLIF(RequestCount, 0) > 0.05 THEN 'High Error Rate'
                    WHEN AvgResponseTime > 1000 THEN 'Slow Response'
                    WHEN LastModifiedDate < DATEADD(month, -3, GETDATE()) THEN 'Stale'
                    ELSE 'Review Needed'
                END as IssueType
            FROM testepoint
            WHERE IsActive = 1
              AND IsDeleted = 0
              AND (
                  CAST(ErrorCount AS FLOAT) / NULLIF(RequestCount, 0) > 0.05  -- Error rate > 5%
                  OR AvgResponseTime > 1000  -- Avg response time > 1 second
                  OR LastModifiedDate < DATEADD(month, -3, GETDATE())  -- Not modified in 3 months
              )
            ORDER BY ErrorCount DESC, AvgResponseTime DESC
        ", Nothing, token)
    End Function

#End Region

#Region "3. Write Operations - Insert"

    ''' <summary>
    ''' Example 3.1: Simple insert - Create a new endpoint
    ''' </summary>
    Public Function CreateEndpoint(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointName",
            "EndpointType",
            "CreatedBy"
        }

        Return Write(json, connectionString, "testepoint", "
            INSERT INTO testepoint (
                EndpointName,
                EndpointType,
                Description,
                IsActive,
                Status,
                Priority,
                CreatedBy,
                OwnerId,
                CreatedDate,
                ConfigJson,
                Metadata,
                ApiKey,
                SecretToken
            )
            VALUES (
                @EndpointName,
                @EndpointType,
                @Description,
                ISNULL(@IsActive, 1),
                ISNULL(@Status, 'ACTIVE'),
                ISNULL(@Priority, 5),
                @CreatedBy,
                @OwnerId,
                GETDATE(),
                @ConfigJson,
                @Metadata,
                @ApiKey,
                @SecretToken
            );
            SELECT SCOPE_IDENTITY() as EndpointId;
        ", validators, token)
    End Function

    ''' <summary>
    ''' Example 3.2: Insert with field mapping - Create endpoint with JSON property mapping
    ''' </summary>
    Public Function CreateEndpointWithMapping(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim fieldMappings As New List(Of FieldMapping) From {
            New FieldMapping("name", "EndpointName", isRequired:=True),
            New FieldMapping("type", "EndpointType", isRequired:=True),
            New FieldMapping("desc", "Description"),
            New FieldMapping("active", "IsActive"),
            New FieldMapping("owner", "OwnerId"),
            New FieldMapping("config", "ConfigJson"),
            New FieldMapping("creator", "CreatedBy", isRequired:=True)
        }

        Return Write(json, connectionString, "testepoint", "
            INSERT INTO testepoint (
                EndpointName,
                EndpointType,
                Description,
                IsActive,
                Status,
                Priority,
                CreatedBy,
                OwnerId,
                CreatedDate,
                ConfigJson
            )
            VALUES (
                @EndpointName,
                @EndpointType,
                @Description,
                ISNULL(@IsActive, 1),
                'ACTIVE',
                5,
                @CreatedBy,
                @OwnerId,
                GETDATE(),
                @ConfigJson
            );
            SELECT SCOPE_IDENTITY() as EndpointId;
        ", Nothing, token, fieldMappings)
    End Function

#End Region

#Region "4. Write Operations - Update"

    ''' <summary>
    ''' Example 4.1: Simple update - Update endpoint status
    ''' </summary>
    Public Function UpdateEndpointStatus(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointId",
            "Status"
        }

        Return Write(json, connectionString, "testepoint", "
            UPDATE testepoint
            SET Status = @Status,
                IsActive = CASE
                    WHEN @Status IN ('ACTIVE', 'TESTING', 'BETA') THEN 1
                    ELSE 0
                END,
                LastModifiedDate = GETDATE()
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 0;
        ", validators, token)
    End Function

    ''' <summary>
    ''' Example 4.2: Update with increment - Increment request and error counters
    ''' </summary>
    Public Function RecordEndpointUsage(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointId"
        }

        Return Write(json, connectionString, "testepoint", "
            UPDATE testepoint
            SET RequestCount = RequestCount + 1,
                ErrorCount = ErrorCount + ISNULL(@IncrementError, 0),
                LastAccessDate = GETDATE(),
                AvgResponseTime = CASE
                    WHEN @ResponseTime IS NOT NULL THEN
                        (ISNULL(AvgResponseTime, 0) * RequestCount + @ResponseTime) / (RequestCount + 1)
                    ELSE AvgResponseTime
                END
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 0;
        ", validators, token)
    End Function

    ''' <summary>
    ''' Example 4.3: Conditional update - Update configuration if not locked
    ''' </summary>
    Public Function UpdateEndpointConfig(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointId",
            "ConfigJson"
        }

        Return Write(json, connectionString, "testepoint", "
            UPDATE testepoint
            SET ConfigJson = @ConfigJson,
                Metadata = @Metadata,
                LastModifiedDate = GETDATE(),
                Version = Version + 1
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 0
              AND Status NOT IN ('LOCKED', 'ARCHIVED');

            SELECT @@ROWCOUNT as UpdatedRows;
        ", validators, token)
    End Function

#End Region

#Region "5. Write Operations - Upsert"

    ''' <summary>
    ''' Example 5.1: Upsert - Insert or update endpoint by name
    ''' </summary>
    Public Function UpsertEndpoint(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointName",
            "EndpointType",
            "CreatedBy"
        }

        Return Write(json, connectionString, "testepoint", "
            IF EXISTS (SELECT 1 FROM testepoint WHERE EndpointName = @EndpointName AND IsDeleted = 0)
            BEGIN
                -- Update existing
                UPDATE testepoint
                SET EndpointType = @EndpointType,
                    Description = @Description,
                    Status = ISNULL(@Status, Status),
                    Priority = ISNULL(@Priority, Priority),
                    ConfigJson = ISNULL(@ConfigJson, ConfigJson),
                    Metadata = ISNULL(@Metadata, Metadata),
                    LastModifiedDate = GETDATE(),
                    Version = Version + 1
                WHERE EndpointName = @EndpointName
                  AND IsDeleted = 0;

                SELECT EndpointId FROM testepoint WHERE EndpointName = @EndpointName AND IsDeleted = 0;
            END
            ELSE
            BEGIN
                -- Insert new
                INSERT INTO testepoint (
                    EndpointName,
                    EndpointType,
                    Description,
                    Status,
                    Priority,
                    CreatedBy,
                    OwnerId,
                    ConfigJson,
                    Metadata,
                    CreatedDate
                )
                VALUES (
                    @EndpointName,
                    @EndpointType,
                    @Description,
                    ISNULL(@Status, 'ACTIVE'),
                    ISNULL(@Priority, 5),
                    @CreatedBy,
                    @OwnerId,
                    @ConfigJson,
                    @Metadata,
                    GETDATE()
                );

                SELECT SCOPE_IDENTITY() as EndpointId;
            END
        ", validators, token)
    End Function

#End Region

#Region "6. Write Operations - Soft Delete"

    ''' <summary>
    ''' Example 6.1: Soft delete - Mark endpoint as deleted
    ''' </summary>
    Public Function SoftDeleteEndpoint(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointId",
            "DeletedBy"
        }

        Return Write(json, connectionString, "testepoint", "
            UPDATE testepoint
            SET IsDeleted = 1,
                IsActive = 0,
                Status = 'DELETED',
                DeletedDate = GETDATE(),
                DeletedBy = @DeletedBy
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 0;

            SELECT @@ROWCOUNT as DeletedRows;
        ", validators, token)
    End Function

    ''' <summary>
    ''' Example 6.2: Restore deleted endpoint
    ''' </summary>
    Public Function RestoreEndpoint(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim validators As New List(Of String) From {
            "EndpointId"
        }

        Return Write(json, connectionString, "testepoint", "
            UPDATE testepoint
            SET IsDeleted = 0,
                IsActive = 1,
                Status = 'ACTIVE',
                DeletedDate = NULL,
                DeletedBy = NULL,
                LastModifiedDate = GETDATE()
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 1;

            SELECT @@ROWCOUNT as RestoredRows;
        ", validators, token)
    End Function

#End Region

#Region "7. Batch Operations"

    ''' <summary>
    ''' Example 7.1: Batch insert - Create multiple endpoints
    ''' </summary>
    Public Function BatchCreateEndpoints(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim batchValidators As New List(Of String) From {
            "endpoints"
        }

        Dim fieldMappings As New List(Of FieldMapping) From {
            New FieldMapping("name", "EndpointName", isRequired:=True),
            New FieldMapping("type", "EndpointType", isRequired:=True),
            New FieldMapping("desc", "Description"),
            New FieldMapping("priority", "Priority", defaultValue:="5"),
            New FieldMapping("creator", "CreatedBy", isRequired:=True)
        }

        Return WriteBatch(json, "endpoints", connectionString, "testepoint", "
            INSERT INTO testepoint (
                EndpointName,
                EndpointType,
                Description,
                Priority,
                CreatedBy,
                Status,
                IsActive,
                CreatedDate
            )
            VALUES (
                @EndpointName,
                @EndpointType,
                @Description,
                @Priority,
                @CreatedBy,
                'ACTIVE',
                1,
                GETDATE()
            );
        ", batchValidators, token, fieldMappings)
    End Function

    ''' <summary>
    ''' Example 7.2: Batch update - Update multiple endpoint priorities
    ''' </summary>
    Public Function BatchUpdatePriorities(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim batchValidators As New List(Of String) From {
            "updates"
        }

        Dim fieldMappings As New List(Of FieldMapping) From {
            New FieldMapping("id", "EndpointId", isRequired:=True),
            New FieldMapping("priority", "Priority", isRequired:=True)
        }

        Return WriteBatch(json, "updates", connectionString, "testepoint", "
            UPDATE testepoint
            SET Priority = @Priority,
                LastModifiedDate = GETDATE()
            WHERE EndpointId = @EndpointId
              AND IsDeleted = 0;
        ", batchValidators, token, fieldMappings)
    End Function

    ''' <summary>
    ''' Example 7.3: Batch upsert - Insert or update multiple endpoints
    ''' </summary>
    Public Function BatchUpsertEndpoints(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Dim batchValidators As New List(Of String) From {
            "endpoints"
        }

        Dim fieldMappings As New List(Of FieldMapping) From {
            New FieldMapping("name", "EndpointName", isRequired:=True),
            New FieldMapping("type", "EndpointType", isRequired:=True),
            New FieldMapping("desc", "Description"),
            New FieldMapping("priority", "Priority", defaultValue:="5"),
            New FieldMapping("creator", "CreatedBy", isRequired:=True)
        }

        Return WriteBatch(json, "endpoints", connectionString, "testepoint", "
            IF EXISTS (SELECT 1 FROM testepoint WHERE EndpointName = @EndpointName AND IsDeleted = 0)
            BEGIN
                UPDATE testepoint
                SET EndpointType = @EndpointType,
                    Description = @Description,
                    Priority = @Priority,
                    LastModifiedDate = GETDATE(),
                    Version = Version + 1
                WHERE EndpointName = @EndpointName AND IsDeleted = 0;
            END
            ELSE
            BEGIN
                INSERT INTO testepoint (EndpointName, EndpointType, Description, Priority, CreatedBy, Status, IsActive, CreatedDate)
                VALUES (@EndpointName, @EndpointType, @Description, @Priority, @CreatedBy, 'ACTIVE', 1, GETDATE());
            END
        ", batchValidators, token, fieldMappings)
    End Function

#End Region

#Region "8. Advanced Scenarios"

    ''' <summary>
    ''' Example 8.1: DestinationIdentifier pattern - Route to different operations
    ''' </summary>
    Public Function HandleEndpointRequest(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        ' Get the operation type from DestinationIdentifier
        Dim operation As String = If(json("DestinationIdentifier")?.ToString(), "")

        Select Case operation.ToUpper()
            Case "GET_ACTIVE"
                Return GetActiveEndpoints(json, connectionString, token)

            Case "GET_BY_TYPE"
                Return GetEndpointsByType(json, connectionString, token)

            Case "CREATE"
                Return CreateEndpoint(json, connectionString, token)

            Case "UPDATE_STATUS"
                Return UpdateEndpointStatus(json, connectionString, token)

            Case "SOFT_DELETE"
                Return SoftDeleteEndpoint(json, connectionString, token)

            Case "GET_STATS"
                Return GetEndpointStatsByType(json, connectionString, token)

            Case Else
                Return New With {
                    .success = False,
                    .error = $"Unknown operation: {operation}",
                    .validOperations = New String() {
                        "GET_ACTIVE", "GET_BY_TYPE", "CREATE",
                        "UPDATE_STATUS", "SOFT_DELETE", "GET_STATS"
                    }
                }
        End Select
    End Function

    ''' <summary>
    ''' Example 8.2: Security pattern - Get endpoints with role-based access
    ''' </summary>
    Public Function GetEndpointsByRole(jsonRequest As JObject, connectionString As String, token As String, userRole As String) As Object
        Dim json As JObject = jsonRequest

        Dim excludedFields As New List(Of String)

        ' Exclude sensitive fields based on role
        If userRole <> "ADMIN" Then
            excludedFields.Add("ApiKey")
            excludedFields.Add("SecretToken")
        End If

        If userRole = "VIEWER" Then
            excludedFields.Add("ConfigJson")
        End If

        Return ReadAdvanced(json, connectionString, "testepoint", "
            SELECT * FROM testepoint
            WHERE IsDeleted = 0
              AND IsActive = 1
            ORDER BY Priority DESC
        ", Nothing, token, excludedFields)
    End Function

    ''' <summary>
    ''' Example 8.3: Performance optimization - Use FOR JSON PATH
    ''' </summary>
    Public Function GetEndpointsJsonPath(jsonRequest As JObject, connectionString As String, token As String) As Object
        Dim json As JObject = jsonRequest

        Return ReadSimpleForJsonPath(json, connectionString, "testepoint", "
            SELECT
                EndpointId,
                EndpointName,
                EndpointType,
                Description,
                Status,
                Priority,
                RequestCount,
                ErrorCount,
                AvgResponseTime
            FROM testepoint
            WHERE IsActive = 1 AND IsDeleted = 0
            ORDER BY Priority DESC
        ", Nothing, token)
    End Function

#End Region

End Class
