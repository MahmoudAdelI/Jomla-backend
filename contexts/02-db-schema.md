# Jomla — Database Schema Context

> Paste this file whenever asking about DB queries, EF Core models, migrations, indexes, or anything data-related.

---

## Database

SQL Server. EF Core with ASP.NET Core Identity (`IdentityUser<Guid>`).

| Generic        | SQL Server                                            |
| -------------- | ----------------------------------------------------- |
| uuid           | uniqueidentifier                                      |
| newid()        | default PK strategy                                   |
| timestamp      | datetime2                                             |
| text / varchar | nvarchar(255) or nvarchar(max)                        |
| boolean        | bit                                                   |
| arrays / JSON  | nvarchar(max) storing JSON string                     |
| enums          | nvarchar(20) stored as string via EF value converters |

Enum values enforced at application layer via EF Core value converters, not DB constraints.

---

## Design decisions & reasons

**No role column on AppUser**
Role managed via ASP.NET Identity role system, not a column. AppUser extends `IdentityUser<Guid>`. Name split into `FirstName` + `LastName`. Optional `ImageUrl` added.

**RefreshToken owned by AppUser**
`RefreshToken` is not a standalone table entity — it's owned/navigated from `AppUser`. Has computed properties `IsExpired` and `IsActive` in domain, not persisted columns.

**SupplierCategoryPreference has composite PK**
No surrogate id. `(SupplierId, CategoryId)` is the PK. `IsActive` column dropped — if a seller wants to stop receiving alerts for a category they delete the preference row instead.

**SupplierOffer.ExpiresAt is nullable**
A seller can create an offer without an expiry date. Hangfire expiry job only fires if `ExpiresAt` is not null.

**SupplierOffer.MinFallbackQuantity replaces expiry_fallback_threshold**
Renamed for consistency with the same concept on `GroupRequestOffer`. Minimum units in the last open batch to honor the deal on expiry. NULL means expiry always cancels.

**SupplierOffer.BatchTargetQuantity replaces hub_target_quantity**
Cleaner naming — directly describes what it is.

**SupplierBatch.TargetQuantity is independent**
Copied from `SupplierOffer.BatchTargetQuantity` at creation but may be smaller for the last batch when remaining stock is less than the full target. Completion check always reads from `SupplierBatch.TargetQuantity`.

**Pay-on-join for BatchParticipant**
Stripe payment intent created at join with `capture_method=manual`. `StripePaymentIntentId` is non-nullable on `BatchParticipant` — every active participant has a hold. Captured when batch completes, cancelled when buyer leaves. `BatchParticipantStatus` is `Active | Left` only — `Paid` dropped because `Active` already implies a held payment and completion is tracked on the batch itself.

**Pay-on-accept for BuyerOfferResponse**
Stripe payment intent created at offer acceptance. `StripePaymentIntentId` is nullable on `BuyerOfferResponse` — null for `Rejected` responses since rejected buyers never touch Stripe. Captured when offer fills to `QuantityAvailable`, cancelled on `Cancelled` response. `GroupRequestParticipantStatus` is `Active | Left` only — payment state lives entirely in `BuyerOfferResponse`.

**No stripe_payment_intent_id on Order**
Order is created after capture confirms. It is a record of a completed payment, not the payment instrument. The intent lives on `BatchParticipant` and `BuyerOfferResponse` respectively.

**GroupRequestOffer self-referencing tree for negotiation**
Added `ParentId` FK self-reference, `RoundNumber`, and `Parent`/`Children` navigation properties. When the AI agent makes a counteroffer it creates a new `GroupRequestOffer` row as a child of the previous one, incrementing `RoundNumber`. This replaces the "mark old as countered" flat approach with a proper negotiation round tree. `GroupRequestOfferStatus.Countered` still applies to the parent row.

**GroupRequest has no avg_market_price**
Dropped — unvalidatable user input. Group requests are pure demand signals. Sellers set their own prices.

**GroupRequestParticipant has no payment concern**
Joining a group request is free. Payment only happens when a buyer accepts a `GroupRequestOffer`. `GroupRequestParticipantStatus` is `Active | Left` only.

**SupplierCategoryPreference has no IsActive**
Dropped. Sellers delete the preference row to stop receiving alerts rather than toggling a flag.

**Order has exactly one of BatchId or OfferId**
Never both, never neither. Enforced at application service layer and as a CHECK constraint in migrations.

**NotificationType enum simplified**
Your implementation uses: `BatchCompleted | GroupRequestOfferPlaced | GroupRequestOfferFilled | GroupRequestMatched | OfferApproved | OfferFlagged`. Cleaner and more concise than the original design.

**group_request_alerts renamed to GroupRequestAlert in code**
Workflow table for seller-to-group-request matching. Not a UI notification table. Has its own `GroupRequestAlertStatus { Pending | Responded | Ignored }` driving business logic independently from the `Notification` table.

---

## Enums

```csharp
public enum UserRole { Buyer, Supplier }

public enum SupplierOfferStatus { Active, Inactive, Expired }

public enum BatchStatus { Open, Completed, Failed }

public enum BatchParticipantStatus { Active, Left }
// Active = Stripe hold in place (join = pay)
// Left = hold cancelled, buyer exited before completion

public enum GroupRequestStatus { Active, Inactive, Closed }

public enum GroupRequestParticipantStatus { Active, Left }
// Active = in the demand pool, no payment implication
// Left = exited the group request

public enum GroupRequestOfferStatus { Open, Accepted, Countered, Expired }

public enum BuyerOfferResponseType { Accepted, Rejected, Cancelled }
// Accepted = Stripe hold placed
// Rejected = no Stripe interaction
// Cancelled = hold placed then cancelled before offer filled

public enum GroupRequestAlertStatus { Pending, Responded, Ignored }

public enum OrderStatus { Pending, Paid, Failed }

public enum ModerationStatus { Pending, Approved, Flagged }

public enum NotificationType {
    BatchCompleted,
    GroupRequestOfferPlaced,
    GroupRequestOfferFilled,
    GroupRequestMatched,
    OfferApproved,
    OfferFlagged
}
```

---

## Entities

### AppUser : IdentityUser\<Guid\>

| Column        | Type                        | Notes                       |
| ------------- | --------------------------- | --------------------------- |
| Id            | Guid                        | inherited from IdentityUser |
| FirstName     | string                      | not null                    |
| LastName      | string                      | not null                    |
| ImageUrl      | string?                     | null                        |
| CreatedAt     | DateTime                    | not null                    |
| RefreshTokens | ICollection\<RefreshToken\> | navigation                  |

---

### RefreshToken

| Column    | Type      | Notes                    |
| --------- | --------- | ------------------------ |
| Token     | string    | not null                 |
| ExpiresOn | DateTime  | not null                 |
| IsExpired | bool      | computed — not persisted |
| CreatedAt | DateTime  | not null                 |
| RevokedAt | DateTime? | null                     |
| IsActive  | bool      | computed — not persisted |

Owned by AppUser, navigated via `AppUser.RefreshTokens`.

---

### Category

| Column   | Type                    | Notes            |
| -------- | ----------------------- | ---------------- |
| Id       | Guid PK                 |                  |
| Name     | string                  | not null         |
| ParentId | Guid? FK→Category.Id    | null = top-level |
| Parent   | Category?               | navigation       |
| Children | ICollection\<Category\> | navigation       |

---

### SupplierCategoryPreference

| Column      | Type                   | Notes                                                               |
| ----------- | ---------------------- | ------------------------------------------------------------------- |
| SupplierId  | Guid PK+FK→AppUser.Id  | composite PK                                                        |
| CategoryId  | Guid PK+FK→Category.Id | composite PK                                                        |
| MinQuantity | int                    | not null — seller notified only when group request reaches this qty |
| Supplier    | AppUser                | navigation                                                          |
| Category    | Category               | navigation                                                          |

No surrogate id. No IsActive — delete row to deactivate.

---

### SupplierOffer

| Column                 | Type                         | Notes                                                  |
| ---------------------- | ---------------------------- | ------------------------------------------------------ |
| Id                     | Guid PK                      |                                                        |
| SupplierId             | Guid FK→AppUser.Id           | not null                                               |
| CategoryId             | Guid FK→Category.Id          | not null                                               |
| Title                  | string                       | not null — grouping key across variants                |
| Description            | string?                      | null                                                   |
| UnitPrice              | decimal                      | not null                                               |
| DiscountPercentage     | decimal                      | not null                                               |
| BatchTargetQuantity    | int                          | not null — units to complete one full batch            |
| TotalQuantityAvailable | int                          | not null — decremented on batch completion             |
| MinFallbackQuantity    | int?                         | null — min units in last batch to honor deal on expiry |
| VariantAttributes      | string?                      | null — JSON object e.g. {"color":"red","size":"large"} |
| ImageUrls              | string?                      | null — JSON array of image URL strings                 |
| Status                 | SupplierOfferStatus          | not null, default Active                               |
| ModerationStatus       | ModerationStatus             | not null, default Pending                              |
| ModerationReason       | string?                      | null                                                   |
| CreatedAt              | DateTime                     | not null                                               |
| ExpiresAt              | DateTime?                    | null — expiry job only fires if set                    |
| Batches                | ICollection\<SupplierBatch\> | navigation                                             |

---

### SupplierBatch

| Column          | Type                            | Notes                                                                           |
| --------------- | ------------------------------- | ------------------------------------------------------------------------------- |
| Id              | Guid PK                         |                                                                                 |
| OfferId         | Guid FK→SupplierOffer.Id        | not null                                                                        |
| BatchNumber     | int                             | not null — sequential per offer                                                 |
| TargetQuantity  | int                             | not null — set at creation, may be less than BatchTargetQuantity for last batch |
| CurrentQuantity | int                             | not null, default 0                                                             |
| Status          | BatchStatus                     | not null, default Open                                                          |
| CreatedAt       | DateTime                        | not null                                                                        |
| CompletedAt     | DateTime?                       | null                                                                            |
| Participants    | ICollection\<BatchParticipant\> | navigation                                                                      |
| Offer           | SupplierOffer                   | navigation                                                                      |

---

### BatchParticipant

| Column                | Type                        | Notes                                                                        |
| --------------------- | --------------------------- | ---------------------------------------------------------------------------- |
| BatchId               | Guid PK+FK→SupplierBatch.Id | composite PK                                                                 |
| BuyerId               | Guid PK+FK→AppUser.Id       | composite PK                                                                 |
| Quantity              | int                         | not null                                                                     |
| StripePaymentIntentId | string                      | not null — capture_method=manual, captured on completion, cancelled on leave |
| Status                | BatchParticipantStatus      | not null, default Active                                                     |
| JoinedAt              | DateTime                    | not null                                                                     |
| Batch                 | SupplierBatch               | navigation                                                                   |
| Buyer                 | AppUser                     | navigation                                                                   |

---

### GroupRequest

| Column           | Type                                   | Notes                                      |
| ---------------- | -------------------------------------- | ------------------------------------------ |
| Id               | Guid PK                                |                                            |
| InitiatorId      | Guid FK→AppUser.Id                     | not null                                   |
| CategoryId       | Guid FK→Category.Id                    | not null — AI resolved from ItemTitle      |
| ItemTitle        | string                                 | not null — free text as typed by buyer     |
| CurrentQuantity  | int                                    | not null — starts at initiator quantity    |
| Status           | GroupRequestStatus                     | not null, default Active                   |
| ModerationStatus | ModerationStatus                       | not null, default Pending                  |
| ModerationReason | string?                                | null                                       |
| InactiveSince    | DateTime?                              | null — stamped when CurrentQuantity hits 0 |
| CreatedAt        | DateTime                               | not null                                   |
| Participants     | ICollection\<GroupRequestParticipant\> | navigation                                 |
| Offers           | ICollection\<GroupRequestOffer\>       | navigation                                 |
| Alerts           | ICollection\<GroupRequestAlert\>       | navigation                                 |
| Initiator        | AppUser                                | navigation                                 |
| Category         | Category                               | navigation                                 |

---

### GroupRequestParticipant

| Column         | Type                          | Notes                    |
| -------------- | ----------------------------- | ------------------------ |
| GroupRequestId | Guid PK+FK→GroupRequest.Id    | composite PK             |
| BuyerId        | Guid PK+FK→AppUser.Id         | composite PK             |
| Quantity       | int                           | not null                 |
| Status         | GroupRequestParticipantStatus | not null, default Active |
| JoinedAt       | DateTime                      | not null                 |
| GroupRequest   | GroupRequest                  | navigation               |
| Buyer          | AppUser                       | navigation               |

---

### GroupRequestAlert

| Column         | Type                       | Notes                     |
| -------------- | -------------------------- | ------------------------- |
| GroupRequestId | Guid PK+FK→GroupRequest.Id | composite PK              |
| SupplierId     | Guid PK+FK→AppUser.Id      | composite PK              |
| Status         | GroupRequestAlertStatus    | not null, default Pending |
| NotifiedAt     | DateTime                   | not null                  |
| GroupRequest   | GroupRequest               | navigation                |
| Supplier       | AppUser                    | navigation                |

Workflow table — not a UI notification table. Tracks seller response to group request matching alerts independently.

---

### GroupRequestOffer

| Column              | Type                              | Notes                                                      |
| ------------------- | --------------------------------- | ---------------------------------------------------------- |
| Id                  | Guid PK                           |                                                            |
| GroupRequestId      | Guid FK→GroupRequest.Id           | not null                                                   |
| SupplierId          | Guid FK→AppUser.Id                | not null                                                   |
| UnitPrice           | decimal                           | not null — opening price, immutable for audit              |
| MinUnitPrice        | decimal?                          | null — private AI floor, never exposed to buyers           |
| CurrentUnitPrice    | decimal                           | not null — live price shown in sidebar                     |
| QuantityAvailable   | int                               | not null                                                   |
| MinFallbackQuantity | int?                              | null — honor offer at this qty if target not met on expiry |
| VariantAttributes   | string?                           | null — JSON object e.g. {"color":"red","size":"large"}     |
| RoundNumber         | int                               | not null, default 1 — increments on each AI counteroffer   |
| ParentId            | Guid? FK→GroupRequestOffer.Id     | null — root offer if null                                  |
| Status              | GroupRequestOfferStatus           | not null, default Open                                     |
| CreatedAt           | DateTime                          | not null                                                   |
| ExpiresAt           | DateTime                          | not null                                                   |
| Parent              | GroupRequestOffer?                | navigation                                                 |
| Children            | ICollection\<GroupRequestOffer\>  | navigation                                                 |
| Responses           | ICollection\<BuyerOfferResponse\> | navigation                                                 |
| NegotiationLogs     | ICollection\<NegotiationLog\>     | navigation                                                 |
| GroupRequest        | GroupRequest                      | navigation                                                 |
| Supplier            | AppUser                           | navigation                                                 |

Self-referencing tree for negotiation rounds. AI counteroffer creates a child row with RoundNumber + 1 and marks parent as Countered. Full trail preserved without destroying history.

---

### BuyerOfferResponse

| Column                | Type                            | Notes                                                       |
| --------------------- | ------------------------------- | ----------------------------------------------------------- |
| OfferId               | Guid PK+FK→GroupRequestOffer.Id | composite PK                                                |
| BuyerId               | Guid PK+FK→AppUser.Id           | composite PK                                                |
| Response              | BuyerOfferResponseType          | not null                                                    |
| StripePaymentIntentId | string?                         | null for Rejected — set on Accepted, cancelled on Cancelled |
| RespondedAt           | DateTime                        | not null                                                    |
| Offer                 | GroupRequestOffer               | navigation                                                  |
| Buyer                 | AppUser                         | navigation                                                  |

---

### NegotiationLog

| Column           | Type                         | Notes      |
| ---------------- | ---------------------------- | ---------- |
| Id               | Guid PK                      |            |
| OfferId          | Guid FK→GroupRequestOffer.Id | not null   |
| PreviousPrice    | decimal                      | not null   |
| NewPrice         | decimal                      | not null   |
| ReasoningSummary | string?                      | null       |
| ActedAt          | DateTime                     | not null   |
| Offer            | GroupRequestOffer            | navigation |

Append-only. Never updated or deleted. Displayed to supplier as step-by-step negotiation history.

---

### Notification

| Column     | Type               | Notes                                                      |
| ---------- | ------------------ | ---------------------------------------------------------- |
| Id         | Guid PK            |                                                            |
| UserId     | Guid FK→AppUser.Id | not null                                                   |
| Type       | NotificationType   | not null                                                   |
| Title      | string             | not null                                                   |
| Body       | string             | not null                                                   |
| EntityId   | Guid?              | null — soft reference, no hard FK                          |
| EntityType | string?            | null — e.g. SupplierBatch, GroupRequest, GroupRequestOffer |
| IsRead     | bool               | not null, default false                                    |
| CreatedAt  | DateTime           | not null                                                   |
| User       | AppUser            | navigation                                                 |

General UI notification table for both buyers and suppliers. Delivered via SignalR push when inserted. Separate from GroupRequestAlert which is a workflow table.

---

### Order

| Column      | Type                          | Notes                     |
| ----------- | ----------------------------- | ------------------------- |
| Id          | Guid PK                       |                           |
| BuyerId     | Guid FK→AppUser.Id            | not null                  |
| BatchId     | Guid? FK→SupplierBatch.Id     | null — seller batch path  |
| OfferId     | Guid? FK→GroupRequestOffer.Id | null — group request path |
| Quantity    | int                           | not null                  |
| TotalAmount | decimal                       | not null                  |
| Status      | OrderStatus                   | not null, default Pending |
| PaidAt      | DateTime?                     | null                      |
| CreatedAt   | DateTime                      | not null                  |
| Buyer       | AppUser                       | navigation                |
| Batch       | SupplierBatch?                | navigation                |
| Offer       | GroupRequestOffer?            | navigation                |

No StripePaymentIntentId — order is created after capture confirms. Payment intent lives on BatchParticipant and BuyerOfferResponse. Exactly one of BatchId or OfferId must be non-null — enforced as CHECK constraint in migrations.

---

## Key relationships

```
AppUser (supplier) ──< SupplierOffers ──< SupplierBatches ──< BatchParticipants >── AppUser (buyer)
AppUser (supplier) ──< SupplierCategoryPreferences >── Category
AppUser (buyer)    ──< GroupRequests >── Category
GroupRequest ──< GroupRequestParticipants >── AppUser (buyer)
GroupRequest ──< GroupRequestAlerts >── AppUser (supplier)
GroupRequest ──< GroupRequestOffers >── AppUser (supplier)
GroupRequestOffer ──< GroupRequestOffer (self — negotiation tree via ParentId)
GroupRequestOffer ──< BuyerOfferResponses >── AppUser (buyer)
GroupRequestOffer ──< NegotiationLogs
Order >── SupplierBatch (OR) GroupRequestOffer
AppUser ──< Notifications
```
