using Common;
using System;
using System.Collections.Generic;

namespace EpicLoot.CraftingV2
{
    [Serializable]
    public enum MaterialConversionType
    {
        Upgrade,
        Convert,
        Junk
    }

    [Serializable]
    public class MaterialConversionRequirement
    {
        public string Item = "";
        public int Amount = 1;
        // >0: require a specific item quality. Used for ShardStones, whose rarity is m_quality (= rarity+1)
        // and which share one name per color; 0 leaves matching quality-agnostic (the default for materials).
        public int Quality = 0;
    }

    [Serializable]
    public class MaterialConversion
    {
        public string Name = "";
        public string Product = "";
        public int Amount = 1;
        // >0: stamp the produced item to this quality. For ShardStones this becomes the upgraded rarity
        // (rarity = quality-1), applied via Shards.StampRarity so the MagicItem metadata matches.
        public int ProductQuality = 0;
        public MaterialConversionType Type;
        public List<MaterialConversionRequirement> Resources = new List<MaterialConversionRequirement>();
    }

    [Serializable]
    public class MaterialConversionsConfig
    {
        public List<MaterialConversion> MaterialConversions = new List<MaterialConversion>();
    }

    public static class MaterialConversions
    {
        public static MaterialConversionsConfig Config;
        public static MultiValueDictionary<MaterialConversionType, MaterialConversion> Conversions = new MultiValueDictionary<MaterialConversionType, MaterialConversion>();
        public static event Action OnSetupMaterialConversions;

        public static void Initialize(MaterialConversionsConfig config)
        {
            Config = config;
            OnSetupMaterialConversions?.Invoke();

            Conversions.Clear();
            foreach (var entry in Config.MaterialConversions)
            {
                Conversions.Add(entry.Type, entry);
            }
        }

        public static MaterialConversionsConfig GetCFG()
        {
            return Config;
        }
    }
}
