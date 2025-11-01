# Security Guide

Comprehensive security guidelines for the Endpoint Library.

## Table of Contents

- [Overview](#overview)
- [Security Features](#security-features)
- [Best Practices](#best-practices)
- [Common Vulnerabilities](#common-vulnerabilities)
- [Secure Configuration](#secure-configuration)
- [Audit and Monitoring](#audit-and-monitoring)
- [Incident Response](#incident-response)

## Overview

Security is built into the Endpoint Library through parameterized queries, token validation, and controlled data access. This guide covers how to use these features effectively.

## Security Features

### 1. Automatic SQL Injection Protection

The library **automatically** uses parameterized queries for all operations.

**How it works:**
```vb
' User provides malicious input
payload: { "UserId": "123'; DROP TABLE Users; --" }

' Library automatically parameterizes
SQL: WHERE UserId = :UserId
Parameters: {"UserId": "123'; DROP TABLE Users; --"}

' Result: Searches for literal string, no SQL injection
```

**You don't need to do anything** - this protection is built-in and cannot be disabled.

### 2. Token Validation

Enable token validation in production:

```vb
' PRODUCTION - Token required
Dim CheckToken = True
Dim PayloadError = DB.Global.ValidatePayloadAndToken(DB, CheckToken, "Context", ParsedPayload, StringPayload)

' Also enable in ProcessActionLink
Return DB.Global.ProcessActionLink(
    DB, validator, logic, "Operation",
    ParsedPayload, StringPayload,
    True  ' CheckForToken = True
)
```

**Required payload structure:**
```json
{
  "Token": "your-valid-token-here",
  "UserId": "123"
}
```

**Validation errors:**
- Missing token: `"Please insert the token in a property called Token."`
- Invalid token: `"Invalid token."`

### 3. Field Exclusion

Always exclude sensitive fields from responses:

```vb
Dim sensitiveFields = New String() {
    "Password",
    "PasswordHash",
    "PasswordSalt",
    "SSN",
    "TaxID",
    "CreditCardNumber",
    "CVV",
    "SecretAnswer",
    "APIKey",
    "RefreshToken",
    "BankAccount"
}

Dim logic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"UserId", "Email"},
    sensitiveFields,  ' These fields NEVER appear in response
    False
)
```

**Critical:** Excluded fields are stripped at the database query level, not just filtered from results.

### 4. Input Validation

Validate all required parameters:

```vb
' Validate required fields exist
Dim validator = DB.Global.CreateValidator(New String() {"UserId", "Action"})

' Additional business logic validation
Dim actionResult = DB.Global.GetStringParameter(payload, "Action")
If actionResult.Item1 Then
    Dim allowedActions = New String() {"READ", "UPDATE", "DELETE"}
    If Not allowedActions.Contains(actionResult.Item2.ToUpper()) Then
        Return DB.Global.CreateErrorResponse("Invalid action")
    End If
End If
```

### 5. Mass Assignment Protection

Use field mappings to control which fields can be modified:

```vb
' Only these fields can be updated
Dim safeMappings As New Dictionary(Of String, FieldMapping)
safeMappings.Add("displayName", DB.Global.CreateFieldMapping("displayName", "DISPLAY_NAME", False, Nothing))
safeMappings.Add("bio", DB.Global.CreateFieldMapping("bio", "BIO", False, Nothing))

' Sensitive fields NOT included: IsAdmin, AccountBalance, Email

Dim logic = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "USER_PROFILES",
    safeMappings,
    New String() {"USER_ID"},
    True, Nothing, Nothing, Nothing
)
```

**Malicious attempt:**
```json
{
  "userId": "123",
  "displayName": "Hacker",
  "IsAdmin": true,
  "AccountBalance": 1000000
}
```

**Result:** Only `displayName` is updated; `IsAdmin` and `AccountBalance` are ignored.

## Best Practices

### 1. Always Enable Token Validation

```vb
' ❌ NEVER IN PRODUCTION
Dim CheckToken = False

' ✅ ALWAYS IN PRODUCTION
Dim CheckToken = True
```

### 2. Principle of Least Privilege

Only expose necessary fields:

```vb
' ❌ BAD - Exposes all fields
Dim allFields = New String() {"*"}

' ✅ GOOD - Explicit field list
Dim allowedFields = New String() {"UserId", "Email", "Name"}
```

### 3. Row-Level Security

Filter data by user ownership:

```vb
Dim conditions As New Dictionary(Of String, Object)

' Force filter to current user (non-admins)
If userRole <> "ADMIN" Then
    Dim defaultWhere = $"UserId = '{currentUserId}'"
Else
    Dim defaultWhere = "1=1"
End If

Dim logic = DB.Global.CreateAdvancedBusinessLogicForReading(
    baseSQL,
    conditions,
    excludeFields,
    defaultWhere,
    Nothing
)
```

### 4. Whitelist, Don't Blacklist

```vb
' ❌ BAD - Blacklist (easy to bypass)
If action = "DROP" Or action = "DELETE_ALL" Then
    Return Error("Invalid action")
End If

' ✅ GOOD - Whitelist (secure by default)
Dim allowedActions = New String() {"READ", "CREATE", "UPDATE"}
If Not allowedActions.Contains(action) Then
    Return Error("Invalid action")
End If
```

### 5. Secure Error Messages

```vb
' ❌ BAD - Leaks information
Catch ex As SqlException
    Return $"SQL Error: {ex.Message}"  ' Reveals database structure

' ✅ GOOD - Generic message, log details
Catch ex As SqlException
    DB.Global.LogCustom(DB, payload, ex.Message, "Database error")
    Return DB.Global.CreateErrorResponse("An error occurred processing your request")
```

### 6. Log Security Events

```vb
' Log authentication attempts
DB.Global.LogCustom(DB, payload, result, $"Login attempt: {userId}")

' Log authorization failures
DB.Global.LogCustom(DB, payload, "Access Denied", $"Unauthorized access attempt: {userId} -> {resource}")

' Log suspicious activity
If failedAttempts > 5 Then
    DB.Global.LogCustom(DB, payload, "ALERT", $"Multiple failed attempts: {userId}")
End If
```

## Common Vulnerabilities

### SQL Injection

**Status:** ✅ **Protected** (automatic parameterized queries)

The library automatically protects against SQL injection. You cannot disable this protection.

**Example:**
```vb
' All these are safe
whereConditions.Add("UserId = :UserId")
whereConditions.Add("Email LIKE :Email")
whereConditions.Add("Status IN (:Status1, :Status2)")
```

### Mass Assignment

**Status:** ⚠️ **Requires Configuration**

**Vulnerability:**
```vb
' ❌ User can update any field
Dim logic = DB.Global.CreateBusinessLogicForWritingRows(
    "Users",
    New String() {"UserId", "Email", "Name", "IsAdmin", "Balance"},
    New String() {"UserId"},
    True
)

' User sends: { "UserId": "123", "IsAdmin": true, "Balance": 1000000 }
' Result: IsAdmin and Balance are updated!
```

**Fix:**
```vb
' ✅ Use field mappings to control updatable fields
Dim mappings As New Dictionary(Of String, FieldMapping)
mappings.Add("email", DB.Global.CreateFieldMapping("email", "EMAIL", False, Nothing))
mappings.Add("name", DB.Global.CreateFieldMapping("name", "NAME", False, Nothing))
' IsAdmin and Balance NOT included

Dim logic = DB.Global.CreateAdvancedBusinessLogicForWriting(
    "Users",
    mappings,
    New String() {"USER_ID"},
    True, Nothing, Nothing, Nothing
)
```

### Insecure Direct Object References (IDOR)

**Status:** ⚠️ **Requires Configuration**

**Vulnerability:**
```vb
' ❌ User can access any user's orders
Dim logic = DB.Global.CreateBusinessLogicForReadingRows(
    "Orders",
    New String() {"OrderId"},
    Nothing,
    False
)

' User A sends: { "OrderId": "999" } (User B's order)
' Result: User A can see User B's order!
```

**Fix:**
```vb
' ✅ Filter by current user
Dim conditions As New Dictionary(Of String, Object)
conditions.Add("OrderId", DB.Global.CreateParameterCondition(
    "OrderId",
    "OrderId = :OrderId AND UserId = :CurrentUserId",
    $"UserId = '{currentUserId}'"
))

Dim logic = DB.Global.CreateAdvancedBusinessLogicForReading(
    "SELECT * FROM Orders {WHERE}",
    conditions,
    Nothing,
    $"UserId = '{currentUserId}'",
    Nothing
)
```

### Information Disclosure

**Status:** ⚠️ **Requires Configuration**

**Vulnerability:**
```vb
' ❌ Sensitive fields exposed
Dim logic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"UserId"},
    Nothing,  ' No excluded fields!
    False
)

' Response includes: Password, SSN, CreditCard, etc.
```

**Fix:**
```vb
' ✅ Always exclude sensitive fields
Dim excludeFields = New String() {
    "Password", "PasswordHash", "SSN",
    "CreditCard", "CVV", "BankAccount"
}

Dim logic = DB.Global.CreateBusinessLogicForReadingRows(
    "Users",
    New String() {"UserId"},
    excludeFields,
    False
)
```

### Broken Access Control

**Status:** ⚠️ **Requires Configuration**

**Vulnerability:**
```vb
' ❌ User can update other users' data
Dim logic = DB.Global.CreateBusinessLogicForWritingRows(
    "UserProfiles",
    New String() {"UserId", "Bio"},
    New String() {"UserId"},
    True
)

' User A sends: { "UserId": "user-b-id", "Bio": "Hacked!" }
' Result: User A modifies User B's profile!
```

**Fix:**
```vb
' ✅ Verify ownership before allowing update
Dim targetUserId = GetStringParameter(payload, "UserId").Item2
If targetUserId <> currentUserId And userRole <> "ADMIN" Then
    Return DB.Global.CreateErrorResponse("Unauthorized")
End If

' Proceed with update...
```

## Secure Configuration

### Development vs. Production

```vb
#If DEBUG Then
    ' Development settings
    Dim CheckToken = False
    Dim DetailedErrors = True
    Dim LogLevel = "DEBUG"
#Else
    ' Production settings
    Dim CheckToken = True
    Dim DetailedErrors = False
    Dim LogLevel = "ERROR"
#End If
```

### Environment-Specific Configuration

Store sensitive configuration securely:

- ✅ Use environment variables or secure configuration store
- ✅ Never commit tokens or credentials to source control
- ✅ Rotate tokens regularly
- ✅ Use different tokens for dev/staging/production

### HTTPS Only

Deploy all endpoints over HTTPS:

```vb
' Check if request is secure
If Not Request.IsSecureConnection Then
    Return DB.Global.CreateErrorResponse("HTTPS required")
End If
```

## Audit and Monitoring

### What to Log

```vb
' 1. Authentication events
DB.Global.LogCustom(DB, payload, result, "LOGIN: " & userId)

' 2. Authorization failures
DB.Global.LogCustom(DB, payload, "DENIED", "AUTHZ_FAIL: " & userId & " -> " & resource)

' 3. Data access
DB.Global.LogCustom(DB, payload, result, "DATA_ACCESS: " & userId & " query: " & tableName)

' 4. Data modifications
DB.Global.LogCustom(DB, payload, result, "DATA_MOD: " & userId & " " & action & " " & recordId)

' 5. Errors and exceptions
DB.Global.LogCustom(DB, payload, ex.Message, "ERROR: " & context)
```

### What NOT to Log

❌ Never log:
- Passwords or password hashes
- Credit card numbers or CVV
- Social Security Numbers
- API tokens or refresh tokens
- Other PII or sensitive data

### Monitoring Alerts

Set up alerts for:

1. **Rate limiting violations:** User exceeds request threshold
2. **Authentication failures:** Multiple failed login attempts
3. **Authorization failures:** Repeated unauthorized access attempts
4. **SQL errors:** May indicate injection attempts
5. **Unusual patterns:** Bulk data access, off-hours activity

### Log Analysis

```sql
-- Failed authentication attempts
SELECT UserId, COUNT(*) as Attempts
FROM SecurityLogs
WHERE LogType = 'LOGIN_FAIL'
  AND LogDate > DATEADD(hour, -1, GETDATE())
GROUP BY UserId
HAVING COUNT(*) > 5

-- Suspicious data access
SELECT UserId, COUNT(*) as AccessCount
FROM SecurityLogs
WHERE LogType = 'DATA_ACCESS'
  AND LogDate > DATEADD(minute, -5, GETDATE())
GROUP BY UserId
HAVING COUNT(*) > 100

-- Authorization failures
SELECT UserId, Resource, COUNT(*) as Attempts
FROM SecurityLogs
WHERE LogType = 'AUTHZ_FAIL'
  AND LogDate > DATEADD(day, -1, GETDATE())
GROUP BY UserId, Resource
ORDER BY COUNT(*) DESC
```

## Incident Response

### If SQL Injection Suspected

1. **Assess:** Review logs for suspicious SQL patterns
2. **Verify:** Library uses parameterized queries (protected by default)
3. **Check:** Ensure no custom SQL uses string concatenation
4. **Monitor:** Watch for unusual database activity

### If Token Compromise Suspected

1. **Immediate:**
   - Revoke compromised token
   - Force user to re-authenticate
   - Review access logs for unauthorized activity

2. **Investigation:**
   - Identify scope of compromise
   - Check for data exfiltration
   - Review system logs

3. **Remediation:**
   - Rotate all tokens
   - Reset affected user passwords
   - Apply additional authentication factors

### If Data Breach Detected

1. **Contain:**
   - Disable affected endpoints
   - Revoke all tokens
   - Block suspicious IPs

2. **Investigate:**
   - Determine what data was accessed
   - Identify attack vector
   - Document timeline

3. **Notify:**
   - Inform affected users
   - Report to appropriate authorities
   - Follow regulatory requirements

4. **Remediate:**
   - Fix vulnerabilities
   - Improve monitoring
   - Update security policies
   - Train development team

## Security Checklist

Use this checklist for every endpoint:

- [ ] Token validation enabled (`CheckForToken = True`)
- [ ] Required parameters validated
- [ ] Input whitelist validation (not blacklist)
- [ ] Sensitive fields excluded from responses
- [ ] Row-level security implemented (user can only access own data)
- [ ] Field mappings used (prevent mass assignment)
- [ ] Authorization checks performed (role/permission validation)
- [ ] Operations logged for audit trail
- [ ] Error messages don't leak sensitive information
- [ ] HTTPS enforced
- [ ] Rate limiting considered
- [ ] Tested with malicious input

## Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

## Contact

For security concerns, contact [security@example.com]

**Do not** report security vulnerabilities in public issues.
