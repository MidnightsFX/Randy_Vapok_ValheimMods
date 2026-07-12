using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EpicLoot
{
    public enum ItemRarity
    {
        Magic,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    [Serializable]
    public class MagicItemEffect
    {
        public const float DefaultValue = 1;

        public int Version = 1;
        public string EffectType { get; set; }
        public float EffectValue;

        public MagicItemEffect(string type, float value = DefaultValue)
        {
            EffectType = type;
            EffectValue = value;
        }
    }

    // A single effect socketed into an item via a Runestone or Shard.
    // Stores enough to apply the effect and to reconstruct the original
    // socketed item (Runestone/Shard) when it is removed from the socket.
    [Serializable]
    public class SocketedEffect
    {
        public int Version = 2;
        public MagicItemEffect Effect;  // null for an inert shard (no effect for the host item type)
        public string SourcePrefab;     // e.g. "EtchedRunestoneEpic" / "Yellow_Epic_ShardStone"
        public ItemRarity SourceRarity; // for tooltip range + reconstruction
        public ShardType ShardType = ShardType.None; // set for shard sockets; None for runestones

        public SocketedEffect()
        {
        }

        public SocketedEffect(MagicItemEffect effect, string sourcePrefab, ItemRarity sourceRarity)
        {
            Effect = effect;
            SourcePrefab = sourcePrefab;
            SourceRarity = sourceRarity;
        }
    }

    [Serializable]
    public class MagicItem
    {
        public int Version = 3;
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public string TypeNameOverride;
        public int AugmentedEffectIndex = -1;
        public List<int> AugmentedEffectIndices = new List<int>();
        public string DisplayName;
        public string LegendaryID;
        public string SetID;
        public bool IsUnidentified = false;
        public int SocketCount = 0;
        public ShardType ShardColor = ShardType.None;
        public List<SocketedEffect> Sockets = new List<SocketedEffect>();

        public string GetItemTypeName(ItemDrop.ItemData baseItem)
        {
            return string.IsNullOrEmpty(TypeNameOverride) ? 
                Localization.instance.Localize(baseItem.m_shared.m_name).ToLowerInvariant() : TypeNameOverride;
        }

        public string GetRarityDisplay()
        {
            var color = GetColorString();
            return $"<color={color}>{EpicLoot.GetRarityDisplayName(Rarity)}</color>";
        }

        // True while the player holds Shift, used to reveal the detailed per-effect tooltip block
        // (description, roll range, config values). Shared by the magic-item and loose-shard tooltips.
        public static bool ShowEffectDetails => ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);

        public string GetTooltip()
        {
            var showRange = ShowEffectDetails;

            var color = GetColorString();
            var tooltip = new StringBuilder();
            tooltip.Append($"\n<color={color}>");
            for (var index = 0; index < Effects.Count; index++)
            {
                var effect = Effects[index];
                var pip = EpicLoot.GetMagicEffectPip(IsEffectAugmented(index));
                if (showRange)
                {
                    // Header without the inline range; the range and other details go in the block below.
                    // Close/reopen the rarity color around the dim detail block rather than nesting tags.
                    tooltip.AppendLine($"{pip} {GetEffectText(effect, Rarity, false, LegendaryID)}");
                    tooltip.Append($"</color><color=#c0c0c0ff>");
                    tooltip.Append(GetEffectDetailBlock(effect, Rarity, LegendaryID, null, "   "));
                    tooltip.Append($"</color><color={color}>");
                }
                else
                {
                    tooltip.AppendLine($"{pip} {GetEffectText(effect, Rarity, false)}");
                }
            }

            tooltip.Append($"</color>");

            if (SocketCount > 0)
            {
                tooltip.AppendLine($"$mod_epicloot_sockets ({GetUsedSocketCount()}/{SocketCount}):");
                foreach (SocketedEffect socket in Sockets)
                {
                    if (socket == null)
                    {
                        continue;
                    }
                    var socketColor = EpicLoot.GetRarityColor(socket.SourceRarity);
                    // Inline the socketed item's own icon, resolved from its source prefab
                    var iconTag = ShardTooltipSprites.GetSpriteTag(socket.SourcePrefab);
                    if (socket.Effect != null)
                    {
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} {GetEffectText(socket.Effect, socket.SourceRarity, false)}</color>");
                        if (showRange)
                        {
                            tooltip.Append($"<color=#c0c0c0ff>");
                            tooltip.Append(GetEffectDetailBlock(socket.Effect, socket.SourceRarity, null, null, "     "));
                            tooltip.Append($"</color>");
                        }
                    }
                    else if (socket.ShardType != ShardType.None)
                    {
                        // An inert shard: socketed but has no defined effect for this item type.
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} $mod_epicloot_shard_noeffect</color>");
                    }
                }
                for (var i = 0; i < GetOpenSocketCount(); i++)
                {
                    tooltip.AppendLine("  ◊<color=#808080> $mod_epicloot_empty_socket</color>");
                }
            }

            tooltip.AppendLine($"$mod_epicloot_itemtooltip_rarity: {GetRarityDisplay()}<pos=75%>" +
                $"$mod_epicloot_itemtooltip_effects: <color={color}>{Effects.Count}</color>");

            return tooltip.ToString();
        }

        public string GetCompactTooltip() {
            var color = GetColorString();
            var tooltip = new StringBuilder();
            tooltip.Append($"<color={color}>");
            for (var index = 0; index < Effects.Count; index++) {
                tooltip.AppendLine($"{GetEffectText(Effects[index], Rarity, true)}");
            }
            tooltip.Append($"</color>");

            return tooltip.ToString();
        }

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(GetColorString(), out Color color))
            {
                return color;
            }
            return Color.white;
        }

        public ShardType GetShardColor()
        {
            return ShardColor;
        }

        public string GetColorString()
        {
            return EpicLoot.GetRarityColor(Rarity);
        }

        // Socket helpers
        public int GetUsedSocketCount() => Sockets.Count;
        public int GetOpenSocketCount() => Mathf.Max(0, SocketCount - Sockets.Count);
        public bool HasSockets() => SocketCount > 0;
        public bool HasOpenSocket() => GetOpenSocketCount() > 0;
        public IEnumerable<MagicItemEffect> GetSocketedEffects() => Sockets.Where(x => x?.Effect != null).Select(x => x.Effect);

        // includeSocketed defaults to false so all crafting/bookkeeping reads (loot roll, augment,
        // rune-extract, disenchant, requirements/exclusivity, names) stay rolled-only. Effect
        // application and tooltip reads pass includeSocketed: true.
        public List<MagicItemEffect> GetEffects(string effectType = null, bool includeSocketed = false)
        {
            IEnumerable<MagicItemEffect> source = Effects;
            if (includeSocketed && Sockets.Count > 0)
            {
                source = source.Concat(GetSocketedEffects());
            }
            return effectType == null ? source.ToList() : source.Where(x => x.EffectType == effectType).ToList();
        }

        // includeSocketed defaults to true here: GetTotalEffectValue is only called from effect
        // application (per-item patches) and tooltip display, never from crafting/bookkeeping, so
        // socketed effects should always count toward the aggregate value.
        public float GetTotalEffectValue(string effectType, float scale = 1.0f, bool includeSocketed = true)
        {
            return GetEffects(effectType, includeSocketed).Sum(x => x.EffectValue) * scale;
        }

        public bool HasEffect(string effectType, bool checkHealthCritical = false, bool includeSocketed = false)
        {
            if (Effects.Exists(x => x.EffectType == effectType))
            {
                return true;
            }
            if (includeSocketed && Sockets.Exists(x => x?.Effect != null && x.Effect.EffectType == effectType))
            {
                return true;
            }
            return checkHealthCritical && HasEffect(ModifyWithLowHealth.EffectNameWithLowHealth(effectType), false, includeSocketed);
        }

        public bool HasAnyEffect(IEnumerable<string> effectTypes)
        {
            return Effects.Any(x => effectTypes.Contains(x.EffectType));
        }

        public bool CanBeDisenchanted()
        {
            return !Effects.Any(x => !MagicItemEffectDefinitions.Get(x.EffectType)?.CanBeDisenchanted ?? false);
        }

        public static string GetEffectText(MagicItemEffectDefinition effectDef, float value)
        {
            var localizedDisplayText = Localization.instance.Localize(effectDef.DisplayText);
            var result = string.Format(localizedDisplayText, value);
            return result;
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity,
            bool showRange, string legendaryID, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            var result = GetEffectText(effectDef, effect.EffectValue);
            var values = valuesOverride ?? (string.IsNullOrEmpty(legendaryID) ?
                effectDef.GetValuesForRarity(rarity) :
                UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, effect.EffectType));
            if (showRange && values != null)
            {
                if (!Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $" [{values.MinValue}-{values.MaxValue}]";
                }
            }
            return result;
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity,
            bool showRange, string legendaryID = null)
        {
            return GetEffectText(effect, rarity, showRange, legendaryID, null);
        }

        public static string GetEffectText(MagicItemEffect effect, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            return GetEffectText(effect, ItemRarity.Legendary, false, null, valuesOverride);
        }

        // Builds the indented, per-effect detail lines shown under an effect when Shift is held: the
        // localized Description (with the X marker filled in with this item's rolled value), the roll
        // range on its own line, and any Config values with human-readable labels. Every piece is
        // optional and omitted when the effect has no data for it, so an effect with none of them
        // contributes nothing. Returns raw text (no color tags); callers wrap it in a dim color.
        public static string GetEffectDetailBlock(MagicItemEffect effect, ItemRarity rarity,
            string legendaryID, MagicItemEffectDefinition.ValueDef valuesOverride, string indent)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (effectDef == null)
            {
                return string.Empty;
            }

            var values = valuesOverride ?? (string.IsNullOrEmpty(legendaryID) ?
                effectDef.GetValuesForRarity(rarity) :
                UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, effect.EffectType));

            var block = new StringBuilder();

            if (!string.IsNullOrEmpty(effectDef.Description))
            {
                var description = Localization.instance.Localize(effectDef.Description);
                description = ApplyValueToDescription(description, effect.EffectValue);
                block.Append($"{indent}{description}\n");
            }

            if (values != null && !Mathf.Approximately(values.MinValue, values.MaxValue))
            {
                block.Append($"{indent}$mod_epicloot_detail_range: [{values.MinValue}-{values.MaxValue}]\n");
            }

            if (effectDef.Config != null && effectDef.Config.Count > 0)
            {
                foreach (var kvp in effectDef.Config)
                {
                    block.Append($"{indent}{effectDef.GetConfigLabel(kvp.Key)}: {kvp.Value:0.###}\n");
                }
            }

            return block.ToString();
        }

        // Replaces the standard "X" value marker embedded in a localized Description with this item's
        // rolled value, preserving the marker's styling. The handful of non-standard markers (X/2, X%)
        // don't match and stay generic.
        private static string ApplyValueToDescription(string localizedDescription, float value)
        {
            return localizedDescription.Replace("<b><color=yellow>X</color></b>",
                $"<b><color=yellow>{value:0.#}</color></b>");
        }

        public void ReplaceEffect(int index, MagicItemEffect newEffect)
        {
            if (index < 0 || index >= Effects.Count)
            {
                EpicLoot.LogError("Tried to replace effect on magic item outside of the range of the effects list!");
                return;
            }

            SetEffectAsAugmented(index);

            Effects[index] = newEffect;
        }

        public bool HasBeenAugmented()
        {
            return AugmentedEffectIndex >= 0 && AugmentedEffectIndex < Effects.Count || AugmentedEffectIndices.Count > 0;
        }

        public bool IsEffectAugmented(int index)
        {
            return AugmentedEffectIndex == index || AugmentedEffectIndices.Contains(index);
        }

        public void SetEffectAsAugmented(int index)
        {
            if (AugmentedEffectIndex == index)
            {
                return;
            }

            if (!IsEffectAugmented(index))
            {
                AugmentedEffectIndices.Add(index);
            }
        }

        public int GetAugmentCount()
        {
            var old = AugmentedEffectIndex >= 0 ? 1 : 0;
            return old + AugmentedEffectIndices.Count;
        }

        public bool IsUniqueLegendary()
        {
            return !string.IsNullOrEmpty(LegendaryID);
        }

        public LegendaryInfo GetLegendaryInfo()
        {
            if (IsUniqueLegendary())
            {
                UniqueLegendaryHelper.TryGetLegendaryInfo(LegendaryID, out var itemInfo);
                return itemInfo;
            }

            return null;
        }

        public string GetFirstEquipEffect(out FxAttachMode mode)
        {
            foreach (var effect in Effects)
            {
                var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
                if (effectDef != null && !string.IsNullOrEmpty(effectDef.EquipFx))
                {
                    mode = effectDef.EquipFxMode;
                    return effectDef.EquipFx;
                }
            }

            mode = FxAttachMode.None;
            return null;
        }

        public bool IsLegendarySetItem()
        {
            return !string.IsNullOrEmpty(SetID);
        }
    }
}
