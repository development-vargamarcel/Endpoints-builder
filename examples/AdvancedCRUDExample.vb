' ===================================
' P_TESTINGTABLEARTICLES - COMPLETE EXAMPLE
' SQL Table Setup + CRUD Operations using EndpointLibrary
' ===================================

' ===================================
' PART 1: SQL TABLE SETUP
' ===================================
' Execute these SQL queries in SQL Server Management Studio (SSMS) or Azure Data Studio
' before running the VB.NET examples below

'---------------------------------------
' 1. DROP TABLE (if exists)
'---------------------------------------
' SQL:
' IF OBJECT_ID('dbo.P_TestingTableArticles', 'U') IS NOT NULL
'     DROP TABLE dbo.P_TestingTableArticles;
' GO

'---------------------------------------
' 2. CREATE TABLE
'---------------------------------------
' SQL:
' CREATE TABLE dbo.P_TestingTableArticles (
'     -- Primary Key
'     ArticleId INT IDENTITY(1,1) PRIMARY KEY,
'
'     -- String columns
'     Title NVARCHAR(200) NOT NULL,
'     Content NVARCHAR(MAX),
'     Category VARCHAR(50),
'     Author NVARCHAR(100),
'
'     -- Numeric columns
'     ViewCount INT DEFAULT 0,
'     LikeCount BIGINT DEFAULT 0,
'     Rating DECIMAL(3,2),
'
'     -- Date/Time columns
'     CreatedDate DATETIME DEFAULT GETDATE(),
'     PublishedDate DATETIME,
'     LastModifiedDate DATETIME,
'
'     -- Boolean columns
'     IsPublished BIT DEFAULT 0,
'     IsFeatured BIT DEFAULT 0,
'     AllowComments BIT DEFAULT 1,
'
'     -- Additional types
'     MetaDataJson NVARCHAR(MAX),
'     Tags VARCHAR(500),
'     Version VARCHAR(20)
' );
' GO

'---------------------------------------
' 3. INSERT SAMPLE DATA (Optional)
'---------------------------------------
' SQL:
' INSERT INTO dbo.P_TestingTableArticles
'     (Title, Content, Category, Author, Rating, ViewCount, LikeCount, IsPublished, IsFeatured, Tags)
' VALUES
'     ('Introduction to SQL Server', 'Complete guide to SQL Server...', 'Technology', 'John Doe', 4.75, 150, 45, 1, 1, 'SQL,Database,Tutorial'),
'     ('Advanced Query Optimization', 'Performance tuning techniques...', 'Technology', 'Jane Smith', 4.50, 200, 60, 1, 0, 'SQL,Performance,Optimization'),
'     ('Database Security Best Practices', 'How to secure your database...', 'Security', 'Bob Johnson', 4.90, 300, 90, 1, 1, 'Security,Database,BestPractices'),
'     ('Draft Article - Work in Progress', 'This is a draft...', 'General', 'Alice Brown', NULL, 0, 0, 0, 0, 'Draft');
' GO


' ===================================
' PART 2: READING EXAMPLES (Using EndpointLibrary)
' ===================================

'---------------------------------------
' EXAMPLE 1: READ WITH MULTIPLE FILTER OPTIONS
'---------------------------------------
' Flexible search with various filter combinations

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "ReadArticles", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Define parameter conditions for flexible search
Dim searchConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

searchConditions.Add("ArticleId", DB.Global.CreateParameterCondition(
    "ArticleId",
    "ArticleId = :ArticleId",
    Nothing
))

searchConditions.Add("Title", DB.Global.CreateParameterCondition(
    "Title",
    "Title LIKE :Title",
    Nothing
))

searchConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

searchConditions.Add("Author", DB.Global.CreateParameterCondition(
    "Author",
    "Author = :Author",
    Nothing
))

searchConditions.Add("IsPublished", DB.Global.CreateParameterCondition(
    "IsPublished",
    "IsPublished = :IsPublished",
    Nothing
))

searchConditions.Add("IsFeatured", DB.Global.CreateParameterCondition(
    "IsFeatured",
    "IsFeatured = :IsFeatured",
    Nothing
))

searchConditions.Add("MinRating", DB.Global.CreateParameterCondition(
    "MinRating",
    "Rating >= :MinRating",
    Nothing
))

searchConditions.Add("MinViewCount", DB.Global.CreateParameterCondition(
    "MinViewCount",
    "ViewCount >= :MinViewCount",
    Nothing
))

' Create read logic with explicit field selection
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ArticleId, Title, Content, Category, Author, ViewCount, LikeCount, Rating, " &
    "CreatedDate, PublishedDate, IsPublished, IsFeatured, Tags FROM P_TestingTableArticles {WHERE} " &
    "ORDER BY CreatedDate DESC",
    searchConditions,
    Nothing  ' No default WHERE clause - returns all if no filters provided
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,  ' No required parameters
    readLogic,
    "Article search",
    ParsedPayload,
    StringPayload,
    False
)

' EXAMPLE PAYLOADS FOR READING:
'
' 1. Get all articles:
'    {}
'
' 2. Get article by ID:
'    { "ArticleId": 5 }
'
' 3. Search by title (wildcard):
'    { "Title": "%SQL%" }
'
' 4. Filter by category and published status:
'    { "Category": "Technology", "IsPublished": 1 }
'
' 5. Get featured articles:
'    { "IsFeatured": 1, "IsPublished": 1 }
'
' 6. Get highly rated articles:
'    { "MinRating": 4.5, "IsPublished": 1 }
'
' 7. Get popular articles (min 100 views):
'    { "MinViewCount": 100, "Category": "Technology" }
'
' 8. Search by specific author:
'    { "Author": "John Doe" }
'
' 9. Combine multiple filters:
'    { "Category": "Technology", "MinRating": 4.0, "IsPublished": 1, "MinViewCount": 50 }


'---------------------------------------
' EXAMPLE 2: READ WITH DATE RANGE FILTER
'---------------------------------------

Dim CheckToken2 = False
Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, CheckToken2, "ReadArticlesByDate", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim dateConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

dateConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "PublishedDate >= :StartDate",
    Nothing
))

dateConditions.Add("EndDate", DB.Global.CreateParameterCondition(
    "EndDate",
    "PublishedDate <= :EndDate",
    Nothing
))

dateConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

Dim dateRangeLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ArticleId, Title, Author, Category, PublishedDate, ViewCount, Rating " &
    "FROM P_TestingTableArticles {WHERE} ORDER BY PublishedDate DESC",
    dateConditions,
    "IsPublished = 1"  ' Default: only published articles
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    dateRangeLogic,
    "Date range search",
    ParsedPayload2,
    StringPayload2,
    False
)

' EXAMPLE PAYLOADS:
'
' 1. Articles published in 2025:
'    { "StartDate": "2025-01-01", "EndDate": "2025-12-31" }
'
' 2. Recent articles in Technology category:
'    { "StartDate": "2025-01-01", "Category": "Technology" }
'
' 3. Articles from last 30 days:
'    { "StartDate": "2025-01-03" }


'---------------------------------------
' EXAMPLE 3: READ WITH HIGH PERFORMANCE MODE (FOR JSON PATH)
'---------------------------------------
' This is 40-60% faster for simple queries

Dim CheckToken3 = False
Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, CheckToken3, "ReadArticlesFast", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim fastConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

fastConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

fastConditions.Add("IsPublished", DB.Global.CreateParameterCondition(
    "IsPublished",
    "IsPublished = :IsPublished",
    "IsPublished = 1"  ' Default to published
))

' Create read logic with FOR JSON PATH enabled (40-60% faster)
Dim fastReadLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ArticleId, Title, Category, Author, Rating, ViewCount FROM P_TestingTableArticles {WHERE}",
    fastConditions,
    "IsPublished = 1",
    Nothing,
    True  ' Enable FOR JSON PATH for better performance
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    fastReadLogic,
    "Fast article search",
    ParsedPayload3,
    StringPayload3,
    False
)

' EXAMPLE PAYLOAD:
'    { "Category": "Technology" }


'---------------------------------------
' EXAMPLE 4: READ WITH AGGREGATION
'---------------------------------------

Dim CheckToken4 = False
Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, CheckToken4, "ArticleStats", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim statsConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

statsConditions.Add("IsPublished", DB.Global.CreateParameterCondition(
    "IsPublished",
    "IsPublished = :IsPublished",
    Nothing
))

Dim statsLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT Category, " &
    "COUNT(*) as ArticleCount, " &
    "AVG(Rating) as AverageRating, " &
    "SUM(ViewCount) as TotalViews, " &
    "MAX(ViewCount) as MaxViews, " &
    "MIN(ViewCount) as MinViews " &
    "FROM P_TestingTableArticles {WHERE} " &
    "GROUP BY Category " &
    "ORDER BY ArticleCount DESC",
    statsConditions,
    "IsPublished = 1"
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    statsLogic,
    "Article statistics",
    ParsedPayload4,
    StringPayload4,
    False
)

' EXAMPLE PAYLOAD:
'    { "IsPublished": 1 }
'
' EXAMPLE RESPONSE:
' {
'   "Result": "OK",
'   "Records": [
'     {
'       "Category": "Technology",
'       "ArticleCount": 150,
'       "AverageRating": 4.65,
'       "TotalViews": 45000,
'       "MaxViews": 1500,
'       "MinViews": 10
'     }
'   ]
' }


' ===================================
' PART 3: WRITING EXAMPLES (Using EndpointLibrary)
' ===================================

'---------------------------------------
' EXAMPLE 5: INSERT ARTICLE (INSERT ONLY, NO UPDATE)
'---------------------------------------

Dim StringPayloadInsert = "" : Dim ParsedPayloadInsert
Dim PayloadErrorInsert = DB.Global.ValidatePayloadAndToken(DB, False, "InsertArticle", ParsedPayloadInsert, StringPayloadInsert)
If PayloadErrorInsert IsNot Nothing Then
    Return PayloadErrorInsert
End If

' Define field mappings (JSON property -> SQL column)
Dim insertMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"title", "content", "category", "author", "rating", "isPublished", "isFeatured", "tags", "version"},
    New System.String() {"Title", "Content", "Category", "Author", "Rating", "IsPublished", "IsFeatured", "Tags", "Version"},
    New Boolean() {True, False, False, False, False, False, False, False, False},  ' Only title is required
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing, Nothing, Nothing, Nothing, Nothing, 0, 0, Nothing, "1.0.0"}  ' Defaults
)

' Create write logic - insert only, no updates
Dim insertLogic = DB.Global.CreateBusinessLogicForWriting(
    "P_TestingTableArticles",
    insertMappings,
    New System.String() {"ArticleId"},  ' Key field
    False  ' allowUpdates = False (insert only)
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"title"}),
    insertLogic,
    "Article inserted",
    ParsedPayloadInsert,
    StringPayloadInsert,
    False
)

' EXAMPLE PAYLOAD:
' {
'   "title": "Introduction to SQL Server",
'   "content": "This article covers SQL Server basics...",
'   "category": "Technology",
'   "author": "John Doe",
'   "rating": 4.75,
'   "isPublished": 1,
'   "isFeatured": 0,
'   "tags": "SQL,Database,Tutorial"
' }
'
' RESPONSE ON SUCCESS:
' { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
'
' RESPONSE IF RECORD EXISTS:
' { "Result": "KO", "Reason": "Record already exists and updates are not allowed" }


'---------------------------------------
' EXAMPLE 6: UPSERT ARTICLE (INSERT OR UPDATE)
'---------------------------------------

Dim StringPayloadUpsert = "" : Dim ParsedPayloadUpsert
Dim PayloadErrorUpsert = DB.Global.ValidatePayloadAndToken(DB, False, "UpsertArticle", ParsedPayloadUpsert, StringPayloadUpsert)
If PayloadErrorUpsert IsNot Nothing Then
    Return PayloadErrorUpsert
End If

Dim upsertMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"articleId", "title", "content", "category", "author", "rating", "viewCount", "likeCount", "isPublished", "isFeatured", "tags"},
    New System.String() {"ArticleId", "Title", "Content", "Category", "Author", "Rating", "ViewCount", "LikeCount", "IsPublished", "IsFeatured", "Tags"},
    New Boolean() {True, True, False, False, False, False, False, False, False, False, False},  ' articleId and title required
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, 0, 0, 0, 0, Nothing}
)

' Create upsert logic - insert or update
Dim upsertLogic = DB.Global.CreateBusinessLogicForWriting(
    "P_TestingTableArticles",
    upsertMappings,
    New System.String() {"ArticleId"},
    True  ' allowUpdates = True (upsert)
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"articleId", "title"}),
    upsertLogic,
    "Article upserted",
    ParsedPayloadUpsert,
    StringPayloadUpsert,
    False
)

' EXAMPLE PAYLOAD FOR INSERT (new article):
' {
'   "articleId": 100,
'   "title": "New Article Title",
'   "content": "Article content here...",
'   "category": "Technology",
'   "author": "Jane Smith",
'   "rating": 4.5,
'   "isPublished": 1
' }
'
' EXAMPLE PAYLOAD FOR UPDATE (existing article):
' {
'   "articleId": 1,
'   "title": "Updated Article Title",
'   "content": "Updated content...",
'   "rating": 4.8,
'   "viewCount": 250
' }
'
' RESPONSE ON INSERT:
' { "Result": "OK", "Action": "INSERTED", "Message": "Record inserted successfully" }
'
' RESPONSE ON UPDATE:
' { "Result": "OK", "Action": "UPDATED", "Message": "Record updated successfully" }


'---------------------------------------
' EXAMPLE 7: BATCH INSERT/UPDATE ARTICLES
'---------------------------------------
' High-performance batch operation with single database existence check

Dim StringPayloadBatch = "" : Dim ParsedPayloadBatch
Dim PayloadErrorBatch = DB.Global.ValidatePayloadAndToken(DB, False, "BatchArticles", ParsedPayloadBatch, StringPayloadBatch)
If PayloadErrorBatch IsNot Nothing Then
    Return PayloadErrorBatch
End If

Dim batchMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"articleId", "title", "content", "category", "author", "rating", "viewCount", "isPublished", "isFeatured"},
    New System.String() {"ArticleId", "Title", "Content", "Category", "Author", "Rating", "ViewCount", "IsPublished", "IsFeatured"},
    New Boolean() {True, True, False, False, False, False, False, False, False},
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, 0, 0, 0}
)

Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "P_TestingTableArticles",
    batchMappings,
    New System.String() {"ArticleId"},
    True  ' Allow updates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidatorForBatch(New System.String() {"Records"}),
    batchLogic,
    "Batch operation completed",
    ParsedPayloadBatch,
    StringPayloadBatch,
    False
)

' EXAMPLE PAYLOAD:
' {
'   "Records": [
'     {
'       "articleId": 1,
'       "title": "Article One",
'       "content": "Content for article one",
'       "category": "Technology",
'       "author": "John Doe",
'       "rating": 4.5,
'       "isPublished": 1
'     },
'     {
'       "articleId": 2,
'       "title": "Article Two",
'       "content": "Content for article two",
'       "category": "Science",
'       "author": "Jane Smith",
'       "rating": 4.8,
'       "isPublished": 1
'     },
'     {
'       "articleId": 3,
'       "title": "Article Three",
'       "content": "Content for article three",
'       "category": "Business",
'       "author": "Bob Johnson",
'       "rating": 4.2,
'       "isPublished": 0
'     }
'   ]
' }
'
' RESPONSE:
' {
'   "Result": "OK",
'   "Inserted": 2,
'   "Updated": 1,
'   "Errors": 0,
'   "ErrorDetails": [],
'   "Message": "Processed 3 records: 2 inserted, 1 updated, 0 errors."
' }


'---------------------------------------
' EXAMPLE 8: UPDATE ARTICLE METRICS (Custom Update SQL)
'---------------------------------------
' Increment view count without providing all fields

Dim StringPayloadMetrics = "" : Dim ParsedPayloadMetrics
Dim PayloadErrorMetrics = DB.Global.ValidatePayloadAndToken(DB, False, "UpdateMetrics", ParsedPayloadMetrics, StringPayloadMetrics)
If PayloadErrorMetrics IsNot Nothing Then
    Return PayloadErrorMetrics
End If

Dim metricsMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"articleId"},
    New System.String() {"ArticleId"},
    New Boolean() {True},
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing}
)

Dim metricsLogic = DB.Global.CreateBusinessLogicForWriting(
    "P_TestingTableArticles",
    metricsMappings,
    New System.String() {"ArticleId"},
    True,
    Nothing,  ' Default existence check
    "UPDATE P_TestingTableArticles SET ViewCount = ViewCount + 1, LastModifiedDate = GETDATE() WHERE ArticleId = :ArticleId",  ' Custom update
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"articleId"}),
    metricsLogic,
    "Article metrics updated",
    ParsedPayloadMetrics,
    StringPayloadMetrics,
    False
)

' EXAMPLE PAYLOAD:
' { "articleId": 5 }
'
' This increments ViewCount by 1 and updates LastModifiedDate
'
' RESPONSE:
' { "Result": "OK", "Action": "UPDATED", "Message": "Record updated successfully" }


'---------------------------------------
' EXAMPLE 9: PUBLISH ARTICLE (Custom Update)
'---------------------------------------
' Set article as published with current timestamp

Dim StringPayloadPublish = "" : Dim ParsedPayloadPublish
Dim PayloadErrorPublish = DB.Global.ValidatePayloadAndToken(DB, False, "PublishArticle", ParsedPayloadPublish, StringPayloadPublish)
If PayloadErrorPublish IsNot Nothing Then
    Return PayloadErrorPublish
End If

Dim publishMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"articleId"},
    New System.String() {"ArticleId"},
    New Boolean() {True},
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing}
)

Dim publishLogic = DB.Global.CreateBusinessLogicForWriting(
    "P_TestingTableArticles",
    publishMappings,
    New System.String() {"ArticleId"},
    True,
    Nothing,
    "UPDATE P_TestingTableArticles SET IsPublished = 1, PublishedDate = GETDATE() WHERE ArticleId = :ArticleId",
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"articleId"}),
    publishLogic,
    "Article published",
    ParsedPayloadPublish,
    StringPayloadPublish,
    False
)

' EXAMPLE PAYLOAD:
' { "articleId": 3 }


'---------------------------------------
' EXAMPLE 10: INCREMENT LIKE COUNT
'---------------------------------------

Dim StringPayloadLike = "" : Dim ParsedPayloadLike
Dim PayloadErrorLike = DB.Global.ValidatePayloadAndToken(DB, False, "LikeArticle", ParsedPayloadLike, StringPayloadLike)
If PayloadErrorLike IsNot Nothing Then
    Return PayloadErrorLike
End If

Dim likeMappings = DB.Global.CreateFieldMappingsDictionary(
    New System.String() {"articleId"},
    New System.String() {"ArticleId"},
    New Boolean() {True},
    Nothing,  ' No primary key array - using explicit keyFields parameter below
    New Object() {Nothing}
)

Dim likeLogic = DB.Global.CreateBusinessLogicForWriting(
    "P_TestingTableArticles",
    likeMappings,
    New System.String() {"ArticleId"},
    True,
    Nothing,
    "UPDATE P_TestingTableArticles SET LikeCount = LikeCount + 1 WHERE ArticleId = :ArticleId",
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New System.String() {"articleId"}),
    likeLogic,
    "Like recorded",
    ParsedPayloadLike,
    StringPayloadLike,
    False
)

' EXAMPLE PAYLOAD:
' { "articleId": 1 }


' ===================================
' PART 4: COMPLETE ENDPOINT WITH DESTINATIONIDENTIFIER PATTERN
' ===================================
' This pattern allows a single endpoint to handle multiple operations
' based on the DestinationIdentifier field in the payload

'---------------------------------------
' COMPLETE ENDPOINT - READ AND WRITE
'---------------------------------------

Dim CheckTokenComplete = False
Dim StringPayloadComplete = "" : Dim ParsedPayloadComplete
Dim PayloadAndTokenValidationErrorComplete = DB.Global.ValidatePayloadAndToken(DB, CheckTokenComplete, "ArticleEndpoint", ParsedPayloadComplete, StringPayloadComplete)
If PayloadAndTokenValidationErrorComplete IsNot Nothing Then
    Return PayloadAndTokenValidationErrorComplete
End If

Dim DestinationIdentifierInfo = DB.Global.GetDestinationIdentifier(ParsedPayloadComplete)

If DestinationIdentifierInfo.Item1 Then
    Dim destinationId As System.String = DestinationIdentifierInfo.Item2

    If destinationId = "article-read" Then
        '===================================
        ' READ OPERATION
        '===================================
        Dim searchConditionsComplete As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

        searchConditionsComplete.Add("ArticleId", DB.Global.CreateParameterCondition(
            "ArticleId", "ArticleId = :ArticleId", Nothing))
        searchConditionsComplete.Add("Title", DB.Global.CreateParameterCondition(
            "Title", "Title LIKE :Title", Nothing))
        searchConditionsComplete.Add("Category", DB.Global.CreateParameterCondition(
            "Category", "Category = :Category", Nothing))
        searchConditionsComplete.Add("Author", DB.Global.CreateParameterCondition(
            "Author", "Author = :Author", Nothing))
        searchConditionsComplete.Add("IsPublished", DB.Global.CreateParameterCondition(
            "IsPublished", "IsPublished = :IsPublished", Nothing))
        searchConditionsComplete.Add("IsFeatured", DB.Global.CreateParameterCondition(
            "IsFeatured", "IsFeatured = :IsFeatured", Nothing))
        searchConditionsComplete.Add("MinRating", DB.Global.CreateParameterCondition(
            "MinRating", "Rating >= :MinRating", Nothing))

        Return DB.Global.ProcessActionLink(DB,
            Nothing,
            DB.Global.CreateBusinessLogicForReading(
                "SELECT ArticleId, Title, Content, Category, Author, Rating, ViewCount, LikeCount, " &
                "IsPublished, IsFeatured, CreatedDate, PublishedDate, Tags " &
                "FROM P_TestingTableArticles {WHERE} ORDER BY CreatedDate DESC",
                searchConditionsComplete,
                "IsPublished = 1"
            ),
            "Article search executed",
            ParsedPayloadComplete, StringPayloadComplete, False)

    ElseIf destinationId = "article-write" Then
        '===================================
        ' WRITE OPERATION (BATCH)
        '===================================
        Dim writeMappings = DB.Global.CreateFieldMappingsDictionary(
            New System.String() {"articleId", "title", "content", "category", "author", "rating", "viewCount", "likeCount", "isPublished", "isFeatured", "tags"},
            New System.String() {"ArticleId", "Title", "Content", "Category", "Author", "Rating", "ViewCount", "LikeCount", "IsPublished", "IsFeatured", "Tags"},
            New Boolean() {True, True, False, False, False, False, False, False, False, False, False},
            Nothing,  ' No primary key array - using explicit keyFields parameter below
            New Object() {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, 0, 0, 0, 0, Nothing}
        )

        Return DB.Global.ProcessActionLink(DB,
            DB.Global.CreateValidatorForBatch(New System.String() {"Records"}),
            DB.Global.CreateBusinessLogicForBatchWriting(
                "P_TestingTableArticles",
                writeMappings,
                New System.String() {"ArticleId"},
                True
            ),
            "Articles written",
            ParsedPayloadComplete, StringPayloadComplete, False)

    ElseIf destinationId = "article-stats" Then
        '===================================
        ' STATISTICS OPERATION
        '===================================
        Dim statsConditionsComplete As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

        statsConditionsComplete.Add("IsPublished", DB.Global.CreateParameterCondition(
            "IsPublished", "IsPublished = :IsPublished", Nothing))

        Return DB.Global.ProcessActionLink(DB,
            Nothing,
            DB.Global.CreateBusinessLogicForReading(
                "SELECT Category, COUNT(*) as ArticleCount, AVG(Rating) as AvgRating, " &
                "SUM(ViewCount) as TotalViews, SUM(LikeCount) as TotalLikes " &
                "FROM P_TestingTableArticles {WHERE} GROUP BY Category ORDER BY ArticleCount DESC",
                statsConditionsComplete,
                "IsPublished = 1"
            ),
            "Statistics retrieved",
            ParsedPayloadComplete, StringPayloadComplete, False)

    ElseIf destinationId = "article-increment-views" Then
        '===================================
        ' INCREMENT VIEW COUNT
        '===================================
        Dim viewMappings = DB.Global.CreateFieldMappingsDictionary(
            New System.String() {"articleId"},
            New System.String() {"ArticleId"},
            New Boolean() {True},
            Nothing,  ' No primary key array - using explicit keyFields parameter below
            New Object() {Nothing}
        )

        Return DB.Global.ProcessActionLink(DB,
            DB.Global.CreateValidator(New System.String() {"articleId"}),
            DB.Global.CreateBusinessLogicForWriting(
                "P_TestingTableArticles",
                viewMappings,
                New System.String() {"ArticleId"},
                True,
                Nothing,
                "UPDATE P_TestingTableArticles SET ViewCount = ViewCount + 1, LastModifiedDate = GETDATE() WHERE ArticleId = :ArticleId",
                Nothing
            ),
            "View count incremented",
            ParsedPayloadComplete, StringPayloadComplete, False)

    Else
        Return DB.Global.CreateErrorResponse("'" & destinationId & "' is not a valid DestinationIdentifier")
    End If
Else
    Return DB.Global.CreateErrorResponse(DestinationIdentifierInfo.Item2)
End If


' ===================================
' EXAMPLE PAYLOADS FOR COMPLETE ENDPOINT
' ===================================

' 1. READ - Get all published Technology articles:
' {
'   "DestinationIdentifier": "article-read",
'   "Category": "Technology",
'   "IsPublished": 1
' }

' 2. READ - Search by title:
' {
'   "DestinationIdentifier": "article-read",
'   "Title": "%SQL%"
' }

' 3. READ - Get featured articles with high rating:
' {
'   "DestinationIdentifier": "article-read",
'   "IsFeatured": 1,
'   "MinRating": 4.5
' }

' 4. WRITE - Batch insert/update articles:
' {
'   "DestinationIdentifier": "article-write",
'   "Records": [
'     {
'       "articleId": 101,
'       "title": "Test Article 1",
'       "content": "Content 1",
'       "category": "Technology",
'       "author": "John Doe",
'       "rating": 4.5,
'       "isPublished": 1
'     },
'     {
'       "articleId": 102,
'       "title": "Test Article 2",
'       "content": "Content 2",
'       "category": "Science",
'       "author": "Jane Smith",
'       "rating": 4.8,
'       "isPublished": 1
'     }
'   ]
' }

' 5. STATS - Get article statistics by category:
' {
'   "DestinationIdentifier": "article-stats",
'   "IsPublished": 1
' }

' 6. INCREMENT VIEWS - Track article view:
' {
'   "DestinationIdentifier": "article-increment-views",
'   "articleId": 1
' }


' ===================================
' TESTING CHECKLIST
' ===================================
'
' 1. Execute the DROP and CREATE TABLE SQL queries first
' 2. Optionally insert sample data using the INSERT SQL
' 3. Test READ operations:
'    - Read all articles (empty payload)
'    - Read by specific ID
'    - Search by title with wildcard
'    - Filter by category
'    - Filter by rating and view count
'    - Date range queries
' 4. Test WRITE operations:
'    - Insert new article
'    - Update existing article (upsert)
'    - Batch insert multiple articles
'    - Custom updates (views, likes, publish)
' 5. Test COMPLETE ENDPOINT with DestinationIdentifier:
'    - article-read
'    - article-write
'    - article-stats
'    - article-increment-views
' 6. Verify error handling:
'    - Missing required fields
'    - Invalid DestinationIdentifier
'    - Duplicate key on insert-only operation
