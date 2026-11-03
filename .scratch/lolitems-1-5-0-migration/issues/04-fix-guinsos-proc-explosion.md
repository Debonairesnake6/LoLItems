# 04: Fix Guinsoo's Rageblade proc coefficient explosion while preserving functionality

**What to build:** Players with Guinsoo's Rageblade get increased proc coefficient exactly as before — the on-hit proc bonus still applies, but it no longer causes runaway proc chains or ability locks after the 1.5.0 update.

**Blocked by:** 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**Status:** ready-for-agent

- [ ] Investigate why modifying procCoefficient in OnHitEnemy before orig() causes a feedback loop/proc explosion with 1.5.0 dependencies (may be a change in how the game resolves procs or calls OnHitEnemy)
- [ ] Find a fix that preserves the existing functionality (proc coefficient bonus per stack) without triggering the explosion — options include: modifying procCoefficient after orig() returns, capping the total procCoefficient increase, or using a different hook point that doesn't interfere with the game's proc resolution
- [ ] Guinsoo's proc coefficient bonus applies correctly per stack (same behavior as before)
- [ ] Loader character (and other proc-heavy characters) do not experience ability locks from runaway procs
- [ ] Proc coefficient bonus is visible in custom tooltip display
- [ ] Network sync of total proc coefficient bonus still works
