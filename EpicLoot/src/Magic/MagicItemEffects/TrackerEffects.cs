using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
    public class TrackerEffects_Humanoid_UpdateEquipmentStatusEffects_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Humanoid __instance)
        {
            if (__instance is not Player player)
            {
                return;
            }

            EquipmentEffectCache.Reset(player);
            Apply(player, MagicEffectType.Andvaranaut, "Andvaranaut");
            Apply(player, MagicEffectType.SkuldsThread, "SkuldsThread");
        }

        // From the item prefab rather than ObjectDB.GetStatusEffect: these status effects are only added to
        // the main menu's ObjectDB, and the world's ObjectDB never gets them.
        private static void Apply(Player player, string effectType, string itemPrefabName)
        {
            if (!player.HasActiveMagicEffect(effectType, out float _))
            {
                return;
            }

            var statusEffect = ObjectDB.instance.GetItemPrefab(itemPrefabName)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_equipStatusEffect;
            if (statusEffect == null || player.m_equipmentStatusEffects.Contains(statusEffect))
            {
                return;
            }

            player.m_equipmentStatusEffects.Add(statusEffect);
            player.m_seman.AddStatusEffect(statusEffect);
        }
    }
}
