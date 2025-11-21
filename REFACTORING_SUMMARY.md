# Refactoring Summary

## Breaking Changes - API Simplification

This refactoring removes backward compatibility and simplifies the API to have only one way of doing things.

### 1. Factory Function Removal
**REMOVED:**
- `CreateParameterCondition()` - Use `New ParameterCondition()` instead
- `CreateFieldMapping()` - Use `New FieldMapping()` instead

**KEPT:**
- `CreateParameterConditionsDictionary()` - Useful for array-to-dictionary conversion
- `CreateFieldMappingsDictionary()` - Useful for array-to-dictionary conversion

### 2. Primary Key Declaration
**REMOVED:**
- `keyFields` parameter from all write functions

**REQUIRED:**
- ALL primary keys MUST be declared using `IsPrimaryKey=True` in field mappings
- Constructor will throw exception if no primary keys are marked

### 3. Validator Unification
**REMOVED:**
- `CreateValidatorForBatch()` function
- `ValidatorForBatchWrapper` class

**UNIFIED:**
- `CreateValidator(requiredParams, requiredArrayParams)` - Single function handles both scalar and array parameter validation

### 4. Validation Approach
**REMOVED:**
- Token validation from `ProcessActionLink()`
- `CheckForToken` parameter

**KEPT ONLY:**
- `ValidatePayloadAndToken()` - Single validation entry point
- Users must call this before `ProcessActionLink()`

### 5. Write Operations
**REMOVED:**
- `CreateBusinessLogicForWriting()` - Single record function
- `BusinessLogicWriterWrapper` class

**KEPT ONLY:**
- `CreateBusinessLogicForBatchWriting()` - Handles both single records (as array of 1) and batch operations

### 6. Performance Optimization
**REMOVED:**
- `useForJsonPath` parameter

**AUTOMATIC:**
- Always tries FOR JSON PATH first (40-60% faster)
- Automatically falls back to standard mode on error
- No user configuration needed

### 7. SQL in Responses
**REMOVED:**
- `includeExecutedSQL` parameter
- `ExecutedSQL` field from all responses

**SECURITY:**
- SQL queries are NEVER included in responses for security reasons

## Migration Guide

### Before (Old API):
```vb
' Creating objects with factory functions
Dim condition = DB.Global.CreateParameterCondition("UserId", "UserId = :UserId", Nothing)
Dim mapping = DB.Global.CreateFieldMapping("userId", "UserId", True, True, Nothing)

' Separate validators
Dim validator = DB.Global.CreateValidator(New String() {"UserId"})
Dim batchValidator = DB.Global.CreateValidatorForBatch(New String() {"Records"})

' Token validation in ProcessActionLink
Return DB.Global.ProcessActionLink(DB, validator, logic, "Message", payload, stringPayload, True)

' Separate single/batch write functions with keyFields parameter
Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users", fieldMappings, New String() {"UserId"}, True)
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users", fieldMappings, New String() {"UserId"}, True)

' Manual performance toggle
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    sql, conditions, Nothing, mappings, True, True, Nothing)
```

### After (New API):
```vb
' Use New directly or dictionary factories
Dim condition = New ParameterCondition("UserId", "UserId = :UserId", Nothing)
Dim mapping = New FieldMapping("userId", "UserId", True, True, Nothing)

' Or use dictionary factories
Dim conditions = DB.Global.CreateParameterConditionsDictionary(...)
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email"},
    New String() {"UserId", "Email"},
    New Boolean() {True, True},     ' isRequired
    New Boolean() {True, False},    ' IsPrimaryKey - MUST specify primary keys here
    Nothing)

' Single unified validator
Dim validator = DB.Global.CreateValidator(
    New String() {"UserId"},      ' scalar params
    New String() {"Records"})     ' array params

' Validation BEFORE ProcessActionLink
Dim validationError = DB.Global.ValidatePayloadAndToken(DB, True, "Context", payload, stringPayload)
If validationError IsNot Nothing Then Return validationError

' No token check in ProcessActionLink
Return DB.Global.ProcessActionLink(DB, validator, logic, "Message", payload, stringPayload)

' Single batch write function (handles both single and batch)
' NOTE: No keyFields parameter - keys must be marked in field mappings
Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users", fieldMappings, True)  ' allowUpdates

' For single record, wrap in array:
' {"Records": [{"userId": 1, "email": "test@example.com"}]}

' Automatic performance optimization (no parameters)
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    sql, conditions, Nothing, mappings, "SET DATEFORMAT ymd;")
```

## Response Format Changes

### Before:
```json
{
    "Result": "OK",
    "ProvidedParameters": "userId",
    "ExecutedSQL": "SELECT * FROM Users WHERE UserId = :UserId FOR JSON PATH",
    "Records": [...]
}
```

### After:
```json
{
    "Result": "OK",
    "ProvidedParameters": "userId",
    "Records": [...]
}
```

**Note:** ExecutedSQL is never included for security reasons.

## Key Benefits

1. **Simpler API** - One way to do each thing
2. **Clearer Intent** - No ambiguity about which approach to use
3. **Better Security** - SQL never exposed in responses
4. **Automatic Optimization** - FOR JSON PATH always attempted
5. **Unified Validation** - Single validator handles all cases
6. **Explicit Primary Keys** - All keys declared in field mappings
7. **Unified Write Operations** - Batch function handles both cases
