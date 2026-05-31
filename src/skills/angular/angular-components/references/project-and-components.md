# Project Structure & Components

## Project Structure (Feature Folders)

Organize by feature, not by technical layer. Each feature folder contains its components, models, services, and routes together.

```
src/app/
  features/
    orders/
      orders.routes.ts          # lazy route config for the feature
      order-list.component.ts    # container (smart) component
      order-card.component.ts    # presentational (dumb) component
      order.models.ts
      order.service.ts
    products/
      ...
  app.config.ts                  # root providers (provideRouter, provideHttpClient, …)
  app.routes.ts
  main.ts
```

## Container vs Presentational Components

Both are valid roles; keep them distinct.

- **Container (smart)** — injects services, owns state, handles navigation and side effects. Few of them.
- **Presentational (dumb)** — `@Input()` data in, `@Output()` events out, no service injection, easy to test and reuse. Many of them.

### Container Pattern

```typescript
@Component({
  selector: 'app-order-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [OrderCardComponent, AsyncPipe],
  template: `
    @for (order of orders(); track order.id) {
      <app-order-card [order]="order" (select)="open($event)" />
    } @empty {
      <p>No orders.</p>
    }
  `,
})
export class OrderListComponent {
  private readonly service = inject(OrderService);
  private readonly router = inject(Router);
  readonly orders = toSignal(this.service.list(), { initialValue: [] as Order[] });
  open(id: number) { this.router.navigate(['/orders', id]); }
}
```

### Presentational Pattern

```typescript
@Component({
  selector: 'app-order-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<button (click)="select.emit(order().id)">{{ order().title }}</button>`,
})
export class OrderCardComponent {
  readonly order = input.required<Order>();
  readonly select = output<number>();
}
```

## Routing & Lazy Loading

Routes are the Angular analogue of API endpoints. Lazy-load feature routes to keep the initial bundle small; scope feature providers to the route.

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES),
  },
];

// features/orders/orders.routes.ts
export const ORDERS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./order-list.component').then((m) => m.OrderListComponent) },
  {
    path: ':id',
    loadComponent: () => import('./order-detail.component').then((m) => m.OrderDetailComponent),
    resolve: { order: orderResolver },
  },
];
```

Bind route params with component input binding (`withComponentInputBinding()`): an `:id` route param flows into an `id = input<string>()` on the component.

## Resolvers

Use a functional resolver to fetch required data before activation, so the view never renders in a half-loaded state.

```typescript
export const orderResolver: ResolveFn<Order> = (route) =>
  inject(OrderService).get(Number(route.paramMap.get('id')));
```

## Related Skills

- **[angular-fundamentals](../../angular-fundamentals/SKILL.md)** — DI and the `provide` pattern for component/route services
- **[angular-state](../../angular-state/SKILL.md)** — Data and state behind components
- **[angular-tester](../../angular-tester/SKILL.md)** — Testing components and routed views
