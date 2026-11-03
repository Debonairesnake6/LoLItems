# 01: Research RoR2 1.5.0 migration - verify exact dependencies and API changes

**What to build:** A verified, comprehensive list of all changes required to migrate LoLItems from RoR2 1.4.0 to 1.5.0, including exact NuGet package versions, R2API split package requirements, and all code-level API changes needed.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Confirm exact NuGet versions for RiskOfRain2.GameLibs, MMHOOK.RoR2, RoR2BepInExPack, and all R2API split packages that are compatible with 1.5.0
- [ ] Verify which 1.5.0 breaking changes from the wiki actually affect this mod's codebase (ItemTag shifts, DamageType changes, HandleCharacterDeath, etc.)
- [ ] Confirm the R2API 5.x split package imports (R2API.Core, R2API.Item, R2API.Buff, R2API.Dot, R2API.Networking, R2API.Language) work as drop-in replacements for the monolithic R2API 5.0.5
- [ ] Document the exact changes needed per file so they can be executed as subsequent tickets
- [ ] Identify any undocumented API changes that may affect the mod's hook patterns (TakeDamage, OnHitEnemy, OnCharacterDeath)
