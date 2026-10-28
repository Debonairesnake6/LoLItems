# 07: Fix Mejai's Soulstealer indentation bug in OnCharacterDeath hook

**What to build:** Mejai's Soulstealer applies the max stacks buff only when the player actually has the item equipped and reaches the maximum stack count.

**Blocked by:** 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**Status:** ready-for-agent

- [ ] Fix indentation in OnCharacterDeath hook: move the max stacks buff check inside the `inventoryCount > 0` block
- [ ] Mejai's Soulstealer max buff is only applied when player has the item and reaches max stacks
- [ ] Mejai's Soulstealer max buff is removed when item is removed or stacks are cleared
- [ ] No regression in regular stack gain behavior
