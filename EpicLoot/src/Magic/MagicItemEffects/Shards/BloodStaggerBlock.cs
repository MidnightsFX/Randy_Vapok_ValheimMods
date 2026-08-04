using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards 
{
    public static class BloodStaggerBlock 
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateBlock))]
        private class BlockState_Patch {
            static GameObject sfx = null;
            static GameObject vfx = null;
            private static void Postfix(Humanoid __instance) {
                var player = Player.m_localPlayer;
                if (!player.HasActiveMagicEffect("BloodStaggerBlock")) return;
                if (!__instance.IsBlocking() || __instance.m_blockTimer != 0f) return; // bundle this into the helper later to do start block effects

                HitData hit = new HitData();
                hit.SetAttacker(player); // self dmg as player. I want to trigger on hit effects.
                                         // Can scrap if its too powerful or jank. I expect this effect to go under utilized.

                hit.m_damage.m_damage = (player.GetMaxHealth() / 20f); // 5% hardcoded as true damage untyped dmg doesnt run through armor or known resistances
                hit.m_staggerMultiplier = 0f;

                // addtions to validate hit

                hit.m_point = player.GetCenterPoint();
                hit.m_dir = Vector3.zero;
                hit.m_hitType = HitData.HitType.Self;
                hit.m_ignorePVP = true; // required to self dmg

                //

                player.Damage(hit);
                if (sfx == null) {
                    sfx = PrefabManager.Instance.GetPrefab("sfx_hit");
                }
                if (vfx == null) {
                    vfx = PrefabManager.Instance.GetPrefab("vfx_BloodHit");
                }
                if (sfx != null) {
                    GameObject.Instantiate(sfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
                }
                if (vfx != null) {
                    GameObject.Instantiate(vfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        public class BloodStaggerBlock_StaggerReduction_Patch 
        {
            private static void Prefix(Character __instance, ref float damage) 
            {
                if (Player.m_localPlayer.IsBlocking()) 
                {
                    damage *= (1f - Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BloodStaggerBlock, .01f));
                }
            }
        }
    }


}
