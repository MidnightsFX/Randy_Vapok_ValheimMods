using EpicLoot.ShardStones;

namespace EpicLoot;

public partial class MagicTooltip
{
    // Loose shard preview: shows what the shard would grant if socketed, at its own rarity, listing
    // only the item types that have a defined effect for this shard color.
    private void AddShardPreview()
    {
        var color = Shards.GetShardColor(item);
        var def = Shards.ShardDefinitions.Get(color);
        var showDetails = MagicItem.ShowEffectDetails;

        text.Append("\n");
        if (def == null || (def.UniformEffect == null && def.TypeEffects.Count == 0))
        {
            text.AppendLine($"<color={magicColor}>$mod_epicloot_shard_noeffect</color>");
            return;
        }

        text.AppendLine("$mod_epicloot_shard_ifsocketed:");

        // A uniform shard (e.g. a boss shard) grants one effect on every slot it is allowed into.
        if (def.UniformEffect != null)
        {
            if (def.UniformEffect.ValuesPerRarity.TryGetValue(magicItem.Rarity, out var uniformValue))
            {
                var uniformDef = MagicItemEffectDefinitions.Get(def.UniformEffect.EffectType);
                if (uniformDef != null)
                {
                    var allSlots = Localization.instance.Localize("$mod_epicloot_shard_allslots");
                    var uniformText = MagicItem.GetEffectText(uniformDef, uniformValue);
                    text.AppendLine($"  <color={magicColor}>{allSlots}: {uniformText}</color>");
                    AppendShardEffectDetails(uniformDef.Type, uniformValue, showDetails);
                }
            }

            if (Shards.IsExclusive(def.Category))
            {
                text.AppendLine($"<color={magicColor}>$mod_epicloot_shard_bossexclusive</color>");
            }
            return;
        }

        foreach (var pair in def.TypeEffects)
        {
            var effectDef = pair.Value;
            if (!effectDef.ValuesPerRarity.TryGetValue(magicItem.Rarity, out var value))
            {
                continue;
            }

            var effectMagicDef = MagicItemEffectDefinitions.Get(effectDef.EffectType);
            if (effectMagicDef == null)
            {
                continue;
            }

            var typeName = Shards.GetCategoryDisplayName(pair.Key);
            var effectText = MagicItem.GetEffectText(effectMagicDef, value);
            text.AppendLine($"  <color={magicColor}>{typeName}: {effectText}</color>");
            AppendShardEffectDetails(effectMagicDef.Type, value, showDetails);
        }
    }

    // Appends the dim, indented detail block (description + config) under a previewed shard effect
    // when Shift is held. A shard grants a fixed value per rarity, so the range line is suppressed by
    // passing a single-value override; only the description and any config lines are shown.
    private void AppendShardEffectDetails(string effectType, float value, bool showDetails)
    {
        if (!showDetails)
        {
            return;
        }

        var fixedValue = new MagicItemEffectDefinition.ValueDef { MinValue = value, MaxValue = value, Increment = 0 };
        var block = MagicItem.GetEffectDetailBlock(new MagicItemEffect(effectType, value), magicItem.Rarity, null, fixedValue, "     ");
        if (block.Length > 0)
        {
            text.Append($"<color=#c0c0c0ff>{block}</color>");
        }
    }
}
