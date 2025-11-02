# Testing Documentation

Comprehensive testing suite for the Endpoint Library.

## 📁 Directory Structure

```
tests/
├── README.md                          # This file
├── TestScenarios.md                   # Original test scenarios (general)
├── TestEndpointScenarios.md          # Complete test scenarios using testepoint
├── database/
│   ├── testepoint_setup.sql          # Table creation and test data
│   └── test_validation.sql           # Validation script for test data
```

## 🚀 Quick Start

### 1. Database Setup

**IMPORTANT**: Drop the table manually first if it exists:

```sql
DROP TABLE IF EXISTS testepoint;
```

Then run the setup script:

```sql
-- Execute the full setup script
USE YourDatabase;
GO

-- Run the setup script
sqlcmd -S your_server -d your_database -i tests/database/testepoint_setup.sql
```

Or run it in SQL Server Management Studio (SSMS):
1. Open `tests/database/testepoint_setup.sql`
2. Execute the script
3. Verify success message: "Test data setup completed successfully!"

### 2. Validate Test Data

Run the validation script to ensure everything is set up correctly:

```sql
sqlcmd -S your_server -d your_database -i tests/database/test_validation.sql
```

Or in SSMS:
1. Open `tests/database/test_validation.sql`
2. Execute the script
3. Review the validation results

Expected output should show all checks passing (✓):
- Table structure validated
- Test data loaded (25+ records)
- Indexes created
- Data quality checks passed

### 3. Run Tests

Follow the test scenarios in:
- **TestEndpointScenarios.md** - Complete testing guide with step-by-step instructions

## 📊 Test Data Overview

The `testepoint` table contains:

| Category | Count | Purpose |
|----------|-------|---------|
| **Total Records** | 30+ | Comprehensive coverage |
| **Active Endpoints** | ~18 | Normal operations testing |
| **Inactive Endpoints** | 2 | Status filtering |
| **Deleted Endpoints** | 2 | Soft delete testing |
| **Special Characters** | 3 | SQL injection prevention |
| **NULL Values** | 2 | NULL handling |
| **High Volume** | 3 | Performance testing |
| **Priority Ranges** | 5 | Sorting and filtering |

### Endpoint Types Covered

- **CRUD**: Full Create, Read, Update, Delete operations
- **READ**: Read-only endpoints
- **WRITE**: Write operations
- **BATCH**: Batch processing

### Test Scenarios Covered

✅ Basic CRUD operations
✅ Advanced queries (filters, aggregates, joins)
✅ Batch operations (insert, update, upsert)
✅ Security (SQL injection, field exclusion, token validation)
✅ Performance (single record, batch, large datasets)
✅ Edge cases (NULL values, special characters, boundaries)
✅ Integration (complete workflows, routing patterns)

## 📖 Documentation

### TestEndpointScenarios.md

Complete testing guide covering:
- Setup instructions
- Basic read tests (filtering, searching, exclusion)
- Advanced read tests (aggregates, date ranges, complex queries)
- Write operation tests (insert, update, upsert, soft delete)
- Batch operation tests
- Security tests (SQL injection, token validation, RBAC)
- Performance tests (load testing, concurrency)
- Edge case tests (NULL handling, special characters, boundaries)
- Integration tests (full workflows, routing)

Each test includes:
- **Objective**: What the test validates
- **Test Case**: JSON request examples
- **Expected Result**: What should happen
- **Validation**: How to verify success
- **Cleanup**: How to restore state

### Example Code

See `examples/TestEndpointExamples.vb` for complete working examples:
- 25+ endpoint functions
- All CRUD operations
- Security patterns
- Performance optimization
- Batch operations
- Advanced scenarios

## 🔧 Usage Examples

### Example 1: Basic Read Test

```json
{
  "DestinationIdentifier": "GET_ACTIVE"
}
```

Expected: Returns all active endpoints, ordered by priority.

### Example 2: Search by Type

```json
{
  "EndpointType": "CRUD"
}
```

Expected: Returns only CRUD endpoints.

### Example 3: Create New Endpoint

```json
{
  "EndpointName": "MyTestEndpoint",
  "EndpointType": "READ",
  "Description": "Testing endpoint creation",
  "CreatedBy": "testuser"
}
```

Expected: Creates new endpoint and returns EndpointId.

### Example 4: Batch Insert

```json
{
  "endpoints": [
    {
      "name": "Endpoint1",
      "type": "READ",
      "creator": "testuser"
    },
    {
      "name": "Endpoint2",
      "type": "WRITE",
      "creator": "testuser"
    }
  ]
}
```

Expected: Creates multiple endpoints in one operation.

## 🛡️ Security Testing

### SQL Injection Tests

The test data includes special strings to test SQL injection prevention:

```json
{
  "EndpointName": "Test' OR '1'='1"
}
```

Expected: Handled safely with parameterized queries.

### Field Exclusion Tests

Sensitive fields (ApiKey, SecretToken) should never be returned:

```json
{
  "EndpointId": 1
}
```

Expected: Response excludes ApiKey and SecretToken fields.

### Token Validation Tests

Test with various token scenarios:
- Valid token → Success
- Invalid token → Error
- Missing token → Error
- Expired token → Error

## ⚡ Performance Testing

### Single Record Performance

```json
{"EndpointId": 1}
```

Expected: < 100ms response time

### Batch Performance

| Records | Max Time |
|---------|----------|
| 10 | < 1 sec |
| 100 | < 5 sec |
| 1000 | < 30 sec |

### Large Result Sets

Query returning 1000+ records should complete without timeout.

### FOR JSON PATH

Use `ReadSimpleForJsonPath` for 40-60% performance improvement on simple queries.

## 🧪 Running Specific Test Categories

### 1. Basic CRUD Tests

Follow sections 1-6 in TestEndpointScenarios.md

### 2. Security Tests Only

Follow section 8 in TestEndpointScenarios.md

### 3. Performance Tests Only

Follow section 9 in TestEndpointScenarios.md

### 4. Full Integration Test

Follow section 11.1 in TestEndpointScenarios.md for complete lifecycle test

## ✅ Validation Checklist

Before marking testing complete:

- [ ] Database setup successful (DROP → CREATE → INSERT)
- [ ] Validation script passes all checks (✓)
- [ ] Basic read tests pass (sections 1-2)
- [ ] Write operation tests pass (sections 3-6)
- [ ] Batch operation tests pass (section 7)
- [ ] Security tests pass (section 8)
- [ ] Performance meets requirements (section 9)
- [ ] Edge cases handled (section 10)
- [ ] Integration tests pass (section 11)
- [ ] Test data cleanup completed

## 🔍 Troubleshooting

### Issue: Table Already Exists

**Solution**: Drop manually before running setup:
```sql
DROP TABLE IF EXISTS testepoint;
```

### Issue: Token Validation Fails

**Solution**: Check token configuration or disable for testing

### Issue: Test Data Not Found

**Solution**: Verify setup script completed successfully:
```sql
SELECT COUNT(*) FROM testepoint;
-- Should return 30+
```

### Issue: Special Characters Not Working

**Solution**: Check database collation supports unicode:
```sql
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation');
-- Should support unicode (e.g., SQL_Latin1_General_CP1_CI_AS)
```

### Issue: Performance Tests Slow

**Solution**:
1. Verify indexes created: `EXEC sp_helpindex 'testepoint'`
2. Update statistics: `UPDATE STATISTICS testepoint`
3. Check query execution plan

## 📚 Additional Resources

- **API Documentation**: See `docs/API.md`
- **Security Guidelines**: See `docs/SECURITY.md`
- **Example Code**: See `examples/` directory
- **Performance Guide**: See `docs/PERFORMANCE_IMPROVEMENTS.md`

## 🤝 Contributing

When adding new tests:
1. Update TestEndpointScenarios.md with new test cases
2. Add corresponding test data to testepoint_setup.sql if needed
3. Update validation script if new validation needed
4. Document expected results and validation steps
5. Include cleanup steps

## 📝 Notes

- **Test Data Persistence**: Test data remains in the database. Clean up after testing if needed.
- **Isolation**: Each test should be independent and not rely on state from previous tests.
- **Cleanup**: Include cleanup SQL for any test records created during testing.
- **Documentation**: Document any non-obvious behavior or edge cases discovered.

## 🎯 Success Criteria

All tests should:
- ✅ Execute without errors
- ✅ Return expected results
- ✅ Maintain data integrity
- ✅ Handle edge cases gracefully
- ✅ Meet performance requirements
- ✅ Enforce security rules
- ✅ Provide clear error messages

---

**Ready to test?** Start with the validation script, then follow TestEndpointScenarios.md step by step.
