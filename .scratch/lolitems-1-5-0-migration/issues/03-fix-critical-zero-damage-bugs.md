# 03: Fix critical zero damage bugs for Rabadon's Deathcap, Imperial Mandate, Mejai's Soulstealer, and Kraken Slayer

**What to build:** Players equipping Rabadon's Deathcap, Imperial Mandate, Mejai's Soulstealer, or Kraken Slayer deal correct damage with all attacks and abilities — no more zero damage after the 1.5.0 update.

**Blocked by:** 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**Status:** ready-for-agent

- [ ] Rabadon's Deathcap: rewrite damage multiplier from TakeDamage hook mutation to RecalculateStatsAPI stat hook — damage scales correctly by configured percentage per stack
- [ ] Imperial Mandate: rewrite damage amplifier from TakeDamage hook mutation to RecalculateStatsAPI — bonus damage applies correctly to enemies with debuffs
- [ ] Mejai's Soulstealer: rewrite damage bonus from TakeDamage hook mutation to RecalculateStatsAPI — bonus damage scales correctly with stack count
- [ ] Kraken Slayer: investigate and fix zero damage — uses OnHitEnemy hook (not TakeDamage) so root cause may differ from items above; verify OnHitEnemy compatibility with 1.5.0 dependencies
- [ ] All affected items: verify that their tracking/description features (custom tooltip values, network sync) still function after the rewrite
- [ ] No zero damage with any of these items equipped
- [ ] No regression in items not affected by this ticket (Guinsoo's, Heartsteel, Liandry's, Banner of Command)
