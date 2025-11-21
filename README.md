# Endpoint Library

A production-ready Visual Basic .NET library for building flexible, secure REST API endpoints with database operations. This library provides a robust framework for creating CRUD operations with advanced features like custom SQL queries, parameter conditions, field mappings, and batch operations.

## Features

- **Flexible Query Building**: Support for custom SQL with parameter conditions
- **CRUD Operations**: Complete Create, Read, Update, Delete functionality
- **Batch Processing**: Handle multiple records in a single request
- **Field Mapping**: Map JSON properties to SQL columns with validation
- **Primary Key Declaration**: Declare primary keys within field mappings for cleaner API (NEW in v2.1)
- **Security**: Built-in token validation and parameterized queries
- **Error Handling**: Comprehensive error handling and reporting
- **Case-Insensitive**: Parameters are case-insensitive for better usability
- **Backward Compatible**: Standard and advanced APIs for different use cases
- **⚡ High Performance**: FOR JSON PATH support for 40-60% faster queries
- **Library Options**: Control response content (e.g., include/exclude executed SQL queries)

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration Constants](#configuration-constants)
- [Core Concepts](#core-concepts)
- [Library Options](#library-options)
- [API Reference](#api-reference)
- [Usage Examples](#usage-examples)
- [Security Best Practices](#security-best-practices)
- [Advanced Features](#advanced-features)
- [Contributing](#contributing)
- [License](#license)

## Installation

1. Copy `src/EndpointLibrary.vb` to your project's library folder
2. Register it as a Library in your application
3. Set `ENABLED=S` to enable the library
4. The library will be accessible via `DB.Global.*` functions

## Quick Start

### Basic Read Operation

```vb
' Parse and validate payload
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, False, "UsersRead", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then Return PayloadError

' Create search conditions
Dim conditions = DB.Global.CreateParameterConditionsDictionary(
    New String() {"UserId", "Email"},
    New String() {"UserId = :UserId", "Email LIKE :Email"}
)

' Create read logic
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name, Department FROM Users {WHERE}",
    conditions,
    Nothing,  ' No default WHERE
    Nothing,  ' No field mappings
    Nothing   ' No prepend SQL
)

' Process the request
Return DB.Global.ProcessActionLink(
    DB,
    Nothing,              ' No validator (optional)
    readLogic,
    "Users query",
    ParsedPayload,
    StringPayload,
    False                 ' Skip token check
)
```

### Batch Write Operation

```vb
' Parse and validate payload
Dim StringPayload = "" : Dim ParsedPayload
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, False, "UsersWrite", ParsedPayload, StringPayload)
If PayloadError IsNot Nothing Then Return PayloadError

' Create field mappings
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name"},
    New String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, False},      ' isRequired
    New Boolean() {True, False, False},     ' isPrimaryKey
    Nothing
)

' Create batch write logic
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users",
    mappings,
    True  ' Allow updates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"Records"}),
    batchLogic,
    "Users batch update",
    ParsedPayload,
    StringPayload,
    False
)
```

## Configuration Constants

The library uses several configuration constants (v2.2+) to ensure security and performance:

### MAX_BATCH_SIZE
**Value:** `1000`

Maximum number of records allowed in a single batch operation. Protects against:
- Denial of Service (DoS) attacks
- Memory exhaustion
- Database connection timeouts

Batch requests exceeding this limit will be rejected with a clear error message.

### MAX_SQL_IDENTIFIER_LENGTH
**Value:** `128`

Maximum length for SQL identifiers (table names, column names). Follows SQL Server's standard identifier length limit and is enforced by `ValidateSqlIdentifier()` for security.

### MAX_CACHE_SIZE
**Value:** `1000`

Maximum number of objects cached in the property name cache. When exceeded, the cache is automatically cleared to prevent memory issues.

### COMPOSITE_KEY_DELIMITER
**Value:** `ASCII 31 (Unit Separator)`

Internal delimiter used for composite key generation in batch operations. Uses a safe ASCII control character to prevent key collisions (e.g., prevents ambiguity between "123|456" as a single key vs. two keys "123" and "456").

## Core Concepts

### 1. Parameter Conditions

Parameter conditions define SQL behavior based on whether a parameter is present in the request:

```vb
Dim condition = DB.Global.CreateParameterCondition(
    "UserId",                    ' Parameter name
    "UserId = :UserId",          ' SQL when parameter is present
    Nothing,                      ' SQL when absent (Nothing = skip)
    True,                        ' Bind parameter value
    Nothing                      ' Default value
)
```

### 2. Field Mappings

Map JSON properties to database columns with validation and primary key declaration:

```vb
Dim mapping = DB.Global.CreateFieldMapping(
    "userId",           ' JSON property name
    "USER_ID",          ' SQL column name
    True,               ' Is required (for validation)
    True,               ' Is primary key (for existence checking)
    Nothing             ' Default value
)
```

**Field Mapping Properties:**
- **JsonProperty**: JSON property name from request payload
- **SqlColumn**: Database column name
- **IsRequired**: If True, field must be present in payload (validation)
- **IsPrimaryKey**: If True, field is used for existence checking (NEW in v2.1)
- **DefaultValue**: Default value if field is not provided

**Key Insight**:
- `IsRequired` controls validation (must be present in payload)
- `IsPrimaryKey` controls existence checking (used in WHERE clause for updates)
- These are independent - a field can be required without being a primary key, and vice versa

### 3. Validators

Validate required parameters before processing:

```vb
Dim validator = DB.Global.CreateValidator(
    New String() {"UserId", "Email"}  ' Required parameters
)
```

### 4. Batch Validators

Validate array parameters for batch operations:

```vb
Dim batchValidator = DB.Global.CreateValidatorForBatch(
    New String() {"Records"}  ' Required array parameters
)
```

## Library Options

The library provides various options to control response behavior and functionality.

### includeExecutedSQL Option

Control whether the executed SQL query is included in the response. This option is available in `CreateBusinessLogicForReading`.

**Parameters:**
- `includeExecutedSQL` (Boolean, Optional): If `True`, includes the executed SQL query in the response. Default: `True` (for backward compatibility)

**Use Cases:**

#### Development/Debug Mode (Default)
```vb
' Include SQL in response (default behavior - backward compatible)
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name FROM Users {WHERE}",
    searchConditions,
    Nothing,      ' defaultWhereClause
    Nothing,      ' fieldMappings
    True          ' useForJsonPath
    ' includeExecutedSQL defaults to True
)

' Response includes ExecutedSQL field:
' {
'     "Result": "OK",
'     "ProvidedParameters": "UserId",
'     "ExecutedSQL": "SELECT UserId, Email, Name FROM Users WHERE UserId = :UserId FOR JSON PATH",
'     "Records": [...]
' }
```

#### Production Mode (Hide SQL)
```vb
' Exclude SQL from response for security/performance
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name FROM Users {WHERE}",
    searchConditions,
    Nothing,      ' defaultWhereClause
    Nothing,      ' fieldMappings
    True,         ' useForJsonPath
    False         ' includeExecutedSQL = False (SQL will NOT be in response)
)

' Response excludes ExecutedSQL field:
' {
'     "Result": "OK",
'     "ProvidedParameters": "UserId",
'     "Records": [...]
' }
```

**Benefits:**
- **Security**: Hide database schema and query structure from clients in production
- **Performance**: Reduce response payload size
- **Debugging**: Keep SQL visible in development for troubleshooting
- **Backward Compatible**: Defaults to `True`, so existing code continues to work unchanged

## API Reference

### Factory Functions

#### CreateValidator
```vb
Function CreateValidator(requiredParams As String()) As Func(Of JObject, String)
```
Creates a validator for required parameters.

#### CreateValidatorForBatch
```vb
Function CreateValidatorForBatch(requiredArrayParams As String()) As Func(Of JObject, String)
```
Creates a validator for batch operations with required array parameters.

#### CreateBusinessLogicForReading
```vb
Function CreateBusinessLogicForReading(
    tableName As String,
    AllParametersList As String(),
    excludeFields As String(),
    Optional useLikeOperator As Boolean = True
) As Func(Of Object, JObject, Object)
```
Creates standard read logic for a table.

#### CreateBusinessLogicForWriting
```vb
Function CreateBusinessLogicForWriting(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```
Creates standard write logic (insert/update) for a table.

#### CreateBusinessLogicForWritingBatch
```vb
Function CreateBusinessLogicForWritingBatch(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```
Creates batch write logic for multiple records.

#### CreateBusinessLogicForReading
```vb
Function CreateBusinessLogicForReading(
    baseSQL As String,
    parameterConditions As Dictionary(Of String, Object),
    Optional defaultWhereClause As String = Nothing,
    Optional fieldMappings As Dictionary(Of String, FieldMapping) = Nothing,
    Optional useForJsonPath As Boolean = False,
    Optional includeExecutedSQL As Boolean = True,
    Optional prependSQL As String = Nothing
) As Func(Of Object, JObject, Object)
```
Creates advanced read logic with custom SQL and parameter conditions.

**Parameters:**
- `baseSQL`: Base SQL query with {WHERE} placeholder
- `parameterConditions`: Dictionary of parameter conditions
- `defaultWhereClause`: Default WHERE clause if no parameters provided (optional)
- `fieldMappings`: JSON-to-SQL field mappings (optional)
- `useForJsonPath`: If True, uses FOR JSON PATH for better performance (optional, default: False)
- `includeExecutedSQL`: If True, includes executed SQL in response (optional, default: True)
- `prependSQL`: SQL to prepend at the beginning of the query (e.g., "SET DATEFORMAT ymd;") (optional, **NEW in v2.2**)

#### CreateAdvancedBusinessLogicForWriting
```vb
Function CreateAdvancedBusinessLogicForWriting(
    tableName As String,
    fieldMappings As Dictionary(Of String, FieldMapping),
    keyFields As String(),
    allowUpdates As Boolean,
    Optional customExistenceCheckSQL As String = Nothing,
    Optional customUpdateSQL As String = Nothing,
    Optional customWhereClause As String = Nothing
) As Func(Of Object, JObject, Object)
```
Creates advanced write logic with field mappings and custom SQL.

### Utility Functions

#### ProcessActionLink
```vb
Function ProcessActionLink(
    database As Object,
    p_validator As Func(Of JObject, String),
    p_businessLogic As Func(Of Object, JObject, Object),
    Optional LogMessage As String = Nothing,
    Optional payload As JObject = Nothing,
    Optional StringPayload As String = "",
    Optional CheckForToken As Boolean = True
) As String
```
Main function to process requests with validation and business logic.

#### ValidatePayloadAndToken
```vb
Function ValidatePayloadAndToken(
    DB As Object,
    Optional CheckForToken As Boolean = True,
    Optional loggerContext As String = "",
    Optional ByRef ParsedPayload As JObject = Nothing,
    Optional ByRef StringPayload As String = ""
) As Object
```
Validates payload and token, returns error object if validation fails, Nothing if successful.

#### GetDestinationIdentifier
```vb
Function GetDestinationIdentifier(ByRef payload As JObject) As Tuple(Of Boolean, String)
```
Extracts the DestinationIdentifier from payload for routing requests.

#### Parameter Getters
```vb
Function GetStringParameter(payload As JObject, paramName As String) As Tuple(Of Boolean, String)
Function GetDateParameter(payload As JObject, paramName As String) As Tuple(Of Boolean, Date)
Function GetIntegerParameter(payload As JObject, paramName As String) As Tuple(Of Boolean, Integer)
Function GetObjectParameter(payload As JObject, paramName As String) As Tuple(Of Boolean, Object)
Function GetArrayParameter(payload As JObject, paramName As String) As Tuple(Of Boolean, JArray)
```

## Usage Examples

See the `examples/` directory for **advanced, production-ready examples**:

### 🏢 Production-Ready Examples

- **EnterpriseEndpointExample.vb**: Complete production endpoint with token validation, role-based access, batch operations, and audit logging
- **AdvancedBatchAndPerformanceExample.vb**: High-performance batch processing (50-90% faster) with FOR JSON PATH optimization (40-60% faster)
- **AdvancedQueryingExample.vb**: Complex filtering, aggregations, JOINs, subqueries, window functions, and analytical queries
- **AdvancedCRUDExample.vb**: Complete CRUD workflow with multiple operation patterns and DestinationIdentifier routing
- **AdvancedSecurityPatterns.vb**: Comprehensive security implementation with 11 security controls
- **AdvancedPrimaryKeyExample.vb**: Primary key declaration in field mappings (v2.1+ feature)
- **AdvancedFieldMappingExample.vb**: 10 comprehensive field mapping scenarios
- **RobustnessImprovementsExample.vb**: Security and robustness features with 10 examples (v2.2+ features)

### 📄 Supporting Files

- **AdvancedCRUDExample_Setup.sql**: Database setup script with table creation and sample data
- **README.md**: Detailed examples documentation with feature comparison and best practices

**👉 Start with `EnterpriseEndpointExample.vb` for a complete template**

## Security Best Practices

### 1. Always Use Parameterized Queries

The library uses parameterized queries to prevent SQL injection:

```vb
' GOOD - Parameterized (built-in)
whereConditions.Add("UserId = :UserId")

' BAD - String concatenation (DON'T DO THIS)
whereConditions.Add("UserId = '" & userId & "'")
```

### 2. Enable Token Validation

```vb
' Enable token validation for production
Return DB.Global.ProcessActionLink(
    DB, validator, logic, "Operation",
    ParsedPayload, StringPayload,
    True  ' CheckForToken = True
)
```

### 3. Exclude Sensitive Fields

```vb
Dim excludeFields = New String() {"Password", "SSN", "CreditCard"}
```

### 4. Validate Required Parameters

```vb
Dim validator = DB.Global.CreateValidator(
    New String() {"UserId", "Email"}
)
```

### 5. Use Field Mappings for Validation

```vb
Dim mappings As New Dictionary(Of String, FieldMapping)
mappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True, False, Nothing))
mappings.Add("email", DB.Global.CreateFieldMapping("email", "EMAIL_ADDRESS", True, False, Nothing))
```

## Advanced Features

### Primary Key Declaration in Field Mappings (v2.1)

**NEW**: You can now declare primary keys directly in field mappings instead of passing a separate `keyFields` parameter. This provides a cleaner, more intuitive API where all field configuration is in one place.

#### Old Approach (Still Supported)
```vb
' Define field mappings
Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name"},
    New String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, False},  ' isRequired
    New Object() {Nothing, Nothing, Nothing}
)

' Separate keyFields parameter
Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    fieldMappings,
    New String() {"UserId"},  ' keyFields passed separately
    True
)
```

#### New Approach (Recommended)
```vb
' Define field mappings with primary key declaration
Dim fieldMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name"},
    New String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, False},      ' isRequired
    New Boolean() {True, False, False},     ' isPrimaryKey (NEW!)
    New Object() {Nothing, Nothing, Nothing}
)

' No keyFields parameter needed - extracted from field mappings
Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    fieldMappings
    ' Primary keys automatically extracted from IsPrimaryKey=True fields
)
```

#### Composite Primary Keys
```vb
' Mark multiple fields as primary keys
Dim compositeMappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"orderId", "productId", "quantity"},
    New String() {"OrderId", "ProductId", "Quantity"},
    New Boolean() {True, True, True},       ' All required
    New Boolean() {True, True, False},      ' Both orderId and productId are PKs
    New Object() {Nothing, Nothing, Nothing}
)

Dim logic = DB.Global.CreateBusinessLogicForWriting("OrderItems", compositeMappings)
' Existence check: WHERE OrderId = :OrderId AND ProductId = :ProductId
```

**Benefits:**
- Cleaner API - all field configuration in one place
- More intuitive - clear which fields are primary keys
- Supports composite keys naturally
- Fully backward compatible with old approach
- Primary keys are used ONLY for existence checking

See `examples/PrimaryKeyDeclarationExample.vb` for comprehensive examples.

### Query Prepending (v2.2)

**NEW**: You can now prepend SQL statements to queries for setting session-level options before execution.

```vb
' Prepend SET DATEFORMAT to ensure consistent date parsing
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT OrderId, OrderDate, Amount FROM Orders {WHERE}",
    searchConditions,
    Nothing,                    ' defaultWhereClause
    Nothing,                    ' fieldMappings
    True,                       ' useForJsonPath
    False,                      ' includeExecutedSQL
    "SET DATEFORMAT ymd;"       ' prependSQL - prepended to query
)

' Multiple SET statements
Dim readLogicMultiple = DB.Global.CreateBusinessLogicForReading(
    "SELECT * FROM TempData {WHERE}",
    searchConditions,
    Nothing,
    Nothing,
    False,
    True,
    "SET DATEFORMAT ymd; SET NOCOUNT ON;"  ' Multiple statements
)
```

**Common Use Cases:**
- `SET DATEFORMAT ymd;` - Ensure consistent date parsing across different server locales
- `SET NOCOUNT ON;` - Suppress "rows affected" messages for performance
- `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` - Read uncommitted data (dirty reads)
- `SET LOCK_TIMEOUT 5000;` - Set lock timeout to 5 seconds
- `SET ARITHABORT ON;` - Control arithmetic error handling

**Benefits:**
- Ensures consistent behavior across different server configurations
- Eliminates date parsing issues with international formats
- Improves performance with NOCOUNT ON
- Session-specific settings without affecting other connections

See `examples/RobustnessImprovementsExample.vb` for comprehensive examples.

### Custom SQL with Placeholders

```vb
Dim sql = "SELECT * FROM Users {WHERE} ORDER BY CreatedDate DESC"
' {WHERE} will be replaced with generated WHERE clause (case-insensitive)
```

### Conditional Parameters

```vb
' Parameter affects query only when present
Dim condition = DB.Global.CreateParameterCondition(
    "Status",
    "Status = :Status",    ' Applied when Status is in payload
    "Status IS NOT NULL"   ' Applied when Status is absent
)
```

### Complex WHERE Clauses

```vb
Dim conditions As New Dictionary(Of String, Object)

' Date range
conditions.Add("startDate", DB.Global.CreateParameterCondition(
    "startDate", "CreatedDate >= :startDate", Nothing))
conditions.Add("endDate", DB.Global.CreateParameterCondition(
    "endDate", "CreatedDate <= :endDate", Nothing))

' Pattern matching
conditions.Add("email", DB.Global.CreateParameterCondition(
    "email", "Email LIKE :email", Nothing))
```

### Batch Operations with Error Handling

```vb
' Returns detailed results for each record
{
    "Result": "PARTIAL",
    "Inserted": 5,
    "Updated": 3,
    "Errors": 2,
    "ErrorDetails": ["Row 1: Missing required field", "Row 3: Duplicate key"]
}
```

## Response Formats

### Success Response (Read)
```json
{
    "Result": "OK",
    "ProvidedParameters": "userId,email",
    "ExecutedSQL": "SELECT UserId, Email, Name FROM Users WHERE UserId = :UserId",
    "Records": [
        {"UserId": "123", "Email": "user@example.com", "Name": "John Doe"}
    ]
}
```

**Note**: The `ExecutedSQL` field is included by default but can be excluded by setting `includeExecutedSQL = False` in `CreateBusinessLogicForReading`. See [Library Options](#library-options) for details.

### Success Response (Write)
```json
{
    "Result": "OK",
    "Action": "INSERTED",
    "Message": "Record inserted successfully"
}
```

### Error Response
```json
{
    "Result": "KO",
    "Reason": "Missing required parameter: UserId"
}
```

### Batch Response
```json
{
    "Result": "PARTIAL",
    "Inserted": 5,
    "Updated": 3,
    "Errors": 2,
    "ErrorDetails": ["Record 1: Missing field", "Record 4: Validation error"],
    "Message": "Processed 10 records: 5 inserted, 3 updated, 2 errors."
}
```

## Project Structure

```
Endpoints-builder/
├── src/
│   └── EndpointLibrary.vb                      # Main library file
├── examples/
│   ├── EnterpriseEndpointExample.vb            # Production-ready endpoint template
│   ├── AdvancedBatchAndPerformanceExample.vb   # High-performance batch processing
│   ├── AdvancedQueryingExample.vb              # Complex filtering and queries
│   ├── AdvancedCRUDExample.vb                  # Complete CRUD workflow
│   ├── AdvancedSecurityPatterns.vb             # Security best practices
│   ├── AdvancedPrimaryKeyExample.vb            # Primary key declaration (v2.1+)
│   ├── AdvancedFieldMappingExample.vb          # JSON-to-SQL field mappings
│   ├── RobustnessImprovementsExample.vb        # Robustness features (v2.2+)
│   ├── AdvancedCRUDExample_Setup.sql           # Database setup script
│   └── README.md                                # Examples documentation
├── docs/
│   ├── API.md                                   # Detailed API documentation
│   ├── SECURITY.md                              # Security guidelines
│   ├── PERFORMANCE_ANALYSIS.md                  # Performance analysis
│   └── PERFORMANCE_IMPROVEMENTS.md              # Performance improvements
├── tests/
│   └── TestScenarios.md                         # Test scenarios
├── README.md                                    # This file
├── CHANGELOG.md                                 # Version history
├── CONTRIBUTING.md                              # Contribution guidelines
└── LICENSE                                      # License file
```

## Error Handling

The library provides comprehensive error handling:

```vb
Try
    ' Your operation
Catch ex As Exception
    Return CreateErrorResponse($"Operation failed: {ex.Message}")
End Try
```

All errors return a consistent format:
```json
{
    "Result": "KO",
    "Reason": "Detailed error message"
}
```

## Performance Optimizations

The library includes several built-in performance optimizations:

### Automatic Optimizations (v2.0+)

1. **Property Name Caching**: Case-insensitive property lookups are cached for 70-90% faster access
2. **HashSet Field Exclusion**: Field filtering uses O(1) lookups instead of O(n) iteration
3. **Bulk Existence Checks**: Batch operations check all records in a single query (80-90% faster)
4. **StringBuilder SQL Building**: Efficient query construction reduces memory allocations

**Performance Gains**:
- Property lookups: 70-90% faster
- Field filtering: 80-95% faster
- Batch operations: 80-90% faster
- Overall improvement: 50-70% for typical workloads

See [PERFORMANCE_IMPROVEMENTS.md](docs/PERFORMANCE_IMPROVEMENTS.md) for detailed information.

### Performance Best Practices

1. **Use indexes** on columns used in WHERE clauses
2. **Specify exact fields in SELECT** instead of `SELECT *` - reduces data transfer and processing
   ```vb
   ' GOOD - Explicit field selection
   baseSQL = "SELECT UserId, Email, Name FROM Users {WHERE}"

   ' AVOID - SELECT * with field exclusion (legacy approach)
   baseSQL = "SELECT * FROM Users {WHERE}"
   excludeFields = New String() {"Password"}
   ```
3. **Use batch operations** for multiple records (significantly faster than individual operations)
4. **Implement pagination** for large result sets
5. **Monitor cache performance** with `GetPropertyCacheStats()`
6. **Enable connection pooling** in your database connection string

### Performance Monitoring

```vb
' Monitor property cache performance
Dim stats = DB.Global.GetPropertyCacheStats()
' Returns: CacheSize, CacheHits, CacheMisses, HitRate

' Clear cache if needed (e.g., memory pressure)
DB.Global.ClearPropertyCache()
```

## Troubleshooting

### Common Issues

**Issue**: "Parameter not specified"
- **Solution**: Check parameter name case-insensitivity, ensure JSON property matches

**Issue**: "Invalid token"
- **Solution**: Verify token generation, check token validation is enabled

**Issue**: "Record already exists"
- **Solution**: Set `allowUpdates = True` or change key fields

**Issue**: "SQL syntax error"
- **Solution**: Verify custom SQL, check parameter names match placeholders

## Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on contributing to this project.

## License

See [LICENSE](LICENSE) for license information.

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Review existing examples in the `examples/` directory
- Check the detailed API documentation in `docs/API.md`

## Authors

- Original implementation: VB.NET Database Endpoint Framework
- Production refactoring: 2025

## Acknowledgments

- Built for the QW platform
- Uses Newtonsoft.Json for JSON parsing
- Designed for SQL Server compatibility
