using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;

namespace EpicLoot.Compendium;

public class SetInfo(string topic, bool showSearchBar = true) : MagicTextInfo(topic, showSearchBar)
{
    public override void Build(MagicPages instance)
    {
        // One entry per set, whatever rarities it rolls at, lowest rarity first.
        foreach (LegendarySetInfo set in UniqueLegendaryHelper.AllSets.Values
                     .OrderBy(x => UniqueLegendaryHelper.GetRarities(x).Min()))
        {
            FormatSetInfo(instance, set);
        }
    }

    private static void FormatSetInfo(MagicPages instance, LegendarySetInfo set)
    {
        List<LegendaryInfo> infos = [];
        foreach (string item in set.LegendaryIDs.Distinct())
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(item, out LegendaryInfo info))
            {
                continue;
            }
            infos.Add(info);
        }

        // A set none of whose pieces exist can never drop (a per-rarity copy whose pieces failed to load).
        if (infos.Count == 0)
        {
            return;
        }

        List<ItemRarity> rarities = UniqueLegendaryHelper.GetRarities(set);
        List<string> content = [];

        content.Add(string.Join(", ", rarities.Select(r =>
            $"<color={EpicLoot.GetRarityColor(r)}>{EpicLoot.GetRarityDisplayName(r)}</color>")));

        content.Add($"$mod_epicloot_set ({infos.Count}):");
        foreach (LegendaryInfo item in infos)
        {
            string requirements = DescribeRequirements(item);
            content.Add(requirements.Length > 0
                ? $" - {item.Name} <color=#c0c0c0ff>({requirements})</color>"
                : $" - {item.Name}");
        }

        content.Add("$mod_epicloot_set_bonuses: ");
        foreach (SetBonusInfo bonus in set.SetBonuses.OrderBy(x => x.Count))
        {
            if (bonus.Effect == null ||
                !MagicItemEffectDefinitions.AllDefinitions.TryGetValue(bonus.Effect.Type, out MagicItemEffectDefinition definition))
            {
                continue;
            }

            content.Add($" - ({bonus.Count}) {DescribeBonus(definition, bonus, rarities)}");
        }

        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>" +
                                        $"<color={EpicLoot.GetRarityColor(rarities.Max())}>" +
                                        $"{set.Name}</color></size>", content.ToArray());
    }

    // The real value, or one value per rarity when the bonus has per-rarity overrides that differ.
    private static string DescribeBonus(MagicItemEffectDefinition definition, SetBonusInfo bonus, List<ItemRarity> rarities)
    {
        List<float> values = rarities.Select(r => SetBonusEvaluator.GetBonusValue(bonus, r)).ToList();
        if (values.Distinct().Count() <= 1)
        {
            return MagicItem.GetEffectText(definition, values.FirstOrDefault());
        }

        string perRarity = string.Join(" / ", rarities.Select((r, i) =>
            $"<color={EpicLoot.GetRarityColor(r)}>{values[i]:0.#}</color>"));
        return $"{MagicItem.GetEffectTextGeneric(definition, "<b><color=yellow>X</color></b>")} (X = {perRarity})";
    }

    private static string DescribeRequirements(LegendaryInfo item)
    {
        MagicItemEffectRequirements requirements = item.Requirements;
        if (requirements?.AllowedItemTypes?.Count > 0)
        {
            return string.Join(", ", requirements.AllowedItemTypes);
        }

        if (requirements?.AllowedSkillTypes?.Count > 0)
        {
            return string.Join(", ", requirements.AllowedSkillTypes);
        }

        return "";
    }
}
