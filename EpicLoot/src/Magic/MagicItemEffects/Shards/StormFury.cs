using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // LightBlue trinket shard: a storm keeps the blood pumping. While one of the game's storm environments is
    // running, the shard pulses a flat amount of adrenaline every TickInterval seconds AND holds the pool
    // where it is -- vanilla's per-frame degen is suppressed for the duration of the storm, so a storm banks
    // adrenaline rather than merely topping up a draining meter.
    //
    // The storm test is StormRider.IsStorm() (environment name), shared with the sibling shard on this same
    // stone. Uses vanilla's adrenaline pool, so it is inert unless the player has a max-adrenaline source --
    // the definition carries ItemHasAdrenaline, granted for this type by
    // ShardEffectDefinitions.AdrenalinePoolEffects because the type name says nothing about adrenaline.
    public static class StormFury
    {
        internal const float TickInterval = 10f;  // seconds between adrenaline pulses

        // What m_adrenalineDegenTimer is held at while suppression is active. Anything above a frame's dt
        // stops the degen branch; keeping it small also caps the leftover grace once the storm ends (or the
        // shard comes off) at ~1 second.
        private const float DegenPin = 1f;

        // Tooltip: "+{0} Adrenaline every {1}s in Storms, No Adrenaline Decay" -- {1} is the interval const
        // so the shown number stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.StormFury,
                value => new object[] { value, TickInterval });
        }

        // One payout tick. Called by StormFuryPulse only after it has confirmed a storm and that the local
        // player has the effect, so value is the gating call's out-value and is not re-read here.
        internal static void Pulse(Player player, float value)
        {
            if (player.IsDead() || player.GetMaxAdrenaline() <= 0f)
            {
                return; // no adrenaline pool -> AddAdrenaline is inert (matches the other adrenaline shards)
            }

            player.AddAdrenaline(value);
        }

        // Adrenaline decay lives inline in Player.UpdateStats(dt):
        //   m_adrenalineDegenTimer -= dt;
        //   if (adrenaline > 0 && m_adrenalineDegenTimer <= 0) AddAdrenaline(-degen * dt);
        // Pinning the timer ahead of that subtraction is the whole suppression. It has to happen every tick
        // rather than once per pulse, because any positive AddAdrenaline resets the timer to the (short)
        // m_adrenalineDegenDelay curve value. Cancelling the negative AddAdrenaline call instead is not an
        // option: that path is indistinguishable from UseAdrenalineAsStamina deliberately spending the pool.
        // Patched by string name because Player has two private UpdateStats overloads.
        [HarmonyPatch(typeof(Player), "UpdateStats", new[] { typeof(float) })]
        private static class UpdateStats_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Player __instance)
            {
                if (__instance != Player.m_localPlayer || !StormRider.IsStorm())
                {
                    return;
                }

                if (!__instance.HasActiveMagicEffect(MagicEffectType.StormFury))
                {
                    return;
                }

                if (__instance.m_adrenalineDegenTimer < DegenPin)
                {
                    __instance.m_adrenalineDegenTimer = DegenPin;
                }
            }
        }
    }

    // Drives the storm pulse from its own DontDestroyOnLoad object, so it survives scene loads, needs no
    // player to exist yet, and costs one scheduled call every ten seconds instead of a per-frame patch.
    // Created once from the plugin Awake. Holds no cross-tick state, so there is nothing to reset when the
    // local player changes.
    internal class StormFuryPulse : MonoBehaviour
    {
        internal static StormFuryPulse instance;

        internal static void Create()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("EL_StormFuryPulse");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<StormFuryPulse>();
        }

        [UsedImplicitly]
        private void Awake()
        {
            instance = this;
            InvokeRepeating(nameof(Pulse), StormFury.TickInterval, StormFury.TickInterval);
        }

        [UsedImplicitly]
        private void Pulse()
        {
            var player = Player.m_localPlayer;
            if (player == null || !StormRider.IsStorm())
            {
                return;
            }

            // Gate on the effect before doing any work: without the shard socketed this pulse is a couple of
            // checks and a return.
            if (!player.HasActiveMagicEffect(MagicEffectType.StormFury, out var value) || value <= 0f)
            {
                return;
            }

            StormFury.Pulse(player, value);
        }
    }
}
