using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards
{
    // White weapon shard: +% weapon damage while it is daytime (EnvMan.IsDay()). Applied per-weapon
    // (mirrors ModifyDamage's ForWeapon effects) so an off-hand shard does not leak onto the main
    // hand when dual-wielding. Shard values are authored as whole-number percents, hence the 0.01f.
    public static class IncreaseDamageDuringDaytime
    {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            var player = Player.m_localPlayer;
            if (player == null || !EnvMan.IsDay() || !player.IsItemEquiped(__instance))
            {
                return;
            }

            var bonus = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, __instance, MagicEffectType.IncreaseDamageDuringDaytime, 0.01f);
            if (bonus != 0f)
            {
                __result.Modify(1f + bonus);
            }
        }
    }
}
