using System.Collections.Generic;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Registers a MagicItemEffectDefinition for every shard effect type that the loaded overhaul config does
    // not already define -- i.e. the new Shardstone-only effects declared in MagicEffectType_Shards.cs.
    // Without a definition, MagicItemEffectDefinitions.Get() synthesizes a blank-named fallback and logs a
    // "definition missing" warning, so socketed shards render with empty tooltip names. The socketed value
    // itself comes from the shard (ShardSocketManager.ResolveSocketedEffect); these definitions supply the
    // display text/requirements and the value ranges used by the loose-shard preview and compendium.
    //
    // Wired to OnSetupMagicItemEffectDefinitions (see EpicLoot.RegisterMagicEffectEvents) so it re-runs
    // after every config (re)load, which clears and rebuilds AllDefinitions. ShardStones types are
    // fully qualified because this namespace ends in ".Shards", which would otherwise shadow the
    // EpicLoot.ShardStones.Shards class.
    public static class ShardEffectDefinitions
    {
        public static void RegisterShardEffectDefinitions()
        {
            foreach (var pair in CollectShardEffects())
            {
                if (MagicItemEffectDefinitions.AllDefinitions.ContainsKey(pair.Key))
                {
                    continue; // already defined by the overhaul config or another source
                }

                MagicItemEffectDefinitions.Add(BuildDefinition(pair.Key, pair.Value));
            }
        }

        // Every effect type used by any shard, mapped to its per-rarity value ramp. Effects are globally
        // unique across shards, so first occurrence wins.
        private static Dictionary<string, Dictionary<ItemRarity, float>> CollectShardEffects()
        {
            var result = new Dictionary<string, Dictionary<ItemRarity, float>>();

            void Consider(global::EpicLoot.ShardStones.ShardEffectDefinition effect)
            {
                if (effect != null && !string.IsNullOrEmpty(effect.EffectType) &&
                    !result.ContainsKey(effect.EffectType))
                {
                    result[effect.EffectType] = effect.ValuesPerRarity;
                }
            }

            foreach (var shard in global::EpicLoot.ShardStones.Shards.ShardDefinitions.ShardEffects.Values)
            {
                Consider(shard.UniformEffect);
                if (shard.TypeEffects != null)
                {
                    foreach (var effect in shard.TypeEffects.Values)
                    {
                        Consider(effect);
                    }
                }
            }

            return result;
        }

        private static MagicItemEffectDefinition BuildDefinition(string type,
            Dictionary<ItemRarity, float> valuesPerRarity)
        {
            var lower = type.ToLowerInvariant();
            var requirements = new MagicItemEffectRequirements { NoRoll = true };

            // Adrenaline effects only function on trinkets (which supply the adrenaline pool), so keep them
            // legal on trinkets alone -- the shard grid already assigns them only to the trinket slot.
            if (type.Contains("Adrenaline"))
            {
                requirements.AllowedItemTypes = new List<string> { "Trinket" };
            }

            return new MagicItemEffectDefinition
            {
                Type = type,
                DisplayText = $"$mod_epicloot_me_{lower}_display",
                Description = $"$mod_epicloot_me_{lower}_desc",
                Requirements = requirements,
                ValuesPerRarity = BuildValues(valuesPerRarity),
                CanBeAugmented = false,
                CanBeDisenchanted = false,
                CanBeRunified = false,
            };
        }

        private static MagicItemEffectDefinition.ValuesPerRarityDef BuildValues(
            Dictionary<ItemRarity, float> valuesPerRarity)
        {
            return new MagicItemEffectDefinition.ValuesPerRarityDef
            {
                Magic = Value(valuesPerRarity, ItemRarity.Magic),
                Rare = Value(valuesPerRarity, ItemRarity.Rare),
                Epic = Value(valuesPerRarity, ItemRarity.Epic),
                Legendary = Value(valuesPerRarity, ItemRarity.Legendary),
                Mythic = Value(valuesPerRarity, ItemRarity.Mythic),
            };
        }

        private static MagicItemEffectDefinition.ValueDef Value(Dictionary<ItemRarity, float> values,
            ItemRarity rarity)
        {
            return values != null && values.TryGetValue(rarity, out var v)
                ? new MagicItemEffectDefinition.ValueDef { MinValue = v, MaxValue = v, Increment = 1 }
                : null;
        }
    }
}
