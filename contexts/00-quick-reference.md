# Jomla — Claude Context Quick Reference
> READ THIS FIRST. Tells you which files to paste for each type of conversation.

---

## Files in this folder

| File | When to paste it |
|---|---|
| `01-project-overview.md` | High-level questions, architecture discussions, explaining flows |
| `02-db-schema.md` | Any DB question, EF Core models, queries, migrations, indexes |
| `03-backend-context.md` | ASP.NET Core, CQRS handlers, SignalR hubs, background jobs, Stripe |
| `04-frontend-context.md` | Angular components, routing, SignalR client, UI flows |
| `05-decisions-log.md` | Architecture decisions, "why did we do X", avoiding re-debates |

---

## Conversation Starters by Topic

### "Help me create an EF Core model for..."
→ Paste `02-db-schema.md`

### "Help me implement the [X] CQRS command/query..."
→ Paste `01-project-overview.md` + `02-db-schema.md` + `03-backend-context.md`

### "Help me build the [X] Angular component..."
→ Paste `04-frontend-context.md` + relevant API contract from `03-backend-context.md`

### "Should we design X this way or that way?"
→ Paste `01-project-overview.md` + `05-decisions-log.md`

### "Help me implement the AI negotiation agent..."
→ Paste `01-project-overview.md` + `02-db-schema.md` (negotiation_log + group_request_offers sections) + `03-backend-context.md`

### "Help me with SignalR hubs..."
→ Paste `03-backend-context.md` + `04-frontend-context.md`

---

## Template for starting any conversation

```
## Jomla Project Context

[paste relevant file(s) here]

---

## My Question

[your specific question]

## What's already done
[list any relevant code/decisions already made in this area]
```

---

## Keeping These Docs Updated

At the end of a productive session, ask Claude:
> "Summarize the decisions and schema/code changes we made. Format it so I can update my context docs."

Then update the relevant file(s) before the next session.
