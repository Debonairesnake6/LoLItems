# 02: Update all project dependencies to RoR2 1.5.0-compatible versions

**What to build:** The project compiles successfully against RoR2 1.5.0 game libraries and the latest R2API split packages, with all dependency strings updated in manifest.json.

**Blocked by:** 01: Research RoR2 1.5.0 migration - verify exact dependencies and API changes

**Status:** ready-for-agent

- [ ] Update RiskOfRain2.GameLibs to 1.5.0-r.1 in LoLItems.csproj
- [ ] Update MMHOOK.RoR2 to latest 1.5.0-compatible version in LoLItems.csproj
- [ ] Update RoR2BepInExPack to latest 1.5.0-compatible version in LoLItems.csproj
- [ ] Replace monolithic R2API package with split packages (Core, Item, Buff, Dot, Networking, Language)
- [ ] Update manifest.json dependency strings for the new R2API split packages
- [ ] Bump plugin version to 1.5.0 in LoLItems.cs
- [ ] Bump manifest version_number to 1.5.0
- [ ] Project compiles with zero errors and zero warnings related to dependency changes
- [ ] All R2API namespaces and APIs (ItemAPI, ContentAddition, LanguageAPI, DotAPI, NetworkingAPI) still resolve correctly
