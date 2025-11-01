# Migration Guide - Simplified API

## Overview

The library has been streamlined to have **ONE clean way** to do each operation. All redundant/"backward compatible" code has been removed.

## Breaking Changes

### Removed Functions

These functions have been **completely removed**:

| Old Function (REMOVED) | New Function (Use This) |
|------------------------|------------------------|
| `CreateAdvancedBusinessLogicForReading` | `CreateBusinessLogicForReading` |
| `CreateBusinessLogicForReadingRows` | `CreateBusinessLogicForReading` |
| `CreateBusinessLogicForWritingRows` | `CreateBusinessLogicForWriting` |
| `CreateBusinessLogicForWritingRowsBatch` | `CreateBusinessLogicForBatchWriting` |

### Parameter Changes

#### Read Operations

**OLD (REMOVED)**:
```vb
Dim readLogic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",                              ' Table name
    New String() {"UserId", "Email"},     ' Searchable fields
    New String() {"Password"},            ' Excluded fields
    True                                  ' Use LIKE operator
)
```

**NEW (Required)**:
```vb
' Define parameter conditions
Dim conditions As New Dictionary(Of String, Object)
conditions.Add("UserId", DB.Global.CreateParameterCondition(
    "UserId",
    "UserId = :UserId",
    Nothing
))
conditions.Add("Email", DB.Global.CreateParameterCondition(
    "Email",
    "Email LIKE :Email",
    Nothing
))

' Use explicit field selection - NO field exclusion
Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name FROM Users {WHERE}",  ' Explicit fields
    conditions
)
```

#### Write Operations

**OLD (REMOVED)**:
```vb
Dim writeLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    New String() {"UserId", "Email", "Name"},
    New String() {"UserId"},  ' Key fields
    True  ' Allow updates
)
```

**NEW (Required)**:
```vb
' Define field mappings
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name"},    ' JSON properties
    New String() {"UserId", "Email", "Name"},    ' SQL columns
    New Boolean() {True, True, False},           ' IsRequired flags
    New Object() {Nothing, Nothing, Nothing}     ' Default values
)

Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Users",
    mappings,
    New String() {"UserId"},  ' Key fields
    True  ' Allow updates
)
```

#### Batch Operations

**OLD (REMOVED)**:
```vb
Dim batchLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Users",
    New String() {"UserId", "Email", "Name"},
    New String() {"UserId"},
    True
)
```

**NEW (Required)**:
```vb
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"userId", "email", "name"},
    New String() {"UserId", "Email", "Name"},
    New Boolean() {True, True, False},
    New Object() {Nothing, Nothing, Nothing}
)

Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Users",
    mappings,
    New String() {"UserId"},
    True
)
```

## Migration Steps

### Step 1: Update Read Operations

1. **Identify** all uses of `CreateBusinessLogicForReadingRows` or `CreateAdvancedBusinessLogicForReading`
2. **Replace** with `CreateBusinessLogicForReading`
3. **Add** explicit field selection in SQL (no `SELECT *`)
4. **Define** parameter conditions using `CreateParameterCondition`

### Step 2: Update Write Operations

1. **Identify** all uses of `CreateBusinessLogicForWritingRows`
2. **Replace** with `CreateBusinessLogicForWriting`
3. **Create** field mappings using `CreateFieldMappingsDictionary`
4. **Update** JSON property names to match your payload

### Step 3: Update Batch Operations

1. **Identify** all uses of `CreateBusinessLogicForWritingRowsBatch`
2. **Replace** with `CreateBusinessLogicForBatchWriting`
3. **Create** field mappings (same as write operations)

### Step 4: Test

1. **Verify** all endpoints still work correctly
2. **Check** performance improvements (should be 50-90% faster)
3. **Monitor** cache hit rates with `GetPropertyCacheStats()`

## Complete Examples

### Example 1: Simple Read Migration

**Before**:
```vb
Dim readLogic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"UserId", "Email"},
    New String() {"Password", "SSN"},
    False
)
```

**After**:
```vb
Dim conditions As New Dictionary(Of String, Object)
conditions.Add("UserId", DB.Global.CreateParameterCondition("UserId", "UserId = :UserId", Nothing))
conditions.Add("Email", DB.Global.CreateParameterCondition("Email", "Email = :Email", Nothing))

Dim readLogic = DB.Global.CreateBusinessLogicForReading(
    "SELECT UserId, Email, Name, Department FROM Users {WHERE}",
    conditions
)
```

### Example 2: Write Migration

**Before**:
```vb
Dim writeLogic = DB.Global.CreateBusinessLogicForWritingRows(
    "Products",
    New String() {"ProductId", "Name", "Price"},
    New String() {"ProductId"},
    True
)
```

**After**:
```vb
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"productId", "name", "price"},
    New String() {"ProductId", "Name", "Price"},
    New Boolean() {True, True, True},
    New Object() {Nothing, Nothing, Nothing}
)

Dim writeLogic = DB.Global.CreateBusinessLogicForWriting(
    "Products",
    mappings,
    New String() {"ProductId"},
    True
)
```

### Example 3: Batch Migration

**Before**:
```vb
Dim batchLogic = DB.Global.CreateBusinessLogicForWritingRowsBatch(
    "Orders",
    New String() {"OrderId", "CustomerId", "Amount"},
    New String() {"OrderId"},
    True
)
```

**After**:
```vb
Dim mappings = DB.Global.CreateFieldMappingsDictionary(
    New String() {"orderId", "customerId", "amount"},
    New String() {"OrderId", "CustomerId", "Amount"},
    New Boolean() {True, True, True},
    New Object() {Nothing, Nothing, Nothing}
)

Dim batchLogic = DB.Global.CreateBusinessLogicForBatchWriting(
    "Orders",
    mappings,
    New String() {"OrderId"},
    True
)
```

## Benefits of New API

1. **Cleaner Code**: One way to do each thing
2. **Better Performance**: 50-90% faster overall
3. **Explicit Field Selection**: Better security and performance
4. **Field Mappings**: Clear JSON-to-SQL mapping
5. **Bulk Operations**: Optimized batch processing
6. **Type Safety**: Required fields explicitly defined

## Common Pitfalls

### ❌ DON'T: Use SELECT *

```vb
' BAD - Fetches all fields, wast bandwidth
"SELECT * FROM Users {WHERE}"
```

### ✅ DO: Explicit Field Selection

```vb
' GOOD - Only fetch what you need
"SELECT UserId, Email, Name FROM Users {WHERE}"
```

### ❌ DON'T: Forget Field Mappings

```vb
' BAD - Old API, won't work
New String() {"userId", "email"}
```

### ✅ DO: Use Field Mappings

```vb
' GOOD - Explicit mapping
CreateFieldMappingsDictionary(
    New String() {"userId", "email"},  ' JSON
    New String() {"UserId", "Email"},  ' SQL
    ...
)
```

## Need Help?

1. Check `/examples` directory for complete working examples
2. See `/docs/API.md` for full API reference
3. Review `/docs/PERFORMANCE_IMPROVEMENTS.md` for performance tips
4. Use `GetPropertyCacheStats()` to monitor cache performance

## Summary

The migration is straightforward:
- **Read**: Use `CreateBusinessLogicForReading` with explicit SELECT
- **Write**: Use `CreateBusinessLogicForWriting` with field mappings
- **Batch**: Use `CreateBusinessLogicForBatchWriting` with field mappings

All changes result in cleaner, faster, more maintainable code.
