# Modern Angular / TypeScript Patterns

Idioms for modern, null-safe, expressive Angular code.

## Standalone APIs

Prefer standalone components, directives, and pipes over NgModules. Bootstrap with `bootstrapApplication` and compose providers with `provideХxx()` functions.

```typescript
@Component({
  selector: 'app-order',
  imports: [RouterLink, CurrencyPipe],
  template: `@if (order(); as o) { <span>{{ o.total | currency }}</span> }`,
})
export class OrderComponent {}
```

## `inject()`

```typescript
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  process(id: number) { return this.http.post(`/api/orders/${id}/process`, {}); }
}
```

## Signals

Use signals for synchronous reactive state; derive with `computed`, react with `effect`.

```typescript
export class CartComponent {
  readonly items = signal<CartItem[]>([]);
  readonly total = computed(() => this.items().reduce((s, i) => s + i.price, 0));
}
```

## Strict Null Safety

Enable `strict` (incl. `strictNullChecks`) in `tsconfig.json`. Model "maybe absent" with `T | null`/`T | undefined` and narrow before use.

```typescript
findUser(id: number): User | null { /* may return null */ }
getUser(id: number): User {
  return this.findUser(id) ?? (() => { throw new UserNotFoundError(id); })();
}
```

## Discriminated Unions & Pattern Matching

Discriminated unions narrowed by `switch` give you exhaustive, type-safe pattern matching in TypeScript.

```typescript
type Discount =
  | { tier: 'gold' }
  | { tier: 'silver' }
  | { tier: 'standard'; orderCount: number };

function rate(d: Discount): number {
  switch (d.tier) {
    case 'gold': return 0.2;
    case 'silver': return 0.1;
    case 'standard': return d.orderCount > 10 ? 0.05 : 0;
  }
}
```

## Teardown (the `CancellationToken` analogue)

Angular cancels async work by **unsubscribing**. Tie subscription lifetime to the component/service with `takeUntilDestroyed()`, and use `DestroyRef` for imperative cleanup. Outstanding `HttpClient` requests are aborted automatically when you unsubscribe.

```typescript
export class FeedComponent {
  private readonly feed = inject(FeedService);
  readonly items = signal<Item[]>([]);

  constructor() {
    this.feed.stream()
      .pipe(takeUntilDestroyed())          // auto-unsubscribe on destroy
      .subscribe((items) => this.items.set(items));
  }
}
```

For non-RxJS async (e.g. `fetch`), pass an `AbortSignal` and abort it from `DestroyRef.onDestroy(...)`.

## Related Skills

- **[angular-components](../../angular-components/SKILL.md)** — Applies these idioms in components, guards, and interceptors
- **[angular-library-builder](../../angular-library-builder/SKILL.md)** — Emits library code using these modern patterns
