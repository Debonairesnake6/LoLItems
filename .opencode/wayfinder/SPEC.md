# LoLItems RoR2 1.5.0 Migration Spec

## Problem Statement

Users of the LoLItems mod (Risk of Rain 2 custom items inspired by League of Legends) are experiencing critical bugs after the RoR2 1.5.0 "Hallowed Concepts" game update. The mod was last compiled against RoR2 1.4.0 libraries. Multiple items cause players to deal zero damage with both basic attacks and skills. Guinsoo's Rageblade causes proc coefficient explosions that lock out character abilities. Additional pre-existing bugs around invulnerability checks, multiplayer networking, and tooltip sync remain unresolved.

The root cause is a dependency version mismatch: the mod's compiled assemblies target RoR2 1.4.0 types, but the running game uses 1.5.0 types. The `DamageInfo` struct layout, damage calculation pipeline, and several API methods have changed between versions, breaking the mod's hook-based damage modification system.

## Solution

Update the mod's entire dependency chain to RoR2 1.5.0-compatible versions, migrate the R2API monolithic package to its split-package model, and rewrite the damage modification system to use the `RecalculateStatsAPI` stat hook pipeline instead of direct `DamageInfo` struct mutation. This approach is compatible with 1.5.0's redesigned damage pipeline and is the standard pattern for RoR2 mods.

Additionally, fix all pre-existing bugs (invulnerability checks, multiplayer networking, tooltips, indentation bugs) as part of the migration to ship a clean, compatible release.

## User Stories

1. As a Risk of Rain 2 player, I want all LoLItems to deal the correct amount of damage after the 1.5.0 game update, so that my builds function as expected
2. As a player with Rabadon's Deathcap, I want my damage to be multiplied by the correct percentage per stack, so that I can reliably plan my damage output
3. As a player with Imperial Mandate, I want bonus damage to apply to enemies with debuffs, so that I can synergize with my team's crowd control
4. As a player with Mejai's Soulstealer, I want bonus damage to scale correctly with stacks, so that I can snowball effectively after kills
5. As a player with Guinsoo's Rageblade, I want proc coefficient increased without causing ability locks or runaway proc chains, so that I can use my skills normally
6. As a player with Kraken Slayer, I want the on-hit bonus damage to proc reliably after the required number of hits, so that I can build it as a consistent DPS option
7. As a player with Guardian's Blade, I want cooldown reduction to apply correctly to my skills, so that I can use my abilities more frequently
8. As a player with Liandry's Anguish, I want the burn damage to apply to vulnerable enemies but not invulnerable bosses, so that the item is balanced
9. As a multiplayer host, I want all LoLItems that track values across sessions to sync correctly between all players, so that every player sees consistent stats
10. As a multiplayer player with Heartsteel, I want health gain from kills and bonus damage from on-hit procs to be properly synchronized, so that my character functions correctly with teammates
11. As a multiplayer player with Infinity Edge, I want my tooltip to show my actual crit damage multiplier, so that I can make informed build decisions
12. As a player using the scoreboard, I want all LoLItems tooltips to display accurate dynamic values for every player, not just myself
13. As a mod user, I want the mod to compile and load without errors on the latest RoR2 version, so that the mod is usable
14. As a mod user, I want the Thunderstore package to declare correct dependencies on the latest R2API versions, so that mod conflicts are avoided
15. As a player with Heartsteel, I want the on-hit sound effect to play at the correct volume and distance for all listeners in multiplayer, so that audio feedback is consistent
16. As a player, I want the mod config to let me enable/disable individual items, so that I can customize which items appear in my runs
17. As a player, I want void items to correctly corrupt into the mod's void equivalents, so that void mode gameplay includes these items
18. As a player playing as Loader, I want Guinsoo's Rageblade to not cause the loader's passive shield proc to fire excessively on every attack, so that I can use other abilities normally

## Implementation Decisions

### 1. Dependency Update Strategy

All NuGet package references in `LoLItems.csproj` will be updated to their latest 1.5.0-compatible versions:

- `RiskOfRain2.GameLibs`: 1.4.0-r.0 → 1.5.0-r.1
- `MMHOOK.RoR2`: 2025.11.19 → 2026.10.8
- `RoR2BepInExPack`: 1.37.0 → 1.45.0+
- `R2API` (monolithic 5.0.5) → split packages (R2API.Core, R2API.Item, R2API.Buff, R2API.Dot, R2API.Networking, R2API.Language) at version 5.3.0 each

The manifest.json dependency strings and plugin version will be bumped to reflect the 1.5.0 target.

### 2. Damage Modification Architecture Change

The mod currently modifies `DamageInfo` structs directly in `HealthComponent.TakeDamage` hooks. This approach is incompatible with 1.5.0's redesigned damage pipeline. The new approach uses `RecalculateStatsAPI.GetStatCoefficients` to apply damage modifications through the stat calculation pipeline.

**Items affected by this change:**
- Rabadon's Deathcap (multiplicative damage amplifier)
- Imperial Mandate (conditional damage amplifier based on enemy debuffs)
- Mejai's Soulstealer (stacked damage amplifier)

**Items NOT affected (use OnHitEnemy for on-hit procs, which remain valid):**
- Guinsoo's Rageblade (proc coefficient modifier)
- Kraken Slayer (conditional on-hit damage)
- Heartsteel (on-hit health-based damage)
- Liandry's Anguish (DOT application on hit)
- Banner of Command (on-hit heal/shield)

**Guinsoo's Rageblade specific fix:** The proc coefficient modification will be moved from the `OnHitEnemy` hook (where it fires before the game's proc resolution) to a per-character tracker that is read during the game's own proc calculation. This prevents the feedback loop that causes ability locks.

### 3. GlobalEventManager Event Migration

All `On.RoR2.GlobalEventManager.OnCharacterDeath` hooks will be updated to `HandleCharacterDeath` per the 1.5.0 API guidance. This affects:
- Mejai's Soulstealer (stack gain on kill)
- Heartsteel (health gain and orb spawn on kill)
- Base item template (death event stub)

### 4. Pre-existing Bug Fixes (as part of migration)

- **Liandry's Anguish invulnerability:** Add a HurtBox invulnerability check before applying DOT
- **Heartsteel networking:** Add `NetworkServer.active` guard on orb spawning, fix sound event routing for multiplayer distance, ensure stat dirty flags are set on all clients
- **Infinity Edge tooltips:** Ensure tooltip data is networked to all clients
- **Mejai's Soulstealer indentation:** Fix missing braces in OnCharacterDeath hook
- **Guardian's Blade cooldown reduction:** Verify RecalculateStatsAPI hook still functions correctly for stat multiplication

### 5. Code Organization

No new modules or interfaces will be introduced. All changes will be made within the existing item classes. The `Utilities` class will be updated if new helper methods are needed for the damage calculation approach. The `NetworkManager` will be used for any new networking requirements.

### 6. Backward Compatibility

The mod will only target RoR2 1.5.0 going forward. No backward compatibility with 1.4.0 will be maintained. Users on older game versions will need to continue using the 1.4.0 branch.

## Testing Decisions

### What Makes a Good Test

Since this is a Risk of Rain 2 mod, automated unit testing of game interactions is not feasible. The testing strategy focuses on:

1. **Compilation verification:** The project must build cleanly with all new dependencies and zero errors
2. **In-game verification:** Each item's functionality must be manually verified in the game against the user stories listed above
3. **Multiplayer verification:** All networked items (Heartsteel, Infinity Edge, Imperial Mandate, Mejai's, Rabadon's) must be tested in multiplayer sessions

### Module-Level Testing Checklist

Each item will be verified against:
- Damage output matches configured values
- No zero-damage behavior
- Proc behavior does not cause ability locks or runaway chains
- Tooltips display correct dynamic values
- In multiplayer, values sync correctly between players
- Config enable/disable works for each item
- Void corruption works for void-tier items

### Pre-migration Baseline

The current 1.4.0 branch serves as the baseline. Any regression in item functionality (beyond what the 1.5.0 update already broke) introduced during migration will be treated as a defect.

## Out of Scope

- New item additions (Hextech Gunblade #47, Runaan's Hurricane, Navori Quickblades, Trinity Force, Goredrinker, Hextech Rocketbelt, Turbo Chemtank from #24)
- Immortal Shieldwig redesign/replacement (#50)
- BORK item fixes or changes (no bugs reported)
- Cull item fixes or changes (no bugs reported)
- Experimental Hexplate fixes or changes (no bugs reported)
- Banner of Command fixes or changes (no bugs reported)
- Gargoyle Stoneplate fixes or changes (no bugs reported)
- Immortal Shieldbow fixes or changes (no bugs reported)
- WhiteClover (currently commented out in LoLItems.cs)
- Changes to item display rules (model attachment per character)
- Changes to item icons or prefab assets
- Changes to the mod's config file structure
- Changes to the Thunderstore packaging pipeline (beyond version bumps)
- Performance profiling or optimization beyond bug fixes

## Further Notes

### Known 1.5.0 Breaking Changes (Wiki)

The following 1.5.0 changes were reviewed against this mod's codebase:

| Change | Affects This Mod? | Action |
|--------|-------------------|--------|
| ItemTag enum shifts (ObjectiveRelated, Technology, HiddenForDroneBuffIcon, SacrificeBlacklist) | No — mod uses ItemTag.Damage, Healing, Utility, OnKillEffect (not in shifted set) | None |
| DamageTypeExtended removal (LunarRuin, Drone, Grounded, ChefSource, Warp, IgniteChefOilBoosted) | No — mod uses DamageType.Generic | None |
| DamageTypeCombo IsChefFireDamage removal | No — mod doesn't use Chef damage types | None |
| Inventory equipment set array (_equipmentSetState[][] → _equipmentSlots[]) | No — mod doesn't access this directly | None |
| GlobalEventManager.OnCharacterDeath → HandleCharacterDeath | Yes — 3 items use OnCharacterDeath | Migrate all hooks |
| CharacterBody.ItemBehavior → BaseItemBodyBehavior | No — mod doesn't use ItemBehavior | None |
| OnHook signature disambiguation (uint overloads) | Possibly — will verify during compilation | Fix any build errors |
| Projectile ghosts (ghostPrefab → ghostPrefabAddress) | No — mod doesn't reference projectiles | None |

### Critical Bug Root Causes

The "zero damage" bug (#52, #54) across multiple items shares a common root cause: the `TakeDamage` hook pattern modifies a `DamageInfo` struct before calling the original method. In 1.5.0, the game's damage pipeline likely recalculates damage from scratch (using the character's stats) rather than trusting the passed-in `DamageInfo.damage` value. The fix is to use `RecalculateStatsAPI.GetStatCoefficients` which modifies the character's stats before damage is calculated.

The Guinsoo's Rageblade proc explosion (#53, #54) is caused by modifying `damageInfo.procCoefficient` before calling the original `OnHitEnemy` method. This inflates the proc coefficient seen by the game's proc resolution, which may trigger additional `OnHitEnemy` calls, which further inflate the coefficient, creating an infinite loop. The fix is to stop modifying the struct and instead track the proc coefficient bonus per-character.

### Files to Be Modified

- `LoLItems.csproj` — NuGet package references
- `manifest.json` — Version numbers and dependency strings
- `LoLItems.cs` — Plugin version constant
- `Rabadons.cs` — TakeDamage hook → RecalculateStatsAPI
- `ImperialMandate.cs` — TakeDamage hook → RecalculateStatsAPI
- `MejaisSoulstealer.cs` — TakeDamage hook → RecalculateStatsAPI, indentation fix, OnCharacterDeath → HandleCharacterDeath
- `GuinsoosRageblade.cs` — OnHitEnemy procCoefficient fix
- `KrakenSlayer.cs` — Verify OnHitEnemy compatibility with 1.5.0
- `Heartsteel.cs` — OnCharacterDeath → HandleCharacterDeath, networking fixes, sound fix
- `Liandrys.cs` — Invulnerability check, OnCharacterDeath → HandleCharacterDeath (TakeDamage hook kept for tracking only)
- `Utilities.cs` — May need helper methods for stat-based damage calculation
- `NetworkManager.cs` — May need updates for new sync requirements
