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

## Continuous Integration

Run tests on:
- Every commit
- Pull requests
- Scheduled (nightly builds)

Report:
- Test coverage
- Performance metrics
- Security scan results
