# Jomla — B2B Group-Buying Marketplace

> **Connecting buyers and suppliers through collective purchasing power, intelligent matching, and AI-driven negotiation.**

---

## Table of Contents

- [Overview](#overview)
- [Business Problem](#business-problem)
- [Core Business Concepts](#core-business-concepts)
  - [Supplier Offers & Batch Purchasing](#1-supplier-offers--batch-purchasing)
  - [Group Requests](#2-group-requests)
  - [AI-Powered Price Negotiation](#3-ai-powered-price-negotiation)
  - [Intelligent Content Moderation](#4-intelligent-content-moderation)
  - [Automated Supplier Matching](#5-automated-supplier-matching)
- [User Roles](#user-roles)
- [End-to-End Workflows](#end-to-end-workflows)
  - [Supplier Offer Workflow](#supplier-offer-workflow)
  - [Group Request Workflow](#group-request-workflow)
  - [Negotiation Lifecycle](#negotiation-lifecycle)
- [Platform Features](#platform-features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)

---

## Overview

**Jomla** is a B2B marketplace that enables groups of buyers to pool their purchasing demand and collectively negotiate better prices from suppliers. The platform eliminates inefficiencies in traditional B2B procurement by:

- Aggregating fragmented buyer demand into high-volume batches
- Automatically matching group requests with relevant suppliers
- Running AI-driven multi-round price negotiations
- Enforcing trust and safety through AI content moderation

---

## Business Problem

Small and medium-sized businesses often face a fundamental disadvantage in procurement: they lack the volume required to negotiate competitive pricing from suppliers. Individually, their orders are too small to unlock wholesale pricing tiers.

Jomla solves this by enabling **collective buying** — buyers with shared demand come together to form a group request. Once enough demand is aggregated, suppliers can bid for the group, and the platform's negotiation engine drives prices down to a fair market rate, benefiting both sides.

---

## Core Business Concepts

### 1. Supplier Offers & Batch Purchasing

Suppliers publish offers for products they want to sell in bulk. Each offer defines:

| Attribute | Description |
|---|---|
| **Unit Price** | The base price per unit |
| **Discount Percentage** | The discount applied to bulk buyers |
| **Batch Target Quantity** | The minimum quantity needed to trigger a batch |
| **Min Fallback Quantity** | The minimum quantity the supplier will accept if the full target is not met |
| **Total Quantity Available** | Overall stock available across all batches |
| **Expiry Date** | Deadline for the offer |

A **Batch** is created when enough buyers express interest in a supplier offer to meet the `BatchTargetQuantity`. Once a batch is completed, all participating buyers receive a fulfilled order at the discounted price. Suppliers may run multiple sequential batches for the same offer until total stock is depleted.

> **Key insight:** The batch model gives suppliers predictable demand signals, enabling efficient production planning, while buyers gain access to wholesale prices without large upfront commitments.

---

### 2. Group Requests

A **Group Request** is the demand-side counterpart to a supplier offer. It is initiated by a buyer who cannot find a suitable existing offer and wants to source a specific product.

**Flow:**
1. A buyer creates a group request with a title, description, quantity, and optional images.
2. The system auto-categorizes the request using an AI agent.
3. The request goes through AI content moderation before being made public.
4. Other buyers with the same demand **join** the group request, increasing its collective quantity.
5. Suppliers are automatically notified and can place competitive offers directly against the group.
6. The group collectively evaluates and responds to supplier offers.

Group requests create a **transparent demand signal** visible to the entire supplier network, encouraging competitive pricing.

---

### 3. AI-Powered Price Negotiation

One of Jomla's most distinctive capabilities is its **autonomous multi-round negotiation engine**, which runs automatically when a supplier's offer expires without being fully accepted.

**How it works:**

1. When a `GroupRequestOffer` expires, the system evaluates whether to enter a new negotiation round.
2. The **Negotiation Agent** (powered by an LLM) recommends the next price by:
   - Searching a **vector database (Qdrant)** for semantically similar past negotiation rounds.
   - Using **Retrieval-Augmented Generation (RAG)** to provide the LLM with historical context including past prices, rejection rates, and discount steps.
   - Generating a new price recommendation that must remain above the supplier's minimum price floor.
3. A new offer round is opened at the AI-recommended price.
4. Buyers respond to each round: **Accept**, **Reject**, or **Move to Next Round**.
5. Each completed round is indexed back into Qdrant, continuously improving future negotiations.

**Negotiation constraints:**
- Maximum of **4 negotiation rounds** per offer.
- The price can never drop below the supplier's declared `MinUnitPrice` (floor).
- If no historical data exists, a fallback rule applies a **25% step** of the original price gap per round.

> **Business value:** This system replaces manual back-and-forth negotiation with an automated, data-driven process that converges on fair market prices faster and at scale.

---

### 4. Intelligent Content Moderation

All supplier offers and group requests pass through an **AI Moderation Agent** before becoming visible on the platform. The agent evaluates content for:

- Illegal products or services (weapons, drugs, counterfeit goods)
- Hate speech or discriminatory language
- Adult or graphic content
- Spam or misleading product claims

Content is either **Approved**, **Flagged** (with a reason), or escalated to **manual admin review**. This ensures platform integrity and legal compliance without requiring a human moderator for every listing.

Admins retain override capability to approve or reject flagged content via the Admin panel.

---

### 5. Automated Supplier Matching

When a group request becomes active, the platform automatically identifies and notifies relevant suppliers based on their **Category Preferences**. Suppliers pre-register their areas of expertise, and the matching engine surfaces new group requests that align with their inventory, without requiring suppliers to monitor the platform manually.

---

## User Roles

| Role | Capabilities |
|---|---|
| **Buyer** | Create & join group requests, browse supplier offers, join batches, respond to negotiation rounds, place orders |
| **Supplier** | Publish offers, manage batches, place offers on group requests, participate in negotiations, set category preferences |
| **Admin** | Approve/flag/review all content, manage categories, oversee platform health |

---

## End-to-End Workflows

### Supplier Offer Workflow

```
Supplier Creates Offer
        |
        v
AI Moderation (Approve / Flag)
        | Approved
        v
Offer Goes Active (visible to buyers)
        |
        v
Buyers Join -> Batch Target Met
        |
        v
Batch Completes -> Orders Created
        |
        v
(Next Batch Opens if stock remains)
        |
        v
Offer Expires when stock depleted or deadline reached
```

---

### Group Request Workflow

```
Buyer Creates Group Request
        |
        v
AI Auto-Categorization
        |
        v
AI Moderation (Approve / Flag)
        | Approved
        v
Group Request Goes Active
        |
        v
Other Buyers Join (quantity aggregates)
        |
        v
Supplier Matching Job Runs -> Suppliers Notified
        |
        v
Supplier Places Offer on Group Request
        |
        v
Buyers Respond to Offer (Accept / Reject)
        |
        +-- All Accept -> Offer Fulfilled -> Orders Created
        |
        +-- Offer Expires -> Negotiation Round Begins
```

---

### Negotiation Lifecycle

```
Offer Expires (insufficient acceptances)
        |
        v
NegotiationAgent queries Qdrant for similar past rounds (RAG)
        |
        v
LLM recommends next price (above floor)
        |
        v
New Offer Round opened at recommended price
        |
        v
Buyers respond again
        |
        +-- Round <= 4 and still unfilled -> Repeat
        |
        +-- Round 4 reached -> Drop to floor price (final round)
                |
                +-- Still unfilled -> Offer Failed
```

---

## Platform Features

| Feature | Description |
|---|---|
| **JWT Authentication** | Secure token-based auth with refresh token rotation |
| **Real-Time Notifications** | SignalR hub delivers instant platform events to users |
| **Email Notifications** | Transactional emails for key lifecycle events |
| **Image Uploads** | Cloudinary-backed image storage for offers and group requests |
| **Background Job Processing** | Hangfire manages all async jobs (expiry, moderation, matching, fulfillment) |
| **Admin Dashboard** | Admin APIs for content review and category management |
| **Category Preferences** | Suppliers opt into product categories to receive relevant alerts |
| **Optimistic Concurrency** | Row-version based conflict resolution for batch and offer updates |

---

## Tech Stack

| Layer | Technology |
|---|---|
| **API** | ASP.NET Core Web API |
| **Application Logic** | CQRS via MediatR |
| **Database** | SQL Server + Entity Framework Core |
| **Background Jobs** | Hangfire (SQL Server-backed) |
| **AI / LLM** | OpenAI (via Microsoft Semantic Kernel) |
| **Vector Search** | Qdrant (for RAG-based negotiation memory) |
| **Authentication** | ASP.NET Core Identity + JWT Bearer |
| **Real-Time** | SignalR |
| **Image Storage** | Cloudinary |
| **Payments** | Stripe |

---

## Architecture

Jomla follows a **Clean Architecture** (Onion Architecture) pattern with clear separation of concerns:

```
+-----------------------------------------------------+
|                    Jomla.API                        |  <- HTTP Controllers, SignalR Hubs, Middleware
+-----------------------------------------------------+
|                 Jomla.Application                   |  <- CQRS Commands/Queries, Business Rules, Job Interfaces
+-----------------------------------------------------+
|                  Jomla.Domain                       |  <- Entities, Enums, Domain Logic
+-----------------------------------------------------+
|               Jomla.Infrastructure                  |  <- EF Core, Hangfire, AI Agents, Qdrant, Payments
+-----------------------------------------------------+
```

**Key design decisions:**
- **CQRS** separates read (Queries) and write (Commands) paths, making the business logic explicit and independently testable.
- **Domain-driven design** — entities like `GroupRequest`, `SupplierOffer`, and `SupplierBatch` encapsulate domain invariants.
- **Job-driven automation** — business workflows (moderation, matching, negotiation, fulfillment) are triggered as background jobs, decoupling them from the HTTP request lifecycle.
- **AI as infrastructure** — AI agents (`ModerationAgent`, `NegotiationAgent`, `CategoryAgent`) are defined as interfaces in the Application layer and implemented in Infrastructure, keeping business logic independent of AI provider details.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server instance
- Qdrant instance (cloud or local)
- OpenAI API key (or compatible endpoint)
- Cloudinary account
- Stripe account (for payment processing)

### Configuration

Update `appsettings.Development.json` with your credentials:

```json
{
  "ConnectionStrings": {
    "Default": "<your-sql-server-connection-string>"
  },
  "Jwt": {
    "Key": "<your-jwt-secret>",
    "Issuer": "<issuer>",
    "Audience": "<audience>"
  },
  "AI": {
    "Token": "<your-openai-api-key>",
    "ModelId": "<chat-model-id>",
    "EmbeddingModelId": "<embedding-model-id>",
    "Endpoint": "<optional-custom-endpoint>"
  },
  "Qdrant": {
    "Url": "<qdrant-host>",
    "ApiKey": "<qdrant-api-key>"
  },
  "CloudinarySettings": {
    "CloudName": "<cloud-name>",
    "ApiKey": "<api-key>",
    "ApiSecret": "<api-secret>"
  }
}
```

### Running the Application

```bash
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project Jomla.Infrastructure --startup-project Jomla.API

# Run the API
dotnet run --project Jomla.API
```

The API will be available at `https://localhost:<port>` and the Hangfire dashboard at `/hangfire`.

---

*Jomla — Strength in numbers, value in every deal.*
