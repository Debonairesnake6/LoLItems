# Blocking Edges

## Dependency Graph

```
001-research-1-5-0-migration
    └── 002-update-dependencies
            ├── 003-fix-critical-damage-bugs (blocks all items)
            ├── 004-fix-mejais-indentation-bug
            ├── 005-fix-liandrys-invulnerable-enemies
            ├── 006-fix-heartsteel-networking
            ├── 007-migrate-to-handlecharacterdeath
            └── 008-fix-infinity-edge-tooltips-multiplayer
```

## Edge Details

| From | To | Reason |
|------|-----|--------|
| 001 | 002 | Research results determine exact package versions and API changes |
| 002 | 003 | Dependencies must compile before testing damage fixes |
| 002 | 004 | Dependencies must compile before testing indent fix |
| 002 | 005 | Dependencies must compile before testing Liandry's fix |
| 002 | 006 | Dependencies must compile before testing Heartsteel networking |
| 002 | 007 | Dependencies must compile before testing HandleCharacterDeath migration |
| 002 | 008 | Dependencies must compile before testing Infinity Edge tooltips |

## Parallelization

- **003, 004, 005, 006, 007, 008** can all run in parallel with each other (after 002 is complete)
- **001** can start immediately (research ticket, no blockers)
