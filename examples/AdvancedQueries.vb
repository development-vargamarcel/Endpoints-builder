' ===================================
' ADVANCED QUERY EXAMPLES
' Complex queries with parameter conditions, date ranges, and custom SQL
' ===================================

'---------------------------------------
' EXAMPLE 1: DATE RANGE FILTERING
'---------------------------------------
' Search records within a date range

Dim CheckToken = False
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "DateRangeSearch", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then
    Return PayloadError
End If

' Define parameter conditions for date range
Dim dateConditions As New System.Collections.Generic.Dictionary(Of String, Object)

dateConditions.Add("startDate", DB.Global.CreateParameterCondition(
    "startDate",
    "CreatedDate >= :startDate",  ' Applied when startDate is provided
    Nothing                        ' No condition when absent
))

dateConditions.Add("endDate", DB.Global.CreateParameterCondition(
    "endDate",
    "CreatedDate <= :endDate",
    Nothing
))

dateConditions.Add("Status", DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    Nothing
))

Dim dateRangeLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Orders {WHERE} ORDER BY CreatedDate DESC",
    dateConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    dateRangeLogic,
    "Orders date range search",
    ParsedPayload,
    StringPayload,
    False
)

' Example payloads:
' { "startDate": "2025-01-01", "endDate": "2025-01-31" }                    - Orders in January
' { "startDate": "2025-01-01", "Status": "Completed" }                      - From Jan 1, completed only
' { "endDate": "2025-01-31" }                                                - Up to Jan 31
' {}                                                                         - All orders

'---------------------------------------
' EXAMPLE 2: NUMERIC RANGE FILTERING
'---------------------------------------
' Filter by numeric ranges (price, quantity, etc.)

Dim StringPayload2 = "" : Dim ParsedPayload2
Dim PayloadError2 = DB.Global.ValidatePayloadAndToken(DB, False, "PriceRange", ParsedPayload2, StringPayload2)
If PayloadError2 IsNot Nothing Then
    Return PayloadError2
End If

Dim priceConditions As New System.Collections.Generic.Dictionary(Of String, Object)

priceConditions.Add("minPrice", DB.Global.CreateParameterCondition(
    "minPrice",
    "Price >= :minPrice",
    Nothing
))

priceConditions.Add("maxPrice", DB.Global.CreateParameterCondition(
    "maxPrice",
    "Price <= :maxPrice",
    Nothing
))

priceConditions.Add("minQuantity", DB.Global.CreateParameterCondition(
    "minQuantity",
    "Quantity >= :minQuantity",
    Nothing
))

priceConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

Dim priceRangeLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ProductId, Name, Price, Quantity, Category FROM Products {WHERE} ORDER BY Price ASC",
    priceConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    priceRangeLogic,
    "Products price range search",
    ParsedPayload2,
    StringPayload2,
    False
)

' Payloads:
' { "minPrice": 10, "maxPrice": 100 }              - Products between $10 and $100
' { "Category": "Electronics", "maxPrice": 500 }   - Electronics under $500
' { "minQuantity": 10 }                            - Products with at least 10 in stock

'---------------------------------------
' EXAMPLE 3: SEARCH WITH DEFAULT CONDITIONS
'---------------------------------------
' Apply default WHERE clause when no parameters provided

Dim StringPayload3 = "" : Dim ParsedPayload3
Dim PayloadError3 = DB.Global.ValidatePayloadAndToken(DB, False, "ActiveProducts", ParsedPayload3, StringPayload3)
If PayloadError3 IsNot Nothing Then
    Return PayloadError3
End If

Dim activeConditions As New System.Collections.Generic.Dictionary(Of String, Object)

activeConditions.Add("ProductId", DB.Global.CreateParameterCondition(
    "ProductId",
    "ProductId = :ProductId",
    Nothing
))

activeConditions.Add("Name", DB.Global.CreateParameterCondition(
    "Name",
    "Name LIKE :Name",
    Nothing
))

' Default WHERE clause applied when no parameters provided
Dim defaultWhereClause = "IsActive = 1 AND Quantity > 0"

Dim activeProductsLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Products {WHERE}",
    activeConditions,
    Nothing,
    defaultWhereClause,  ' Default: only active products with stock
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    activeProductsLogic,
    "Active products search",
    ParsedPayload3,
    StringPayload3,
    False
)

' Payloads:
' {}                              - Returns only active products with stock (default)
' { "Name": "Laptop%" }           - Active products with stock, name starts with "Laptop"
' { "ProductId": "123" }          - Specific product if active and in stock

'---------------------------------------
' EXAMPLE 4: COMPLEX AND/OR CONDITIONS
'---------------------------------------
' Combine multiple conditions with different logic

Dim StringPayload4 = "" : Dim ParsedPayload4
Dim PayloadError4 = DB.Global.ValidatePayloadAndToken(DB, False, "ComplexSearch", ParsedPayload4, StringPayload4)
If PayloadError4 IsNot Nothing Then
    Return PayloadError4
End If

Dim complexConditions As New System.Collections.Generic.Dictionary(Of String, Object)

' Priority filter: if provided, must be High or Critical
complexConditions.Add("Priority", DB.Global.CreateParameterCondition(
    "Priority",
    "(Priority = :Priority)",
    "1=1"  ' No restriction if not provided
))

' Status filter
complexConditions.Add("Status", DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    "Status IN ('New', 'InProgress')"  ' Default: only active statuses
))

' Assigned user
complexConditions.Add("AssignedTo", DB.Global.CreateParameterCondition(
    "AssignedTo",
    "AssignedTo = :AssignedTo",
    Nothing
))

Dim complexLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Tickets {WHERE} ORDER BY Priority DESC, CreatedDate DESC",
    complexConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    complexLogic,
    "Complex ticket search",
    ParsedPayload4,
    StringPayload4,
    False
)

' Payloads:
' {}                                                    - Returns New and InProgress tickets
' { "Priority": "High" }                                - High priority New/InProgress tickets
' { "Status": "Closed", "AssignedTo": "john@ex.com" }  - Closed tickets assigned to John

'---------------------------------------
' EXAMPLE 5: IN CLAUSE SIMULATION
'---------------------------------------
' Simulate IN clause for multiple values

Dim StringPayload5 = "" : Dim ParsedPayload5
Dim PayloadError5 = DB.Global.ValidatePayloadAndToken(DB, False, "MultiStatus", ParsedPayload5, StringPayload5)
If PayloadError5 IsNot Nothing Then
    Return PayloadError5
End If

Dim inConditions As New System.Collections.Generic.Dictionary(Of String, Object)

' Note: For actual IN clause with array, you'd need custom logic
' This example shows how to handle predefined sets
inConditions.Add("StatusGroup", DB.Global.CreateParameterCondition(
    "StatusGroup",
    "Status IN ('New', 'InProgress', 'Pending')",  ' Active statuses
    "1=1"  ' No filter if not specified
))

inConditions.Add("Department", DB.Global.CreateParameterCondition(
    "Department",
    "Department = :Department",
    Nothing
))

Dim inLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Tasks {WHERE} ORDER BY DueDate ASC",
    inConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    inLogic,
    "Tasks by status group",
    ParsedPayload5,
    StringPayload5,
    False
)

' Payloads:
' { "StatusGroup": "Active" }              - Only active tasks (New, InProgress, Pending)
' { "Department": "IT" }                   - All IT tasks
' {}                                       - All tasks

'---------------------------------------
' EXAMPLE 6: AGGREGATE QUERIES
'---------------------------------------
' Queries with GROUP BY and aggregate functions

Dim StringPayload6 = "" : Dim ParsedPayload6
Dim PayloadError6 = DB.Global.ValidatePayloadAndToken(DB, False, "SalesSummary", ParsedPayload6, StringPayload6)
If PayloadError6 IsNot Nothing Then
    Return PayloadError6
End If

Dim aggregateConditions As New System.Collections.Generic.Dictionary(Of String, Object)

aggregateConditions.Add("startDate", DB.Global.CreateParameterCondition(
    "startDate",
    "OrderDate >= :startDate",
    Nothing
))

aggregateConditions.Add("endDate", DB.Global.CreateParameterCondition(
    "endDate",
    "OrderDate <= :endDate",
    Nothing
))

aggregateConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

Dim aggregateLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "Category, " &
    "COUNT(*) as OrderCount, " &
    "SUM(Amount) as TotalSales, " &
    "AVG(Amount) as AverageSale, " &
    "MIN(Amount) as MinSale, " &
    "MAX(Amount) as MaxSale " &
    "FROM Orders {WHERE} " &
    "GROUP BY Category " &
    "ORDER BY TotalSales DESC",
    aggregateConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    aggregateLogic,
    "Sales summary",
    ParsedPayload6,
    StringPayload6,
    False
)

' Payloads:
' { "startDate": "2025-01-01", "endDate": "2025-01-31" }   - January sales by category
' { "Category": "Electronics" }                             - Electronics sales only
' {}                                                        - All-time sales by category

'---------------------------------------
' EXAMPLE 7: SUBQUERY EXAMPLE
'---------------------------------------
' Using subqueries in custom SQL

Dim StringPayload7 = "" : Dim ParsedPayload7
Dim PayloadError7 = DB.Global.ValidatePayloadAndToken(DB, False, "TopCustomers", ParsedPayload7, StringPayload7)
If PayloadError7 IsNot Nothing Then
    Return PayloadError7
End If

Dim subqueryConditions As New System.Collections.Generic.Dictionary(Of String, Object)

subqueryConditions.Add("minOrders", DB.Global.CreateParameterCondition(
    "minOrders",
    "OrderCount >= :minOrders",
    Nothing
))

subqueryConditions.Add("minTotal", DB.Global.CreateParameterCondition(
    "minTotal",
    "TotalAmount >= :minTotal",
    Nothing
))

Dim subqueryLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT " &
    "c.CustomerId, " &
    "c.Name, " &
    "c.Email, " &
    "COUNT(o.OrderId) as OrderCount, " &
    "SUM(o.Amount) as TotalAmount " &
    "FROM Customers c " &
    "INNER JOIN Orders o ON c.CustomerId = o.CustomerId " &
    "GROUP BY c.CustomerId, c.Name, c.Email " &
    "HAVING 1=1 {WHERE} " &
    "ORDER BY TotalAmount DESC",
    subqueryConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    subqueryLogic,
    "Top customers",
    ParsedPayload7,
    StringPayload7,
    False
)

' Note: {WHERE} in HAVING clause - conditions are applied after GROUP BY
' Payloads:
' { "minOrders": 10 }                      - Customers with 10+ orders
' { "minTotal": 1000 }                     - Customers who spent $1000+
' { "minOrders": 5, "minTotal": 500 }     - Customers with 5+ orders and $500+ spent

'---------------------------------------
' EXAMPLE 8: FULL-TEXT SEARCH SIMULATION
'---------------------------------------
' Multiple field search

Dim StringPayload8 = "" : Dim ParsedPayload8
Dim PayloadError8 = DB.Global.ValidatePayloadAndToken(DB, False, "FullTextSearch", ParsedPayload8, StringPayload8)
If PayloadError8 IsNot Nothing Then
    Return PayloadError8
End If

Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)

' Search term applied to multiple fields
searchConditions.Add("searchTerm", DB.Global.CreateParameterCondition(
    "searchTerm",
    "(Title LIKE :searchTerm OR Description LIKE :searchTerm OR Tags LIKE :searchTerm)",
    Nothing
))

searchConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

Dim fullTextLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT ArticleId, Title, Description, Category, Tags, PublishedDate " &
    "FROM Articles {WHERE} " &
    "ORDER BY PublishedDate DESC",
    searchConditions,
    Nothing,
    "IsPublished = 1",  ' Default: only published articles
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    fullTextLogic,
    "Article search",
    ParsedPayload8,
    StringPayload8,
    False
)

' Payloads:
' { "searchTerm": "%API%" }                        - Articles mentioning "API" in title, description, or tags
' { "searchTerm": "%security%", "Category": "Tech" }  - Tech articles about security
' { "Category": "Business" }                       - All business articles

'---------------------------------------
' EXAMPLE 9: PAGINATION WITH TOP N
'---------------------------------------
' Limit results (TOP N pattern)

Dim StringPayload9 = "" : Dim ParsedPayload9
Dim PayloadError9 = DB.Global.ValidatePayloadAndToken(DB, False, "LatestOrders", ParsedPayload9, StringPayload9)
If PayloadError9 IsNot Nothing Then
    Return PayloadError9
End If

Dim paginationConditions As New System.Collections.Generic.Dictionary(Of String, Object)

paginationConditions.Add("CustomerId", DB.Global.CreateParameterCondition(
    "CustomerId",
    "CustomerId = :CustomerId",
    Nothing
))

paginationConditions.Add("Status", DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",
    Nothing
))

Dim paginatedLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT TOP 50 * FROM Orders {WHERE} ORDER BY OrderDate DESC",  ' Latest 50 orders
    paginationConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    paginatedLogic,
    "Latest orders",
    ParsedPayload9,
    StringPayload9,
    False
)

' Payloads:
' {}                                   - Latest 50 orders overall
' { "CustomerId": "123" }             - Latest 50 orders for customer 123
' { "Status": "Pending" }             - Latest 50 pending orders

'---------------------------------------
' EXAMPLE 10: DYNAMIC SORTING
'---------------------------------------
' Note: For true dynamic sorting, you'd need to modify the library
' This example shows different fixed sort patterns

Dim StringPayload10 = "" : Dim ParsedPayload10
Dim PayloadError10 = DB.Global.ValidatePayloadAndToken(DB, False, "SortedProducts", ParsedPayload10, StringPayload10)
If PayloadError10 IsNot Nothing Then
    Return PayloadError10
End If

' Example with price-based sorting
Dim sortConditions As New System.Collections.Generic.Dictionary(Of String, Object)

sortConditions.Add("Category", DB.Global.CreateParameterCondition(
    "Category",
    "Category = :Category",
    Nothing
))

sortConditions.Add("minPrice", DB.Global.CreateParameterCondition(
    "minPrice",
    "Price >= :minPrice",
    Nothing
))

Dim sortedLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM Products {WHERE} " &
    "ORDER BY " &
    "CASE WHEN Price < 10 THEN 1 WHEN Price < 100 THEN 2 ELSE 3 END, " &  ' Price tier
    "Name ASC",
    sortConditions,
    Nothing,
    Nothing,
    Nothing
)

Return DB.Global.ProcessActionLink(
    DB,
    Nothing,
    sortedLogic,
    "Products sorted by tier and name",
    ParsedPayload10,
    StringPayload10,
    False
)

' Products sorted by price tier, then alphabetically within each tier
