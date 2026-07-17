using EpicLoot.General;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // DarkRed legs shard (replaces the old duplicate BulkUp). A blood-magic tradeoff: it lowers the
    // player's maximum health but returns a share of the damage they deal as lifesteal.
    //
    //  * Max-health cost -- subtracted from the food-pool HP the same way IncreaseHealth adds to it
    //    (GetTotalFoodValue drives Player.SetMaxHealth; GetBaseFoodHP keeps the HUD's base segment in
    //    sync). The shard value is the lifesteal percent; the health cost is MaxHealthPerPercent x that
    //    (5 -> -15/-30/-45/-60/-75 across Magic..Mythic). Floored so it can never pull max health to a
    //    lethal value on an unfed player (vanilla base is 25).
    //  * Lifesteal -- OnDamageDealt is invoked attacker-side from SharedCharacterDamagePatch's post-damage
    //    pass, healing value% of the damage dealt. Unlike weapon LifeSteal this is an armor effect, so it
    //    is not gated on the weapon being magical.
    //
    // Shard values are authored as whole-number percents, hence the 0.01f on the lifesteal read.
    public static class BloodDrinker
    {
        // Max health removed per 1% of lifesteal granted. With the shard's 3/6/9/12/15 values this yields
        // -15/-30/-45/-60/-75 max health across Magic..Mythic.
        private const float MaxHealthPerPercent = 5f;

        // Never let the reduction pull max health below this, so equipping on an unfed character
        // (vanilla base 25) can't produce a lethal/zero max-health state.
        private const float MinResultingMaxHealth = 10f;

        private static void ApplyMaxHealthReduction(Player player, ref float hp)
        {
            if (player != Player.m_localPlayer)
            {
                return;
            }

            var reduction = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodDrinker) * MaxHealthPerPercent;
            if (reduction <= 0f)
            {
                return;
            }

            hp = Mathf.Max(hp - reduction, MinResultingMaxHealth);
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side).
        public static void OnDamageDealt(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodDrinker, 0.01f);
            if (fraction <= 0f)
            {
                return;
            }

            var heal = hit.m_damage.EpicLootGetTotalDamage() * fraction;
            if (heal > 0f)
            {
                player.Heal(heal);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        public static class Player_GetTotalFoodValue_Patch
        {
            public static void Postfix(Player __instance, ref float hp)
            {
                ApplyMaxHealthReduction(__instance, ref hp);
            }
        }

        // Keeps the HUD's base-health segment in sync with the reduced pool (mirrors BulkUp / IncreaseHealth).
        [HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
        public static class Player_GetBaseFoodHP_Patch
        {
            public static void Postfix(Player __instance, ref float __result)
            {
                ApplyMaxHealthReduction(__instance, ref __result);
            }
        }
    }
}
