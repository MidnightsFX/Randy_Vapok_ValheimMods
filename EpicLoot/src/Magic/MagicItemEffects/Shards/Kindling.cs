using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Orange legs shard ("Kindling", took the slot over from Trailblazer): the fire you soak stokes your
    // second wind -- restore a flat amount of stamina each time the cumulative fire damage the local player
    // has taken crosses a threshold. Pairs with the rest of the Orange family, which rewards being on fire
    // (see the sibling BurningSpeed shoulder shard).
    //
    // Hook choice: this patches Character.ApplyDamage, NOT either of the damage dispatchers. Vanilla
    // RPC_Damage zeroes hit.m_damage.m_fire and hands the fire to SE_Burning instead (Character.RPC_Damage
    // -> AddFireDamage), and SE_Burning then ticks that damage straight through Character.ApplyDamage,
    // bypassing Character.Damage/RPC_Damage entirely -- the same DoT routing already noted on
    // IncreaseAllPoisonDamageDone and ChanceToCritOnHit. A handler on a dispatcher would therefore see zero
    // fire for every real burn. ApplyDamage also means only fire that actually cost health counts: a burn
    // doused by water, or one too small for SE_Burning to accept, never reaches here.
    //
    // Shard values are the flat stamina granted per trigger, so there is no percent scale on the read.
    public static class Kindling
    {
        // Fire damage taken per stamina trigger. Tunable; higher = a slower trickle.
        private const float FireDamagePerTrigger = 75f;

        // Tooltip: "Restore {0} Stamina per {1} Fire Damage Taken" -- {1} is the FireDamagePerTrigger const
        // so the shown threshold stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Kindling,
                value => new object[] { value, FireDamagePerTrigger });
        }

        // Cumulative fire damage the local player has taken with the effect active but not yet paid out as
        // stamina. Carries the sub-threshold remainder across burn ticks.
        private static float _accumulatedFireDamage;

        // Postfix rather than prefix so the read happens after vanilla applies Game.m_localDamgeTakenRate to
        // the hit in place -- we accumulate the fire damage the player actually suffered.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class ApplyDamage_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit)
            {
                var player = Player.m_localPlayer;
                if (__instance != player || player.IsDead() || hit == null || hit.m_damage.m_fire <= 0f)
                {
                    return;
                }

                var staminaPerTrigger = player.GetTotalActiveMagicEffectValue(MagicEffectType.Kindling);
                if (staminaPerTrigger <= 0f)
                {
                    return;
                }

                _accumulatedFireDamage += hit.m_damage.m_fire;
                if (_accumulatedFireDamage < FireDamagePerTrigger)
                {
                    return;
                }

                var triggers = (int)(_accumulatedFireDamage / FireDamagePerTrigger);
                _accumulatedFireDamage -= triggers * FireDamagePerTrigger;
                player.AddStamina(triggers * staminaPerTrigger);
            }
        }
    }
}
