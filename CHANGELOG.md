# Changelog

All notable changes to the Endpoint Library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.0] - 2025-11-21

### Added
- **prependSQL for Batch Operations**: Added `prependSQL` parameter to `CreateBusinessLogicForBatchWriting()`
  - Allows prepending SQL statements before batch operations (e.g., `"SET DATEFORMAT ymd; SET NOCOUNT ON;"`)
  - Applied to all batch operations: bulk existence check, INSERT queries, and UPDATE queries
  - Ensures consistent session configuration across all batch operations
  - Optional parameter (default: Nothing) - fully backward compatible
- **Documentation Guidelines**: New `docs/DOCUMENTATION_GUIDELINES.md` file
  - Establishes minimalism principle for documentation
  - One comprehensive example per operation type policy
  - Maintenance rules and review triggers
  - Documentation style guide

### Changed
- **Updated Examples**: Enhanced examples with prependSQL usage
  - `AdvancedBatchAndPerformanceExample.vb`: Added Example 7 demonstrating prependSQL with batch operations
  - `EnterpriseEndpointExample.vb`: Updated batch operation to use prependSQL for session configuration
  - All batch examples now use correct function signatures (removed deprecated `keyFields` parameter)
- **Updated Documentation**:
  - `docs/API.md`: Added prependSQL parameter documentation for batch writing operations
  - `README.md`: Added batch operations section with prependSQL examples
  - `examples/README.md`: Streamlined to reference only 3 core examples per documentation guidelines

### Removed
- **Redundant Documentation Files**:
  - `docs/PERFORMANCE_ANALYSIS.md` (redundant with PERFORMANCE_IMPROVEMENTS.md)
  - `REFACTORING_SUMMARY.md` (migration guide no longer needed after multiple versions)
- **Outdated Examples**:
  - `examples/RobustnessImprovementsExample.vb` (features integrated into remaining examples)

### Technical Details
- `BusinessLogicBatchWriterWrapper` constructor now accepts optional `prependSQL` parameter
- `BulkExistenceCheck()` function updated to accept and apply `prependSQL` parameter
- All SQL operations in batch writing now prepend configured SQL when provided
- Maintains backward compatibility - existing code continues to work without changes

## [2.2.0] - 2025-11-20

### Added - Robustness and Security Improvements

#### Critical Security Fixes
- **SQL Identifier Validation**: Added `ValidateSqlIdentifier()` function to prevent SQL injection via table/column names
  - Validates table names, column names, and key fields
  - Checks for SQL injection patterns (comments, semicolons, quotes)
  - Enforces naming conventions (alphanumeric, underscore, dot for schema notation)
  - Maximum identifier length of 128 characters
- **Batch Size Limits**: Added configurable `MAX_BATCH_SIZE` constant (default: 1000 records)
  - Prevents DoS attacks through memory exhaustion
  - Returns clear error message when limit exceeded
  - Helps prevent SQL parameter limit issues

#### New Features
- **Query Prepending**: Added `prependSQL` parameter to `CreateBusinessLogicForReading()`
  - Allows prepending SQL statements (e.g., `"SET DATEFORMAT ymd;"`)
  - Useful for setting session-level options before query execution
  - Added to both factory function and `BusinessLogicReaderWrapper` constructor

#### Robustness Improvements
- **Resource Leak Prevention**: Added try-finally blocks to all database operations
  - `ExecuteQueryToDictionary()` now guarantees cleanup on error
  - All QWTable instances properly disposed even on exceptions
  - Prevents connection pool exhaustion
- **Composite Key Collision Fix**: Changed delimiter from pipe (`|`) to ASCII 31 (Unit Separator)
  - Prevents ambiguous composite keys (e.g., "123|456|789" vs "123" and "456|789")
  - Uses `COMPOSITE_KEY_DELIMITER` constant for consistency
  - Applied to `BulkExistenceCheck()` and `GetCompositeKey()`
- **Integer Overflow Protection**: Enhanced `GetIntegerParameter()` with range validation
  - Checks values are within `Integer.MinValue` to `Integer.MaxValue`
  - Handles Long to Integer conversions safely
  - Returns `(False, 0)` instead of throwing exceptions on overflow
- **DBNull Handling**: `ExecuteQueryToDictionary()` now converts DBNull to Nothing
  - Ensures proper JSON serialization (null instead of DBNull)
  - Prevents serialization errors with NULL database values
- **Stream Seekability Check**: `ParsePayload()` now checks `CanSeek` before seeking
  - Handles non-seekable streams (network streams, compressed streams)
  - Prevents exceptions with certain stream types
- **Array Length Validation**: Factory functions validate parallel array lengths
  - `CreateFieldMappingsDictionary()` validates all array parameters
  - `CreateParameterConditionsDictionary()` validates all array parameters
  - Throws clear exceptions with detailed error messages
- **Duplicate Key Detection**: Factory functions check for duplicate keys
  - Prevents duplicate field mappings (e.g., two "userId" mappings)
  - Prevents duplicate parameter conditions
  - Throws exception with specific duplicate key name and index
- **Key Field Validation**: Writer validates all key fields are present in payload
  - Checks key fields exist before executing existence queries
  - Returns clear error: "Key field 'X' is missing from payload"
  - Prevents SQL errors from unbound parameters
- **Parameter Name Collision Fix**: `BulkExistenceCheck()` uses better parameter naming
  - Changed from `{keyField}_{paramIndex}` to `p{paramIndex}_{keyField}`
  - Prevents collisions like "UserId_0" vs "UserId" with paramIndex "0"
- **Cache Race Condition Fix**: Improved thread-safe cache management
  - Creates new cache instead of clearing existing one
  - Prevents race conditions during cache size checks
  - Uses double-check pattern for efficiency
- **Case-Insensitive {WHERE} Placeholder**: Regex-based replacement supports all cases
  - `{WHERE}`, `{where}`, `{Where}`, `{wHeRe}` all work correctly
  - Uses `RegexOptions.IgnoreCase` for reliability
- **Single WHERE Placeholder Validation**: Constructor validates only one {WHERE} exists
  - Prevents invalid SQL like "SELECT * FROM T {WHERE} AND {WHERE}"
  - Throws exception at initialization with clear error message
- **LogMessage Fix**: `ProcessActionLink()` now uses LogMessage parameter correctly
  - Changed from hardcoded "Error at ValidatePayloadAndToken: " prefix
  - Uses provided LogMessage directly for accurate logging

### Changed
- **BusinessLogicReaderWrapper Constructor**: Added `prependSQL` parameter (optional, backward compatible)
- **CreateBusinessLogicForReading Factory**: Added `prependSQL` parameter (optional, backward compatible)
- **BusinessLogicWriterWrapper Constructor**: Now validates table names and column names on initialization
- **BusinessLogicBatchWriterWrapper Constructor**: Now validates table names and column names on initialization
- **Cache Clear Logic**: Changed from `_propertyNameCache.Clear()` to atomic replacement
- **WHERE Placeholder Replacement**: Changed from `String.Contains()` to `Regex.IsMatch()` for case-insensitivity

### Examples
- **RobustnessImprovementsExample.vb**: Comprehensive example demonstrating all new features
  - 10 detailed examples showing security and robustness improvements
  - Query prepending for date format handling
  - Batch size limit demonstration
  - SQL injection prevention examples
  - Key field validation scenarios
  - DBNull handling examples
  - Resource cleanup demonstration

### Internal Improvements
- Added `MAX_BATCH_SIZE`, `MAX_SQL_IDENTIFIER_LENGTH`, and `COMPOSITE_KEY_DELIMITER` constants
- Improved code documentation with "ROBUST:", "SECURITY:", and "FEATURE:" comment markers
- Enhanced error messages with specific details about validation failures
- Better separation of concerns with dedicated validation functions

### Performance
- No performance regressions despite added validations
- Validations occur at initialization time, not per-request
- Cache improvements maintain or improve performance

### Backward Compatibility
- ✅ All changes are backward compatible
- New parameters are optional with sensible defaults
- Existing code continues to work unchanged
- Additional validations throw exceptions only for invalid configurations

## [2.0.0] - 2025-01-20

### Added - Production Refactoring Release

#### Project Structure
- Organized codebase into proper directory structure (`src/`, `examples/`, `docs/`, `tests/`)
- Added proper file extensions (`.vb`) to all source files
- Created comprehensive README with quick start guide and API reference
- Added CHANGELOG.md for version tracking
- Added LICENSE file (MIT License)
- Added CONTRIBUTING.md with contribution guidelines

#### Documentation
- **README.md**: Complete library documentation with:
  - Installation instructions
  - Quick start examples
  - Core concepts explanation
  - API reference for all functions
  - Security best practices
  - Performance tips
  - Troubleshooting guide

- **docs/API.md**: Detailed API documentation with:
  - Complete function signatures
  - Parameter descriptions
  - Return value specifications
  - Usage examples for each function

- **docs/SECURITY.md**: Security guidelines covering:
  - Token validation
  - SQL injection prevention
  - Input validation
  - Output sanitization
  - Role-based access control
  - Audit logging

- **docs/MIGRATION.md**: Migration guide for:
  - Upgrading from version 1.x
  - Breaking changes
  - Deprecated features
  - New features adoption

#### Examples (7 comprehensive example files)
- **BasicUsageExample.vb**: Original usage example with DestinationIdentifier pattern
- **CrudOperations.vb**: 10 complete CRUD operation examples covering:
  - Simple read operations
  - Insert operations (no updates)
  - Upsert operations (insert or update)
  - Update specific fields
  - Soft delete operations
  - Read with exact match
  - Read active records only
  - Read with required parameters
  - Multi-table join reads
  - Count records with aggregation

- **AdvancedQueries.vb**: 10 advanced query examples covering:
  - Date range filtering
  - Numeric range filtering
  - Search with default conditions
  - Complex AND/OR conditions
  - IN clause simulation
  - Aggregate queries with GROUP BY
  - Subquery examples
  - Full-text search simulation
  - Pagination with TOP N
  - Dynamic sorting patterns

- **BatchOperations.vb**: 10 batch operation examples covering:
  - Basic batch insert/update
  - Batch insert only (no updates)
  - Batch with error handling
  - Bulk update existing records
  - Single record fallback
  - Import data from external sources
  - Batch soft delete
  - Synchronization patterns
  - Batch with computed fields
  - Monitoring batch results

- **FieldMappingExample.vb**: 10 field mapping examples covering:
  - Basic field mapping (JSON to SQL)
  - Required vs optional fields
  - Default values
  - Reading with field mappings
  - Complex mapping scenarios
  - Dynamic field mapping
  - Validation with field mappings
  - Exclude unmapped fields
  - Versioned API mappings
  - Audit trail with mappings

- **SecurityPatterns.vb**: 10 security pattern examples covering:
  - Token validation (enabled)
  - Exclude sensitive fields
  - Parameterized queries (built-in)
  - Input validation
  - Role-based data filtering
  - Rate limiting (log-based)
  - Secure update operations
  - Prevent mass assignment
  - Secure batch operations
  - Comprehensive security checklist

#### Testing
- Added test scenarios documentation in `tests/TestScenarios.md`
- Documented testing patterns for all major features

### Changed

#### Core Library
- Enhanced documentation comments in source code
- Improved error messages for better debugging
- Standardized response formats across all operations

### Fixed
- Consistent error handling across all operations
- Improved case-insensitive parameter handling

### Security
- Documented SQL injection protection (automatic parameterized queries)
- Added security best practices guide
- Included comprehensive security examples
- Documented token validation patterns

## [1.0.0] - 2024-XX-XX

### Added - Initial Release

#### Core Features
- Basic CRUD operations support
- Standard business logic reader for simple queries
- Standard business logic writer with upsert capability
- Batch operations support
- Token validation
- Case-insensitive parameter handling
- Field exclusion for sensitive data
- JSON payload parsing

#### Advanced Features
- Advanced business logic reader with custom SQL
- Advanced business logic writer with field mappings
- Parameter conditions (conditional SQL based on parameter presence)
- Field mappings (JSON to SQL column mapping)
- Custom existence checks
- Custom update SQL
- Custom WHERE clauses

#### Utility Functions
- ValidatePayloadAndToken: Payload and token validation
- GetDestinationIdentifier: Request routing support
- Parameter getters (String, Date, Integer, Object, Array)
- CreateErrorResponse: Standardized error responses
- ExecuteQueryToDictionary: Query execution helper
- LogCustom: Custom logging support

#### Factory Functions
- CreateValidator: Required parameter validation
- CreateValidatorForBatch: Batch operation validation
- CreateBusinessLogicForReadingRows: Standard read logic
- CreateBusinessLogicForWritingRows: Standard write logic
- CreateBusinessLogicForWritingRowsBatch: Batch write logic
- CreateAdvancedBusinessLogicForReading: Advanced read logic
- CreateAdvancedBusinessLogicForWriting: Advanced write logic
- CreateParameterCondition: Parameter condition creation
- CreateFieldMapping: Field mapping creation
- CreateParameterConditionsDictionary: Bulk parameter condition creation
- CreateFieldMappingsDictionary: Bulk field mapping creation

#### Classes
- ParameterCondition: Conditional SQL behavior
- FieldMapping: JSON to SQL column mapping
- ValidatorWrapper: Parameter validation
- ValidatorForBatchWrapper: Batch parameter validation
- BusinessLogicReaderWrapper: Standard read operations
- BusinessLogicWriterWrapper: Standard write operations
- BusinessLogicBatchWriterWrapper: Batch operations
- BusinessLogicAdvancedReaderWrapper: Advanced read operations
- BusinessLogicAdvancedWriterWrapper: Advanced write operations

## Future Roadmap

### Planned for 2.1.0
- Add async operation support
- Implement caching layer
- Add pagination helper functions
- Add bulk delete operations
- Add soft delete configuration
- Add field-level encryption support

### Planned for 2.2.0
- Add GraphQL-style field selection
- Add data validation rules engine
- Add automatic audit trail generation
- Add change tracking
- Add optimistic concurrency control

### Planned for 3.0.0
- Add support for NoSQL databases
- Add distributed transaction support
- Add event-driven architecture support
- Add real-time data synchronization
- Add API versioning framework

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
