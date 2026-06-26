# Jomla — Project Overview Context

> Paste this file at the start of any high-level, architecture, or cross-cutting conversation.

---

## What is Jomla?

Jomla is a group-buying marketplace that connects buyers and sellers for bulk pricing deals.

- Buyers group together to reach quantity targets, unlocking discounted bulk prices from sellers.
- Sellers post offers with sequential batch logic, or respond to buyer demand signals (group requests).
- Built as an MVP graduation project. No shipping management. No stock management. Mock Stripe payments.

---

## Two Core Flows

### 1. Seller-initiated flow

1. Seller creates an offer with unit price, discount %, hub target quantity per batch, total quantity available, optional expiry fallback threshold, and optional variant attributes (JSON key-value pairs e.g. `{"color":"red","size":"large"}`).
2. Offer goes through AI content moderation before becoming visible. Starts as `moderation_status = pending`.
3. First batch opens automatically on offer approval.
4. Buyer browses offers on Discover page and joins a batch by committing a quantity.
5. On join, Stripe payment intent is created immediately with `capture_method = manual`. Payment is held, not captured.
6. Buyer can leave anytime before batch completes — hold is cancelled, participant status flips to `left`, `current_quantity` decremented.
7. When batch `current_quantity` reaches `target_quantity` — all holds are captured simultaneously, batch status flips to `completed`, orders created for all participants.
8. Next batch opens immediately after completion. There is always exactly one open batch per offer at any time.
9. `total_quantity_available` on the offer is decremented by the completed batch quantity. When it hits 0 the offer auto-sets to `inactive`.
10. Last batch target may be smaller than `hub_target_quantity` if remaining stock is less — `seller_batches.target_quantity` is set independently at batch creation to handle this.
11. On offer expiry: Hangfire checks the last open batch. If `current_quantity >= expiry_fallback_threshold` → capture held payments, complete the batch. If not → cancel all holds, mark batch `failed`, mark offer `expired`. If `expiry_fallback_threshold` is NULL → expiry always cancels.

### 2. Buyer-initiated flow (group requests)

1. Buyer creates a group request with a free-text item title and quantity. No price expectation — group requests are pure demand signals.
2. AI categorization agent resolves `category_id` from `item_title`. Buyer can override if confidence is low.
3. Group request goes through AI content moderation before becoming visible.
4. `current_quantity` starts at the initiator's committed quantity, not 0. Initiator's participant row is created in the same transaction as the hub.
5. Other buyers join, incrementing `current_quantity`.
6. On every join, Hangfire runs a notification matching query: find sellers in `seller_category_preferences` where `category_id` matches AND `min_quantity <= current_quantity` AND no existing `group_request_alerts` row for this seller + request pair. Matched sellers get a notification.
7. Notified sellers browse the group request and can place a group request offer with: unit price, optional min unit price (AI negotiation floor, private), quantity available, optional min fallback quantity, variant attributes, and expiry.
8. Buyers in the hub see incoming offers in a sidebar ordered by price ascending. Each offer shows unit price, quantity available, acceptance progress, and expiry countdown.
9. Buyer accepts an offer → immediately goes through Stripe payment intent with `capture_method = manual`. Hold placed. Only on successful hold is the buyer counted as accepted and their quantity added to the offer's accepted count. No ghost acceptances.
10. Buyer can cancel acceptance before the offer fills → hold cancelled, accepted count decremented, offer may revert from `accepted` back to `open`.
11. When accepted quantity reaches `quantity_available` → all holds captured simultaneously, orders created, offer status flips to `accepted`.
12. AI negotiation agent may lower `current_unit_price` toward `min_unit_price` based on buyer response patterns and RAG context (see "Negotiation pricing & RAG" below). Each move creates a new `group_request_offers` row and marks the old one `countered`. Full trail logged in `negotiation_log`.
13. If a buyer leaves and `current_quantity` hits 0 → `inactive_since` stamped, status flips to `inactive`. Hub remains browsable. A buyer joining reactivates it (clears `inactive_since`, flips to `active`).
14. If no buyer joins within 24h of `inactive_since` → Hangfire flips status to `closed`. Closed hubs are archived and hidden from Discover.
15. On group request offer expiry: if accepted quantity >= `min_fallback_quantity` → capture held payments, close offer as accepted. If not → cancel all holds, flip offer to `expired`.

---

## Negotiation pricing & RAG (Qdrant)

The negotiation agent's job is to pick the next `current_unit_price` for a `group_request_offers` row once buyers start rejecting it, moving toward the private `min_unit_price` floor. Qdrant's main role in Jomla is to ground this decision in similar historical negotiations — this is the primary use of the vector DB (see the Database Schema Context for full collection details).

**Tracking negotiation rounds:**

- `group_request_offers.round_number` (int, default 1) — which attempt this row is. A fresh seller offer starts at 1. An AI counteroffer's `round_number = previous_row.round_number + 1`.
- `group_request_offers.parent_offer_id` (nullable, self-referencing FK) — links a counteroffer back to the row it superseded. NULL starts a fresh negotiation thread (e.g. a brand-new offer posted after a prior thread expired). Lets the full back-and-forth be reconstructed later for reporting, though the negotiation agent itself only needs `round_number`.
- `unit_price` and `min_unit_price` are copied forward onto every row in a thread, so any single round-row is self-sufficient for grounding/payload purposes without joining back to round 1.

**RAG retrieval flow:**

1. When buyers reject a round, the agent reads that round's `category_id`, `item_title`-derived description, `current_quantity`, `current_unit_price`, `min_unit_price`, and computes `rejection_rate_at_round` from `buyer_offer_responses`.
2. It queries the Qdrant `negotiation_rounds` collection (filtered by `category_id`) for similar past rounds and their resulting price-step percentages.
3. The LLM uses these as grounding to recommend a `new_price`.
4. The agent writes a new `group_request_offers` row (`round_number + 1`, `parent_offer_id` = current row, `current_unit_price` = new_price), flips the old row to `countered`, and appends a `negotiation_log` entry.

**Fallback (no/insufficient retrieval results):** a fixed step computed once at round 1 — `step = 0.25 × (unit_price - min_unit_price)` — applied as `new_price = max(current_unit_price - step, min_unit_price)` for each subsequent move. Predictable and finite (at most 4 moves to the floor).

**Seeding:** the `negotiation_rounds` collection is seeded with synthetic data covering all categories ahead of launch, so the fallback is mainly a safety net for novel category/price combinations.

---

## Payment model

Both flows use the same Stripe pattern: **hold at commitment, capture at completion.**

- Seller batch path: hold placed at batch join, captured when batch fills to target.
- Group request path: hold placed at offer acceptance, captured when offer fills to quantity_available.
- Leaving before completion always cancels the hold — never a refund of a captured charge.
- Abandoned checkouts (buyer starts Stripe flow and exits) are not handled for MVP — no payment intent is created until the flow completes successfully, so there is nothing to clean up.
- `stripe_payment_intent_id` lives on `batch_participants` for the seller batch path and on `buyer_offer_responses` for the group request path.

---

## AI agents

| Agent              | Trigger                                            | What it does                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| ------------------ | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Content moderation | New seller offer or group request created          | Reviews title and description for inappropriate or illegal content. Flips `moderation_status` to `approved` or `flagged`. Stores reason in `moderation_reason` if flagged                                                                                                                                                                                                                                                                               |
| Categorization     | Buyer creates a group request                      | Maps free-text `item_title` to correct `category_id` from the categories tree. Returns confidence signal — if low, surfaces top 2 options to buyer as confirmation chips                                                                                                                                                                                                                                                                                |
| Negotiation        | Buyer rejection threshold on a group request offer | Lowers `current_unit_price` toward `min_unit_price`. Retrieves similar past negotiation rounds from Qdrant (`negotiation_rounds` collection, filtered by category) as RAG context for the price step; falls back to a fixed 25%-of-range step if retrieval is insufficient. Creates new offer row (`round_number + 1`, `parent_offer_id` set), marks old as `countered`. Logs every move in `negotiation_log`. Never exposes `min_unit_price` to buyers |

---

## Notifications

Two separate concerns — do not conflate them.

**`group_request_alerts` table** (renamed from `seller_notifications`): a workflow table specifically for matching sellers to group requests. Has its own `responded | ignored` state that drives business logic. Not a UI notification table.

**`notifications` table**: general in-app notification display for both buyers and sellers. Stores `user_id`, `type`, `title`, `body`, `entity_id`, `entity_type` (soft reference for frontend navigation), `is_read`. Populated by Hangfire after business events complete.

Critical notification events:

- Buyer: offer placed on a group request you're in, batch you joined completed, group request offer you accepted filled
- Seller: group request matched your category preferences (via `group_request_alerts`), your batch completed, your offer moderation result

## Frontend receives notifications in real time via SignalR. When Hangfire completes a business event and inserts a row into the notifications table, it triggers a SignalR push to the affected user's connection. The notification bell count updates instantly and the toaster fires without any client-side polling.

## Key business rules

- One offer → many sequential batches. Always exactly one open batch per offer at any time.
- `seller_batches.target_quantity` is set independently at creation — not always equal to `offer.hub_target_quantity`. Last batch may be reduced to remaining stock.
- Completion check always reads from `seller_batches.target_quantity`, never from the offer.
- `total_quantity_available` decrements on batch completion. When 0 → offer auto-sets to `inactive`.
- `expiry_fallback_threshold` on seller offers: NULL = expiry always cancels. Set = completes if met.
- `min_fallback_quantity` on group request offers: same concept for the group request path.
- `min_unit_price` is private — never returned in any API response to buyers.
- `current_quantity` on group requests starts at initiator quantity, never 0.
- `inactive_since` is distinct from `last_activity_at` — only `inactive_since` drives the 24h close logic.
- Orders have exactly one of `batch_id` OR `offer_id` populated — never both, never neither. Enforced as CHECK constraint.
- Buyer join validation: if `requested_quantity > (batch.target_quantity - batch.current_quantity)` → reject and return remaining slots. No cross-batch splitting.
- Variant support: `variant_attributes` is a JSON object on both `seller_offers` and `group_request_offers`. Same offer `title` groups variants — no separate product table.
- No `avg_market_price` anywhere — unvalidatable user input, dropped entirely.
- No `min_quantity_per_buyer` — buyers choose quantity freely.
- `group_request_offers.round_number` tracks negotiation attempt number (1 = fresh offer, increments on each AI counteroffer). `parent_offer_id` (nullable, self-referencing) links a counteroffer to the row it superseded; NULL starts a fresh thread. `unit_price`/`min_unit_price` are copied forward onto every row of a thread.
- Pure association/junction tables (`seller_category_preferences`, `batch_participants`, `group_request_participants`, `group_request_alerts`, `buyer_offer_responses`) use composite primary keys on their natural key pair instead of a surrogate `id`.

---

## Background jobs (Hangfire)

| Trigger                                                                | Action                                                                                                                                                                        |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `group_request_participants` leave                                     | Recalculate `current_quantity`. If 0 → stamp `inactive_since`, flip to `inactive`                                                                                             |
| `group_request_participants` join on inactive hub                      | Clear `inactive_since`, flip to `active`                                                                                                                                      |
| `group_request_participants` join (any)                                | Run seller notification matching query against `seller_category_preferences`                                                                                                  |
| `group_requests.inactive_since` + 24h elapsed                          | Flip status to `closed`                                                                                                                                                       |
| `seller_offers.expires_at`                                             | Check last open batch. If `current_quantity >= expiry_fallback_threshold` → capture holds, complete batch. If not → cancel holds, fail batch. Mark offer `expired` either way |
| `group_request_offers.expires_at`                                      | If accepted quantity >= `min_fallback_quantity` → capture holds. If not → cancel holds. Flip offer to `expired`                                                               |
| Batch `current_quantity` reaches `target_quantity`                     | Capture all held payment intents, flip batch to `completed`, open next batch                                                                                                  |
| Offer accepted quantity reaches `quantity_available`                   | Capture all held payment intents, flip offer to `accepted`, create orders                                                                                                     |
| Negotiation round reaches terminal status (countered/accepted/expired) | Compute `rejection_rate_at_round` and (for countered rounds) `discount_step_pct`/`next_price`, write a point to Qdrant `negotiation_rounds` collection                        |

---

## Tech stack

- **Backend**: ASP.NET Core, C#, EF Core, SQL Server, Identity, JWT auth
- **Architecture**: Clean Architecture, CQRS with MediatR, FluentValidation, global exception handling via ProblemDetails, AutoMapper, Swagger
- **Payments**: Stripe — PaymentIntent with `capture_method = manual`. `stripe_payment_intent_id` on `batch_participants` and `buyer_offer_responses`
- **Background jobs**: Hangfire
- **AI**: Semantic Kernel or direct LLM API for the 3 agents
- **Vector DB**: Qdrant — `negotiation_rounds` collection, one point per `group_request_offers` round (countered/accepted/expired), used as RAG context to ground the negotiation agent's price-step recommendations. Primary purpose of Qdrant in this project; offer/group-request similarity search is a deferred secondary use case for a separate collection later.
- **Real-time**: SignalR for batch fill progress and offer sidebar updates
- **Frontend**: Angular standalone components with signals
- **Notification delivery**: SignalR — server pushes to the connected client when a notification row is created. No polling. Reuses the same SignalR infrastructure already in place for batch fill progress and offer sidebar updates.

---

## Project status

- [DONE] DB schema finalized (including composite PKs on association tables, negotiation round tracking via `round_number`/`parent_offer_id`)
- [DONE] UI/UX concept and feature scope defined
- [DONE] AI agent responsibilities defined
- [DONE] Payment model finalized (hold at commitment, capture at completion)
- [DONE] Notification architecture defined
- [DONE] Qdrant negotiation RAG design (`negotiation_rounds` collection, retrieval flow, fixed-step fallback)
- [NEXT] EF Core models and migrations
- [NEXT] Qdrant collection seeding strategy (synthetic data generation across categories)
