# Test Scenarios

Comprehensive test scenarios for the Endpoint Library.

## Testing Strategy

### Test Levels
1. **Unit Tests**: Individual functions
2. **Integration Tests**: Database operations
3. **Security Tests**: Validation and authorization
4. **Performance Tests**: Load and stress testing

## Core Functionality Tests

### 1. Read Operations

#### Test Case 1.1: Simple Read with Parameters
**Input:**
```json
{ "UserId": "123", "Email": "john%" }
```

**Expected SQL:**
```sql
SELECT * FROM Users WHERE UserId LIKE :UserId AND Email LIKE :Email
```

**Expected Result:**
- Status: OK
- Records: Matching users
- Excluded fields not present

#### Test Case 1.2: Read with No Parameters
**Input:**
```json
{}
```

**Expected SQL:**
```sql
SELECT * FROM Users
```

**Expected Result:**
- Status: OK
- Records: All users (or limited by default WHERE clause)

#### Test Case 1.3: Read with Excluded Fields
**Input:**
```json
{ "UserId": "123" }
```

**Configuration:**
```vb
excludeFields = New String() {"Password", "SSN"}
```

**Expected Result:**
- Password and SSN fields not in response
- Other fields present

### 2. Write Operations

#### Test Case 2.1: Insert New Record
**Input:**
```json
{ "UserId": "456", "Email": "new@example.com", "Name": "New User" }
```

**Precondition:** Record does not exist

**Expected Result:**
```json
{
  "Result": "OK",
  "Action": "INSERTED",
  "Message": "Record inserted successfully"
}
```

#### Test Case 2.2: Update Existing Record
**Input:**
```json
{ "UserId": "456", "Email": "updated@example.com" }
```

**Precondition:** Record exists, `allowUpdates = True`

**Expected Result:**
```json
{
  "Result": "OK",
  "Action": "UPDATED",
  "Message": "Record updated successfully"
}
```

#### Test Case 2.3: Insert Only (No Updates)
**Input:**
```json
{ "UserId": "456", "Email": "test@example.com" }
```

**Precondition:** Record exists, `allowUpdates = False`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "456 - Record already exists and updates are not allowed"
}
```

### 3. Batch Operations

#### Test Case 3.1: Batch Insert Multiple Records
**Input:**
```json
{
  "Records": [
    {"UserId": "101", "Email": "user1@example.com"},
    {"UserId": "102", "Email": "user2@example.com"},
    {"UserId": "103", "Email": "user3@example.com"}
  ]
}
```

**Precondition:** None of the records exist

**Expected Result:**
```json
{
  "Result": "OK",
  "Inserted": 3,
  "Updated": 0,
  "Errors": 0,
  "ErrorDetails": [],
  "Message": "Processed 3 records: 3 inserted, 0 updated, 0 errors."
}
```

#### Test Case 3.2: Batch with Errors
**Input:**
```json
{
  "Records": [
    {"UserId": "101", "Email": "valid@example.com"},
    {"Email": "missing-userid@example.com"},
    {"UserId": "103", "Email": "another-valid@example.com"}
  ]
}
```

**Expected Result:**
```json
{
  "Result": "PARTIAL",
  "Inserted": 2,
  "Updated": 0,
  "Errors": 1,
  "ErrorDetails": ["Record skipped - Missing required parameters: UserId"],
  "Message": "Processed 3 records: 2 inserted, 0 updated, 1 errors."
}
```

## Advanced Features Tests

### 4. Parameter Conditions

#### Test Case 4.1: Parameter Present
**Input:**
```json
{ "Status": "Active" }
```

**Configuration:**
```vb
CreateParameterCondition("Status", "Status = :Status", Nothing)
```

**Expected SQL:**
```sql
SELECT * FROM Users WHERE Status = :Status
```

#### Test Case 4.2: Parameter Absent
**Input:**
```json
{}
```

**Configuration:**
```vb
CreateParameterCondition("Status", "Status = :Status", "Status IS NOT NULL")
```

**Expected SQL:**
```sql
SELECT * FROM Users WHERE Status IS NOT NULL
```

#### Test Case 4.3: Date Range
**Input:**
```json
{ "startDate": "2025-01-01", "endDate": "2025-01-31" }
```

**Expected SQL:**
```sql
WHERE CreatedDate >= :startDate AND CreatedDate <= :endDate
```

### 5. Field Mappings

#### Test Case 5.1: JSON to SQL Mapping
**Input:**
```json
{ "userId": "123", "firstName": "John" }
```

**Mapping:**
```vb
"userId" -> "USER_ID"
"firstName" -> "FIRST_NAME"
```

**Expected SQL:**
```sql
WHERE USER_ID = :USER_ID
```

**Database Insert:**
- USER_ID = "123"
- FIRST_NAME = "John"

#### Test Case 5.2: Required Field Missing
**Input:**
```json
{ "userId": "123" }
```

**Mapping:**
```vb
"userId" -> "USER_ID" (required)
"email" -> "EMAIL" (required)
```

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Missing required fields: email"
}
```

#### Test Case 5.3: Default Values
**Input:**
```json
{ "userId": "123" }
```

**Mapping:**
```vb
"userId" -> "USER_ID" (required)
"status" -> "STATUS" (optional, default: "ACTIVE")
```

**Database Insert:**
- USER_ID = "123"
- STATUS = "ACTIVE" (default applied)

## Security Tests

### 6. Token Validation

#### Test Case 6.1: Valid Token
**Input:**
```json
{ "Token": "valid-token", "UserId": "123" }
```

**Configuration:** `CheckForToken = True`

**Expected Result:** Success (request processed)

#### Test Case 6.2: Missing Token
**Input:**
```json
{ "UserId": "123" }
```

**Configuration:** `CheckForToken = True`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Please insert the token in a property called Token."
}
```

#### Test Case 6.3: Invalid Token
**Input:**
```json
{ "Token": "invalid-token", "UserId": "123" }
```

**Configuration:** `CheckForToken = True`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Invalid token."
}
```

### 7. SQL Injection Protection

#### Test Case 7.1: Malicious Input in Parameter
**Input:**
```json
{ "UserId": "123'; DROP TABLE Users; --" }
```

**Expected Behavior:**
- Input is treated as literal string
- SQL: `WHERE UserId LIKE :UserId`
- Parameter: `UserId = "123'; DROP TABLE Users; --"`
- Result: No records found (or records matching literal string)
- **No SQL injection occurs**

#### Test Case 7.2: Union Injection Attempt
**Input:**
```json
{ "Email": "test@example.com' UNION SELECT * FROM Passwords --" }
```

**Expected Behavior:**
- Treated as literal string
- No unauthorized data access

### 8. Authorization Tests

#### Test Case 8.1: User Accessing Own Data
**Input:**
```json
{ "UserId": "123" }
```

**User Context:** UserId = "123"

**Expected Result:** Success

#### Test Case 8.2: User Accessing Other User's Data
**Input:**
```json
{ "UserId": "456" }
```

**User Context:** UserId = "123", Role = "USER"

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Unauthorized"
}
```

#### Test Case 8.3: Admin Accessing Any Data
**Input:**
```json
{ "UserId": "456" }
```

**User Context:** UserId = "123", Role = "ADMIN"

**Expected Result:** Success

### 9. Mass Assignment Protection

#### Test Case 9.1: Attempt to Modify Restricted Field
**Input:**
```json
{
  "userId": "123",
  "displayName": "New Name",
  "IsAdmin": true,
  "AccountBalance": 1000000
}
```

**Field Mappings:** Only "displayName" is mapped

**Expected Result:**
- displayName: Updated
- IsAdmin: Ignored (not in mappings)
- AccountBalance: Ignored (not in mappings)

## Validation Tests

### 10. Parameter Validation

#### Test Case 10.1: Required Parameter Present
**Input:**
```json
{ "UserId": "123", "Email": "test@example.com" }
```

**Validator:** `CreateValidator(New String() {"UserId", "Email"})`

**Expected Result:** Validation passes

#### Test Case 10.2: Required Parameter Missing
**Input:**
```json
{ "UserId": "123" }
```

**Validator:** `CreateValidator(New String() {"UserId", "Email"})`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Parameter Email not specified. Required parameters: UserId,Email"
}
```

### 11. Batch Validation

#### Test Case 11.1: Valid Records Array
**Input:**
```json
{ "Records": [{"UserId": "123"}] }
```

**Validator:** `CreateValidatorForBatch(New String() {"Records"})`

**Expected Result:** Validation passes

#### Test Case 11.2: Missing Records Array
**Input:**
```json
{ "UserId": "123" }
```

**Validator:** `CreateValidatorForBatch(New String() {"Records"})`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Parameter Records not specified"
}
```

#### Test Case 11.3: Records Not an Array
**Input:**
```json
{ "Records": "not-an-array" }
```

**Validator:** `CreateValidatorForBatch(New String() {"Records"})`

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Parameter Records must be an array"
}
```

## Error Handling Tests

### 12. Database Errors

#### Test Case 12.1: Table Not Found
**Configuration:** Table name = "NonExistentTable"

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Error reading records: ..."
}
```

#### Test Case 12.2: Invalid Column Name
**Configuration:** Field name = "NonExistentColumn"

**Expected Result:** Error response

### 13. Edge Cases

#### Test Case 13.1: Empty Payload
**Input:**
```json
{}
```

**Expected Behavior:**
- Validators: Fail if required params
- Read operations: Return all records or default WHERE clause
- Write operations: Fail if required params

#### Test Case 13.2: NULL Values
**Input:**
```json
{ "UserId": "123", "Email": null }
```

**Expected Behavior:**
- NULL value handled correctly
- Database INSERT/UPDATE accepts NULL

#### Test Case 13.3: Very Long String
**Input:**
```json
{ "Description": "[10000 character string]" }
```

**Expected Behavior:**
- Parameterized query handles length
- Database enforces column constraints

#### Test Case 13.4: Special Characters
**Input:**
```json
{ "Name": "O'Brien", "Description": "Test \"quotes\" and <tags>" }
```

**Expected Behavior:**
- All special characters handled correctly
- No SQL syntax errors

## Performance Tests

### 14. Load Testing

#### Test Case 14.1: Single Record Performance
- Measure time for single read/write operation
- Baseline: < 100ms

#### Test Case 14.2: Batch Performance
- Test batches of 10, 100, 1000 records
- Measure throughput (records/second)

#### Test Case 14.3: Concurrent Users
- Simulate 10, 50, 100 concurrent users
- Measure response times and errors

### 15. Scalability Tests

#### Test Case 15.1: Large Result Sets
- Query returning 1000, 10000, 100000 records
- Measure memory usage and response time

#### Test Case 15.2: Complex Queries
- Multi-table joins
- Aggregate functions
- Subqueries

## Integration Tests

### 16. End-to-End Workflows

#### Test Case 16.1: Complete CRUD Cycle
1. Create record (INSERT)
2. Read record (SELECT)
3. Update record (UPDATE)
4. Read updated record (verify changes)
5. Soft delete (UPDATE IsDeleted = true)
6. Verify record not in active queries

#### Test Case 16.2: Batch Import Workflow
1. Prepare batch of 100 records
2. Submit batch insert
3. Verify all records inserted
4. Query records
5. Update subset via batch
6. Verify updates

## Regression Tests

Maintain tests for all fixed bugs to prevent regressions.

### Example: Bug #123 - Case Sensitivity Issue
**Bug:** Parameter names case-sensitive

**Test:**
```json
Input: { "userid": "123" }
Expected: Should work (case-insensitive)
```

## Test Data

### Sample Users
```sql
INSERT INTO Users (UserId, Email, Name, Status) VALUES
('U001', 'user1@example.com', 'User One', 'ACTIVE'),
('U002', 'user2@example.com', 'User Two', 'ACTIVE'),
('U003', 'user3@example.com', 'User Three', 'INACTIVE')
```

### Sample Orders
```sql
INSERT INTO Orders (OrderId, UserId, Amount, OrderDate) VALUES
('O001', 'U001', 100.00, '2025-01-15'),
('O002', 'U001', 200.00, '2025-01-16'),
('O003', 'U002', 150.00, '2025-01-17')
```

## Test Checklist

Before release:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All security tests pass
- [ ] Performance benchmarks met
- [ ] Edge cases handled
- [ ] Error handling verified
- [ ] Documentation examples tested
- [ ] Backwards compatibility verified (if applicable)

## Automated Testing

Recommended test automation approach:

```vb
Public Sub TestSimpleRead()
    ' Arrange
    Dim payload = CreateTestPayload(...)
    Dim logic = CreateBusinessLogicForReadingRows(...)

    ' Act
    Dim result = logic.Invoke(testDB, payload)

    ' Assert
    Assert.AreEqual("OK", result.Result)
    Assert.IsNotNull(result.Records)
End Sub
```

## Version 2.2 Feature Tests

### 17. SQL Identifier Validation Tests

#### Test Case 17.1: Valid Table Name
**Input:**
```vb
tableName = "Users"
```

**Expected:** Validation passes

#### Test Case 17.2: Invalid Table Name (SQL Injection Attempt)
**Input:**
```vb
tableName = "Users; DROP TABLE Users--"
```

**Expected:** ArgumentException thrown with message about invalid identifier

#### Test Case 17.3: Valid Schema.Table Notation
**Input:**
```vb
tableName = "dbo.Users"
```

**Expected:** Validation passes

#### Test Case 17.4: Bracket Notation
**Input:**
```vb
tableName = "[User Orders]"
```

**Expected:** Validation passes (brackets allowed)

#### Test Case 17.5: Invalid Characters
**Input:**
```vb
tableName = "Users@#$"
```

**Expected:** ArgumentException thrown

#### Test Case 17.6: Exceeds Maximum Length
**Input:**
```vb
tableName = [String with 129 characters]
```

**Expected:** ArgumentException thrown (max length is 128)

### 18. Batch Size Limit Tests

#### Test Case 18.1: Batch Within Limit
**Input:**
```json
{
  "Records": [Array of 500 records]
}
```

**Configuration:** MAX_BATCH_SIZE = 1000

**Expected Result:** All records processed successfully

#### Test Case 18.2: Batch Exceeds Limit
**Input:**
```json
{
  "Records": [Array of 1500 records]
}
```

**Configuration:** MAX_BATCH_SIZE = 1000

**Expected Result:**
```json
{
  "Result": "KO",
  "Reason": "Batch size 1500 exceeds maximum allowed size of 1000. Please split into smaller batches."
}
```

#### Test Case 18.3: Batch Exactly at Limit
**Input:**
```json
{
  "Records": [Array of 1000 records]
}
```

**Configuration:** MAX_BATCH_SIZE = 1000

**Expected Result:** All 1000 records processed successfully

### 19. Query Prepending Tests

#### Test Case 19.1: Prepend SET DATEFORMAT
**Configuration:**
```vb
prependSQL = "SET DATEFORMAT ymd;"
```

**Input:**
```json
{
  "OrderDate": "2025-01-20"
}
```

**Expected SQL:**
```sql
SET DATEFORMAT ymd; SELECT OrderId, OrderDate FROM Orders WHERE OrderDate = :OrderDate
```

**Expected Result:** Date parsed correctly regardless of server locale

#### Test Case 19.2: Multiple SET Statements
**Configuration:**
```vb
prependSQL = "SET DATEFORMAT ymd; SET NOCOUNT ON;"
```

**Expected SQL:**
```sql
SET DATEFORMAT ymd; SET NOCOUNT ON; SELECT * FROM Orders WHERE OrderId = :OrderId
```

**Expected Result:** All SET statements executed before main query

#### Test Case 19.3: FOR JSON PATH with Prepend
**Configuration:**
```vb
prependSQL = "SET DATEFORMAT ymd;"
useForJsonPath = True
```

**Expected SQL:**
```sql
SET DATEFORMAT ymd; SELECT CAST(( SELECT * FROM Orders WHERE OrderId = :OrderId FOR JSON PATH, INCLUDE_NULL_VALUES ) AS NVARCHAR(MAX)) AS JsonResult
```

**Expected Result:** Prepend placed before outer SELECT CAST

#### Test Case 19.4: Empty Prepend
**Configuration:**
```vb
prependSQL = Nothing
```

**Expected Result:** No prepend SQL, query executes normally

### 20. Property Cache Tests

#### Test Case 20.1: Cache Hit
**Scenario:** Access same property multiple times on same object

**Expected Behavior:**
- First access: Cache miss (builds cache)
- Subsequent accesses: Cache hits (70-90% faster)
- GetPropertyCacheStats() shows increasing hit rate

#### Test Case 20.2: Cache Size Limit
**Scenario:** Cache exceeds MAX_CACHE_SIZE (1000)

**Expected Behavior:**
- Cache automatically clears when limit exceeded
- New cache created (thread-safe)
- No exceptions thrown
- Performance degrades slightly during rebuild

#### Test Case 20.3: Cache Collision Protection
**Scenario:** Two different objects with same hash code

**Expected Behavior:**
- Cached mapping validated against actual object
- Invalid cache entries removed
- Correct property value returned
- No data corruption

### 21. Composite Key Delimiter Tests

#### Test Case 21.1: Simple Composite Key
**Input:**
```vb
keyFields = {"OrderId", "ProductId"}
recordParams = {OrderId: "123", ProductId: "456"}
```

**Expected Composite Key:** `"123␟456"` (using ASCII 31 delimiter)

#### Test Case 21.2: Prevent Key Collision
**Scenario 1:**
```vb
recordParams = {OrderId: "123", ProductId: "456|789"}
```
**Composite Key:** `"123␟456|789"`

**Scenario 2:**
```vb
recordParams = {OrderId: "123|456", ProductId: "789"}
```
**Composite Key:** `"123|456␟789"`

**Expected Behavior:** Both scenarios produce different keys (no collision)

#### Test Case 21.3: NULL Key Field
**Input:**
```vb
keyFields = {"OrderId", "ProductId"}
recordParams = {OrderId: "123", ProductId: null}
```

**Expected Composite Key:** `"123␟[NULL]"`

### 22. includeExecutedSQL Option Tests

#### Test Case 22.1: Include SQL (Default)
**Configuration:**
```vb
includeExecutedSQL = True  ' Default
```

**Expected Response:**
```json
{
  "Result": "OK",
  "ProvidedParameters": "UserId",
  "ExecutedSQL": "SELECT UserId, Email FROM Users WHERE UserId = :UserId",
  "Records": [...]
}
```

#### Test Case 22.2: Exclude SQL
**Configuration:**
```vb
includeExecutedSQL = False
```

**Expected Response:**
```json
{
  "Result": "OK",
  "ProvidedParameters": "UserId",
  "Records": [...]
}
```

**Note:** ExecutedSQL field should NOT be present

#### Test Case 22.3: Backward Compatibility
**Configuration:**
```vb
' Parameter not specified (uses default)
```

**Expected Behavior:** includeExecutedSQL defaults to True, maintaining backward compatibility

## Continuous Integration

Run tests on:
- Every commit
- Pull requests
- Scheduled (nightly builds)

Report:
- Test coverage
- Performance metrics
- Security scan results
