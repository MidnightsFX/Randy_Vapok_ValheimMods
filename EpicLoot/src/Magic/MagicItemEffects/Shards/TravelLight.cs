using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Peach shoulder shard ("Travel Light", took the slot over from BatteringRam). A mobility tradeoff: the player
    // moves faster but can carry less -- the inverse of the rest of the Peach (Weight) family, which rewards
    // a full pack.
    //
    //  * Move speed -- a Postfix on SEMan.ApplyStatusEffectSpeedMods, the same hook vanilla's own speed
    //    status effects use (and the sibling BurningSpeed shard), so it applies to walk/run/swim alike and
    //    the player gets exactly the advertised percent.
    //  * Carry weight -- subtracted in SEMan.ModifyMaxCarryWeight alongside AddCarryWeight/NightCarryWeight.
    //    The cost is CarryWeightPerValue per point of shard value, so the single rarity ramp drives both
    //    numbers: 3/6/9/12/15% speed for -30/-60/-90/-120/-150 carry weight across Magic..Mythic.
    //
    // Shard values are authored as whole-number percents, hence the 0.01f on the speed read.
    public static class TravelLight
    {
        // Max carry weight removed per 1 point of shard value (i.e. per 1% of move speed gained).
        private const float CarryWeightPerValue = 10f;

        // Backstop so stacked sources can't push the carry cap to zero or below.
        private const float MinResultingCarryWeight = 50f;

        // Tooltip: "+{0}% Move Speed, -{1} Carry Weight" -- {1} is derived from the rolled value so the
        // shown cost stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.TravelLight,
                value => new object[] { value, value * CarryWeightPerValue });
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player)
                {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.TravelLight, 0.01f);
                if (bonus > 0f)
                {
                    speed *= 1f + bonus;
                }
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch
        {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float limit)
            {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player)
                {
                    return;
                }

                var reduction = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.TravelLight, CarryWeightPerValue);
                if (reduction > 0f)
                {
                    limit = Mathf.Max(limit - reduction, MinResultingCarryWeight);
                }
            }
        }
    }
}
