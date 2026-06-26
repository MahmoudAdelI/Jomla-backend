# Jomla — Frontend Context
> Paste this file when working on Angular components, routing, state, UI flows, or API integration.

---

## Tech Stack
- **Angular** (latest, standalone components architecture)
- **Signals** for reactive state (prefer over RxJS where possible for local state)
- **TypeScript** strict mode
- **TailwindCSS** (assumed — confirm if different)

---

## Dual-Theme UI Design

Jomla has two completely distinct visual identities based on user role:

### Buyer Interface — Light Theme
- Clean, marketplace-like feel
- Product browsing, deal tracking, group joining
- Warm, approachable colors

### Seller Interface — Dark Terminal Theme
- Dark background, terminal/dashboard aesthetic
- Data-heavy: offer management, batch tracking, notifications
- Professional, dense information display

Theme is determined at login by `user.role` and persists for the session.

---

## Key Buyer-Facing Pages/Features [PLANNED]
- Browse seller offers (paginated, filterable by category)
- Offer detail page + join batch flow
- Active batches tracker (real-time fill progress via SignalR)
- Create wish hub (group request)
- Browse wish hubs + join
- View seller offers on a wish hub (with current_discount_percent — NOT max)
- Accept/reject offer vote
- Orders history

---

## Key Seller-Facing Pages/Features [PLANNED]
- Dashboard: active offers, batch status, completion stats
- Create/edit offer
- Wish hub notifications feed
- Respond to wish hub (create group_request_offer)
- Negotiation settings per offer (set max_discount_percent — visible only to seller)
- Order management

---

## Angular Architecture Patterns
- Standalone components (no NgModules)
- Feature-based folder structure:
  ```
  src/app/
  ├── core/           # Auth, interceptors, guards, global services
  ├── shared/         # Reusable UI components
  ├── features/
  │   ├── buyer/      # All buyer-facing features
  │   └── seller/     # All seller-facing features
  └── app.routes.ts
  ```
- HTTP interceptor for JWT attachment + refresh token logic
- Route guards for role-based access (buyer vs seller routes)
- SignalR service in core, injected into feature components

---

## Real-time (SignalR on frontend)
- Connect to hub on login
- Join SignalR group for active batches/wish hubs the user is in
- Update UI reactively on:
  - `batch_quantity_updated` → progress bar update
  - `wish_hub_quantity_updated` → participant count update
  - `new_offer_on_wish_hub` → notify buyers in hub
  - `seller_notified` → notification badge for seller

---

## API Contract Notes
- All monetary values: `decimal` (not float)
- `max_discount_percent`: never present in buyer-facing responses (backend enforces)
- Offer/wish hub listings only show `moderation_status === approved` items
- `current_discount_percent` is the only discount value buyers ever see on group request offers

---

## Status: [PLANNED — UI design concept done, implementation not started]
