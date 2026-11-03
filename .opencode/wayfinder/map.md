# LoLItems 1.5.0 Migration Map

## Destination

Update LoLItems mod from RoR2 1.4.0 to 1.5.0 "Hallowed Concepts", resolving all dependency incompatibilities and fixing the critical "zero damage" bugs reported by users.

## Notes

- Mod target: Risk of Rain 2 custom items (League of Legends inspired)
- Current branch: 1.4.0
- Current manifest version: 1.3.2
- Items: Banner of Command, BORK, Cull, Experimental Hexplate, Gargoyle Stoneplate, Guardian's Blade, Guinsoo's Rageblade, Heartsteel, Immortal Shieldbow, Imperial Mandate, Infinity Edge, Kraken Slayer, Liandry's Anguish, Mejai's Soulstealer, Rabadon's Deathcap
- Tracker: Local markdown (Matt Pocock skills not configured for GitHub)

## Decisions so far

- [Destination](#destination): Update LoLItems mod from RoR2 1.4.0 to 1.5.0 "Hallowed Concepts"
- [Spec published](.opencode/wayfinder/SPEC.md): Full spec with 18 user stories, implementation decisions, testing strategy

## Open Tickets

Tickets are in `.scratch/lolitems-1-5-0-migration/issues/`:

1. **[Research 1.5.0 migration](.scratch/lolitems-1-5-0-migration/issues/01-research-1-5-0-migration.md)** — Verify exact dependencies and API changes
2. **[Update all dependencies](.scratch/lolitems-1-5-0-migration/issues/02-update-dependencies.md)** — Upgrade packages and R2API split migration
3. **[Fix zero damage](.scratch/lolitems-1-5-0-migration/issues/03-fix-critical-zero-damage-bugs.md)** — Rabadon's, Imperial Mandate, Mejai's, Kraken Slayer
4. **[Fix Guinsoo's proc explosion](.scratch/lolitems-1-5-0-migration/issues/04-fix-guinsos-proc-explosion.md)** — Preserve functionality without ability locks
5. **[Fix Guardian's Blade](.scratch/lolitems-1-5-0-migration/issues/05-fix-guardians-blade-multiply-damage.md)** — Multi-stack and zero damage
6. **[Migrate HandleCharacterDeath](.scratch/lolitems-1-5-0-migration/issues/06-migrate-handlecharacterdeath.md)** — Forward compat migration
7. **[Fix Mejai's indentation](.scratch/lolitems-1-5-0-migration/issues/07-fix-mejais-indentation-bug.md)** — Missing braces in OnCharacterDeath

## Skipped for Now

- Heartsteel networking (#48)
- Liandry's invulnerability (#49)
- Infinity Edge tooltips (#38)

## Not yet specified

- Whether the Kraken Slayer zero damage has a different root cause than the TakeDamage-hook items (may need investigation in ticket 03)
- Whether Guardian's Blade "multiplies items" is expected hyperbolic scaling or a real bug (ticket 05)

## Out of scope

- New item additions (Hextech Gunblade #47, Runaan's etc. #24)
- Immortal Shieldbow redesign (#50)
- BORK, Cull, Hexplate, Banner, Gargoyle, Immortal Shieldbow (no bugs reported)
