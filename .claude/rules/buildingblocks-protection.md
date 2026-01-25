---
paths:
  - "src/BuildingBlocks/**/*"
---

# ⚠️ BuildingBlocks Protection

**STOP. You are modifying BuildingBlocks.**

Changes to BuildingBlocks affect ALL modules across the entire framework. These are core abstractions that many projects depend on.

## Before Proceeding

1. **Confirm explicit approval** - Has the user specifically approved this change?
2. **Consider alternatives** - Can this be done in the module instead?
3. **Assess impact** - What modules will this affect?

## If Approved

- Make minimal, focused changes
- Ensure backward compatibility
- Update all affected modules
- Run full test suite: `dotnet test src/FSH.Framework.slnx`
- Document the change

## Alternatives to Consider

| Instead of... | Consider... |
|---------------|-------------|
| Modifying Core | Extension method in module |
| Changing Persistence | Custom repository in module |
| Updating Web | Module-specific middleware |

## If Not Approved

Do not proceed. Suggest alternatives that don't require BuildingBlocks modifications.
