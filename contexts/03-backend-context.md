# Jomla — Backend Architecture Context
> Paste this file when working on ASP.NET Core, EF Core, API design, CQRS handlers, SignalR, or background jobs.

---

## Architecture: Clean Architecture + CQRS

```
Jomla.sln
├── Jomla.Domain          # Entities, enums, domain interfaces
├── Jomla.Application     # CQRS commands/queries (MediatR), DTOs, validators, interfaces
├── Jomla.Infrastructure  # EF Core DbContext, repositories, external services (Stripe, AI agents, Hangfire)
└── Jomla.API             # ASP.NET Core controllers, SignalR hubs, middleware
```

### Key patterns in use:
- **MediatR** for CQRS — every use case is a Command or Query
- **FluentValidation** — validators live in Application layer alongside their command/query
- **Global exception handler** — ProblemDetails format (RFC 7807)
- **Repository + Unit of Work** pattern via EF Core
- **Result pattern** (or exceptions) — TBD during implementation

---

## Authentication
- ASP.NET Core Identity + JWT
- HttpOnly cookie for refresh token (security — not localStorage)
- Refresh token revocation logic

---

## Real-time (SignalR)
Used for:
- Batch fill progress (buyer watching a seller offer batch fill up)
- Wish hub quantity updates (buyers see others joining)
- New offer notifications to buyers
- Seller notifications when wish hub threshold is hit

Hub design: group-based (batch group, wish hub group) using `ConcurrentDictionary` for thread safety.

---

## Background Jobs (Hangfire)
- **Offer expiry check**: runs on schedule, checks `seller_offers.expires_at`
  - If last open batch `current_quantity >= expiry_fallback_threshold` → complete batch
  - Else → fail batch, mark offer as `expired`
- **Wish hub close timer**: triggered when `group_requests.inactive_since` is set
  - After 24h with no new participant → status → `closed`
- **AI negotiation scheduler**: periodic check on open `group_request_offers` to trigger negotiation agent

---

## Payments (Stripe)
- PaymentIntent created when buyer commits to pay
- `orders.stripe_payment_intent_id` stores the PI id
- Webhook handler updates `orders.status` and `orders.paid_at` on confirmation

---

## AI Agents Integration

### 1. Content Moderation Agent
- Triggered: after `seller_offers` or `group_requests` insert
- Input: title, description (or item_title)
- Output: `moderation_status` (approved/flagged) + optional `moderation_reason`
- Implementation: background job or async post-save hook

### 2. Categorization Agent
- Triggered: on `group_requests` creation
- Input: `item_title`
- Output: `category_id` (from existing categories tree)
- Note: buyer selects from AI suggestion or overrides

### 3. Negotiation Agent
- Triggered: when buyers respond to a `group_request_offer` or on schedule
- Input: `current_discount_percent`, `max_discount_percent`, buyer accept/reject ratio, `avg_market_price`
- Output: new `current_discount_percent` (never exceeds `max_discount_percent`)
- Side effect: inserts row into `negotiation_log`
- Security rule: `max_discount_percent` MUST NEVER be returned in any buyer-facing API response

---

## Critical API Security Rules
- `group_request_offers.max_discount_percent` → **never serialize to buyer responses**
- Role-based authorization: buyers and sellers have distinct endpoints
- Sellers can only modify their own offers/batches
- Buyers can only see/join active + approved offers and wish hubs

---

## Status: [PLANNED — not yet implemented]
