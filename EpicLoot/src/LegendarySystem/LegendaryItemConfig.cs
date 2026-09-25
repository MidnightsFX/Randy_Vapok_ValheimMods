using Common;
using System;
using System.Collections.Generic;

namespace EpicLoot.LegendarySystem
{
    [Serializable]
    public class GuaranteedMagicEffect
    {
        public string Type;
        public MagicItemEffectDefinition.ValueDef Values;
        // Optional per-rarity override of Values, for uniques and sets enabled at several rarities. A
        // rarity left out falls back to Values (see UniqueLegendaryHelper.ResolveValues).
        public MagicItemEffectDefinition.ValuesPerRarityDef ValuesPerRarity;
    }

    [Serializable]
    public class TextureReplacement
    {
        public string ItemID;
        public string MainTexture;
        public string ChestTex;
        public string LegsTex;
    }

    [Serializable]
    public class LegendaryInfo
    {
        public string ID;
        public string Name;
        public string Description;
        // The rarities this unique can roll at. Left empty here because Newtonsoft appends to a
        // pre-filled list: UniqueLegendaryHelper backfills it from the block the entry came from
        // (LegendaryItems -> Legendary, MythicItems -> Mythic). A set piece always takes its set's list.
        public List<ItemRarity> Rarities = new List<ItemRarity>();
        // Default instance: an entry that omits "Requirements" must not be null (legendary rolls
        // call CheckRequirements on every entry).
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
        public int GuaranteedEffectCount = -1;
        public float SelectionWeight = 1;
        public string EquipFx;
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public List<TextureReplacement> TextureReplacements = new List<TextureReplacement>();
        // Kept for config compatibility only: set membership comes from the sets' LegendaryIDs, and a
        // mismatch is reported at load.
        public bool IsSetItem;
        public bool Enchantable;
        public List<RecipeRequirementConfig> EnchantCost = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class SetBonusInfo
    {
        public int Count;
        public GuaranteedMagicEffect Effect;
    }

    [Serializable]
    public class LegendarySetInfo
    {
        public string ID;
        public string Name;
        // The rarities the set's pieces can roll at. Empty here for the same reason as
        // LegendaryInfo.Rarities; backfilled from the block (LegendarySets -> Legendary, MythicSets -> Mythic).
        public List<ItemRarity> Rarities = new List<ItemRarity>();
        public List<string> LegendaryIDs = new List<string>();
        public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();
    }

    [Serializable]
    public class LegendaryItemConfig
    {
        // Every unique and set, whatever rarities it rolls at, belongs in these two blocks.
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> LegendarySets = new List<LegendarySetInfo>();
        // Legacy per-rarity blocks, still loaded and merged into the ones above with a default of Mythic.
        // Kept so existing patches that target $.MythicItems / $.MythicSets keep applying.
        public List<LegendaryInfo> MythicItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> MythicSets = new List<LegendarySetInfo>();
    }
}
