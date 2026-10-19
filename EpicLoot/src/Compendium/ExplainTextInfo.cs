using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EpicLoot.Crafting;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLoot.Compendium;

public class ExplainTextInfo(string topic) : MagicTextInfo(topic)
{
    private const string ValueToken = "<b><color=yellow>X</color></b>";
    private const string Separator = " <color=#808080ff>/</color> ";
    private const string Dash = "<color=#808080ff>-</color>";

    private static readonly Regex RichTextTag = new("<[^>]*>", RegexOptions.Compiled);

    private static readonly ItemRarity[] Rarities =
    {
        ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic, ItemRarity.Ancient,
    };

    private class Entry
    {
        public MagicItemEffectDefinition Definition;
        public string Text;
        public string SortKey;
    }

    private enum SourceKind
    {
        Legendary,
        Set,
        Shard,
    }

    /// <summary>Something other than a loot roll that puts an effect on an item.</summary>
    private class Source
    {
        public SourceKind Kind;
        public string Name;
        public int Count;
        public List<string> Slots = new();
        public List<ItemRarity> Rarities;

        public string Label => Kind switch
        {
            SourceKind.Set => Format("$mod_epicloot_effectlist_setbonus", Name, Count),
            SourceKind.Shard when Slots.Count > 0 => $"{Name} ({string.Join(", ", Slots)})",
            _ => Name,
        };
    }

    public override void Build(MagicPages instance)
    {
        Dictionary<string, List<Source>> grantedBy = GetGrantedBy();
        Dictionary<string, List<Entry>> byCategory = MagicItemEffectDefinitions.AllDefinitions.Values
            .Where(x => !EnchantCostsHelper.EffectIsDeprecated(x))
            .Select(x => new Entry
            {
                Definition = x,
                Text = MagicItem.GetEffectTextGeneric(x, ValueToken),
                SortKey = RichTextTag.Replace(Localization.instance.Localize(MagicItem.GetEffectTextGeneric(x, "X")), "")
                    .TrimStart('+', '-', ' '),
            })
            .GroupBy(x => EffectGroupsSection.GetCategory(x.Definition.Type))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortKey).ToList());

        foreach (string category in EffectGroupsSection.CategoryNames)
        {
            if (!byCategory.TryGetValue(category, out List<Entry> entries))
            {
                continue;
            }

            instance.MagicPagesTextArea.Add("");
            instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>{category}</size>");
            MagicTextTable table = instance.MagicPagesTextArea.AddTable(
                new MagicTableColumn { Header = "$mod_epicloot_effectlist_col_effect", Weight = 0.44f, Outline = true },
                new MagicTableColumn { Header = "$mod_epicloot_effectlist_col_items", Weight = 0.38f },
                new MagicTableColumn { Header = "$mod_epicloot_effectlist_col_rarity", Weight = 0.18f, Alignment = TextAnchor.MiddleRight });

            foreach (Entry entry in entries)
            {
                MagicItemEffectDefinition definition = entry.Definition;
                grantedBy.TryGetValue(definition.Type, out List<Source> sources);
                List<ItemRarity> rarities = GetRarities(definition, sources);
                string[] cells =
                {
                    MagicEffectRarity.Decorate(definition, entry.Text, false),
                    DescribeItems(definition, sources),
                    DescribeOrigin(definition, rarities, sources),
                };
                table.AddRow(cells, GetDetail(definition, rarities, sources), false);
            }

            table.Finish();
        }
    }

    private static List<string> GetDetail(MagicItemEffectDefinition definition, List<ItemRarity> rarities,
        List<Source> sources)
    {
        MagicItemEffectRequirements requirements = definition.Requirements ?? new MagicItemEffectRequirements();
        List<string> detail = new();

        if (!string.IsNullOrEmpty(definition.Description))
        {
            detail.Add(Grey(MagicItem.GetEffectDescriptionGeneric(definition, ValueToken)));
        }

        string ranges = DescribeRanges(definition, rarities);
        if (ranges.Length > 0)
        {
            detail.Add(Grey($"$mod_epicloot_detail_range: {ranges}"));
        }

        List<string> excluded = new();
        if (requirements.ExcludedItemTypes?.Count > 0)
        {
            excluded.AddRange(requirements.ExcludedItemTypes.Distinct().Select(SetDescriptions.DescribeItemType));
        }

        if (requirements.ExcludedSkillTypes?.Count > 0)
        {
            excluded.AddRange(requirements.ExcludedSkillTypes.Distinct().Select(SetDescriptions.DescribeSkill));
        }

        if (requirements.ExcludedItemNames?.Count > 0)
        {
            excluded.AddRange(requirements.ExcludedItemNames.Distinct().Select(SetDescriptions.DescribeItemName));
        }

        if (excluded.Count > 0)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_notfor", string.Join(", ", excluded.Distinct()))));
        }

        List<string> properties = DescribeProperties(requirements);
        if (properties.Count > 0)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_requires", string.Join(", ", properties))));
        }

        if (requirements.MustHaveEffectTypes?.Count > 0)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_needs",
                string.Join(", ", requirements.MustHaveEffectTypes.Select(GetEffectName)))));
        }

        List<string> exclusive = (requirements.ExclusiveEffectTypes ?? new List<string>())
            .Where(x => x != definition.Type).Select(GetEffectName).ToList();
        if (exclusive.Count > 0)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_excludes", string.Join(", ", exclusive))));
        }

        if (sources != null)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_grantedby", string.Join(", ", sources.Select(x => x.Label)))));
        }

        if (definition.Config != null)
        {
            foreach (KeyValuePair<string, float> kvp in definition.Config)
            {
                detail.Add(Grey($"{definition.GetConfigLabel(kvp.Key)}: {kvp.Value:0.###}"));
            }
        }

        List<string> flags = new();
        if (requirements.NoRoll)
        {
            flags.Add("$mod_epicloot_effectlist_noroll");
        }
        else if (!ModifyWithLowHealth.MayRoll(definition.Type))
        {
            flags.Add("$mod_epicloot_effectlist_disabled");
        }

        if (!definition.CanBeAugmented)
        {
            flags.Add("$mod_epicloot_effectlist_noaugment");
        }

        if (!definition.CanBeRunified)
        {
            flags.Add("$mod_epicloot_effectlist_norune");
        }

        if (!definition.CanBeDisenchanted)
        {
            flags.Add("$mod_epicloot_effectlist_nodisenchant");
        }

        if (flags.Count > 0)
        {
            string text = Localization.instance.Localize(string.Join(", ", flags));
            detail.Add(Grey(char.ToUpperInvariant(text[0]) + text.Substring(1)));
        }

        if (!requirements.NoRoll)
        {
            detail.Add(Grey(Format("$mod_epicloot_effectlist_rollweight", definition.SelectionWeight.ToString("0.###"))));
        }

        return detail;
    }

    /// <summary>
    /// What the effect can be found on: the item types it rolls on, or for one that never rolls, the sets
    /// and legendaries that grant it.
    /// </summary>
    private static string DescribeItems(MagicItemEffectDefinition definition, List<Source> sources)
    {
        MagicItemEffectRequirements requirements = definition.Requirements;
        if ((requirements?.NoRoll ?? false) && sources != null)
        {
            return string.Join(", ", sources.Select(x => x.Kind == SourceKind.Shard ? x.Label : x.Name).Distinct());
        }

        if (requirements == null)
        {
            return "$mod_epicloot_setrune_anyitem";
        }

        List<string> parts = new();
        if (requirements.AllowedItemTypes?.Count > 0)
        {
            parts.AddRange(requirements.AllowedItemTypes.Select(SetDescriptions.DescribeItemType));
        }

        if (requirements.AllowedItemNames?.Count > 0)
        {
            parts.AddRange(requirements.AllowedItemNames.Select(SetDescriptions.DescribeItemName));
        }

        string skills = requirements.AllowedSkillTypes?.Count > 0
            ? string.Join(", ", requirements.AllowedSkillTypes.Distinct().Select(SetDescriptions.DescribeSkill))
            : "";

        if (parts.Count == 0)
        {
            return skills.Length > 0 ? skills : "$mod_epicloot_setrune_anyitem";
        }

        // Localized before the Distinct so the two two-handed types, which share a display name, collapse to one.
        string text = string.Join(", ", parts.Select(x => Localization.instance.Localize(x)).Distinct());
        return skills.Length > 0 ? $"{text} ({skills})" : text;
    }

    /// <summary>The rarities the effect rolls at, or for one that never rolls, those its sources exist at.</summary>
    private static List<ItemRarity> GetRarities(MagicItemEffectDefinition definition, List<Source> sources)
    {
        MagicItemEffectRequirements requirements = definition.Requirements;
        if ((requirements?.NoRoll ?? false) && sources != null)
        {
            return Rarities.Where(r => sources.Any(x => x.Rarities.Contains(r))).ToList();
        }

        IEnumerable<ItemRarity> rarities = Rarities;
        if (requirements?.AllowedRarities?.Count > 0)
        {
            rarities = rarities.Where(requirements.AllowedRarities.Contains);
        }

        if (requirements?.ExcludedRarities?.Count > 0)
        {
            rarities = rarities.Where(x => !requirements.ExcludedRarities.Contains(x));
        }

        return rarities.ToList();
    }

    /// <summary>
    /// Where the effect comes from: the rarities it rolls at, or for one that never rolls, the sets and
    /// the rarities of the legendaries that grant it.
    /// </summary>
    private static string DescribeOrigin(MagicItemEffectDefinition definition, List<ItemRarity> rarities, List<Source> sources)
    {
        if (!(definition.Requirements?.NoRoll ?? false))
        {
            return DescribeRarities(rarities);
        }

        if (sources == null)
        {
            return Dash;
        }

        List<string> parts = new();
        if (sources.Any(x => x.Kind == SourceKind.Shard))
        {
            parts.Add("$mod_epicloot_effectlist_shard");
        }

        if (sources.Any(x => x.Kind == SourceKind.Set))
        {
            parts.Add("$mod_epicloot_effectlist_set");
        }

        List<ItemRarity> granted = Rarities
            .Where(r => sources.Any(x => x.Kind == SourceKind.Legendary && x.Rarities.Contains(r)))
            .ToList();
        if (granted.Count > 0)
        {
            parts.Add(DescribeRarities(granted));
        }

        return string.Join(", ", parts);
    }

    private static string DescribeRarities(List<ItemRarity> rarities)
    {
        if (rarities.Count == Rarities.Length)
        {
            return "$mod_epicloot_effectlist_any";
        }

        bool contiguousToTop = rarities.Count > 0 &&
            rarities.SequenceEqual(Rarities.Skip(System.Array.IndexOf(Rarities, rarities[0])));
        if (contiguousToTop)
        {
            return Format("$mod_epicloot_effectlist_andup", Colored(rarities[0], EpicLoot.GetRarityDisplayName(rarities[0])));
        }

        return SetDescriptions.FormatRarities(rarities);
    }

    private static string DescribeRanges(MagicItemEffectDefinition definition, List<ItemRarity> rarities)
    {
        List<string> parts = new();
        foreach (ItemRarity rarity in rarities)
        {
            MagicItemEffectDefinition.ValueDef values = definition.ValuesPerRarity?.GetValueDefForRarity(rarity);
            if (values == null)
            {
                continue;
            }

            string range = Mathf.Approximately(values.MinValue, values.MaxValue)
                ? $"{values.MinValue:0.##}"
                : $"{values.MinValue:0.##}-{values.MaxValue:0.##}";
            parts.Add(Colored(rarity, range));
        }

        return string.Join(Separator, parts);
    }

    private static List<string> DescribeProperties(MagicItemEffectRequirements r)
    {
        List<string> parts = new();
        Describe(parts, r.ItemHasPhysicalDamage, "physicaldamage");
        Describe(parts, r.ItemHasElementalDamage, "elementaldamage");
        Describe(parts, r.ItemHasChopDamage, "chopdamage");
        Describe(parts, r.ItemUsesDurability, "durability");
        Describe(parts, r.ItemHasNegativeMovementSpeedModifier, "movementpenalty");
        Describe(parts, r.ItemHasBlockPower, "blockpower");
        Describe(parts, r.ItemHasParryPower, "parrypower");
        Describe(parts, r.ItemHasNoParryPower.HasValue ? !r.ItemHasNoParryPower.Value : null, "parrypower");
        Describe(parts, r.ItemHasArmor, "armor");
        Describe(parts, r.ItemHasBackstabBonus, "backstab");
        Describe(parts, r.ItemUsesStaminaOnAttack, "attackstamina");
        Describe(parts, r.ItemUsesEitrOnAttack, "attackeitr");
        Describe(parts, r.ItemUsesHealthOnAttack, "attackhealth");
        Describe(parts, r.ItemUsesDrawStaminaOnAttack, "drawstamina");
        Describe(parts, r.ItemGivesAdrenaline, "givesadrenaline");
        Describe(parts, r.ItemHasAdrenaline, "adrenaline");
        return parts;
    }

    private static void Describe(List<string> parts, bool? required, string property)
    {
        if (!required.HasValue)
        {
            return;
        }

        string text = $"$mod_epicloot_effectlist_req_{property}";
        parts.Add(required.Value ? text : Format("$mod_epicloot_effectlist_without", text));
    }

    private static Dictionary<string, List<Source>> GetGrantedBy()
    {
        Dictionary<string, List<Source>> result = new();
        foreach (LegendaryInfo legendary in UniqueLegendaryHelper.AllUniques.Values.Concat(UniqueLegendaryHelper.MythicInfo.Values))
        {
            foreach (GuaranteedMagicEffect effect in legendary.GuaranteedMagicEffects ?? new List<GuaranteedMagicEffect>())
            {
                Add(result, effect.Type, new Source
                {
                    Kind = SourceKind.Legendary,
                    Name = legendary.Name,
                    Rarities = UniqueLegendaryHelper.GetRarities(legendary),
                });
            }
        }

        foreach (LegendarySetInfo set in UniqueLegendaryHelper.AllSets.Values.Concat(UniqueLegendaryHelper.MythicSets.Values))
        {
            foreach (SetBonusInfo bonus in set.SetBonuses ?? new List<SetBonusInfo>())
            {
                if (bonus.Effect != null)
                {
                    Add(result, bonus.Effect.Type, new Source
                    {
                        Kind = SourceKind.Set,
                        Name = set.Name,
                        Count = bonus.Count,
                        Rarities = UniqueLegendaryHelper.GetRarities(set),
                    });
                }
            }
        }

        foreach (KeyValuePair<ShardType, ShardDefinition> pair in Shards.ShardDefinitions.ShardEffects)
        {
            ShardDefinition shard = pair.Value;
            if (pair.Key == ShardType.None || shard == null)
            {
                continue;
            }

            string name = $"<color={Shards.GetShardNameColor(pair.Key)}>$mod_epicloot_shard_{pair.Key} $mod_epicloot_assets_shardstone</color>";
            if (shard.UniformEffect != null)
            {
                AddShard(result, shard.UniformEffect, name, shard, null);
            }

            foreach (KeyValuePair<ShardSlotCategory, ShardEffectDefinition> slot in shard.TypeEffects ?? new Dictionary<ShardSlotCategory, ShardEffectDefinition>())
            {
                AddShard(result, slot.Value, name, shard, Shards.GetCategoryDisplayName(slot.Key));
            }
        }

        return result;
    }

    private static void AddShard(Dictionary<string, List<Source>> result, ShardEffectDefinition effect, string name,
        ShardDefinition shard, string slot)
    {
        if (effect == null || string.IsNullOrEmpty(effect.EffectType))
        {
            return;
        }

        Source source = result.TryGetValue(effect.EffectType, out List<Source> list)
            ? list.FirstOrDefault(x => x.Kind == SourceKind.Shard && x.Name == name)
            : null;
        if (source == null)
        {
            source = new Source { Kind = SourceKind.Shard, Name = name, Rarities = shard.Rarities ?? new List<ItemRarity>() };
            Add(result, effect.EffectType, source);
        }

        if (slot != null && !source.Slots.Contains(slot))
        {
            source.Slots.Add(slot);
        }
    }

    private static void Add(Dictionary<string, List<Source>> result, string effectType, Source source)
    {
        if (string.IsNullOrEmpty(effectType) || string.IsNullOrEmpty(source.Name))
        {
            return;
        }

        if (!result.TryGetValue(effectType, out List<Source> list))
        {
            list = new List<Source>();
            result.Add(effectType, list);
        }

        if (list.All(x => x.Label != source.Label))
        {
            list.Add(source);
        }
    }

    private static string GetEffectName(string effectType) =>
        MagicItemEffectDefinitions.TryGet(effectType, out MagicItemEffectDefinition definition) ? definition.GetName() : effectType;

    private static string Colored(ItemRarity rarity, string text) => $"<color={EpicLoot.GetRarityColor(rarity)}>{text}</color>";

    private static string Format(string token, params object[] args) =>
        string.Format(Localization.instance.Localize(token), args);

    private static string Grey(string text) => StatSourceLines.Grey(text);
}
