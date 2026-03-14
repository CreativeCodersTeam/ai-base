# C# Specific Instructions

**Scope: C# Projects**

## Code Style
- Follow Microsoft C# Coding Conventions
- Use PascalCase for class names and method names
- Use camelCase for local variables and parameters
- Use async/await for asynchronous operations

## Framework Specifics
- Use nullable reference types where available
- Prefer LINQ for collection operations
- Use dependency injection for loose coupling
- Follow ASP.NET Core best practices for web applications

## Error Handling
- Use exceptions for exceptional cases only
- Catch specific exceptions, not general Exception
- Use try-catch-finally or using statements for resource cleanup

## Testing
- Use xUnit, NUnit, or MSTest for unit testing
- Use Moq or NSubstitute for mocking
- Follow AAA pattern (Arrange, Act, Assert)
