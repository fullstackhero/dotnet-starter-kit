# Module: Catalog

Product catalog — products, categories (tree), brands — with soft-delete/restore/trash and search. Module `Order = 600`. This is the **reference module** for the soft-delete + image patterns; copy from here.

**Entities / DbContext:** `Product` (aggregate, soft-deletable) + `ProductImage`, `Brand`, `Category` (self-referencing tree), `Money` (owned value object). `CatalogDbContext`. Domain events (`ProductCreated`/`PriceChanged`/`StockAdjusted`) are **internal**, not integration events.
**Areas:** Products (+ price/stock/images), Categories (+ tree), Brands — each with Create/Update/Delete/Search/ListTrashed/Restore. Full list: `Features/v1/` or `/scalar`.

## Gotchas / patterns to copy

- **Soft-delete + restore + trashed-listing** is the standard pattern. Unique indexes are **filtered on `"IsDeleted" = FALSE`** so SKU/Slug stay unique-per-tenant among *live* rows only (a deleted SKU can be reused). Replicate this on any soft-deletable unique field.
- **EF value-generation for nav children** — `ProductImageConfiguration` sets `Id.ValueGeneratedNever()` (same nav-child footgun as Chat — see `database.md`).
- **Single-thumbnail invariant** is enforced by the **aggregate**, not a partial unique index (Postgres non-deferrable partial unique indexes can't handle the demote/promote ordering in one transaction). Enforce such invariants in the domain.
- Registers `ProductFileAccessPolicy` (OwnerType `"Product"`) for product images via the Files module.
- Route ordering: literal segments (`/trash`, `/tree`, `/restore`) are registered **before** `/{id:guid}` catch-alls.
