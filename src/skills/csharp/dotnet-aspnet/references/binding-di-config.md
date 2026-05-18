# Binding, DI & Configuration

## Model Binding & Validation

- Use binding source attributes explicitly: `[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromHeader]`
- With `[ApiController]`, complex types default to `[FromBody]`, simple types to `[FromRoute]`/`[FromQuery]`
- Validate with Data Annotations or FluentValidation:

```csharp
public record CreateOrderRequest(
    [Required] string CustomerId,
    [Required, MinLength(1)] List<OrderItemRequest> Items);
```

- For FluentValidation: register validators via `AddValidatorsFromAssemblyContaining<T>()` and use a validation filter or `IEndpointFilter`
- Return `ValidationProblemDetails` (automatic with `[ApiController]`) for validation failures

## Dependency Injection

- Register services in `Program.cs` or via extension methods
- Use appropriate lifetimes:
  - **Transient**: Stateless, lightweight services
  - **Scoped**: Per-request services (DbContext, unit of work)
  - **Singleton**: Thread-safe, shared state (caches, configuration)
- Use keyed services (.NET 8+) when multiple implementations of the same interface are needed:
  ```csharp
  builder.Services.AddKeyedScoped<IPaymentGateway, StripeGateway>("stripe");
  builder.Services.AddKeyedScoped<IPaymentGateway, PayPalGateway>("paypal");
  ```
- Prefer interface-based registration for testability
- Avoid service locator pattern — do not inject `IServiceProvider` into business logic

## Configuration

- Use the Options pattern for strongly-typed configuration:

```csharp
public class SmtpOptions
{
    public const string SectionName = "Smtp";
    public required string Host { get; init; }
    public int Port { get; init; } = 587;
    public required string Username { get; init; }
}

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

- Use `ValidateOnStart()` to catch configuration errors at startup, not at first use
- Use `appsettings.json` for defaults, `appsettings.{Environment}.json` for overrides
- Use User Secrets (`dotnet user-secrets`) for local development — never commit secrets
- Use environment variables or a vault for production secrets
- Inject `IOptions<T>` for static config, `IOptionsMonitor<T>` for reloadable config
