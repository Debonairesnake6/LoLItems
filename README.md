# LoL Items

Adding items from League of Legends to Risk of Rain 2.

# Items Added
These are just the items implemented so far. I am planning to continue to add more items as I develop this mod.
Icon | Name | Tier | Description
--- | --- | --- | ---
![BannerOfCommand](https://static.wikia.nocookie.net/leagueoflegends/images/a/a4/Banner_of_Command_item_HD.png/revision/latest/scale-to-width-down/64?cb=20201104170517) | BannerOfCommand | White | Your minions do bonus damage.
![MejaisSoulstealer](https://static.wikia.nocookie.net/leagueoflegends/images/8/88/Mejai%27s_Soulstealer_item_HD.png/revision/latest/scale-to-width-down/64?cb=20221103165010) | MejaisSoulstealer | White | Killing enemies grants more damage for a short time.
![GuardiansBlade](https://static.wikia.nocookie.net/leagueoflegends/images/f/f2/Guardian%27s_Blade_item.png/revision/latest?cb=20221019163250) | GuardiansBlade | White | Reduce cooldown on secondary and utility skills.
![GuinsoosRageblade](https://static.wikia.nocookie.net/leagueoflegends/images/6/64/Guinsoo%27s_Rageblade_item_HD.png/revision/latest/scale-to-width-down/64?cb=20201110230134) | GuinsoosRageblade | Green | Gain extra proc coefficient on everything.
![InfinityEdge](https://static.wikia.nocookie.net/leagueoflegends/images/a/aa/Infinity_Edge_item_HD.png/revision/latest/scale-to-width-down/64?cb=20221230173431) | InfinityEdge | Green | Crit chance and crit damage.
![Liandrys](https://static.wikia.nocookie.net/leagueoflegends/images/3/30/Liandry%27s_Anguish_item.png/revision/latest?cb=20201118211533) | Liandrys | Green | Burn enemies for % max health damage on hit.
![KrakenSlayer](https://static.wikia.nocookie.net/leagueoflegends/images/e/e9/Kraken_Slayer_item_HD.png/revision/latest/scale-to-width-down/64?cb=20201110232124) | KrakenSlayer | Green | Bonus damage every few hits.
![ImmortalShieldbow](https://static.wikia.nocookie.net/leagueoflegends/images/2/2b/Immortal_Shieldbow_item.png/revision/latest?cb=20201118205028) | ImmortalShieldbow | Green | Gives a barrier based on your max health when low.
![Rabadons](https://static.wikia.nocookie.net/leagueoflegends/images/c/c5/Rabadon%27s_Deathcap_item.png/revision/latest?cb=20201118205704) | Rabadons | Red | Do more damage.
![Heartsteel](https://static.wikia.nocookie.net/leagueoflegends/images/8/87/Heartsteel_item_HD.png/revision/latest/scale-to-width-down/64?cb=20221115195510) | Heartsteel | Red | Gain permanent health on kill with no cap. Every few seconds deal a portion of your health as extra damage on hit.
![ImperialMandate](https://static.wikia.nocookie.net/leagueoflegends/images/b/bc/Imperial_Mandate_item.png/revision/latest?cb=20201104212814) | ImperialMandate | Void Green | Bonus damage per debuff. Corrupts **Death Mark**.
![Bork](https://static.wikia.nocookie.net/leagueoflegends/images/2/2f/Blade_of_the_Ruined_King_item.png/revision/latest?cb=20221210230042) | Bork | Void Green | Attack speed. Every few hits do damage and heal, and has a cooldown. Corrupts **Syringes** and **Leaching Seeds**.
![GargoyleStoneplate](https://static.wikia.nocookie.net/leagueoflegends/images/6/62/Gargoyle_Stoneplate_item_HD.png/revision/latest/scale-to-width-down/64?cb=20221230182936) | GargoyleStoneplate | Equipment | Temporarily gain armor and a barrier based on your maximum health.

## Changelog

**1.1.1**
* Added sound effects for Bork and ImmortalShieldbow procs
* Bork's number of hits changed from 5 to 3 to match the tooltip (delete your LoLItems config for this update!)
* Updated GargoyleStoneplate's tooltip to properly reflect its effect
* Bork now requires the proper amount of hits before proccing between cooldowns
* Tooltips should now also update in multiplayer
* Improved the way stats are recalculated

**1.1.0**

* Buffed GuardiansBlade
* Buffed Liandrys when stacking the item
* Added GargoyleStoneplate
  * Credit to [ggreer91](https://github.com/ggreer91) for implementing
* Item tooltip stats will now always display when the stat is still 0
* Item tooltip text will now append to the tooltip, rather than overwrite text from other mods
* Fixed Rabadons not showing up as an in game model

**1.0.0**

* Added config settings for every item. Balance the mod as you see fit! I'm still welcoming reports for balancing the base mod
  * https://github.com/Debonairesnake6/LoLItems/issues/32
* Nerfed BannerOfCommand to give a smaller bonus
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* Moved some duplicated code to the utilities file. This includes allowing any item to be set as void, depending on the config settings
* Moved GuinsoosRageblade into the green tier
  * https://github.com/Debonairesnake6/LoLItems/issues/32

**0.1.15**

* Fixed a bug with GuardiansBlade that broke characters/minions without a secondary or utility skill
  * https://github.com/Debonairesnake6/LoLItems/issues/30

**0.1.14**

* Fixed KrakenSlayer not properly scaling with additional stacks
  * https://github.com/Debonairesnake6/LoLItems/issues/27
* Fixed KrakenSlayer taking 3 hits on the same enemy rather than any enemy
  * https://github.com/Debonairesnake6/LoLItems/issues/28
* Added a visual buff indicator for KrakenSlayer
  * https://github.com/Debonairesnake6/LoLItems/issues/28

**0.1.13**

* Added KrakenSlayer
  * https://github.com/Debonairesnake6/LoLItems/issues/23
* Added GuardiansBlade
  * https://github.com/Debonairesnake6/LoLItems/issues/24
* Added ImmortalShieldbow
  * https://github.com/Debonairesnake6/LoLItems/issues/24

**0.1.12**

* Reduced the sound of Heartsteel
  * https://github.com/Debonairesnake6/LoLItems/issues/21

**0.1.11**

* Fixed the Heartsteel sound effect to properly scale with your SFX volume slider
  * https://github.com/Debonairesnake6/LoLItems/issues/19

**0.1.10**

* Fixed a bug where on hit effects would only proc when you had Heartsteel
  * https://github.com/Debonairesnake6/LoLItems/issues/17

**0.1.9**

* Improved the way void items are defined
* Added the CLING sound effect to Heartsteel and gave it a damage proc every few seconds
* Fixed the description for Bork to also indicate it now gives attack speed
* Fixed a bug with InfinityEdge that causes victory/defeat screen to glitch when you have this item in your inventory
* Fixed InfinityEdge giving Railgunner crit chance rather than crit damage
  * https://github.com/Debonairesnake6/LoLItems/issues/14
* Fixed a bug with Heartsteel where Engineer's turrets would grant the player health
  * https://github.com/Debonairesnake6/LoLItems/issues/15

**0.1.8**

* Now displays the item stats (e.g. damage dealt) in the tab screen and end game screen
* Added an in game model for Rabadons and added to every playable character in the game (Do not expect frequent in game models)
* Bug fix with Heartsteel granting more health than it should
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* Nerfed Mejais max damage, but increased duration to make it more satisfying
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* Nerfed BannerOfCommand's bonus damage amp
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* Removed the blue tint from the Liandrys burn icon and adjusted the damage text to be blue
  * https://github.com/Debonairesnake6/LoLItems/issues/12
* Added a floor and ceiling for the damage Liandrys can do. It now behaves similar to poison and should be more balanceable
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* ImperialMandate is now a void which corrupts Death Marks. It gives bonus damage per debuff
  * https://github.com/Debonairesnake6/LoLItems/issues/11
* Bork is now a void which corrupts Syringes and Leaching Seeds. It gives attack speed and every few hits deals damage and heals
  * https://github.com/Debonairesnake6/LoLItems/issues/11

**0.1.7**

* Added InfinityEdge
* Added ImperialMandate
* Added MejaisSoulstealer

**0.1.6**

* Fixed a bug that caused issues when using blood shrines or void cradles
* Improved item descriptions for all items
* Nerfed Liandrys damage
* Minion damage now counts towards your total item damage when hovered over in your HUD if they can leverage the items (banner, engi turrets, etc)

**0.1.5**

* Fixed issue that could cause problems when updating
* Added banner of command

**0.1.4**

* Fixed bug that was generating errors for Heartsteel
* Added GuinsoosRageblade

**0.1.3**

* General code clean up and implemented some best practices (should work better in multiplayer)
* Nerfed Bork big hit damage
* Nerfed Bork on hit damage
* Buffed Heartsteel health gain

**0.1.2**

* Added items table in readme

**0.1.1**

* Fixed an issue loading assets

**0.1.0**

* Release of my first mod.

None of the visual assets are mine. I have copied most of them from the league wiki.
