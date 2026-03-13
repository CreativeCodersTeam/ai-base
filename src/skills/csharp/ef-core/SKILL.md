---
name: ef-core
description: Work with Entity Framework Core for database operations in C# projects
---

# Entity Framework Core Skill

**Scope: C# Projects using Entity Framework Core**

## Best Practices

### 1. DbContext Configuration
- Register DbContext with dependency injection
- Use connection strings from configuration
- Configure entities in OnModelCreating
- Keep DbContext focused and separated by bounded context

### 2. Entity Configuration
- Use Fluent API for complex configurations
- Define relationships explicitly
- Configure indexes for frequently queried columns
- Use value objects for complex types

### 3. Migrations
- Create migrations for schema changes
- Review generated migrations before applying
- Use meaningful migration names
- Never modify applied migrations

### 4. Query Optimization
- Use AsNoTracking() for read-only queries
- Avoid N+1 queries with Include() and ThenInclude()
- Use projection (Select) to load only needed data
- Consider compiled queries for frequently used queries

### 5. Repository Pattern
- Consider using repository pattern for abstraction
- Keep repositories focused on data access
- Use specification pattern for complex queries
- Inject IUnitOfWork for transaction management

## Common Operations

### Querying Data
```csharp
// Single entity
var entity = await context.Entities
    .Include(e => e.RelatedEntity)
    .FirstOrDefaultAsync(e => e.Id == id);

// List with filtering
var entities = await context.Entities
    .Where(e => e.IsActive)
    .OrderBy(e => e.Name)
    .ToListAsync();
```

### Adding/Updating/Deleting
```csharp
// Add
context.Entities.Add(newEntity);
await context.SaveChangesAsync();

// Update
entity.Property = newValue;
await context.SaveChangesAsync();

// Delete
context.Entities.Remove(entity);
await context.SaveChangesAsync();
```

### Transactions
```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```
