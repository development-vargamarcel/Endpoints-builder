# Contributing to Endpoint Library

Thank you for your interest in contributing to the Endpoint Library! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Process](#development-process)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment for all contributors, regardless of experience level, background, or identity.

### Our Standards

- Be respectful and constructive in all interactions
- Welcome newcomers and help them get started
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

### Unacceptable Behavior

- Harassment or discriminatory language
- Trolling or insulting comments
- Personal or political attacks
- Publishing others' private information
- Other conduct which could reasonably be considered inappropriate

## Getting Started

### Prerequisites

- Visual Basic .NET development environment
- Access to QW platform (for testing)
- Familiarity with SQL Server
- Understanding of REST API design principles

### Setting Up Development Environment

1. Clone the repository
2. Review the existing codebase in `src/EndpointLibrary.vb`
3. Read through the examples in `examples/`
4. Familiarize yourself with the documentation in `docs/`

## Development Process

### Branching Strategy

- `main` branch contains stable, production-ready code
- Create feature branches from `main` for new features
- Use descriptive branch names: `feature/add-pagination`, `fix/token-validation-bug`, `docs/api-reference-update`

### Commit Messages

Write clear, descriptive commit messages:

```
feat: Add pagination support for read operations

- Add page size and page number parameters
- Update response format to include total count
- Add examples for paginated queries
- Update documentation

Resolves #123
```

Commit message format:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

## Coding Standards

### Visual Basic .NET Style Guide

#### Naming Conventions

```vb
' Classes: PascalCase
Public Class BusinessLogicReaderWrapper

' Public methods and properties: PascalCase
Public Function Execute(database As Object) As Object
Public Property ParameterName As String

' Private fields: _camelCase with underscore prefix
Private _tableName As String
Private _excludeFields As String()

' Parameters: camelCase
Public Sub New(tableName As String, parametersList As String())

' Constants: UPPER_SNAKE_CASE
Const MAX_RETRY_COUNT As Integer = 3
```

#### Code Organization

```vb
' 1. Imports (if needed)

' 2. Class declaration with summary
''' <summary>
''' Provides read operations for database tables
''' </summary>
Public Class BusinessLogicReaderWrapper

    ' 3. Private fields
    Private ReadOnly _tableName As String
    Private ReadOnly _parameters As String()

    ' 4. Constructor
    Public Sub New(tableName As String, parameters As String())
        _tableName = tableName
        _parameters = parameters
    End Sub

    ' 5. Public methods
    Public Function Execute() As Object
        ' Implementation
    End Function

    ' 6. Private helper methods
    Private Function ValidateInput() As Boolean
        ' Implementation
    End Function
End Class
```

#### Error Handling

Always use structured error handling:

```vb
Public Function Execute() As Object
    Try
        ' Main logic here
        Return New With {.Result = "OK"}
    Catch ex As SqlException
        ' Handle SQL-specific errors
        Return CreateErrorResponse($"Database error: {ex.Message}")
    Catch ex As Exception
        ' Handle general errors
        Return CreateErrorResponse($"Error: {ex.Message}")
    End Try
End Function
```

#### Comments and Documentation

```vb
''' <summary>
''' Creates advanced business logic for reading records with custom SQL
''' </summary>
''' <param name="baseSQL">Base SQL query (can include {WHERE} placeholder)</param>
''' <param name="parameterConditions">Dictionary of parameter-specific conditions</param>
''' <param name="excludeFields">Fields to exclude from results</param>
''' <returns>Function that executes the read logic</returns>
''' <example>
''' Dim conditions As New Dictionary(Of String, Object)
''' conditions.Add("status", CreateParameterCondition("status", "Status = :status", Nothing))
''' Dim logic = CreateAdvancedBusinessLogicForReading("SELECT * FROM Users {WHERE}", conditions)
''' </example>
Public Function CreateAdvancedBusinessLogicForReading(...) As Func(Of Object, JObject, Object)
```

### SQL Best Practices

1. **Always use parameterized queries**
   ```vb
   ' GOOD
   whereConditions.Add($"{param.Key} = :{param.Key}")

   ' BAD - Never do this!
   whereConditions.Add($"{param.Key} = '{param.Value}'")
   ```

2. **Use consistent SQL formatting**
   ```vb
   Dim sql = "SELECT * FROM Users WHERE UserId = :UserId AND Status = :Status"
   ```

3. **Handle NULL values properly**
   ```vb
   If value Is Nothing OrElse IsDBNull(value) Then
       ' Handle NULL case
   End If
   ```

## Testing Guidelines

### Test Coverage

All new features should include:
1. Unit tests for core functionality
2. Integration tests for database operations
3. Security tests for validation and authorization
4. Example code demonstrating usage

### Test Scenarios

Document test scenarios in `tests/TestScenarios.md`:

```markdown
### Feature: Advanced Parameter Conditions

#### Test Case 1: Parameter Present
- **Input**: `{ "status": "Active" }`
- **Expected SQL**: `WHERE Status = :Status`
- **Expected Result**: Records with Status = "Active"

#### Test Case 2: Parameter Absent
- **Input**: `{}`
- **Expected SQL**: No WHERE clause or default condition
- **Expected Result**: All records or default filtered set
```

### Testing Checklist

Before submitting:
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] Edge cases covered (NULL, empty, invalid input)
- [ ] Security scenarios tested (injection attempts, unauthorized access)
- [ ] Performance tested with realistic data volumes
- [ ] Documentation updated

## Documentation

### Required Documentation

When adding new features:

1. **Code Comments**: Add XML documentation comments to all public functions
2. **README Updates**: Update relevant sections in README.md
3. **Examples**: Add usage examples to appropriate example file
4. **API Documentation**: Update docs/API.md with new functions
5. **CHANGELOG**: Add entry describing the change

### Documentation Style

- Use clear, concise language
- Provide practical examples
- Include both simple and advanced use cases
- Document edge cases and limitations
- Add troubleshooting tips

### Example Documentation Template

```vb
''' <summary>
''' Brief description of what the function does
''' </summary>
''' <param name="paramName">Description of parameter</param>
''' <returns>Description of return value</returns>
''' <remarks>
''' Additional details, limitations, or important notes
''' </remarks>
''' <example>
''' Example usage:
''' <code>
''' Dim result = FunctionName(parameter)
''' ' Expected output: ...
''' </code>
''' </example>
```

## Submitting Changes

### Pull Request Process

1. **Before Starting**
   - Check existing issues and PRs to avoid duplication
   - Discuss major changes in an issue first
   - Fork the repository and create a feature branch

2. **Development**
   - Write clean, well-documented code
   - Follow coding standards
   - Add appropriate tests
   - Update documentation

3. **Before Submitting**
   - Test your changes thoroughly
   - Update CHANGELOG.md
   - Ensure code compiles without warnings
   - Run existing tests to verify no regressions

4. **Submit Pull Request**
   - Provide clear description of changes
   - Reference related issues
   - Include examples of new functionality
   - Request review from maintainers

5. **After Submission**
   - Respond to review feedback promptly
   - Make requested changes
   - Keep PR up to date with main branch

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Changes Made
- List of specific changes
- Another change
- etc.

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] Examples added/updated

## Documentation
- [ ] Code comments added
- [ ] README updated
- [ ] API documentation updated
- [ ] CHANGELOG updated

## Related Issues
Fixes #123
Relates to #456

## Screenshots (if applicable)
```

## Reporting Issues

### Bug Reports

Include:
- Clear, descriptive title
- Detailed description of the issue
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details (VB.NET version, QW version, etc.)
- Code samples demonstrating the issue
- Error messages or logs

### Feature Requests

Include:
- Clear, descriptive title
- Use case and motivation
- Proposed solution
- Alternative solutions considered
- Impact on existing functionality

### Security Issues

**Do not** report security vulnerabilities in public issues. Instead:
1. Email security concerns to [security@example.com]
2. Provide detailed description
3. Include steps to reproduce
4. Wait for acknowledgment before public disclosure

## Types of Contributions

### Good First Issues

Look for issues labeled `good-first-issue`:
- Documentation improvements
- Adding examples
- Fixing typos
- Writing tests
- Minor bug fixes

### Areas for Contribution

- **Core Library**: Enhance existing features
- **Examples**: Add more usage scenarios
- **Documentation**: Improve clarity and completeness
- **Testing**: Increase test coverage
- **Performance**: Optimize query generation
- **Security**: Enhance security features
- **Tooling**: Improve development tools

## Recognition

Contributors will be:
- Listed in project documentation
- Mentioned in release notes
- Credited in CHANGELOG.md

## Questions?

- Review existing documentation
- Check closed issues for similar questions
- Open a new issue with the `question` label
- Reach out to maintainers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to Endpoint Library! Your efforts help make this project better for everyone.
