---
name: dotnet-aspnet
description: Applies ASP.NET Core best practices for building Web and REST APIs. Use when creating controllers, minimal APIs, configuring middleware, routing, model binding, validation, dependency injection, authentication, authorization, error handling with ProblemDetails, OpenAPI/Swagger, health checks, CORS, rate limiting, or structuring an ASP.NET Core project.
---

# ASP.NET Core Web API Best Practices

## When to Use

- Creating new controllers, minimal API endpoints, or feature folders in an ASP.NET Core project
- Configuring the middleware pipeline, routing, or model binding
- Adding authentication, authorization policies, or `[Authorize]`-based access control
- Implementing error handling with `ProblemDetails` (RFC 9457) or `IExceptionHandler`
- Wiring up OpenAPI/Swagger, health checks, CORS, rate limiting, output caching, or response compression
- Reviewing or restructuring an existing ASP.NET Core project to align with best practices

## Core Principles

- Use feature folders, not layer folders. Keep `Program.cs` lean — extract registrations into extension methods.
- Always use `[ApiController]` on MVC controllers; prefer `TypedResults` in minimal APIs.
- Accept `CancellationToken` on every async endpoint.
- Validate configuration at startup with `ValidateOnStart()`.
- Map domain exceptions to ProblemDetails — never leak internal exception messages.
- Middleware order matters: exception handler → status code pages → HSTS/HTTPS → CORS → auth → rate limiter → endpoints.

## Reference Index

Detailed patterns and code samples live in `references/`:

- **[project-and-endpoints.md](references/project-and-endpoints.md)** — Project structure, Controllers vs Minimal APIs, routing, API conventions, response types (`TypedResults`, `[ProducesResponseType]`)
- **[binding-di-config.md](references/binding-di-config.md)** — Model binding, validation (Data Annotations, FluentValidation), DI lifetimes, keyed services, Options pattern, User Secrets
- **[middleware.md](references/middleware.md)** — Pipeline order, custom middleware with primary constructors
- **[auth.md](references/auth.md)** — JWT Bearer, authorization policies, `IAuthorizationHandler`, `RequireAuthorization`
- **[error-handling.md](references/error-handling.md)** — ProblemDetails, `IExceptionHandler` (.NET 8+), `UseStatusCodePages`
- **[openapi-and-cross-cutting.md](references/openapi-and-cross-cutting.md)** — OpenAPI/Swagger, XML doc generation, health checks (liveness/readiness), CORS, rate limiting, output caching, response compression

## Related Skills

- **[ef-core](../ef-core/SKILL.md)** — Data access with Entity Framework Core
- **[dotnet-tester](../dotnet-tester/SKILL.md)** — Unit and integration testing for endpoints
- **[csharp-docs](../csharp-docs/SKILL.md)** — XML documentation comments (feed OpenAPI)
- **[nuget-manager](../nuget-manager/SKILL.md)** — Invoked for adding health-check, resilience, and middleware packages
- **[dotnet-sdk-builder](../dotnet-sdk-builder/SKILL.md)** — Generates typed HTTP clients for consuming these APIs
- **[dotnet-reviewer](../dotnet-reviewer/SKILL.md)** — Reviews ASP.NET Core code for security, performance, and architecture issues
