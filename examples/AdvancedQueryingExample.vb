' ===================================
' ADVANCED QUERYING PATTERNS
' Complex filtering, aggregations, joins, and analytical queries
' ===================================
'
' This example demonstrates:
' - Complex WHERE clause construction
' - Date range filtering
' - Numeric range queries
' - Pattern matching and full-text search
' - Aggregations and GROUP BY
' - JOIN operations
' - Subqueries and CTEs
' - Window functions
' - Conditional logic in queries
' - Dynamic sorting
'
' TABLE SCHEMAS (example):
'
' CREATE TABLE Sales (
'     SaleId VARCHAR(50) PRIMARY KEY,
'     ProductId VARCHAR(50) NOT NULL,
'     CustomerId VARCHAR(50) NOT NULL,
'     SaleDate DATETIME NOT NULL,
'     Quantity INT NOT NULL,
'     UnitPrice DECIMAL(18,2) NOT NULL,
'     TotalAmount DECIMAL(18,2) NOT NULL,
'     DiscountPercent DECIMAL(5,2) DEFAULT 0,
'     Region VARCHAR(50),
'     SalesPersonId VARCHAR(50),
'     Notes NVARCHAR(MAX)
' )
'
' CREATE TABLE Products (
'     ProductId VARCHAR(50) PRIMARY KEY,
'     ProductName NVARCHAR(200),
'     Category VARCHAR(50),
'     UnitCost DECIMAL(18,2),
'     IsActive BIT
' )
'
' CREATE TABLE Customers (
'     CustomerId VARCHAR(50) PRIMARY KEY,
'     CustomerName NVARCHAR(200),
'     CustomerType VARCHAR(20),
'     Region VARCHAR(50),
'     CreditLimit DECIMAL(18,2)
' )

' ===================================
' EXAMPLE 1: COMPLEX DATE RANGE AND NUMERIC FILTERING
' ===================================
' Multiple filters with flexible combinations

Dim CheckToken1 = False
Dim StringPayload1 = "" : Dim ParsedPayload1
Dim PayloadError1 = DB.Global.ValidatePayloadAndToken(DB, CheckToken1, "ComplexSearch", ParsedPayload1, StringPayload1)
If PayloadError1 IsNot Nothing Then
    Return PayloadError1
End If

' Define comprehensive search conditions
Dim complexConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

' Exact match filters
complexConditions.Add("SaleId", DB.Global.CreateParameterCondition(
    "SaleId",
    "SaleId = :SaleId",
    Nothing
))

complexConditions.Add("ProductId", DB.Global.CreateParameterCondition(
    "ProductId",
    "ProductId = :ProductId",
    Nothing
))

complexConditions.Add("CustomerId", DB.Global.CreateParameterCondition(
    "CustomerId",
    "CustomerId = :CustomerId",
    Nothing
))

complexConditions.Add("Region", DB.Global.CreateParameterCondition(
    "Region",
    "Region = :Region",
    Nothing
))

complexConditions.Add("SalesPersonId", DB.Global.CreateParameterCondition(
    "SalesPersonId",
    "SalesPersonId = :SalesPersonId",
    Nothing
))

' Date range filters
complexConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "SaleDate >= :StartDate",
    Nothing
))

complexConditions.Add("EndDate", DB.Global.CreateParameterCondition(
    "EndDate",
    "SaleDate <= :EndDate",
    Nothing
))

complexConditions.Add("ExactDate", DB.Global.CreateParameterCondition(
    "ExactDate",
    "CAST(SaleDate AS DATE) = CAST(:ExactDate AS DATE)",
    Nothing
))

' Numeric range filters
complexConditions.Add("MinAmount", DB.Global.CreateParameterCondition(
    "MinAmount",
    "TotalAmount >= :MinAmount",
    Nothing
))

complexConditions.Add("MaxAmount", DB.Global.CreateParameterCondition(
    "MaxAmount",
    "TotalAmount <= :MaxAmount",
    Nothing
))

complexConditions.Add("MinQuantity", DB.Global.CreateParameterCondition(
    "MinQuantity",
    "Quantity >= :MinQuantity",
    Nothing
))

complexConditions.Add("MaxQuantity", DB.Global.CreateParameterCondition(
    "MaxQuantity",
    "Quantity <= :MaxQuantity",
    Nothing
))

complexConditions.Add("MinDiscount", DB.Global.CreateParameterCondition(
    "MinDiscount",
    "DiscountPercent >= :MinDiscount",
    Nothing
))

' Pattern matching
complexConditions.Add("NoteKeyword", DB.Global.CreateParameterCondition(
    "NoteKeyword",
    "Notes LIKE :NoteKeyword",
    Nothing
))

' Create read logic with explicit field selection
Dim complexLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT SaleId, ProductId, CustomerId, SaleDate, Quantity, UnitPrice, " &
    "TotalAmount, DiscountPercent, Region, SalesPersonId " &
    "FROM Sales {WHERE} " &
    "ORDER BY SaleDate DESC, TotalAmount DESC",
    complexConditions,
    Nothing  ' No default WHERE - allows unrestricted search with filters
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, complexLogic,
    "Complex search completed",
    ParsedPayload1, StringPayload1, False
)

' EXAMPLE PAYLOADS:
'
' 1. Date range with minimum amount:
' {
'   "StartDate": "2025-01-01",
'   "EndDate": "2025-03-31",
'   "MinAmount": 1000
' }
'
' 2. Region and date filter:
' {
'   "Region": "North",
'   "StartDate": "2025-01-01",
'   "MaxAmount": 5000
' }
'
' 3. Complex combination:
' {
'   "Region": "East",
'   "StartDate": "2025-01-01",
'   "EndDate": "2025-12-31",
'   "MinAmount": 500,
'   "MaxAmount": 10000,
'   "MinQuantity": 5,
'   "MinDiscount": 10
' }
'
' 4. Pattern search in notes:
' {
'   "NoteKeyword": "%urgent%",
'   "StartDate": "2025-01-01"
' }


' ===================================
' EXAMPLE 2: AGGREGATIONS AND GROUP BY
' ===================================
' Statistical analysis with multiple aggregation functions

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "SalesAnalytics", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim analyticsConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

analyticsConditions.Add("Region", DB.Global.CreateParameterCondition(
    "Region",
    "Region = :Region",
    Nothing
))

analyticsConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "SaleDate >= :StartDate",
    Nothing
))

analyticsConditions.Add("EndDate", DB.Global.CreateParameterCondition(
    "EndDate",
    "SaleDate <= :EndDate",
    Nothing
))

' Comprehensive aggregation query
Dim analyticsLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "Region, " &
    "COUNT(*) as SaleCount, " &
    "SUM(TotalAmount) as TotalRevenue, " &
    "AVG(TotalAmount) as AverageOrderValue, " &
    "MIN(TotalAmount) as SmallestSale, " &
    "MAX(TotalAmount) as LargestSale, " &
    "SUM(Quantity) as TotalUnitsSold, " &
    "AVG(DiscountPercent) as AverageDiscount, " &
    "COUNT(DISTINCT CustomerId) as UniqueCustomers, " &
    "COUNT(DISTINCT ProductId) as UniqueProducts, " &
    "COUNT(DISTINCT SalesPersonId) as ActiveSalespeople, " &
    "MIN(SaleDate) as FirstSaleDate, " &
    "MAX(SaleDate) as LastSaleDate " &
    "FROM Sales {WHERE} " &
    "GROUP BY Region " &
    "ORDER BY TotalRevenue DESC",
    analyticsConditions,
    Nothing,
    Nothing,
    True  ' FOR JSON PATH for performance
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, analyticsLogic,
    "Analytics retrieved",
    ParsedPayload2, StringPayload2, False
)

' PAYLOAD:
' {
'   "StartDate": "2025-01-01",
'   "EndDate": "2025-12-31"
' }
'
' RESPONSE:
' {
'   "Result": "OK",
'   "Records": [
'     {
'       "Region": "North",
'       "SaleCount": 1250,
'       "TotalRevenue": 2500000.00,
'       "AverageOrderValue": 2000.00,
'       "SmallestSale": 10.00,
'       "LargestSale": 50000.00,
'       "TotalUnitsSold": 15000,
'       "AverageDiscount": 5.2,
'       "UniqueCustomers": 320,
'       "UniqueProducts": 150,
'       "ActiveSalespeople": 25
'     }
'   ]
' }


' ===================================
' EXAMPLE 3: JOINS AND RELATED DATA
' ===================================
' Query multiple related tables with JOIN operations

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "SalesWithDetails", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim joinConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

joinConditions.Add("CustomerName", DB.Global.CreateParameterCondition(
    "CustomerName",
    "c.CustomerName LIKE :CustomerName",
    Nothing
))

joinConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "p.Category = :Category",
    Nothing
))

joinConditions.Add("MinAmount", DB.Global.CreateParameterCondition(
    "MinAmount",
    "s.TotalAmount >= :MinAmount",
    Nothing
))

joinConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "s.SaleDate >= :StartDate",
    Nothing
))

' Complex JOIN query with multiple tables
Dim joinLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "s.SaleId, " &
    "s.SaleDate, " &
    "s.TotalAmount, " &
    "s.Quantity, " &
    "s.DiscountPercent, " &
    "c.CustomerId, " &
    "c.CustomerName, " &
    "c.CustomerType, " &
    "c.Region as CustomerRegion, " &
    "p.ProductId, " &
    "p.ProductName, " &
    "p.Category, " &
    "(s.TotalAmount - (s.Quantity * p.UnitCost)) as GrossProfit, " &
    "((s.TotalAmount - (s.Quantity * p.UnitCost)) / s.TotalAmount * 100) as ProfitMarginPercent " &
    "FROM Sales s " &
    "INNER JOIN Customers c ON s.CustomerId = c.CustomerId " &
    "INNER JOIN Products p ON s.ProductId = p.ProductId " &
    "{WHERE} " &
    "ORDER BY s.SaleDate DESC",
    joinConditions,
    "p.IsActive = 1",  ' Default: only active products
    Nothing,
    True  ' FOR JSON PATH
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, joinLogic,
    "Sales with details retrieved",
    ParsedPayload3, StringPayload3, False
)

' PAYLOAD:
' {
'   "Category": "Electronics",
'   "MinAmount": 500,
'   "StartDate": "2025-01-01"
' }


' ===================================
' EXAMPLE 4: SUBQUERIES AND ADVANCED FILTERING
' ===================================
' Using subqueries for complex business logic

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "TopCustomers", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim subqueryConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

subqueryConditions.Add("MinTotalSpent", DB.Global.CreateParameterCondition(
    "MinTotalSpent",
    "TotalSpent >= :MinTotalSpent",
    Nothing
))

subqueryConditions.Add("MinOrderCount", DB.Global.CreateParameterCondition(
    "MinOrderCount",
    "OrderCount >= :MinOrderCount",
    Nothing
))

subqueryConditions.Add("Region", DB.Global.CreateParameterCondition(
    "Region",
    "Region = :Region",
    Nothing
))

' Query with subquery for customer lifetime value
Dim subqueryLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "c.CustomerId, " &
    "c.CustomerName, " &
    "c.CustomerType, " &
    "c.Region, " &
    "CustomerStats.OrderCount, " &
    "CustomerStats.TotalSpent, " &
    "CustomerStats.AverageOrderValue, " &
    "CustomerStats.LastPurchaseDate, " &
    "CustomerStats.DaysSinceLastPurchase, " &
    "CASE " &
    "  WHEN CustomerStats.TotalSpent > 100000 THEN 'Platinum' " &
    "  WHEN CustomerStats.TotalSpent > 50000 THEN 'Gold' " &
    "  WHEN CustomerStats.TotalSpent > 10000 THEN 'Silver' " &
    "  ELSE 'Bronze' " &
    "END as CustomerTier " &
    "FROM Customers c " &
    "INNER JOIN ( " &
    "  SELECT " &
    "    CustomerId, " &
    "    COUNT(*) as OrderCount, " &
    "    SUM(TotalAmount) as TotalSpent, " &
    "    AVG(TotalAmount) as AverageOrderValue, " &
    "    MAX(SaleDate) as LastPurchaseDate, " &
    "    DATEDIFF(DAY, MAX(SaleDate), GETDATE()) as DaysSinceLastPurchase " &
    "  FROM Sales " &
    "  GROUP BY CustomerId " &
    ") CustomerStats ON c.CustomerId = CustomerStats.CustomerId " &
    "{WHERE} " &
    "ORDER BY CustomerStats.TotalSpent DESC",
    subqueryConditions,
    Nothing,
    Nothing,
    True
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, subqueryLogic,
    "Top customers retrieved",
    ParsedPayload4, StringPayload4, False
)

' PAYLOAD:
' {
'   "MinTotalSpent": 10000,
'   "MinOrderCount": 5
' }


' ===================================
' EXAMPLE 5: WINDOW FUNCTIONS AND RANKING
' ===================================
' Advanced analytical queries with ranking and partitioning

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "SalesRanking", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

Dim rankingConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

rankingConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "SaleDate >= :StartDate",
    Nothing
))

rankingConditions.Add("EndDate", DB.Global.CreateParameterCondition(
    "EndDate",
    "SaleDate <= :EndDate",
    Nothing
))

' Window functions for ranking and running totals
Dim rankingLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "SaleId, " &
    "SaleDate, " &
    "ProductId, " &
    "CustomerId, " &
    "TotalAmount, " &
    "Region, " &
    "ROW_NUMBER() OVER (PARTITION BY Region ORDER BY TotalAmount DESC) as RankInRegion, " &
    "RANK() OVER (ORDER BY TotalAmount DESC) as OverallRank, " &
    "DENSE_RANK() OVER (PARTITION BY Region ORDER BY TotalAmount DESC) as DenseRankInRegion, " &
    "SUM(TotalAmount) OVER (PARTITION BY Region ORDER BY SaleDate) as RunningTotalInRegion, " &
    "AVG(TotalAmount) OVER (PARTITION BY Region) as RegionAverage, " &
    "TotalAmount - AVG(TotalAmount) OVER (PARTITION BY Region) as DeviationFromRegionAvg, " &
    "PERCENT_RANK() OVER (PARTITION BY Region ORDER BY TotalAmount) as PercentileInRegion " &
    "FROM Sales " &
    "{WHERE} " &
    "ORDER BY Region, RankInRegion",
    rankingConditions,
    Nothing,
    Nothing,
    True
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, rankingLogic,
    "Sales ranking retrieved",
    ParsedPayload5, StringPayload5, False
)

' PAYLOAD:
' {
'   "StartDate": "2025-01-01",
'   "EndDate": "2025-12-31"
' }


' ===================================
' EXAMPLE 6: TIME-BASED AGGREGATIONS
' ===================================
' Daily, weekly, monthly sales trends

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "SalesTrends", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim trendsConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

trendsConditions.Add("Granularity", DB.Global.CreateParameterCondition(
    "Granularity",
    "1=1",  ' Always true - granularity handled in SELECT
    Nothing
))

trendsConditions.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "SaleDate >= :StartDate",
    Nothing
))

trendsConditions.Add("EndDate", DB.Global.CreateParameterCondition(
    "EndDate",
    "SaleDate <= :EndDate",
    Nothing
))

' Get granularity parameter (daily, weekly, monthly)
Dim granularityResult = DB.Global.GetStringParameter(ParsedPayload6, "Granularity")
Dim granularity As System.String = If(granularityResult.Item1, granularityResult.Item2.ToUpper(), "DAILY")

' Dynamic GROUP BY based on granularity
Dim dateGrouping As System.String
Select Case granularity
    Case "DAILY"
        dateGrouping = "CAST(SaleDate AS DATE)"
    Case "WEEKLY"
        dateGrouping = "DATEPART(YEAR, SaleDate), DATEPART(WEEK, SaleDate)"
    Case "MONTHLY"
        dateGrouping = "DATEPART(YEAR, SaleDate), DATEPART(MONTH, SaleDate)"
    Case "QUARTERLY"
        dateGrouping = "DATEPART(YEAR, SaleDate), DATEPART(QUARTER, SaleDate)"
    Case "YEARLY"
        dateGrouping = "DATEPART(YEAR, SaleDate)"
    Case Else
        dateGrouping = "CAST(SaleDate AS DATE)"  ' Default to daily
End Select

Dim trendsLogic = DB.Global.CreateBusinessLogicForReading(
    $"SELECT " &
    $"{dateGrouping} as Period, " &
    "COUNT(*) as SaleCount, " &
    "SUM(TotalAmount) as Revenue, " &
    "AVG(TotalAmount) as AverageOrderValue, " &
    "SUM(Quantity) as UnitsSold, " &
    "COUNT(DISTINCT CustomerId) as UniqueCustomers, " &
    "MIN(SaleDate) as PeriodStart, " &
    "MAX(SaleDate) as PeriodEnd " &
    "FROM Sales {WHERE} " &
    $"GROUP BY {dateGrouping} " &
    "ORDER BY Period",
    trendsConditions,
    Nothing,
    Nothing,
    True
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, trendsLogic,
    "Sales trends retrieved",
    ParsedPayload6, StringPayload6, False
)

' PAYLOAD:
' {
'   "Granularity": "MONTHLY",
'   "StartDate": "2025-01-01",
'   "EndDate": "2025-12-31"
' }


' ===================================
' EXAMPLE 7: COHORT ANALYSIS
' ===================================
' Customer cohort analysis by first purchase date

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "CohortAnalysis", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim cohortConditions As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

cohortConditions.Add("CohortStartDate", DB.Global.CreateParameterCondition(
    "CohortStartDate",
    "FirstPurchaseMonth >= :CohortStartDate",
    Nothing
))

' Cohort analysis with CTE
Dim cohortLogic = DB.Global.CreateBusinessLogicForReading(
    "WITH CustomerCohorts AS ( " &
    "  SELECT " &
    "    CustomerId, " &
    "    DATEFROMPARTS(DATEPART(YEAR, MIN(SaleDate)), DATEPART(MONTH, MIN(SaleDate)), 1) as FirstPurchaseMonth " &
    "  FROM Sales " &
    "  GROUP BY CustomerId " &
    "), " &
    "CohortMetrics AS ( " &
    "  SELECT " &
    "    cc.FirstPurchaseMonth, " &
    "    DATEDIFF(MONTH, cc.FirstPurchaseMonth, s.SaleDate) as MonthsSinceFirst, " &
    "    COUNT(DISTINCT s.CustomerId) as ActiveCustomers, " &
    "    COUNT(*) as OrderCount, " &
    "    SUM(s.TotalAmount) as Revenue, " &
    "    AVG(s.TotalAmount) as AvgOrderValue " &
    "  FROM CustomerCohorts cc " &
    "  INNER JOIN Sales s ON cc.CustomerId = s.CustomerId " &
    "  GROUP BY cc.FirstPurchaseMonth, DATEDIFF(MONTH, cc.FirstPurchaseMonth, s.SaleDate) " &
    ") " &
    "SELECT * FROM CohortMetrics " &
    "{WHERE} " &
    "ORDER BY FirstPurchaseMonth, MonthsSinceFirst",
    cohortConditions,
    Nothing,
    Nothing,
    True
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, cohortLogic,
    "Cohort analysis retrieved",
    ParsedPayload7, StringPayload7, False
)


' ===================================
' EXAMPLE 8: FULL-TEXT SEARCH SIMULATION
' ===================================
' Advanced text search across multiple fields

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "FullTextSearch", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

Dim searchConditions8 As New System.Collections.Generic.Dictionary(Of System.String, System.Object)

' Multiple LIKE conditions for comprehensive search
searchConditions8.Add("SearchTerm", DB.Global.CreateParameterCondition(
    "SearchTerm",
    "(Notes LIKE :SearchTerm OR " &
    "CAST(SaleId AS VARCHAR) LIKE :SearchTerm OR " &
    "CAST(CustomerId AS VARCHAR) LIKE :SearchTerm OR " &
    "CAST(ProductId AS VARCHAR) LIKE :SearchTerm)",
    Nothing
))

searchConditions8.Add("StartDate", DB.Global.CreateParameterCondition(
    "StartDate",
    "SaleDate >= :StartDate",
    Nothing
))

Dim searchLogic8 = DB.Global.CreateBusinessLogicForReading(
    "SELECT SaleId, CustomerId, ProductId, SaleDate, TotalAmount, Notes, " &
    "CASE " &
    "  WHEN Notes LIKE :SearchTerm THEN 'Notes' " &
    "  WHEN CAST(SaleId AS VARCHAR) LIKE :SearchTerm THEN 'SaleId' " &
    "  WHEN CAST(CustomerId AS VARCHAR) LIKE :SearchTerm THEN 'CustomerId' " &
    "  WHEN CAST(ProductId AS VARCHAR) LIKE :SearchTerm THEN 'ProductId' " &
    "END as MatchedField " &
    "FROM Sales " &
    "{WHERE} " &
    "ORDER BY SaleDate DESC",
    searchConditions8,
    Nothing,
    Nothing,
    True
)

Return DB.Global.ProcessActionLink(
    DB, Nothing, searchLogic8,
    "Search completed",
    ParsedPayload8, StringPayload8, False
)

' PAYLOAD:
' {
'   "SearchTerm": "%urgent%"
' }


' ===================================
' ADVANCED QUERYING BEST PRACTICES
' ===================================
'
' PARAMETER CONDITIONS:
' ✓ Use specific conditions for better query optimization
' ✓ Leverage database indexes with equality/range conditions
' ✓ Use LIKE with leading wildcards sparingly (can't use indexes)
' ✓ Combine multiple conditions for flexible filtering
'
' AGGREGATIONS:
' ✓ Use appropriate aggregate functions (COUNT, SUM, AVG, MIN, MAX)
' ✓ Group by appropriate dimensions
' ✓ Use HAVING for aggregate filtering
' ✓ Consider performance with large datasets
'
' JOINS:
' ✓ Use appropriate JOIN types (INNER, LEFT, RIGHT, FULL)
' ✓ Index foreign key columns
' ✓ Limit result sets with WHERE clauses
' ✓ Select only needed columns
'
' SUBQUERIES & CTEs:
' ✓ Use CTEs for readability and complex logic
' ✓ Consider subquery performance vs. JOINs
' ✓ Avoid correlated subqueries when possible
' ✓ Test query plans for optimization
'
' WINDOW FUNCTIONS:
' ✓ Use for rankings, running totals, moving averages
' ✓ Understand PARTITION BY and ORDER BY
' ✓ Consider performance with large datasets
' ✓ Window functions execute after WHERE clause
'
' PERFORMANCE:
' ✓ Use explicit column lists (not SELECT *)
' ✓ Add appropriate indexes
' ✓ Use FOR JSON PATH for large result sets
' ✓ Limit result sets with TOP/LIMIT
' ✓ Test with production-scale data
' ✓ Monitor query execution plans
' ✓ Use query hints when necessary
