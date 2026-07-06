using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Harvest / gather / economy shard effects. IncreaseHarvestDamage, IncreaseHarvestXPGain,
    // IncreaseMeleeSkills, SpendCoinsToIncreaseDamage, LuckWhileFishing, ChanceDoubleDamage and
    // SailingSpeed are implemented in their own files. The two below complete the group.

    // LightGreen utility shard: a chance, when the local player picks a harvestable (berries, mushrooms,
    // crops, ...), to reel in one bonus unit of the same drop. The bonus is spawned as a networked ItemDrop
    // (mirrors IncreaseDrop.DropExtraItems). Interact runs on the picking client, so the effect is scoped to
    // the picker; gated on the pickable transitioning from un-picked to picked so it fires once per harvest.
    // Shard value is the bonus chance authored as a whole-number percent, hence the 0.01f.
    public static class BountifulHarvest
    {
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Pickable_Interact_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Pickable __instance, out bool __state)
            {
                __state = __instance.GetPicked();
            }

            [UsedImplicitly]
            private static void Postfix(Pickable __instance, Humanoid character, bool repeat, bool __state)
            {
                // Only a fresh pick by the local player (not a held-Use repeat).
                if (repeat || __state || __instance.GetPicked() == false || character != Player.m_localPlayer
                    || __instance.m_itemPrefab == null)
                {
                    return;
                }

                var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BountifulHarvest, 0.01f);
                if (chance <= 0f || Random.value >= chance)
                {
                    return;
                }

                var offset = Random.insideUnitCircle * 0.5f;
                var position = __instance.transform.position + Vector3.up + new Vector3(offset.x, 0f, offset.y);
                var rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
                ItemDrop.OnCreateNew(Object.Instantiate(__instance.m_itemPrefab, position, rotation));
            }
        }
    }

    // LightGreen head shard: consumed potions/meads last longer — extend the duration of the status effect a
    // consumable applies by value%. Player.ConsumeItem adds the item's m_consumeStatusEffect on a successful
    // consume, so a postfix lengthens the just-added effect's time-to-live. Shard value is authored as a
    // whole-number percent, hence the 0.01f.
    public static class PotionEfficacy
    {
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        private static class Player_ConsumeItem_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
            {
                if (!__result || __instance != Player.m_localPlayer
                    || item?.m_shared?.m_consumeStatusEffect == null)
                {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PotionEfficacy, 0.01f);
                if (fraction <= 0f)
                {
                    return;
                }

                var se = __instance.GetSEMan().GetStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash());
                if (se != null && se.m_ttl > 0f)
                {
                    se.m_ttl *= 1f + fraction;
                }
            }
        }
    }
}
