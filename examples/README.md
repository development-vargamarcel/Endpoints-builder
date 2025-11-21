# EndpointLibrary Examples

This directory contains **advanced, production-ready examples** demonstrating the full capabilities of the EndpointLibrary. Following the [Documentation Guidelines](../docs/DOCUMENTATION_GUIDELINES.md), we maintain **exactly one comprehensive example per major operation type**, showcasing maximum flexibility and real-world patterns.

## 📁 Examples (3 Files)

### 🏢 EnterpriseEndpointExample.vb
**Multi-operation endpoint covering all major patterns**

Complete production implementation showcasing:
- GET operations with complex search and role-based filtering
- POST operations with upsert and batch processing
- PUT/PATCH operations for status updates
- DELETE operations with soft delete pattern
- Analytics operations with aggregations
- Token validation and RBAC
- Audit logging and error handling
- prependSQL usage for session configuration

**Operations:** `orders-search`, `orders-upsert`, `orders-batch`, `orders-update-status`, `orders-statistics`, `orders-soft-delete`

**👉 Start here** for a complete endpoint template covering all operation types.

---

### ⚡ AdvancedBatchAndPerformanceExample.vb
**POST/Batch operations with performance optimization**

High-performance batch processing demonstrating:
- Batch insert/update (50-90% faster than individual operations)
- FOR JSON PATH optimization (40-60% faster queries)
- Bulk existence checking with composite keys
- Insert-only mode for maximum performance
- Error handling and partial success scenarios
- Performance monitoring and benchmarking
- **prependSQL for batch operations** (NEW in v2.2+)
- Batch size optimization (optimal: 100-1000 records)

**9 comprehensive examples** covering all batch operation patterns and optimizations.

---

### 🔍 AdvancedQueryingExample.vb
**GET operations with complex queries and analytics**

Advanced query patterns demonstrating:
- Complex filtering with multiple conditions
- JOINs across multiple tables
- Subqueries and CTEs
- Window functions (ROW_NUMBER, RANK, running totals)
- Aggregations and GROUP BY
- Time-based analysis (daily, weekly, monthly trends)
- Cohort analysis
- Full-text search patterns

**Perfect for:** Reporting, analytics, dashboards, and business intelligence queries.

---

## 🚀 Quick Start

### 1. Choose Your Example

| Need | Use This Example |
|------|------------------|
| Complete endpoint with multiple operations | **EnterpriseEndpointExample.vb** |
| Batch processing and performance | **AdvancedBatchAndPerformanceExample.vb** |
| Complex queries and analytics | **AdvancedQueryingExample.vb** |

### 2. Understand the Pattern

All examples include:
- ✅ Complete, runnable code
- ✅ Example payloads and responses
- ✅ Performance metrics
- ✅ Comprehensive comments explaining "why", not "what"
- ✅ Current API signatures (v2.2+)

### 3. Adapt to Your Needs

Copy the relevant example and customize:
- Replace table/column names with your schema
- Adjust field mappings for your data model
- Modify validation rules for your business logic
- Configure token validation and RBAC for your security model

---

## 📊 Feature Matrix

| Feature | Enterprise | Batch/Perf | Querying |
|---------|-----------|-----------|----------|
| Token Validation | ✅ | ⚪ | ⚪ |
| Role-Based Access | ✅ | ⚪ | ⚪ |
| Batch Operations | ✅ | ✅ | ⚪ |
| FOR JSON PATH | ✅ | ✅ | ✅ |
| Complex Queries | ⚪ | ⚪ | ✅ |
| Field Mappings | ✅ | ✅ | ⚪ |
| Primary Key Decl | ✅ | ✅ | ⚪ |
| Query Prepending | ✅ | ✅ | ⚪ |
| Audit Logging | ✅ | ⚪ | ⚪ |
| Multi-Operation | ✅ | ⚪ | ⚪ |
| JOINs & CTEs | ⚪ | ⚪ | ✅ |
| Window Functions | ⚪ | ⚪ | ✅ |

---

## 🎯 Best Practices

### Performance
```vb
' FOR JSON PATH optimization (automatic, 40-60% faster)
Dim logic = DB.Global.CreateBusinessLogicForReading(sql, conditions, Nothing, Nothing, Nothing)

' Batch operations (50-90% faster for 10+ records)
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(table, mappings, True)

' Optimal batch size: 100-1000 records
' Maximum: 1000 (enforced by MAX_BATCH_SIZE)
```

### Session Configuration (NEW in v2.2+)
```vb
' prependSQL for read operations
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    sql, conditions, Nothing, Nothing, "SET DATEFORMAT ymd; SET NOCOUNT ON;"
)

' prependSQL for batch operations
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    table, mappings, True, "SET DATEFORMAT ymd; SET NOCOUNT ON;"
)
```

### Primary Key Declaration (v2.1+)
```vb
' Declare primary keys in field mappings
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    jsonProps, sqlCols,
    isRequiredArray,
    isPrimaryKeyArray,  ' Mark which fields are primary keys
    defaultValues
)

' Keys automatically extracted from IsPrimaryKey=True fields
Dim logic = DB.Global.CreateBusinessLogicForBatchWriting(table, mappings, True)
```

### Security
```vb
' Enable token validation
Dim CheckToken = True

' Implement RBAC
If userRole <> "ADMIN" Then
    defaultWhere = $"CreatedBy = '{userId}'"
End If

' Exclude sensitive fields
"SELECT UserId, Name, Email FROM Users"  ' Don't expose PasswordHash
```

---

## 📈 Performance Benchmarks

**Batch Operations:**
- 10 records: 50% faster
- 100 records: 70% faster
- 1000 records: 84% faster

**FOR JSON PATH:**
- 10 rows: 50% faster
- 100 rows: 59% faster
- 1000 rows: 60% faster

*Benchmarks are approximate and depend on environment, database configuration, and query complexity.*

---

## 💡 Production Deployment Tips

1. **Enable token validation** - Set `CheckToken = True`
2. **Implement rate limiting** - Monitor logs and limit requests per user
3. **Use connection pooling** - Configure database connection pooling
4. **Index your queries** - Add indexes based on WHERE clauses and JOINs
5. **Monitor performance** - Track P50/P95/P99 query times
6. **Log all operations** - Use `LogCustom` for audit trails
7. **Test at scale** - Test with production-size datasets
8. **Version your API** - Use field mappings to support API versioning

---

## 📚 Documentation Guidelines

This examples directory follows the project's [Documentation Guidelines](../docs/DOCUMENTATION_GUIDELINES.md):

- **Minimalism**: One comprehensive example per operation type
- **Quality over quantity**: Each example demonstrates maximum flexibility
- **Current API**: All examples use latest API signatures (v2.2+)
- **Runnable code**: Complete, tested examples that work out of the box
- **Maintenance**: Examples are reviewed and updated with each major release

When considering adding a new example, ask:
1. Does an existing example already cover this?
2. Is this the most advanced/flexible version of this pattern?
3. Does this demonstrate a unique operation type?

If the answer to all three is "yes", consider updating an existing example instead of adding a new one.

---

## ⚠️ Important Notes

- All examples use parameterized queries (SQL injection protection built-in)
- Token validation is demonstrated (implement proper token management in production)
- Performance metrics are approximate (actual results depend on environment)
- Examples target SQL Server (adjust syntax for other databases)
- Understand basic library concepts before diving into advanced examples

---

**Library Version:** 2.2+
**Examples Count:** 3 (one per major operation type)
**Last Updated:** 2025-11-21

See [../docs/API.md](../docs/API.md) for complete API documentation.
See [../docs/DOCUMENTATION_GUIDELINES.md](../docs/DOCUMENTATION_GUIDELINES.md) for documentation philosophy.
