using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ItemStats;
using System;
using System.Collections.Generic;
using System.Text;
using ItemStats.ValueFormatters;


namespace LoLItems
{
    public static class ItemStatsModCompatibility
    {
        private static bool? _enabled;

        public static bool enabled 
        {
            get 
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.ontrigger.itemstats");
                }
                return (bool)_enabled;
            }
        }

        public static void InvokeAddCustomItemStatDef(ItemIndex myItemIndex, ItemStats.ItemStatDef myItemStatDef)
        {
            ItemStats.ItemStatsMod.AddCustomItemStatDef(myItemIndex, myItemStatDef);
        }

        public static void InvokeAddStatModifier(ItemStats.StatModification.AbstractStatModifier MyStatModifier)
        {
            ItemStats.ItemStatsMod.AddStatModifier(MyStatModifier);
        }
    }

    // public class MyHeartSteelModifier : ItemStats.StatModification.AbstractStatModifier
    // {
    //     protected override Func<float, ItemIndex, int, StatContext, float> ModifyValueFunc =>
    //         (result, itemIndex, itemStatIndex, context) =>
    //         {
    //             var body = context.Master.GetComponent<CharacterBody>();
    //             return body.GetHeartSteelHealth();
    //         };

    //     protected override Func<float, ItemIndex, int, StatContext, string> FormatFunc =>
    //         (result, itemIndex, itemStatIndex, ctx) =>
    //         {
    //             float heartSteelHealth = ctx.Master.GetComponent<CharacterBody>().GetHeartSteelHealth();

    //             var stringBuilder = new StringBuilder();
    //             stringBuilder.Append("Health gained: " + heartSteelHealth);

    //             return stringBuilder.ToString();
    //         };

    //     public override Dictionary<ItemIndex, IEnumerable<int>> AffectedItems =>
    //         new Dictionary<ItemIndex, IEnumerable<int>>
    //         {
    //             [HeartSteel.myItemDef.itemIndex] = new[] {0}
    //         };
    // }

    // public class MyLuckModifier : ItemStats.StatModification.AbstractStatModifier
    // {
    //     protected override Func<float, ItemIndex, int, StatContext, float> ModifyValueFunc =>
    //         (result, itemIndex, itemStatIndex, context) =>
    //         {
    //             // if chance is already >= 100% then return same value so
    //             // that there are no contribution stats
    //             if (result >= 1)
    //             {
    //                 return result;
    //             }

    //             // var cloverCount = context.CountItems(ItemCatalog.FindItemIndex("Clover"));
    //             // var purityCount = context.CountItems(ItemCatalog.FindItemIndex("LunarBadLuck"));

    //             var luck = context.Master.luck;
    //             if (luck > 0)
    //             {
    //                 return 1 - Mathf.Pow(1 - result, 1 + luck);
    //             }

    //             return (float) Math.Round(Math.Pow(result, 1 + Math.Abs(luck)), 4);
    //         };

    //     protected override Func<float, ItemIndex, int, StatContext, string> FormatFunc =>
    //         (result, itemIndex, itemStatIndex, ctx) =>
    //         {
    //             // TODO: pass the original value to be able to properly show clover and purity contribution
    //             var itemCount = ctx.CountItems(itemIndex);
    //             if (itemCount <= 0)
    //             {
    //                 return $"{result.FormatPercentage(signed: true, color: Colors.ModifierColor)} from luck";
    //             }

    //             var itemStatDef = ItemStatsMod.GetItemStatDef(itemIndex);
    //             var itemStat = itemStatDef.Stats[itemStatIndex];

    //             // ReSharper disable once PossibleInvalidOperationException
    //             var originalValue = Mathf.Clamp01(itemStat.GetInitialStat(itemCount, ctx).Value);

    //             var cloverCount = ctx.CountItems(ItemCatalog.FindItemIndex("Clover"));
    //             var purityCount = ctx.CountItems(ItemCatalog.FindItemIndex("LunarBadLuck"));
    //             var whiteCloverCount = ctx.CountItems(ItemCatalog.FindItemIndex("WhiteClover"));

    //             var cloverContribution = 1 - Mathf.Pow(1 - originalValue, 1 + cloverCount) - originalValue;

    //             var purityContribution =
    //                 (float) Math.Round(Mathf.Pow(originalValue, 1 + purityCount), 3) - originalValue;

    //             var whiteCloverContribution = 1 - Mathf.Pow(1 - originalValue, 1 + (whiteCloverCount * WhiteClover.luckAmount)) - originalValue;

    //             var stringBuilder = new StringBuilder();

    //             if (cloverCount > 0)
    //             {
    //                 stringBuilder
    //                     .Append(cloverContribution.FormatPercentage(signed: true, color: Colors.ModifierColor))
    //                     .Append(" from Clover");

    //                 if (purityCount > 0 || whiteCloverCount > 0) stringBuilder.AppendLine().Append("  ");
    //             }

    //             if (purityCount > 0)
    //             {
    //                 stringBuilder
    //                     .Append(purityContribution.FormatPercentage(signed: true, color: Colors.ModifierColor))
    //                     .Append(" from Purity");

    //                 if (whiteCloverCount > 0) stringBuilder.AppendLine().Append("  ");
    //             }

    //             if (whiteCloverCount > 0)
    //             {
    //                 stringBuilder
    //                     .Append(whiteCloverContribution.FormatPercentage(signed: true, color: Colors.ModifierColor))
    //                     .Append(" from White Clover");

    //             }

    //             return stringBuilder.ToString();
    //         };

    //     public override Dictionary<ItemIndex, IEnumerable<int>> AffectedItems =>
    //         new Dictionary<ItemIndex, IEnumerable<int>>
    //         {
    //             [ItemCatalog.FindItemIndex("GhostOnKill")] = new[] {1},
    //             [ItemCatalog.FindItemIndex("StunChanceOnHit")] = new[] {0},
    //             [ItemCatalog.FindItemIndex("BleedOnHit")] = new[] {0},
    //             [ItemCatalog.FindItemIndex("GoldOnHit")] = new[] {1},
    //             [ItemCatalog.FindItemIndex("ChainLightning")] = new[] {2},
    //             [ItemCatalog.FindItemIndex("BounceNearby")] = new[] {0},
    //             [ItemCatalog.FindItemIndex("StickyBomb")] = new[] {0},
    //             [ItemCatalog.FindItemIndex("Missile")] = new[] {1},
    //             [ItemCatalog.FindItemIndex("BonusGoldPackOnKill")] = new[] {1},
    //             [ItemCatalog.FindItemIndex("Incubator")] = new[] {0},
    //             [ItemCatalog.FindItemIndex("FireballsOnHit")] = new[] {1}
    //         };
    // }
}