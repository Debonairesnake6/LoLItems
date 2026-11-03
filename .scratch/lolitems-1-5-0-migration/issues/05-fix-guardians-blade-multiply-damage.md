# 05: Fix Guardian's Blade multi-stack and zero damage issues

**What to build:** Guardian's Blade applies cooldown reduction correctly without causing the item to "multiply" on pickup or zero out ability damage.

**Blocked by:** 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**Status:** ready-for-agent

- [ ] Investigate Guardian's Blade cooldown reduction implementation — the `HandleInventoryChanged` hook and `RecalculateStatsAPI.GetStatCoefficients` hook both modify cooldown multipliers
- [ ] Determine if the "multiplies items on pickup" behavior is a real bug or expected (hyperbolic scaling)
- [ ] Determine if "zero damage of certain abilities" is caused by over-reducing cooldown multipliers (args.utilityCooldownMultAdd / args.secondaryCooldownMultAdd going too negative)
- [ ] Fix any issues found — cap cooldown reduction at a reasonable maximum, or use a different modification approach compatible with 1.5.0
- [ ] Guardian's Blade cooldown reduction applies correctly per stack with hyperbolic scaling
- [ ] Abilities do not deal zero damage after equipping Guardian's Blade
