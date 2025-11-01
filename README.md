# Endpoint Library

A production-ready Visual Basic .NET library for building flexible, secure REST API endpoints with database operations. This library provides a robust framework for creating CRUD operations with advanced features like custom SQL queries, parameter conditions, field mappings, and batch operations.

## Features

- **Flexible Query Building**: Support for custom SQL with parameter conditions
- **CRUD Operations**: Complete Create, Read, Update, Delete functionality
- **Batch Processing**: Handle multiple records in a single request
- **Field Mapping**: Map JSON properties to SQL columns with validation
- **Security**: Built-in token validation and parameterized queries
- **Error Handling**: Comprehensive error handling and reporting
- **Case-Insensitive**: Parameters are case-insensitive for better usability
- **Backward Compatible**: Standard and advanced APIs for different use cases

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
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
' Create a simple read endpoint
Dim readLogic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",                                    ' Table name
    New String() {"UserId", "Email", "Name"},  ' Searchable fields
    New String() {"Password"},                  ' Exclude fields
    True                                        ' Use LIKE operator
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

### Basic Write Operation

```vb
' Create a write endpoint with upsert capability
Dim writeLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",                                        ' Table name
    New String() {"UserId", "Email", "Name"},      ' All fields
    New String() {"UserId"},                       ' Key fields (required)
    True                                           ' Allow updates
)

Return DB.Global.ProcessActionLink(
    DB,
    DB.Global.CreateValidator(New String() {"UserId", "Email"}),
    writeLogic,
    "User created/updated",
    ParsedPayload,
    StringPayload,
    False
)
```

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

Map JSON properties to database columns with validation:

```vb
Dim mapping = DB.Global.CreateFieldMapping(
    "userId",           ' JSON property name
    "USER_ID",          ' SQL column name
    True,               ' Is required
    Nothing             ' Default value
)
```

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

#### CreateBusinessLogicForReadingRows
```vb
Function CreateBusinessLogicForReadingRows(
    tableName As String,
    AllParametersList As String(),
    excludeFields As String(),
    Optional useLikeOperator As Boolean = True
) As Func(Of Object, JObject, Object)
```
Creates standard read logic for a table.

#### CreateBusinessLogicForWritingRows
```vb
Function CreateBusinessLogicForWritingRows(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```
Creates standard write logic (insert/update) for a table.

#### CreateBusinessLogicForWritingRowsBatch
```vb
Function CreateBusinessLogicForWritingRowsBatch(
    tableName As String,
    AllParametersList As String(),
    RequiredParametersList As String(),
    allowUpdates As Boolean
) As Func(Of Object, JObject, Object)
```
Creates batch write logic for multiple records.

#### CreateAdvancedBusinessLogicForReading
```vb
Function CreateAdvancedBusinessLogicForReading(
    baseSQL As String,
    parameterConditions As Dictionary(Of String, Object),
    Optional excludeFields As String() = Nothing,
    Optional defaultWhereClause As String = Nothing,
    Optional fieldMappings As Dictionary(Of String, FieldMapping) = Nothing
) As Func(Of Object, JObject, Object)
```
Creates advanced read logic with custom SQL and parameter conditions.

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

See the `examples/` directory for comprehensive examples:

- **BasicUsageExample.vb**: Original usage example with DestinationIdentifier pattern
- **CrudOperations.vb**: Complete CRUD operation examples
- **AdvancedQueries.vb**: Complex queries with parameter conditions
- **BatchOperations.vb**: Batch insert/update operations
- **FieldMappingExample.vb**: JSON to SQL field mapping
- **ErrorHandling.vb**: Error handling patterns
- **SecurityPatterns.vb**: Token validation and security

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
mappings.Add("userId", DB.Global.CreateFieldMapping("userId", "USER_ID", True))
```

## Advanced Features

### Custom SQL with Placeholders

```vb
Dim sql = "SELECT * FROM Users {WHERE} ORDER BY CreatedDate DESC"
' {WHERE} will be replaced with generated WHERE clause
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
    "Records": [
        {"UserId": "123", "Email": "user@example.com", "Name": "John Doe"}
    ]
}
```

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
│   └── EndpointLibrary.vb          # Main library file
├── examples/
│   ├── BasicUsageExample.vb        # Original usage example
│   ├── CrudOperations.vb           # CRUD examples
│   ├── AdvancedQueries.vb          # Advanced query examples
│   ├── BatchOperations.vb          # Batch operation examples
│   ├── FieldMappingExample.vb      # Field mapping examples
│   ├── ErrorHandling.vb            # Error handling patterns
│   └── SecurityPatterns.vb         # Security best practices
├── docs/
│   ├── API.md                      # Detailed API documentation
│   ├── SECURITY.md                 # Security guidelines
│   └── MIGRATION.md                # Migration guide
├── tests/
│   └── TestScenarios.md            # Test scenarios
├── README.md                        # This file
├── CHANGELOG.md                     # Version history
├── CONTRIBUTING.md                  # Contribution guidelines
└── LICENSE                          # License file
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

## Performance Tips

1. **Use indexes** on columns used in WHERE clauses
2. **Exclude unnecessary fields** to reduce payload size
3. **Use batch operations** for multiple records
4. **Implement pagination** for large result sets
5. **Cache frequently accessed data** at the application level

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
