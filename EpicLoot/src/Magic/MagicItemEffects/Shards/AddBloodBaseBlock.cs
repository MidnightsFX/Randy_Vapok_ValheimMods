using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards 
{
    public class AddBloodBaseBlock 
    {
        public static bool startBlock;

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateBlock))]
        private class BlockState_Patch 
        {
            private static void Prefix(Humanoid __instance, out bool __state) 
            {
                __state = __instance.m_blocking;
            }

            private static void Postfix(Humanoid __instance, bool __state) {
                var player = Player.m_localPlayer;
                if (!player.HasActiveMagicEffect("BloodBaseBlock")) return;
                startBlock = (!__state && __instance.m_blocking); // bundle this into the helper later to do start block effects
                if (startBlock) {
                    HitData hit = new HitData();
                    hit.SetAttacker(player); // self dmg as player. I want to trigger on hit effects.
                                             // Can scrap if its too powerful or jank. I expect this effect to go under utilized.

                    hit.m_damage.m_damage = (player.GetMaxHealth() / 20f); // 5% hardcoded as true damage untyped dmg doesnt run through armor or known resistances
                    hit.m_staggerMultiplier = 0f;
                    player.Damage(hit);
                }
            }
        }

        public static void Apply(ref float baseBlock) 
        {
            var player = Player.m_localPlayer;
            float bloodBaseBlock = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodBaseBlock, 1f); // whole number

            baseBlock += (bloodBaseBlock);
        }
    }


}
