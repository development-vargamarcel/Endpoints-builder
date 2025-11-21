# Documentation and Examples Guidelines

## Purpose

This document establishes the principles and standards for maintaining documentation and examples in the Endpoints-builder library. Following these guidelines ensures the project remains maintainable, accessible, and focused.

## Core Principles

### 1. Minimalism

**Documentation should be concise and purposeful.**

- Only document what is necessary for users to understand and use the library
- Avoid redundant documentation across multiple files
- Remove outdated documentation promptly when features change
- Focus on "what" and "why" rather than obvious "how"

### 2. Single Source of Truth

**Each concept should be documented in exactly one place.**

- Avoid duplicating information across multiple documents
- Use references/links instead of copying content
- When information appears in multiple places, consolidate it

### 3. Examples Over Exposition

**Show, don't tell.**

- Prefer working code examples over lengthy explanations
- Each example should be complete and runnable
- Comments in examples should explain the "why", not the "what"

## Documentation Structure

### Primary Documentation

| File | Purpose | Update Frequency |
|------|---------|------------------|
| `README.md` | Project overview, quick start, basic usage | With major features |
| `docs/API.md` | Complete API reference for all functions | With every API change |
| `docs/SECURITY.md` | Security best practices and patterns | When security features change |
| `docs/PERFORMANCE_IMPROVEMENTS.md` | Performance optimization features | When performance features are added |
| `CHANGELOG.md` | Version history and breaking changes | With every release |
| `CONTRIBUTING.md` | Contribution guidelines | Rarely |

### Example Documentation

| File | Purpose |
|------|---------|
| `examples/README.md` | Example selection guide |
| Individual example files | Working code demonstrating features |

## Example Guidelines

### One Example Per Operation Type

**Keep exactly ONE comprehensive example for each major operation type:**

- **GET operations** → `AdvancedQueryingExample.vb`
- **POST/Batch operations** → `AdvancedBatchAndPerformanceExample.vb`
- **Complete multi-operation system** → `EnterpriseEndpointExample.vb`

### Example Quality Standards

Each example must:

1. **Be Complete**: Runnable without modifications (except connection strings)
2. **Be Advanced**: Showcase multiple features working together
3. **Be Flexible**: Demonstrate various configuration options
4. **Be Current**: Use the latest API signatures and best practices
5. **Be Commented**: Explain design decisions and why certain patterns are used

### Example Structure

```vb
' ===================================
' [EXAMPLE NAME]
' Purpose: Brief description
' Demonstrates: Feature 1, Feature 2, Feature 3
' Version: Library version this example targets
' ===================================

' SETUP: Database configuration
' [Setup code]

' FEATURE 1: [Feature name and why it matters]
' [Implementation]

' FEATURE 2: [Feature name and why it matters]
' [Implementation]

' USAGE EXAMPLE: How to call the endpoint
' [Usage code]
```

## What NOT to Document

### Avoid These Documentation Anti-Patterns

1. **Obvious Code Comments**
   ```vb
   ' Bad: Dim x As Integer = 5 ' Set x to 5
   ' Good: Dim maxRetries As Integer = 5 ' Retry up to 5 times to handle transient network errors
   ```

2. **Implementation Details Users Don't Need**
   - Internal helper functions
   - Private class members
   - Optimization techniques that don't affect usage

3. **Redundant Migration Guides**
   - After 2-3 versions, remove old migration documentation
   - Users should upgrade incrementally, not skip multiple major versions

4. **Performance Benchmarks That Become Stale**
   - Document performance characteristics, not specific numbers
   - "40-60% faster" becomes outdated quickly; "uses FOR JSON PATH for better performance" stays relevant

## Maintenance Rules

### When Adding a Feature

1. Update `docs/API.md` with new function signature and parameters
2. Add entry to `CHANGELOG.md`
3. Update or create ONE example demonstrating the feature
4. Update `README.md` only if it's a major feature

### When Deprecating a Feature

1. Mark as deprecated in `docs/API.md`
2. Add to `CHANGELOG.md` with migration path
3. Remove from examples or replace with current approach

### When Removing a Feature

1. Remove from `docs/API.md`
2. Document in `CHANGELOG.md` under "Breaking Changes"
3. Remove all examples using that feature
4. Update `README.md` if it was prominently featured

### Documentation Review Triggers

Review and clean up documentation when:

- Adding a new major feature (v2.0, v3.0, etc.)
- Three or more minor versions have passed
- User feedback indicates confusion
- Examples fail to run with current API

## Documentation Style

### API Documentation

Use this format in `docs/API.md`:

```markdown
### FunctionName

**Purpose:** What this function does and when to use it

**Parameters:**
- `paramName` (Type, optional/required): Description and valid values
- `paramName2` (Type, optional): Description and default value

**Returns:** Return type and what it contains

**Example:**
```vb
[Code example]
```

**Notes:**
- Important consideration 1
- Important consideration 2
```

### README Format

Keep README sections short:

1. **Title and Description** (2-3 sentences)
2. **Quick Start** (5-10 lines of code)
3. **Features** (bullet list, max 8 items)
4. **Basic Usage** (1-2 simple examples)
5. **Documentation Links** (point to docs/ folder)
6. **License** (one line)

## Version Guidelines

### Documenting Version Requirements

- Always specify minimum library version in examples
- Use version ranges for compatibility: "v2.1+" means v2.1 or higher
- Remove references to versions older than 2 major versions

### Changelog Format

```markdown
## [Version Number] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes to existing features

### Deprecated
- Features marked for removal

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
```

## Questions to Ask Before Adding Documentation

1. **Is this already documented elsewhere?** → Consolidate instead of duplicating
2. **Will users actually need this?** → If not, skip it
3. **Can an example show this better?** → Prefer examples over text
4. **Will this stay current?** → Avoid documentation that becomes outdated quickly
5. **Does this help or overwhelm?** → When in doubt, leave it out

## Example Pruning Strategy

### Review Examples Quarterly

Ask for each example:

1. **Does it demonstrate unique functionality?** If another example covers it, remove it
2. **Is it using current API signatures?** If not, update or remove it
3. **Is it more advanced than other examples?** If not, remove it
4. **Would a newcomer learn something valuable?** If not, remove it

### Keep Only The Best

- If two examples demonstrate similar features, keep the more comprehensive one
- If an example is only partially complete, either finish it or remove it
- If an example requires significant setup, ensure the value justifies the complexity

## Conclusion

**Documentation is a liability, not an asset.** Every piece of documentation must:

- Be accurate
- Stay current
- Provide value
- Be maintained

When documentation fails any of these criteria, remove it. Better to have less documentation that's accurate and helpful than more documentation that's outdated and confusing.

## Enforcement

- All pull requests adding features must update relevant documentation
- All pull requests must not increase documentation beyond these guidelines
- Quarterly documentation reviews should remove outdated content
- Examples must be tested with each release
