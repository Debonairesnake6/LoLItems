# LoLItems RoR2 1.5.0 Migration Tickets

## Dependency Graph

```
01 (Research)
  └── 02 (Update Dependencies)
        ├── 03 (Fix Zero Damage - Rabadon's, Imperial Mandate, Mejai's, Kraken Slayer)
        ├── 04 (Fix Guinsoo's Proc Explosion)
        ├── 05 (Fix Guardian's Blade Multi-stack/Zero Damage)
        ├── 06 (Migrate HandleCharacterDeath)
        └── 07 (Fix Mejai's Indentation Bug)
```

## Ticket List

| # | Title | Blocked by | Priority |
|---|-------|------------|----------|
| 01 | Research 1.5.0 migration | None | High (first step) |
| 02 | Update all dependencies | 01 | High |
| 03 | Fix zero damage (Rabadon's, Imperial Mandate, Mejai's, Kraken Slayer) | 02 | Critical |
| 04 | Fix Guinsoo's proc explosion (preserve functionality) | 02 | Critical |
| 05 | Fix Guardian's Blade multi-stack/zero damage | 02 | High |
| 06 | Migrate HandleCharacterDeath | 02 | Low |
| 07 | Fix Mejai's indentation bug | 02 | Low |

## Skipped for Now

- Heartsteel networking (#48) — not a priority
- Liandry's invulnerability (#49) — not a priority
- Infinity Edge tooltips (#38) — not a priority

## Parallel Execution

After ticket 02 completes, tickets 03-07 can all run in parallel.

- **Critical path:** 01 → 02 → 03 → testing
- **Parallel work:** 04, 05, 06, 07 can all run simultaneously
