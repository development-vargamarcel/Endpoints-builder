# Test Endpoint Scenarios - Comprehensive Testing Guide

Complete testing guide for the Endpoint Library using the `testepoint` table.

## Table of Contents

1. [Setup Instructions](#setup-instructions)
2. [Test Data Overview](#test-data-overview)
3. [Basic Read Tests](#basic-read-tests)
4. [Advanced Read Tests](#advanced-read-tests)
5. [Write Operation Tests](#write-operation-tests)
6. [Batch Operation Tests](#batch-operation-tests)
7. [Security Tests](#security-tests)
8. [Performance Tests](#performance-tests)
9. [Edge Case Tests](#edge-case-tests)
10. [Integration Tests](#integration-tests)

---

## Setup Instructions

### 1. Database Setup

```sql
-- Step 1: Drop existing table (if needed)
DROP TABLE IF EXISTS testepoint;

-- Step 2: Run the setup script
-- Execute: tests/database/testepoint_setup.sql
```

### 2. Verify Test Data

```sql
-- Verify record counts
SELECT Status, COUNT(*) as Count
FROM testepoint
GROUP BY Status;

-- Expected results:
-- ACTIVE: ~18 records
-- INACTIVE: 2 records
-- TESTING: 1 record
-- MAINTENANCE: 1 record
-- BETA: 1 record
-- DELETED: 2 records
```

### 3. Configuration

Ensure your test environment has:
- Valid connection string to test database
- Token validation configured (or disabled for testing)
- Proper error logging enabled

---

## Test Data Overview

The `testepoint` table contains diverse test data covering:

| Category | Count | Purpose |
|----------|-------|---------|
| Active endpoints | ~18 | Normal operation testing |
| Inactive endpoints | 2 | Status filtering tests |
| Testing/Beta endpoints | 2 | Status workflow tests |
| Soft-deleted endpoints | 2 | Soft delete functionality |
| Special characters | 3 | SQL injection prevention |
| NULL values | 2 | NULL handling tests |
| High-volume endpoints | 3 | Performance tests |
| Various priorities | 5 | Sorting tests |

---

## Basic Read Tests

### Test 1.1: Get All Active Endpoints

**Objective**: Verify basic read functionality

**Test Case**:
```json
{
  "DestinationIdentifier": "GET_ACTIVE"
}
```

**Expected Result**:
- Returns all endpoints where `IsActive = 1` and `IsDeleted = 0`
- Ordered by Priority DESC, then EndpointName
- Should return ~18 records
- Should NOT include ApiKey or SecretToken fields

**Validation**:
- ✓ All returned records have `IsActive = true`
- ✓ All returned records have `IsDeleted = false`
- ✓ Results are properly sorted
- ✓ JSON structure is valid

---

### Test 1.2: Search by Endpoint Type

**Objective**: Test parameter-based filtering

**Test Case**:
```json
{
  "EndpointType": "CRUD"
}
```

**Expected Result**:
- Returns only endpoints with `EndpointType = "CRUD"`
- Should return ~5 CRUD endpoints

**Validation**:
- ✓ All returned records have `EndpointType = "CRUD"`
- ✓ No other endpoint types are included

**Additional Tests**:
```json
// Test READ endpoints
{"EndpointType": "READ"}

// Test WRITE endpoints
{"EndpointType": "WRITE"}

// Test BATCH endpoints
{"EndpointType": "BATCH"}
```

---

### Test 1.3: Search Endpoint by Name (LIKE)

**Objective**: Test LIKE operator functionality

**Test Cases**:
```json
// Test 1: Partial match
{"EndpointName": "User"}
// Expected: Returns "UserManagement"

// Test 2: Case-insensitive search
{"EndpointName": "user"}
// Expected: Returns "UserManagement"

// Test 3: Wildcard search
{"EndpointName": "API"}
// Expected: Returns endpoints containing "API"

// Test 4: No matches
{"EndpointName": "NonExistent"}
// Expected: Returns empty array
```

**Validation**:
- ✓ LIKE operator works correctly
- ✓ Case-insensitive matching works
- ✓ Returns empty result gracefully when no matches

---

### Test 1.4: Field Exclusion (Security)

**Objective**: Verify sensitive fields are excluded

**Test Case**:
```json
{
  "DestinationIdentifier": "GET_ACTIVE"
}
```

**Expected Result**:
- Returns endpoint data WITHOUT `ApiKey` field
- Returns endpoint data WITHOUT `SecretToken` field
- All other fields should be present

**Validation**:
- ✓ ApiKey field is NOT present in response
- ✓ SecretToken field is NOT present in response
- ✓ Other fields (EndpointName, Description, etc.) ARE present

---

### Test 1.5: Multiple Filter Conditions

**Objective**: Test combining multiple parameters

**Test Case**:
```json
{
  "OwnerId": "USR001",
  "Status": "ACTIVE"
}
```

**Expected Result**:
- Returns endpoints where BOTH conditions match
- Should return endpoints owned by USR001 with ACTIVE status

**Additional Test Cases**:
```json
// Test: Owner with different status
{
  "OwnerId": "USR002",
  "Status": "TESTING"
}

// Test: Non-existent owner
{
  "OwnerId": "USR999",
  "Status": "ACTIVE"
}
// Expected: Empty result
```

**Validation**:
- ✓ AND logic works correctly (both conditions must match)
- ✓ Empty result when no matches
- ✓ All returned records satisfy both conditions

---

## Advanced Read Tests

### Test 2.1: Date Range Filtering

**Objective**: Test date-based queries

**Test Case**:
```json
{
  "StartDate": "2025-01-15",
  "EndDate": "2025-01-31"
}
```

**Expected Result**:
- Returns endpoints created between Jan 15-31, 2025
- Ordered by CreatedDate DESC

**Additional Test Cases**:
```json
// Test: Only start date
{"StartDate": "2025-01-20"}

// Test: Only end date
{"EndDate": "2025-01-25"}

// Test: No date parameters (returns all)
{}

// Test: Future date range (no results)
{
  "StartDate": "2026-01-01",
  "EndDate": "2026-12-31"
}
```

**Validation**:
- ✓ Date filtering works correctly
- ✓ Single date parameter works (StartDate OR EndDate)
- ✓ No parameters returns all records
- ✓ Empty result for invalid ranges

---

### Test 2.2: Aggregate Statistics

**Objective**: Test GROUP BY and aggregate functions

**Test Case**:
```json
{
  "DestinationIdentifier": "GET_STATS"
}
```

**Expected Result**:
```json
[
  {
    "EndpointType": "CRUD",
    "TotalCount": 5,
    "ActiveCount": 4,
    "TotalRequests": 50000,
    "TotalErrors": 500,
    "AvgResponseTime": 250.5,
    "FirstCreated": "2024-06-10T12:00:00",
    "LastCreated": "2025-02-01T10:00:00"
  },
  // ... more records
]
```

**Validation**:
- ✓ Aggregates calculated correctly
- ✓ Grouped by EndpointType
- ✓ All endpoint types represented
- ✓ Date aggregates (MIN, MAX) work correctly

---

### Test 2.3: Top N Query

**Objective**: Test TOP clause and ranking

**Test Cases**:
```json
// Test 1: Top 5 endpoints
{"TopN": 5}

// Test 2: Top 10 endpoints (default)
{}

// Test 3: Top 1 endpoint
{"TopN": 1}
```

**Expected Result**:
- Returns requested number of top-performing endpoints
- Ordered by RequestCount DESC, AvgResponseTime ASC
- Should include calculated ErrorRate field

**Validation**:
- ✓ Correct number of records returned
- ✓ Proper sorting (highest traffic first, fastest response for ties)
- ✓ ErrorRate calculated correctly
- ✓ Only includes endpoints with RequestCount > 0

---

### Test 2.4: Complex Business Logic

**Objective**: Test complex conditional queries

**Test Case**:
```json
{
  "DestinationIdentifier": "GET_NEEDS_ATTENTION"
}
```

**Expected Result**:
- Returns endpoints with:
  - Error rate > 5%, OR
  - Avg response time > 1000ms, OR
  - Not modified in 3 months
- Includes calculated `IssueType` field
- Ordered by severity (ErrorCount DESC, AvgResponseTime DESC)

**Validation**:
- ✓ Complex WHERE conditions work correctly
- ✓ CASE statement for IssueType works
- ✓ Multiple OR conditions evaluated properly
- ✓ Results make business sense

---

## Write Operation Tests

### Test 3.1: Simple Insert

**Objective**: Test basic INSERT functionality

**Test Case**:
```json
{
  "EndpointName": "TestEndpoint_New1",
  "EndpointType": "CRUD",
  "Description": "Test endpoint created during testing",
  "Priority": 7,
  "CreatedBy": "testuser",
  "OwnerId": "USR999",
  "ApiKey": "TEST_API_KEY_001",
  "SecretToken": "TEST_SECRET_TOKEN_001"
}
```

**Expected Result**:
- Record inserted successfully
- Returns new `EndpointId`
- Default values applied (IsActive=1, Status='ACTIVE', CreatedDate=now)

**Validation**:
```sql
-- Verify insertion
SELECT * FROM testepoint WHERE EndpointName = 'TestEndpoint_New1';
```

- ✓ Record exists
- ✓ All provided values saved correctly
- ✓ Default values applied where not provided
- ✓ CreatedDate is set to current date

**Cleanup**:
```sql
DELETE FROM testepoint WHERE EndpointName = 'TestEndpoint_New1';
```

---

### Test 3.2: Insert with Field Mapping

**Objective**: Test JSON property to SQL column mapping

**Test Case**:
```json
{
  "name": "MappedEndpoint",
  "type": "READ",
  "desc": "Testing field mapping",
  "active": true,
  "owner": "USR888",
  "creator": "testuser",
  "config": "{\"timeout\":30}"
}
```

**Expected Result**:
- JSON properties mapped to SQL columns:
  - `name` → `EndpointName`
  - `type` → `EndpointType`
  - `desc` → `Description`
  - `active` → `IsActive`
  - `owner` → `OwnerId`
  - `creator` → `CreatedBy`
  - `config` → `ConfigJson`
- Record inserted successfully

**Validation**:
```sql
SELECT * FROM testepoint WHERE EndpointName = 'MappedEndpoint';
```

- ✓ Field mapping works correctly
- ✓ Required fields validated
- ✓ Optional fields handled properly

**Cleanup**:
```sql
DELETE FROM testepoint WHERE EndpointName = 'MappedEndpoint';
```

---

### Test 3.3: Insert Validation - Missing Required Fields

**Objective**: Test required field validation

**Test Cases**:
```json
// Test 1: Missing EndpointName
{
  "EndpointType": "CRUD",
  "CreatedBy": "testuser"
}
// Expected: Error - EndpointName is required

// Test 2: Missing EndpointType
{
  "EndpointName": "TestEndpoint",
  "CreatedBy": "testuser"
}
// Expected: Error - EndpointType is required

// Test 3: Missing CreatedBy
{
  "EndpointName": "TestEndpoint",
  "EndpointType": "CRUD"
}
// Expected: Error - CreatedBy is required
```

**Expected Result**:
- Request fails with validation error
- Error message indicates which field is missing
- No record inserted

**Validation**:
- ✓ Validation catches missing required fields
- ✓ Clear error message returned
- ✓ Database remains unchanged

---

### Test 4.1: Simple Update

**Objective**: Test basic UPDATE functionality

**Test Case**:
```json
{
  "EndpointId": 1,
  "Status": "MAINTENANCE"
}
```

**Expected Result**:
- Status updated to MAINTENANCE
- IsActive automatically set based on status
- LastModifiedDate updated to current time

**Validation**:
```sql
-- Verify update
SELECT EndpointId, Status, IsActive, LastModifiedDate
FROM testepoint
WHERE EndpointId = 1;
```

- ✓ Status changed to MAINTENANCE
- ✓ IsActive updated appropriately
- ✓ LastModifiedDate is recent

**Additional Test Cases**:
```json
// Change to ACTIVE
{"EndpointId": 1, "Status": "ACTIVE"}

// Change to INACTIVE
{"EndpointId": 1, "Status": "INACTIVE"}

// Invalid endpoint ID
{"EndpointId": 999999, "Status": "ACTIVE"}
// Expected: No records updated
```

---

### Test 4.2: Update with Increment

**Objective**: Test counter increment logic

**Test Case**:
```json
{
  "EndpointId": 1,
  "IncrementError": 1,
  "ResponseTime": 150
}
```

**Expected Result**:
- RequestCount incremented by 1
- ErrorCount incremented by 1
- AvgResponseTime recalculated
- LastAccessDate updated

**Validation**:
```sql
-- Get before values
SELECT RequestCount, ErrorCount, AvgResponseTime
FROM testepoint WHERE EndpointId = 1;

-- Run update (above JSON)

-- Get after values and verify increment
SELECT RequestCount, ErrorCount, AvgResponseTime
FROM testepoint WHERE EndpointId = 1;
```

- ✓ RequestCount increased by 1
- ✓ ErrorCount increased by IncrementError value
- ✓ AvgResponseTime recalculated correctly
- ✓ LastAccessDate is current

**Additional Test Cases**:
```json
// Test: No error increment
{
  "EndpointId": 1,
  "ResponseTime": 200
}
// Expected: RequestCount +1, ErrorCount unchanged

// Test: Only error increment (no response time)
{
  "EndpointId": 1,
  "IncrementError": 1
}
// Expected: RequestCount +1, ErrorCount +1, AvgResponseTime unchanged
```

---

### Test 4.3: Conditional Update

**Objective**: Test updates with conditions

**Test Case**:
```json
{
  "EndpointId": 1,
  "ConfigJson": "{\"timeout\":60,\"retries\":5}",
  "Metadata": "updated:true"
}
```

**Expected Result**:
- Updates if Status is NOT 'LOCKED' or 'ARCHIVED'
- Version incremented
- LastModifiedDate updated

**Test Scenarios**:
1. **Update unlocked endpoint** → Should succeed
2. **Update locked endpoint** → Should fail (0 rows updated)

**Validation**:
```sql
-- Set endpoint to LOCKED
UPDATE testepoint SET Status = 'LOCKED' WHERE EndpointId = 1;

-- Try to update (should fail)
-- Run update with test case JSON above

-- Verify not updated
SELECT Version, ConfigJson FROM testepoint WHERE EndpointId = 1;
```

- ✓ Update succeeds for unlocked endpoints
- ✓ Update blocked for locked endpoints
- ✓ Version incremented only on successful update

---

### Test 5.1: Upsert Operation

**Objective**: Test INSERT or UPDATE logic

**Test Scenario 1: Insert (name doesn't exist)**
```json
{
  "EndpointName": "UpsertTest1",
  "EndpointType": "READ",
  "Description": "First insert",
  "Priority": 5,
  "CreatedBy": "testuser"
}
```

**Expected Result**:
- New record inserted
- Returns new EndpointId
- CreatedDate set

**Test Scenario 2: Update (name exists)**
```json
{
  "EndpointName": "UpsertTest1",
  "EndpointType": "WRITE",
  "Description": "Updated description",
  "Priority": 8,
  "CreatedBy": "testuser"
}
```

**Expected Result**:
- Existing record updated
- EndpointType changed to WRITE
- Description updated
- Priority changed to 8
- Version incremented
- LastModifiedDate updated
- CreatedDate unchanged
- Returns existing EndpointId

**Validation**:
```sql
-- After second upsert
SELECT
  EndpointId,
  EndpointName,
  EndpointType,
  Description,
  Priority,
  Version,
  CreatedDate,
  LastModifiedDate
FROM testepoint
WHERE EndpointName = 'UpsertTest1';
```

- ✓ Only ONE record exists with name 'UpsertTest1'
- ✓ EndpointType = 'WRITE' (updated)
- ✓ Description = 'Updated description' (updated)
- ✓ Priority = 8 (updated)
- ✓ Version = 2 (incremented)
- ✓ CreatedDate unchanged from first insert
- ✓ LastModifiedDate is recent

**Cleanup**:
```sql
DELETE FROM testepoint WHERE EndpointName = 'UpsertTest1';
```

---

### Test 6.1: Soft Delete

**Objective**: Test soft delete functionality

**Setup**:
```sql
-- Create a test endpoint to delete
INSERT INTO testepoint (EndpointName, EndpointType, CreatedBy, Status)
VALUES ('ToBeDeleted', 'READ', 'testuser', 'ACTIVE');
```

**Test Case**:
```json
{
  "EndpointId": <id_of_ToBeDeleted>,
  "DeletedBy": "testuser"
}
```

**Expected Result**:
- IsDeleted set to 1
- IsActive set to 0
- Status set to 'DELETED'
- DeletedDate set to current time
- DeletedBy set to 'testuser'
- Record still exists in database (not hard deleted)

**Validation**:
```sql
-- Verify soft delete
SELECT
  EndpointId,
  EndpointName,
  IsDeleted,
  IsActive,
  Status,
  DeletedDate,
  DeletedBy
FROM testepoint
WHERE EndpointName = 'ToBeDeleted';
```

- ✓ IsDeleted = 1
- ✓ IsActive = 0
- ✓ Status = 'DELETED'
- ✓ DeletedDate is set
- ✓ DeletedBy = 'testuser'
- ✓ Record still exists (not hard deleted)

**Test Case 2: Soft delete already deleted**
```json
{
  "EndpointId": <id_of_ToBeDeleted>,
  "DeletedBy": "testuser"
}
```

**Expected Result**:
- 0 rows affected (already deleted)

**Validation**:
- ✓ Returns 0 rows updated
- ✓ No error occurs

---

### Test 6.2: Restore Deleted Endpoint

**Objective**: Test restore functionality

**Test Case**:
```json
{
  "EndpointId": <id_of_previously_deleted_endpoint>
}
```

**Expected Result**:
- IsDeleted set to 0
- IsActive set to 1
- Status set to 'ACTIVE'
- DeletedDate set to NULL
- DeletedBy set to NULL
- LastModifiedDate updated

**Validation**:
```sql
SELECT
  EndpointId,
  EndpointName,
  IsDeleted,
  IsActive,
  Status,
  DeletedDate,
  DeletedBy,
  LastModifiedDate
FROM testepoint
WHERE EndpointName = 'ToBeDeleted';
```

- ✓ IsDeleted = 0
- ✓ IsActive = 1
- ✓ Status = 'ACTIVE'
- ✓ DeletedDate = NULL
- ✓ DeletedBy = NULL
- ✓ LastModifiedDate updated

**Cleanup**:
```sql
DELETE FROM testepoint WHERE EndpointName = 'ToBeDeleted';
```

---

## Batch Operation Tests

### Test 7.1: Batch Insert

**Objective**: Test creating multiple records

**Test Case**:
```json
{
  "endpoints": [
    {
      "name": "BatchTest1",
      "type": "READ",
      "desc": "Batch insert test 1",
      "priority": 5,
      "creator": "testuser"
    },
    {
      "name": "BatchTest2",
      "type": "WRITE",
      "desc": "Batch insert test 2",
      "priority": 6,
      "creator": "testuser"
    },
    {
      "name": "BatchTest3",
      "type": "CRUD",
      "desc": "Batch insert test 3",
      "priority": 7,
      "creator": "testuser"
    }
  ]
}
```

**Expected Result**:
- All 3 records inserted successfully
- Success count = 3
- Error count = 0
- Returns detailed results for each record

**Validation**:
```sql
SELECT * FROM testepoint
WHERE EndpointName IN ('BatchTest1', 'BatchTest2', 'BatchTest3')
ORDER BY EndpointName;
```

- ✓ All 3 records exist
- ✓ All values saved correctly
- ✓ Default values applied

**Cleanup**:
```sql
DELETE FROM testepoint
WHERE EndpointName IN ('BatchTest1', 'BatchTest2', 'BatchTest3');
```

---

### Test 7.2: Batch Insert with Errors

**Objective**: Test error handling in batch operations

**Test Case**:
```json
{
  "endpoints": [
    {
      "name": "BatchValid1",
      "type": "READ",
      "creator": "testuser"
    },
    {
      "name": "BatchInvalid",
      "type": "READ"
      // Missing required field: creator
    },
    {
      "name": "BatchValid2",
      "type": "WRITE",
      "creator": "testuser"
    }
  ]
}
```

**Expected Result**:
- 2 records inserted successfully (BatchValid1, BatchValid2)
- 1 record failed (BatchInvalid - missing required field)
- Success count = 2
- Error count = 1
- Error details provided for failed record

**Validation**:
```sql
SELECT * FROM testepoint
WHERE EndpointName IN ('BatchValid1', 'BatchInvalid', 'BatchValid2');
```

- ✓ BatchValid1 exists
- ✓ BatchValid2 exists
- ✓ BatchInvalid does NOT exist
- ✓ Error reported with clear message

**Cleanup**:
```sql
DELETE FROM testepoint
WHERE EndpointName IN ('BatchValid1', 'BatchValid2');
```

---

### Test 7.3: Batch Update

**Objective**: Test updating multiple records

**Setup**:
```sql
-- Create test records
INSERT INTO testepoint (EndpointName, EndpointType, CreatedBy, Priority, Status)
VALUES
  ('BatchUpdate1', 'READ', 'testuser', 5, 'ACTIVE'),
  ('BatchUpdate2', 'WRITE', 'testuser', 5, 'ACTIVE'),
  ('BatchUpdate3', 'CRUD', 'testuser', 5, 'ACTIVE');
```

**Test Case**:
```json
{
  "updates": [
    {
      "id": <id_of_BatchUpdate1>,
      "priority": 10
    },
    {
      "id": <id_of_BatchUpdate2>,
      "priority": 8
    },
    {
      "id": <id_of_BatchUpdate3>,
      "priority": 6
    }
  ]
}
```

**Expected Result**:
- All 3 records updated
- Priorities changed to 10, 8, and 6 respectively
- LastModifiedDate updated for all

**Validation**:
```sql
SELECT EndpointId, EndpointName, Priority, LastModifiedDate
FROM testepoint
WHERE EndpointName IN ('BatchUpdate1', 'BatchUpdate2', 'BatchUpdate3')
ORDER BY EndpointName;
```

- ✓ BatchUpdate1 priority = 10
- ✓ BatchUpdate2 priority = 8
- ✓ BatchUpdate3 priority = 6
- ✓ All LastModifiedDate values updated

**Cleanup**:
```sql
DELETE FROM testepoint
WHERE EndpointName IN ('BatchUpdate1', 'BatchUpdate2', 'BatchUpdate3');
```

---

### Test 7.4: Batch Upsert

**Objective**: Test batch insert/update combination

**Setup**:
```sql
-- Create one existing record
INSERT INTO testepoint (EndpointName, EndpointType, CreatedBy, Priority, Description)
VALUES ('BatchUpsert1', 'READ', 'testuser', 5, 'Original description');
```

**Test Case**:
```json
{
  "endpoints": [
    {
      "name": "BatchUpsert1",
      "type": "WRITE",
      "desc": "Updated description",
      "priority": 9,
      "creator": "testuser"
    },
    {
      "name": "BatchUpsert2",
      "type": "CRUD",
      "desc": "New record",
      "priority": 7,
      "creator": "testuser"
    }
  ]
}
```

**Expected Result**:
- BatchUpsert1: Updated (type changed to WRITE, desc updated, priority = 9)
- BatchUpsert2: Inserted (new record)
- Total operations: 2 successful

**Validation**:
```sql
SELECT
  EndpointName,
  EndpointType,
  Description,
  Priority,
  Version,
  CreatedDate,
  LastModifiedDate
FROM testepoint
WHERE EndpointName IN ('BatchUpsert1', 'BatchUpsert2')
ORDER BY EndpointName;
```

- ✓ BatchUpsert1: EndpointType = 'WRITE' (updated)
- ✓ BatchUpsert1: Description = 'Updated description' (updated)
- ✓ BatchUpsert1: Priority = 9 (updated)
- ✓ BatchUpsert1: Version = 2 (incremented)
- ✓ BatchUpsert1: CreatedDate unchanged
- ✓ BatchUpsert1: LastModifiedDate updated
- ✓ BatchUpsert2: New record exists
- ✓ BatchUpsert2: Version = 1 (new)

**Cleanup**:
```sql
DELETE FROM testepoint
WHERE EndpointName IN ('BatchUpsert1', 'BatchUpsert2');
```

---

## Security Tests

### Test 8.1: SQL Injection Prevention

**Objective**: Verify SQL injection attacks are prevented

**Test Cases**:
```json
// Test 1: Single quote injection
{
  "EndpointName": "Test' OR '1'='1"
}

// Test 2: Comment injection
{
  "EndpointName": "Test'; DROP TABLE testepoint;--"
}

// Test 3: UNION injection
{
  "EndpointName": "Test' UNION SELECT * FROM Users--"
}

// Test 4: Batch separator
{
  "Description": "Test; DELETE FROM testepoint WHERE 1=1;"
}
```

**Expected Result**:
- All queries execute safely using parameterized queries
- Special characters are escaped
- No SQL injection occurs
- Table remains intact
- Queries return legitimate results or empty sets

**Validation**:
```sql
-- Verify table still exists and data intact
SELECT COUNT(*) FROM testepoint;
-- Should return original count (~30 records)

-- Verify no malicious data stored literally
SELECT * FROM testepoint WHERE EndpointName LIKE '%DROP%';
-- Should return empty or literal string stored
```

- ✓ Table still exists
- ✓ Record count unchanged (except legitimate searches)
- ✓ No SQL code executed from input
- ✓ Special characters stored as literal values

---

### Test 8.2: Token Validation

**Objective**: Test authentication/authorization

**Test Cases**:
```vb
// Test 1: Valid token
GetActiveEndpoints(json, connectionString, "valid_token_here")
// Expected: Success

// Test 2: Missing token
GetActiveEndpoints(json, connectionString, "")
// Expected: Error - "Token validation failed"

// Test 3: Invalid token
GetActiveEndpoints(json, connectionString, "invalid_token")
// Expected: Error - "Token validation failed"

// Test 4: Expired token (if applicable)
GetActiveEndpoints(json, connectionString, "expired_token")
// Expected: Error - "Token expired"
```

**Expected Result**:
- Valid token: Operation succeeds
- Invalid token: Operation fails with authentication error
- Empty token: Operation fails with authentication error
- No database access granted without valid token

**Validation**:
- ✓ Valid tokens allow access
- ✓ Invalid tokens block access
- ✓ Clear error messages returned
- ✓ No data leakage in error messages

---

### Test 8.3: Field Exclusion (Sensitive Data)

**Objective**: Ensure sensitive fields are never exposed

**Test Case**:
```json
{
  "EndpointId": 1
}
```

**Expected Result**:
- Response includes: EndpointName, Description, Status, etc.
- Response EXCLUDES: ApiKey, SecretToken

**Validation**:
- ✓ ApiKey field not present in JSON response
- ✓ SecretToken field not present in JSON response
- ✓ Other fields present and correct
- ✓ Cannot retrieve sensitive fields even with explicit request

**Additional Test**:
```json
// Try to explicitly request sensitive fields
{
  "EndpointId": 1,
  "IncludeApiKey": true
}
```

**Expected Result**:
- Sensitive fields still excluded regardless of request parameters

---

### Test 8.4: Role-Based Access Control

**Objective**: Test different access levels

**Test Scenarios**:

**Admin Role**:
```vb
GetEndpointsByRole(json, connectionString, token, "ADMIN")
```
- ✓ Can see all fields including ApiKey and SecretToken
- ✓ Can see ConfigJson

**Developer Role**:
```vb
GetEndpointsByRole(json, connectionString, token, "DEVELOPER")
```
- ✓ Can see most fields
- ✓ Cannot see ApiKey or SecretToken
- ✓ Can see ConfigJson

**Viewer Role**:
```vb
GetEndpointsByRole(json, connectionString, token, "VIEWER")
```
- ✓ Can see basic fields only
- ✓ Cannot see ApiKey, SecretToken, or ConfigJson
- ✓ Read-only access

**Validation**:
- ✓ Each role has appropriate access level
- ✓ Sensitive fields properly filtered by role
- ✓ No privilege escalation possible

---

### Test 8.5: Mass Assignment Protection

**Objective**: Prevent unauthorized field updates

**Test Case**:
```json
{
  "EndpointId": 1,
  "Status": "INACTIVE",
  "IsDeleted": 1,
  "Version": 999,
  "CreatedBy": "hacker"
}
```

**Expected Result**:
- Only Status is updated (if explicitly allowed)
- IsDeleted NOT changed (should use soft delete function)
- Version NOT changed (auto-incremented only)
- CreatedBy NOT changed (set on insert only)

**Validation**:
```sql
SELECT EndpointId, Status, IsDeleted, Version, CreatedBy
FROM testepoint WHERE EndpointId = 1;
```

- ✓ Only allowed fields updated
- ✓ Protected fields unchanged
- ✓ System fields (Version, CreatedBy) not manipulatable

---

## Performance Tests

### Test 9.1: Single Record Performance

**Objective**: Measure basic query performance

**Test Case**:
```json
{
  "EndpointId": 1
}
```

**Measurement**:
- Execute query 100 times
- Measure average response time

**Expected Result**:
- Average response time < 100ms for simple SELECT
- Consistent performance across iterations

**Validation**:
- ✓ Query completes quickly
- ✓ No performance degradation over iterations
- ✓ Response time meets requirements

---

### Test 9.2: Batch Performance

**Objective**: Test batch operation performance

**Test Case**:
```json
{
  "endpoints": [
    // 100 endpoint records
  ]
}
```

**Scenarios**:
- 10 records: Should complete quickly
- 100 records: Should complete in reasonable time
- 1000 records: Should complete without timeout

**Expected Results**:
| Batch Size | Max Time |
|------------|----------|
| 10 | < 1 second |
| 100 | < 5 seconds |
| 1000 | < 30 seconds |

**Validation**:
- ✓ Batch operations scale linearly
- ✓ No timeout errors
- ✓ All records processed
- ✓ Memory usage acceptable

---

### Test 9.3: Large Result Set

**Objective**: Test handling of large data volumes

**Setup**:
```sql
-- Insert 10,000 test records
DECLARE @i INT = 0;
WHILE @i < 10000
BEGIN
    INSERT INTO testepoint (EndpointName, EndpointType, CreatedBy, Status)
    VALUES ('PerfTest' + CAST(@i AS VARCHAR), 'READ', 'testuser', 'ACTIVE');
    SET @i = @i + 1;
END
```

**Test Case**:
```json
{
  "Status": "ACTIVE"
}
```

**Expected Result**:
- Returns all 10,000+ records
- No timeout
- Proper pagination support (if implemented)

**Validation**:
- ✓ All records returned or properly paginated
- ✓ Response time acceptable
- ✓ No memory issues
- ✓ JSON properly formatted

**Cleanup**:
```sql
DELETE FROM testepoint WHERE EndpointName LIKE 'PerfTest%';
```

---

### Test 9.4: FOR JSON PATH Performance

**Objective**: Compare performance with and without FOR JSON PATH

**Test Case A (Normal)**:
```vb
GetActiveEndpoints(json, connectionString, token)
```

**Test Case B (FOR JSON PATH)**:
```vb
GetEndpointsJsonPath(json, connectionString, token)
```

**Measurement**:
- Execute each 100 times
- Compare average response times

**Expected Result**:
- FOR JSON PATH should be 40-60% faster for simple queries
- Results should be identical

**Validation**:
- ✓ FOR JSON PATH faster
- ✓ Same data returned
- ✓ JSON format consistent

---

### Test 9.5: Concurrent Users

**Objective**: Test system under concurrent load

**Setup**:
- Simulate 10 concurrent users
- Each executes 100 random queries

**Test Scenarios**:
- Read operations (80% of requests)
- Write operations (15% of requests)
- Batch operations (5% of requests)

**Expected Result**:
- All operations complete successfully
- No deadlocks
- No connection pool exhaustion
- Acceptable response times maintained

**Validation**:
- ✓ All operations succeed
- ✓ No errors logged
- ✓ Average response time acceptable
- ✓ Database connections managed properly

---

## Edge Case Tests

### Test 10.1: NULL Value Handling

**Objective**: Test NULL parameter and field handling

**Test Case 1: NULL in optional fields**
```json
{
  "EndpointName": "NullTest",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": null,
  "OwnerId": null,
  "ConfigJson": null
}
```

**Expected Result**:
- Record inserted successfully
- NULL fields stored as NULL
- Non-NULL fields stored correctly

**Test Case 2: Query with NULL values**
```json
{
  "OwnerId": null
}
```

**Expected Result**:
- Returns endpoints where OwnerId IS NULL
- Handles NULL comparison correctly (not using = but IS NULL)

**Validation**:
- ✓ NULL values handled correctly
- ✓ NULL comparison uses IS NULL syntax
- ✓ No errors with NULL parameters

---

### Test 10.2: Empty String vs NULL

**Objective**: Differentiate empty strings from NULL

**Test Cases**:
```json
// Test 1: Empty string
{
  "EndpointName": "EmptyTest1",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": ""
}

// Test 2: NULL
{
  "EndpointName": "EmptyTest2",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": null
}
```

**Validation**:
```sql
SELECT
  EndpointName,
  Description,
  CASE
    WHEN Description IS NULL THEN 'NULL'
    WHEN Description = '' THEN 'EMPTY'
    ELSE 'HAS_VALUE'
  END as DescriptionType
FROM testepoint
WHERE EndpointName IN ('EmptyTest1', 'EmptyTest2');
```

- ✓ EmptyTest1 Description = empty string
- ✓ EmptyTest2 Description = NULL
- ✓ Different handling for empty vs NULL

---

### Test 10.3: Special Characters

**Objective**: Test handling of special characters

**Test Cases**:
```json
// Test 1: Quotes
{
  "EndpointName": "Special'Quotes\"Test",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": "Testing 'single' and \"double\" quotes"
}

// Test 2: XML/HTML characters
{
  "EndpointName": "XMLTest",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": "<tag>Test & 'quotes' > data</tag>"
}

// Test 3: Unicode
{
  "EndpointName": "UnicodeTest",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": "测试 Test テスト مرحبا 🚀"
}

// Test 4: Line breaks and tabs
{
  "EndpointName": "WhitespaceTest",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": "Line1\nLine2\tTabbed"
}
```

**Expected Result**:
- All special characters stored correctly
- No encoding issues
- Special characters properly escaped in SQL
- Retrieval returns original values

**Validation**:
- ✓ Special characters stored as-is
- ✓ No corruption or encoding issues
- ✓ Retrieval matches insertion
- ✓ No SQL syntax errors

---

### Test 10.4: Long Strings

**Objective**: Test field length limits

**Test Cases**:
```json
// Test 1: Maximum length string (within limit)
{
  "EndpointName": "<100 character string>",
  "EndpointType": "READ",
  "CreatedBy": "testuser"
}

// Test 2: Exceeds maximum length
{
  "EndpointName": "<200 character string exceeding 100 limit>",
  "EndpointType": "READ",
  "CreatedBy": "testuser"
}

// Test 3: Very long description (within NVARCHAR(500) limit)
{
  "EndpointName": "LongDesc",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Description": "<500 character description>"
}
```

**Expected Results**:
- Test 1: Success
- Test 2: Error or string truncation (depending on implementation)
- Test 3: Success

**Validation**:
- ✓ Within-limit strings stored completely
- ✓ Over-limit strings handled gracefully
- ✓ Clear error message for violations
- ✓ Data integrity maintained

---

### Test 10.5: Extreme Numeric Values

**Objective**: Test numeric boundaries

**Test Cases**:
```json
// Test 1: Maximum INT value
{
  "EndpointName": "MaxInt",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "RequestCount": 2147483647
}

// Test 2: Negative values
{
  "EndpointName": "Negative",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "Priority": -1
}

// Test 3: Zero values
{
  "EndpointName": "Zero",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "RequestCount": 0,
  "ErrorCount": 0
}

// Test 4: Large decimal
{
  "EndpointName": "LargeDecimal",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "AvgResponseTime": 12345678.99
}
```

**Expected Result**:
- Valid values stored correctly
- Invalid values rejected with clear error
- Overflow handled gracefully

**Validation**:
- ✓ Boundary values handled correctly
- ✓ Overflow doesn't corrupt data
- ✓ Type validation enforced

---

### Test 10.6: Date Edge Cases

**Objective**: Test date boundaries

**Test Cases**:
```json
// Test 1: Minimum date
{
  "EndpointName": "MinDate",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "ExpiryDate": "1753-01-01"
}

// Test 2: Maximum date
{
  "EndpointName": "MaxDate",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "ExpiryDate": "9999-12-31"
}

// Test 3: Invalid date
{
  "EndpointName": "InvalidDate",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "ExpiryDate": "2025-02-30"
}

// Test 4: Date format variations
{
  "EndpointName": "DateFormat",
  "EndpointType": "READ",
  "CreatedBy": "testuser",
  "ExpiryDate": "02/15/2025"
}
```

**Expected Results**:
- Test 1 & 2: Success (within SQL Server date range)
- Test 3: Error (invalid date)
- Test 4: Success if date parsing handles format, otherwise error

**Validation**:
- ✓ Valid dates stored correctly
- ✓ Invalid dates rejected
- ✓ Date format properly handled
- ✓ Clear error messages

---

## Integration Tests

### Test 11.1: Complete CRUD Workflow

**Objective**: Test full lifecycle

**Workflow**:
```json
// Step 1: CREATE
{
  "EndpointName": "WorkflowTest",
  "EndpointType": "CRUD",
  "Description": "Testing complete workflow",
  "Priority": 5,
  "CreatedBy": "testuser",
  "Status": "TESTING"
}
// Capture EndpointId from response

// Step 2: READ
{
  "EndpointId": <captured_id>
}
// Verify all fields correct

// Step 3: UPDATE
{
  "EndpointId": <captured_id>,
  "Status": "ACTIVE",
  "Priority": 8
}

// Step 4: READ again
{
  "EndpointId": <captured_id>
}
// Verify updates applied

// Step 5: SOFT DELETE
{
  "EndpointId": <captured_id>,
  "DeletedBy": "testuser"
}

// Step 6: VERIFY deleted
{
  "EndpointId": <captured_id>
}
// Should return nothing or show IsDeleted = true

// Step 7: RESTORE
{
  "EndpointId": <captured_id>
}

// Step 8: VERIFY restored
{
  "EndpointId": <captured_id>
}
// Should show as active again
```

**Validation**:
- ✓ All steps execute successfully
- ✓ Data consistency maintained throughout
- ✓ Soft delete and restore work correctly
- ✓ Audit fields (dates, versions) updated properly

---

### Test 11.2: DestinationIdentifier Routing

**Objective**: Test routing pattern

**Test Cases**:
```json
// Test 1: GET_ACTIVE
{"DestinationIdentifier": "GET_ACTIVE"}

// Test 2: GET_BY_TYPE
{
  "DestinationIdentifier": "GET_BY_TYPE",
  "EndpointType": "CRUD"
}

// Test 3: CREATE
{
  "DestinationIdentifier": "CREATE",
  "EndpointName": "RoutedEndpoint",
  "EndpointType": "READ",
  "CreatedBy": "testuser"
}

// Test 4: UPDATE_STATUS
{
  "DestinationIdentifier": "UPDATE_STATUS",
  "EndpointId": 1,
  "Status": "MAINTENANCE"
}

// Test 5: Invalid operation
{
  "DestinationIdentifier": "INVALID_OP"
}
```

**Expected Results**:
- Tests 1-4: Route to correct operations
- Test 5: Returns error with list of valid operations

**Validation**:
- ✓ Routing works correctly
- ✓ Each operation executes as expected
- ✓ Invalid operations handled gracefully
- ✓ Clear error messages for invalid routes

---

### Test 11.3: Multi-User Scenario

**Objective**: Test concurrent multi-user operations

**Scenario**:
- User A creates endpoint
- User B updates same endpoint
- User C reads endpoint
- User D soft deletes endpoint
- All operations tracked with proper user attribution

**Validation**:
- ✓ All operations succeed
- ✓ Proper user tracking (CreatedBy, DeletedBy)
- ✓ No data corruption
- ✓ Audit trail maintained

---

## Test Execution Checklist

### Pre-Test Setup
- [ ] Database connection configured
- [ ] Test database created
- [ ] testepoint table created (DROP TABLE IF EXISTS run manually)
- [ ] Test data loaded (30+ records)
- [ ] Backup of test database created
- [ ] Token validation configured

### During Testing
- [ ] Execute tests in order (dependencies)
- [ ] Document any failures
- [ ] Capture error messages
- [ ] Note performance metrics
- [ ] Clean up test data between tests

### Post-Test Validation
- [ ] All tests executed
- [ ] Results documented
- [ ] Database state verified
- [ ] Performance acceptable
- [ ] No security vulnerabilities found
- [ ] Edge cases handled properly

### Test Coverage Summary
- [ ] Basic CRUD operations
- [ ] Advanced queries
- [ ] Batch operations
- [ ] Security features
- [ ] Performance requirements
- [ ] Edge cases
- [ ] Integration scenarios

---

## Troubleshooting

### Common Issues

**Issue**: Test data not found
**Solution**: Verify testepoint_setup.sql executed successfully

**Issue**: Token validation fails
**Solution**: Check token configuration or disable for testing

**Issue**: Timeout errors
**Solution**: Check database connection, increase timeout values

**Issue**: Special characters not working
**Solution**: Verify database collation supports unicode (e.g., SQL_Latin1_General_CP1_CI_AS)

**Issue**: Batch operations slow
**Solution**: Check indexes, verify batch size limits

---

## Success Criteria

All tests should:
- ✅ Execute without errors
- ✅ Return expected results
- ✅ Maintain data integrity
- ✅ Handle edge cases gracefully
- ✅ Meet performance requirements
- ✅ Enforce security rules
- ✅ Provide clear error messages

---

## Next Steps

After completing all tests:

1. **Document Results**: Record all test outcomes
2. **Fix Issues**: Address any failures or performance issues
3. **Optimize**: Improve slow queries or operations
4. **Security Review**: Verify all security measures working
5. **Production Readiness**: Confirm system ready for production use

---

**End of Test Scenarios**
