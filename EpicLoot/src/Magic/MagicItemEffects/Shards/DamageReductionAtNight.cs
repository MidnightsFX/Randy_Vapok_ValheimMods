using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Black chest shard: scale down incoming damage while it is night (EnvMan.IsNight()). Shard values
    // are authored as whole-number percents, hence the 0.01f; the reduction is clamped to 0-100%.
    public static class DamageReductionAtNight
    {
        // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier).
        public static void ModifyIncoming(Character __instance, HitData hit)
        {
            if (hit == null || __instance != Player.m_localPlayer || !EnvMan.IsNight())
            {
                return;
            }

            var reduction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DamageReductionAtNight, 0.01f);
            if (reduction > 0f)
            {
                hit.m_damage.Modify(1f - Mathf.Clamp01(reduction));
            }
        }
    }
}
