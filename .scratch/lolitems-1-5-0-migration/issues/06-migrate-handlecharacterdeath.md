# 06: Migrate GlobalEventManager.OnCharacterDeath to HandleCharacterDeath for 1.5.0 compatibility

**What to build:** All character death event hooks use the new HandleCharacterDeath API introduced in RoR2 1.5.0, ensuring forward compatibility and correct behavior with the split death event system.

**Where this comes from:** The RoR2 1.5.0 wiki (https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Updating-Your-Mods/Updating-Core-API-Changelog-1.5.0/) states: "Switch any calls from GlobalEventManager.instance.OnCharacterDeath to GlobalEventManager.instance.HandleCharacterDeath. Nothing breaks if you dont, but the latter is more accurate due to some extensions. The death events are now also split into GlobalEventManager.onCharacterDeathServer and GlobalEventManager.onDeathProcServer to handle the above distinction."

**Blocked by:** 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**Status:** ready-for-agent

- [ ] Update Mejai's Soulstealer OnCharacterDeath hook to HandleCharacterDeath
- [ ] Update Heartsteel OnCharacterDeath hook to HandleCharacterDeath
- [ ] Update base item template OnCharacterDeath hook to HandleCharacterDeath
- [ ] No duplicate death events — each item's death behavior fires exactly once per kill
- [ ] Mejai's stacks are still granted on kill after migration
- [ ] Heartsteel orb still spawns on kill after migration
- [ ] Verify against both onCharacterDeathServer and onDeathProcServer if applicable
