using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ItemDrop;

namespace EpicLoot.ShardStones
{
    // A Shard's color determines which set of magical effects it has along with its icon
    public enum ShardType
    {
        // Core Shards
        Red,  // Vitality
        Yellow, // Stamina
        Cyan,  // Eitr
        // Standard Shards
        Black, // Night time effects
        Green, // Movement
        Orange, // Fire
        Pink, // Dodge
        Purple, // Eitr
        White, // Daytime
        //Grey,
        // Dark shards
        DarkGreen,
        DarkPurple,
        DarkRed,
        DarkBlue, // cold resistances
        Golden,
        // Light shards
        LightBlue,
        LightGreen,
        Peach,
        //LightRed
        // Boss shards
        Eikthyr, // Stamina as eitr
        Elder, // Summon roots occasionally to fight for you
        Bonemass, // Applies poison to enemies on hit (chance)
        Moder, // Chance to cause a frost nova when you are hit
        Yagluth, // Chance to summon a meteor on hit
        Queen, // Gain a small amount of eitr from stamina usage
        Fader, // Chance to cause an aoe fire explosion on hit

        // This is the error path
        None
    }

    // Groups shards into classes. A category may be flagged exclusive (see Shards.IsExclusive),
    // meaning a player may wear at most one socketed shard of that category at a time.
    public enum ShardCategory
    {
        Core,
        Dark,
        Light,
        Boss,
        Unique
    }

    // The equipment "slot" a shard resolves to when socketed. This mirrors EpicLoot's ItemInfo types
    // (config/iteminfo.json) so a shard can grant a different effect to each individual item type, while
    // still assigning one effect to a whole group (all melee weapons, all shields, all armor) as a
    // fallback. Shards.ResolveCategory maps a host item to the most specific fine type it can; a fine
    // effect overrides its group and a group effect covers every member that has no fine effect.
    public enum ShardSlotCategory
    {
        // -------- Groups (broad fallback keys; not resolution targets in the primary path) --------
        MeleeWeapon,
        RangedWeapon,
        MagicWeapon,
        Shield,
        Armor,

        // -------- Fine weapon types (mirror ItemInfo) --------
        Swords,
        Axes,
        TwoHandAxes,
        Knives,
        Fists,
        Clubs,
        Sledges,
        Polearms,
        Spears,
        Pickaxes,
        Tools,
        Torches,
        Bows,
        Staffs,

        // -------- Fine shield types --------
        Bucklers,
        RoundShields,
        TowerShields,

        // -------- Fine armor types --------
        Head,
        Chest,
        Legs,
        Shoulders,

        // -------- Standalone (no group) --------
        Trinket,
        Utility
    }

    public class ShardEffectDefinition
    {
        public string EffectType;
        public Dictionary<ItemRarity, float> ValuesPerRarity = new Dictionary<ItemRarity, float>();
    }

    public class ShardDefinition
    {
        public ShardCategory Category = ShardCategory.Core;

        // When non-null, the shard grants this single effect on ANY host item type it is allowed
        // into (a "uniform" shard), instead of the per-item-type TypeEffects mapping below.
        public ShardEffectDefinition UniformEffect = null;

        public Dictionary<ShardSlotCategory, ShardEffectDefinition> TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();

        // Layers a fine-type (or group) override onto this shard, returning `this` so authoring can chain:
        // e.g. Shard(...).With(ShardSlotCategory.Swords, Skill(MagicEffectType.AddSwordsSkill)). A fine
        // entry takes precedence over its group in Shards.GetShardEffect.
        public ShardDefinition With(ShardSlotCategory slot, ShardEffectDefinition effect)
        {
            if (effect != null)
            {
                TypeEffects[slot] = effect;
            }
            return this;
        }

        public float GetValue(ShardSlotCategory category, ItemRarity rarity)
        {
            if (UniformEffect != null)
            {
                return UniformEffect.ValuesPerRarity.TryGetValue(rarity, out var uniform) ? uniform : 0f;
            }
            if (TypeEffects.TryGetValue(category, out var effectDef))
            {
                return effectDef.ValuesPerRarity.TryGetValue(rarity, out var value) ? value : 0f;
            }
            return 0f;
        }
    }

    public static class Shards {
        public static readonly String ShardIndicator = "ShardStone";

        // Build out shards based on the colors and all magic rarities.


        // This will be written to disk as a new config file Shardstones.json
        public static class ShardDefinitions
        {
            // -------- Value ramp helpers (Magic -> Mythic). Low power; tunable later. --------
            private static ShardEffectDefinition Fx(string type, float m, float r, float e, float l, float my)
                => new ShardEffectDefinition
                {
                    EffectType = type,
                    ValuesPerRarity = new Dictionary<ItemRarity, float>
                    {
                        { ItemRarity.Magic, m }, { ItemRarity.Rare, r }, { ItemRarity.Epic, e },
                        { ItemRarity.Legendary, l }, { ItemRarity.Mythic, my }
                    }
                };

            private static ShardEffectDefinition Pct(string t) => Fx(t, 2, 3, 4, 5, 6);    // small percentages
            private static ShardEffectDefinition Pool(string t) => Fx(t, 2, 4, 6, 8, 10);  // flat stat pools
            private static ShardEffectDefinition Skill(string t) => Fx(t, 1, 2, 3, 4, 5);  // skill levels
            private static ShardEffectDefinition Proc(string t) => Fx(t, 1, 2, 3, 4, 5);   // proc / leech chances
            private static ShardEffectDefinition Boss(string t) => Fx(t, 5, 10, 15, 20, 25); // boss-tier

            // Builds a per-slot shard. Null slots are gaps (the shard sits inert there). Weapon slots
            // are passed individually; a color that shares one weapon effect just passes the same value.
            private static ShardDefinition Shard(ShardCategory cat,
                ShardEffectDefinition melee = null, ShardEffectDefinition ranged = null, ShardEffectDefinition magic = null,
                ShardEffectDefinition head = null, ShardEffectDefinition chest = null, ShardEffectDefinition legs = null,
                ShardEffectDefinition trinket = null, ShardEffectDefinition utility = null)
            {
                var te = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();
                if (melee != null) te[ShardSlotCategory.MeleeWeapon] = melee;
                if (ranged != null) te[ShardSlotCategory.RangedWeapon] = ranged;
                if (magic != null) te[ShardSlotCategory.MagicWeapon] = magic;
                if (head != null) te[ShardSlotCategory.Head] = head;
                if (chest != null) te[ShardSlotCategory.Chest] = chest;
                if (legs != null) te[ShardSlotCategory.Legs] = legs;
                if (trinket != null) te[ShardSlotCategory.Trinket] = trinket;
                if (utility != null) te[ShardSlotCategory.Utility] = utility;
                return new ShardDefinition { Category = cat, TypeEffects = te };
            }

            // A boss shard grants one signature effect on every slot it is allowed into.
            private static ShardDefinition BossShard(string effect)
            {
                return new ShardDefinition { Category = ShardCategory.Boss, UniformEffect = Boss(effect) };
            }

            // Full effect grid. Effect names are MagicEffectType constants; ones declared in
            // MagicEffectType_Shards.cs are new (inert until their behavior is implemented). Every
            // effect is globally unique across shards (weapon slots that share an effect count once).
            public static readonly Dictionary<ShardType, ShardDefinition> ShardEffects =
                new Dictionary<ShardType, ShardDefinition>
                {
                    // ---- Core ----
                    { ShardType.Red, Shard(ShardCategory.Core,                 // Vitality
                        melee: Proc(MagicEffectType.LifeSteal), ranged: Proc(MagicEffectType.LifeSteal), magic: Proc(MagicEffectType.LifeSteal),
                        head: Pct(MagicEffectType.ModifyHealthRegen), chest: Pool(MagicEffectType.IncreaseHealth), legs: Pool(MagicEffectType.PercentHealth),
                        trinket: Pct(MagicEffectType.DamageTakenGivesAdrenaline), utility: Pct(MagicEffectType.AddHealthRegen)) },

                    { ShardType.Yellow, Shard(ShardCategory.Core,              // Stamina
                        melee: Pct(MagicEffectType.ModifyAttackStaminaUse), ranged: Pct(MagicEffectType.ModifyDrawStaminaUse), magic: Pct(MagicEffectType.StaminaReturnFromEitr),
                        head: Pool(MagicEffectType.PercentStamina), chest: Pct(MagicEffectType.ModifyStaminaRegen), legs: Pool(MagicEffectType.IncreaseStamina),
                        trinket: Pct(MagicEffectType.UseAdrenalineAsStamina), utility: Pct(MagicEffectType.ModifySprintStaminaUse)) },

                    { ShardType.Cyan, Shard(ShardCategory.Core,                // Eitr
                        melee: Pct(MagicEffectType.EitrImbueAttack), ranged: Pct(MagicEffectType.EitrImbueAttack), magic: Pct(MagicEffectType.ModifyAttackEitrUse),
                        head: Pool(MagicEffectType.PercentEitr), chest: Pool(MagicEffectType.IncreaseEitr), legs: Pct(MagicEffectType.ModifyEitrRegen),
                        trinket: Pct(MagicEffectType.EitrUseGivesAdrenaline), utility: Pct(MagicEffectType.EitrShield)) },

                    { ShardType.Orange, Shard(ShardCategory.Core,              // Fire
                        melee: Pct(MagicEffectType.AddFireDamage), ranged: Pct(MagicEffectType.AddFireDamage), magic: Pct(MagicEffectType.AddFireDamage),
                        head: Pct(MagicEffectType.AddFireResistancePercentage), chest: Pct(MagicEffectType.PhysToFire), legs: Pct(MagicEffectType.Stampede),
                        trinket: Pct(MagicEffectType.BurningAdrenaline), utility: Pct(MagicEffectType.IncreaseHeatResistance)) },

                    { ShardType.Pink, Shard(ShardCategory.Core,               // Dodge
                        melee: Pct(MagicEffectType.PerfectDodgeGivesHealth), ranged: Pct(MagicEffectType.PerfectDodgeGivesStamina), magic: Pct(MagicEffectType.PerfectDodgeGivesEitr),
                        head: Pct(MagicEffectType.DecreaseDodgeCost), chest: Pct(MagicEffectType.ReduceFallDamage), legs: Pct(MagicEffectType.DodgeBuff),
                        trinket: Proc(MagicEffectType.PerfectDodge), utility: Pct(MagicEffectType.RollCleanse)) },

                    { ShardType.Black, Shard(ShardCategory.Core,              // Night-time (EnvMan.IsNight)
                        melee: Pct(MagicEffectType.IncreaseDamageDuringNighttime), ranged: Pct(MagicEffectType.IncreaseDamageDuringNighttime), magic: Pct(MagicEffectType.IncreaseDamageDuringNighttime),
                        head: Pct(MagicEffectType.NightStaminaRegenIncrease), chest: Pct(MagicEffectType.DamageReductionAtNight), legs: Skill(MagicEffectType.AddKnivesSkill),
                        trinket: Pct(MagicEffectType.SummonBatWhenActivatingAdrenaline), utility: Pct(MagicEffectType.ModifyNoise)) },

                    { ShardType.White, Shard(ShardCategory.Core,             // Daytime (EnvMan.IsDay)
                        melee: Pct(MagicEffectType.IncreaseDamageDuringDaytime), ranged: Pct(MagicEffectType.IncreaseDamageDuringDaytime), magic: Pct(MagicEffectType.IncreaseDamageDuringDaytime),
                        head: Pct(MagicEffectType.DayDiscovery), chest: Pct(MagicEffectType.DayArmor), legs: Pct(MagicEffectType.DayStaminaRegen),
                        trinket: Pct(MagicEffectType.DayHealthRegen), utility: Skill(MagicEffectType.AddCrafterSkills)) },

                    { ShardType.Green, Shard(ShardCategory.Core,             // Movement
                        melee: Pct(MagicEffectType.DamageIncreaseFromMovementPenalty), ranged: Pct(MagicEffectType.DamageIncreaseFromMovementPenalty), magic: Pct(MagicEffectType.DamageIncreaseFromMovementPenalty),
                        head: Pct(MagicEffectType.IncreaseXPGainFromMovementPenalty), chest: Pct(MagicEffectType.CarryWeightForMovementPenalty), legs: Pct(MagicEffectType.StaminaIncreaseForMovementPenalty),
                        trinket: Skill(MagicEffectType.AddMovementSkills), utility: Pct(MagicEffectType.ModifyJumpStaminaUse)) },

                    { ShardType.Purple, Shard(ShardCategory.Core,           // Eitr / Caster
                        melee: Proc(MagicEffectType.EitrLeech), ranged: Proc(MagicEffectType.EitrLeech), magic: Pct(MagicEffectType.ModifyMagicFireRate),
                        head: Pct(MagicEffectType.DartingThoughts), chest: Pct(MagicEffectType.ConsumeEitrFirstForBloodCosts), legs: Pct(MagicEffectType.EveryXPointsOfEitrIncreasesStamina),
                        trinket: Pct(MagicEffectType.ConvertEitrCostToStaminaCost), utility: Pct(MagicEffectType.RunningOnEmpty)) },

                    // ---- Dark ----
                    { ShardType.DarkRed, Shard(ShardCategory.Dark,          // Wrath / Berserk
                        // RangedWeapon lumps bows + crossbows; AddBowsSkill covers the "ranged" theme.
                        melee: Skill(MagicEffectType.IncreaseMeleeSkills), ranged: Skill(MagicEffectType.AddBowsSkill), magic: Pct(MagicEffectType.AddBluntDamage),
                        head: Pct(MagicEffectType.HeadHunter), chest: Proc(MagicEffectType.ChanceToCritOnHit), legs: Pct(MagicEffectType.BulkUp),
                        trinket: Pct(MagicEffectType.DecreaseForsakenCooldown), utility: Pct(MagicEffectType.OffSetAttack)) },

                    { ShardType.DarkGreen, Shard(ShardCategory.Dark,        // Venom
                        melee: Pct(MagicEffectType.AddPoisonDamage), ranged: Pct(MagicEffectType.AddPoisonDamage), magic: Pct(MagicEffectType.AddPoisonDamage),
                        head: Pct(MagicEffectType.AddPoisonResistancePercentage), chest: Pct(MagicEffectType.PhysToPoison), legs: Skill(MagicEffectType.AddBlockingSkill),
                        trinket: Pct(MagicEffectType.GainAdrenalineWhenApplyingPoison), utility: Pct(MagicEffectType.IncreaseAllPoisonDamageDone)) },

                    { ShardType.DarkBlue, Shard(ShardCategory.Dark,         // Cold / Frost
                        melee: Pct(MagicEffectType.AddFrostDamage), ranged: Pct(MagicEffectType.AddFrostDamage), magic: Pct(MagicEffectType.AddFrostDamage),
                        head: Pct(MagicEffectType.AddFrostResistancePercentage), chest: Pct(MagicEffectType.PhysToFrost), legs: Skill(MagicEffectType.AddElementalMagicSkill),
                        trinket: Pct(MagicEffectType.AdrenalineIncreasesFrostDamage), utility: Pct(MagicEffectType.Warmth)) },

                    { ShardType.DarkPurple, Shard(ShardCategory.Dark,       // Blood Magic
                        melee: Pct(MagicEffectType.ModifyAttackHealthUse), ranged: Pct(MagicEffectType.ModifyAttackHealthUse), magic: Pct(MagicEffectType.ModifyAttackHealthUse),
                        head: Pct(MagicEffectType.KillsReduceNextBloodCost), chest: Pct(MagicEffectType.ReflectDamage), legs: Pct(MagicEffectType.BloodMagicLevelIncreasesHealthRegen),
                        trinket: Pct(MagicEffectType.GainAdrenalineWhenSacrificingHealth), utility: Skill(MagicEffectType.AddBloodMagicSkill)) },

                    { ShardType.Golden, Shard(ShardCategory.Dark,           // Fortune / Glory
                        melee: Proc(MagicEffectType.ChanceDoubleDamage), ranged: Proc(MagicEffectType.ChanceDoubleDamage), magic: Proc(MagicEffectType.ChanceDoubleDamage),
                        head: Pct(MagicEffectType.QuickLearner), chest: Pct(MagicEffectType.SpendCoinsToIncreaseDamage), legs: Pct(MagicEffectType.LuckWhileFishing),
                        trinket: Proc(MagicEffectType.Luck), utility: Pct(MagicEffectType.Riches)) },

                    // ---- Light ----
                    { ShardType.LightBlue, Shard(ShardCategory.Light,       // Storm / Lightning
                        melee: Pct(MagicEffectType.AddLightningDamage), ranged: Pct(MagicEffectType.AddLightningDamage), magic: Pct(MagicEffectType.AddLightningDamage),
                        head: Pct(MagicEffectType.AddLightningResistancePercentage), chest: Pct(MagicEffectType.PhysToLightning), legs: Pct(MagicEffectType.StormRider),
                        trinket: Pct(MagicEffectType.IncreaseAdrenalineGainDuringStorm), utility: Pct(MagicEffectType.ConvertPhysicalDamageToLightning)) },

                    { ShardType.LightGreen, Shard(ShardCategory.Light,      // Renewal / Nature
                        melee: Pct(MagicEffectType.HealthGainPerXDamageDone), ranged: Pct(MagicEffectType.HealthGainPerXDamageDone), magic: Pct(MagicEffectType.HealthGainPerXDamageDone),
                        head: Pct(MagicEffectType.PotionEfficacy), chest: Pct(MagicEffectType.Comfortable), legs: Skill(MagicEffectType.AddPickaxesSkill),
                        trinket: Pct(MagicEffectType.AdrenalineIncreasesHealthRegen), utility: Pct(MagicEffectType.BountifulHarvest)) },

                    { ShardType.Peach, Shard(ShardCategory.Light,           // Logistics
                        melee: Pct(MagicEffectType.DamageBonusFromPlayerWeight), ranged: Pct(MagicEffectType.DamageBonusFromPlayerWeight), magic: Pct(MagicEffectType.DamageBonusFromPlayerWeight),
                        head: Pool(MagicEffectType.GainMaxStaminaBasedOnPlayerMaxHealth), chest: Pct(MagicEffectType.StaminaRegenBonusFromPlayerWeight), legs: Pct(MagicEffectType.FeatherFall),
                        trinket: Pct(MagicEffectType.SailingSpeed), utility: Pool(MagicEffectType.AddCarryWeight)) },

                    // ---- Boss (single signature effect on every slot) ----
                    { ShardType.Eikthyr, BossShard(MagicEffectType.ShockingCharge) },
                    { ShardType.Elder, BossShard(MagicEffectType.ForestsAid) },
                    { ShardType.Bonemass, BossShard(MagicEffectType.PoisonCoating) },
                    { ShardType.Moder, BossShard(MagicEffectType.IcyRetribution) },
                    { ShardType.Yagluth, BossShard(MagicEffectType.MeteorSummoner) },
                    { ShardType.Queen, BossShard(MagicEffectType.EitrSiphon) },
                    { ShardType.Fader, BossShard(MagicEffectType.LastFire) },

                    // NOTE: Grey (Craft/Gather) is designed but its ShardColor enum value is commented
                    // out; add it back to the enum and uncomment the block below to enable it.
                    // { ShardColor.Grey, Shard(ShardCategory.Core,
                    //     melee: Pct(MagicEffectType.IncreaseHarvestDamage), ranged: Pct(MagicEffectType.IncreaseHarvestDamage), magic: Pct(MagicEffectType.IncreaseHarvestDamage),
                    //     head: Pct(MagicEffectType.IncreaseMiningDrop), chest: Skill(MagicEffectType.AddFishingSkill), legs: Pct(MagicEffectType.IncreaseTreeDrop),
                    //     trinket: Pct(MagicEffectType.GainAdrenalineFromHarvesting), utility: Pct(MagicEffectType.IncreaseHarvestXPGain)) },
                };

            public static ShardDefinition Get(ShardType color)
            {
                return ShardEffects.TryGetValue(color, out var def) ? def : null;
            }
        }

        // Check if this is a shard item
        public static bool IsShard(ItemDrop.ItemData item)
        {
            return item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                item.m_shared.m_ammoType.EndsWith(ShardIndicator);
        }

        public static ShardType GetShardColor(ItemDrop.ItemData item)
        {
            if (!IsShard(item))
            {
                return ShardType.None;
            }
            string[] parts = item.m_shared.m_ammoType.Split('|');
            if (parts.Length < 3)
            {
                return ShardType.None;
            }
            if (Enum.TryParse(parts[1], true, out ShardType color))
            {
                return color;
            }
            return ShardType.None;
        }

        public static ItemRarity GetShardRarity(ItemDrop.ItemData item)
        {
            // A shard's rarity lives in parts[0] of its 3-part ammoType (e.g. "Epic|Yellow|ShardStone").
            // Parse it directly here, mirroring GetShardColor -- GetCraftingMaterialRarity() rejects the
            // 3-part format and would silently fall back to Magic.
            if (!IsShard(item))
            {
                return ItemRarity.Magic;
            }
            string[] parts = item.m_shared.m_ammoType.Split('|');
            if (parts.Length < 3)
            {
                return ItemRarity.Magic;
            }
            return Enum.TryParse(parts[0], true, out ItemRarity rarity) ? rarity : ItemRarity.Magic;
        }

        // Human-readable label for an item type, e.g. "$mod_epicloot_itemtype_helmet" -> "Helmet".
        // Falls back to the raw enum name when no localization key is defined.
        public static string GetItemTypeDisplayName(ItemDrop.ItemData.ItemType itemType)
        {
            var token = $"mod_epicloot_itemtype_{itemType.ToString().ToLowerInvariant()}";
            var localized = Localization.instance.Localize($"${token}");
            return string.Equals(localized, token, StringComparison.Ordinal) ? itemType.ToString() : localized;
        }

        public static ShardEffectDefinition GetShardEffect(ItemDrop.ItemData item, ShardType color)
        {
            if (!ShardDefinitions.ShardEffects.TryGetValue(color, out var colorEffects))
            {
                return null;
            }

            // A uniform shard (e.g. a boss shard) grants the same effect on any host item type.
            if (colorEffects.UniformEffect != null)
            {
                return colorEffects.UniformEffect;
            }

            if (colorEffects.TypeEffects == null)
            {
                return null;
            }

            // A fine-type effect (e.g. Swords) overrides its group (MeleeWeapon); a group effect covers
            // every member with no fine effect of its own.
            var slot = ResolveCategory(item);
            if (colorEffects.TypeEffects.TryGetValue(slot, out var fineEffect))
            {
                return fineEffect;
            }
            if (GroupOf(slot) is ShardSlotCategory group &&
                colorEffects.TypeEffects.TryGetValue(group, out var groupEffect))
            {
                return groupEffect;
            }
            return null;
        }

        // Maps each EpicLoot ItemInfo type string (config/iteminfo.json "Type") to its fine slot. This is
        // the primary resolution path -- it distinguishes types that share a skill/ItemType (e.g. Axes vs
        // TwoHandAxes, the three shield subtypes) that the fallback heuristic below cannot.
        private static readonly Dictionary<string, ShardSlotCategory> ItemInfoTypeToSlot =
            new Dictionary<string, ShardSlotCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "Swords", ShardSlotCategory.Swords },
                { "Axes", ShardSlotCategory.Axes },
                { "TwoHandAxes", ShardSlotCategory.TwoHandAxes },
                { "Knives", ShardSlotCategory.Knives },
                { "Fists", ShardSlotCategory.Fists },
                { "Clubs", ShardSlotCategory.Clubs },
                { "Sledges", ShardSlotCategory.Sledges },
                { "Polearms", ShardSlotCategory.Polearms },
                { "Spears", ShardSlotCategory.Spears },
                { "Pickaxes", ShardSlotCategory.Pickaxes },
                { "Tools", ShardSlotCategory.Tools },
                { "Torches", ShardSlotCategory.Torches },
                { "Bows", ShardSlotCategory.Bows },
                { "Staffs", ShardSlotCategory.Staffs },
                { "Bucklers", ShardSlotCategory.Bucklers },
                { "RoundShields", ShardSlotCategory.RoundShields },
                { "TowerShields", ShardSlotCategory.TowerShields },
                { "HeadArmor", ShardSlotCategory.Head },
                { "ChestArmor", ShardSlotCategory.Chest },
                { "LegsArmor", ShardSlotCategory.Legs },
                { "ShouldersArmor", ShardSlotCategory.Shoulders },
                { "Trinket", ShardSlotCategory.Trinket },
                { "Utility", ShardSlotCategory.Utility }
            };

        // The broad group a fine slot falls back to when a shard defines no effect for the exact type.
        // Returns null for standalone slots (Trinket, Utility) and for group values themselves.
        public static ShardSlotCategory? GroupOf(ShardSlotCategory slot)
        {
            switch (slot)
            {
                case ShardSlotCategory.Swords:
                case ShardSlotCategory.Axes:
                case ShardSlotCategory.TwoHandAxes:
                case ShardSlotCategory.Knives:
                case ShardSlotCategory.Fists:
                case ShardSlotCategory.Clubs:
                case ShardSlotCategory.Sledges:
                case ShardSlotCategory.Polearms:
                case ShardSlotCategory.Spears:
                case ShardSlotCategory.Pickaxes:
                case ShardSlotCategory.Tools:
                case ShardSlotCategory.Torches:
                    return ShardSlotCategory.MeleeWeapon;
                case ShardSlotCategory.Bows:
                    return ShardSlotCategory.RangedWeapon;
                case ShardSlotCategory.Staffs:
                    return ShardSlotCategory.MagicWeapon;
                case ShardSlotCategory.Bucklers:
                case ShardSlotCategory.RoundShields:
                case ShardSlotCategory.TowerShields:
                    return ShardSlotCategory.Shield;
                case ShardSlotCategory.Head:
                case ShardSlotCategory.Chest:
                case ShardSlotCategory.Legs:
                case ShardSlotCategory.Shoulders:
                    return ShardSlotCategory.Armor;
                default:
                    return null;
            }
        }

        // Maps a host equipment item to the most specific fine slot a shard uses to pick its effect.
        // Primary: EpicLoot's own ItemInfo classification (covers vanilla + any item listed in
        // iteminfo.json, including modded ones). Fallback (items EpicLoot doesn't classify): a
        // skill/ItemType heuristic, resolved as finely as those raw fields allow.
        public static ShardSlotCategory ResolveCategory(ItemDrop.ItemData item)
        {
            var prefabName = item.m_dropPrefab?.name;
            if (!string.IsNullOrEmpty(prefabName) &&
                GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(prefabName, out var details) &&
                !string.IsNullOrEmpty(details.Type) &&
                ItemInfoTypeToSlot.TryGetValue(details.Type, out var mapped))
            {
                return mapped;
            }

            return ResolveCategoryFallback(item);
        }

        // Best-effort slot from an item's raw combat/type fields, for items not in iteminfo.json. Where a
        // subtype can't be distinguished (e.g. any shield), returns the broad group; GetShardEffect treats
        // a group value the same as a fine one, so a group-level shard effect still applies.
        private static ShardSlotCategory ResolveCategoryFallback(ItemDrop.ItemData item)
        {
            var shared = item.m_shared;
            var skill = shared.m_skillType;
            var twoHanded = shared.m_itemType == ItemData.ItemType.TwoHandedWeapon ||
                shared.m_itemType == ItemData.ItemType.TwoHandedWeaponLeft;

            switch (skill)
            {
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                    return ShardSlotCategory.Staffs;
                case Skills.SkillType.Bows:
                case Skills.SkillType.Crossbows:
                    return ShardSlotCategory.Bows;
                case Skills.SkillType.Swords:
                    return ShardSlotCategory.Swords;
                case Skills.SkillType.Knives:
                    return ShardSlotCategory.Knives;
                case Skills.SkillType.Spears:
                    return ShardSlotCategory.Spears;
                case Skills.SkillType.Polearms:
                    return ShardSlotCategory.Polearms;
                case Skills.SkillType.Pickaxes:
                    return ShardSlotCategory.Pickaxes;
                case Skills.SkillType.Unarmed:
                    return ShardSlotCategory.Fists;
                case Skills.SkillType.Axes:
                    return twoHanded ? ShardSlotCategory.TwoHandAxes : ShardSlotCategory.Axes;
                case Skills.SkillType.Clubs:
                    return twoHanded ? ShardSlotCategory.Sledges : ShardSlotCategory.Clubs;
            }

            switch (shared.m_itemType)
            {
                case ItemData.ItemType.Helmet:
                    return ShardSlotCategory.Head;
                case ItemData.ItemType.Chest:
                    return ShardSlotCategory.Chest;
                case ItemData.ItemType.Legs:
                    return ShardSlotCategory.Legs;
                case ItemData.ItemType.Shoulder:
                    return ShardSlotCategory.Shoulders;
                case ItemData.ItemType.Shield:
                    return ShardSlotCategory.Shield; // subtype indistinguishable -> broad group
                case ItemData.ItemType.Torch:
                    return ShardSlotCategory.Torches;
                case ItemData.ItemType.Tool:
                    return ShardSlotCategory.Tools;
                case ItemData.ItemType.OneHandedWeapon:
                case ItemData.ItemType.TwoHandedWeapon:
                case ItemData.ItemType.TwoHandedWeaponLeft:
                    return ShardSlotCategory.MeleeWeapon; // unknown melee weapon -> broad group
            }

            return ShardSlotCategory.Utility;
        }

        // Human-readable label for a slot category, e.g. "$mod_epicloot_shardslot_meleeweapon" ->
        // "Melee Weapon". Falls back to the raw enum name when no localization key is defined.
        public static string GetCategoryDisplayName(ShardSlotCategory category)
        {
            var token = $"mod_epicloot_shardslot_{category.ToString().ToLowerInvariant()}";
            var localized = Localization.instance.Localize($"${token}");
            return string.Equals(localized, token, StringComparison.Ordinal) ? category.ToString() : localized;
        }

        // Categories whose shards are mutually exclusive: a player may wear at most one socketed
        // shard of each such category at a time. Exclusivity is a property of the category, so it
        // applies uniformly to every color in it.
        private static readonly HashSet<ShardCategory> ExclusiveCategories = new HashSet<ShardCategory>
        {
            ShardCategory.Boss,
            ShardCategory.Unique
        };

        public static bool IsExclusive(ShardCategory category) => ExclusiveCategories.Contains(category);

        public static ShardCategory GetCategory(ShardType color)
        {
            var def = ShardDefinitions.Get(color);
            return def != null ? def.Category : ShardCategory.Core;
        }

        internal static void CreateAndLoadShardItems()
        {
            GameObject genericPrefab = EpicAssets.AssetBundle.LoadAsset<GameObject>("_ShardStone");
            CustomItem genericShard = new CustomItem(genericPrefab, false);
            ItemManager.Instance.AddItem(genericShard);
            genericPrefab.SetActive(false);

            var shardPrefabNames = new List<string>();

            foreach (string shardColor in Enum.GetNames(typeof(ShardType)))
            {
                if (shardColor == "None")
                {
                    continue;
                }

                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    var prefab = UnityEngine.Object.Instantiate(genericPrefab);
                    string PrefabName = $"{shardColor}_{rarity}_ShardStone";
                    prefab.name = PrefabName;
                    ItemDrop pid = prefab.GetComponent<ItemDrop>();
                    var magicItemComponent = pid.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                    pid.m_itemData.m_dropPrefab = prefab;
                    pid.m_itemData.m_shared.m_icons = new Sprite[] { EpicAssets.AssetBundle.LoadAsset<Sprite>($"Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png") };
                    pid.m_itemData.m_shared.m_ammoType = $"{rarity}|{shardColor}|ShardStone";
                    magicItemComponent.SetMagicItem(new MagicItem
                    {
                        Rarity = rarity,
                    });
                    magicItemComponent.Save();
                    pid.Save();

                    ItemConfig ShardItemConfig = new ItemConfig()
                    {
                        Name = $"$mod_epicloot_{rarity} $mod_epicloot_shard_{shardColor} $mod_epicloot_assets_shardstone",
                        Description = "$mod_epicloot_assets_shardstone_introduce",
                    };

                    CustomItem custom = new CustomItem(prefab, false, ShardItemConfig);
                    ItemManager.Instance.AddItem(custom);

                    shardPrefabNames.Add(PrefabName);
                }
            }

            // Enable items once things are working so that ZNet issues don't happen.
            // A single idempotent handler activates every registered prefab; a null
            // lookup logs and continues so one missing prefab can't leave the rest inactive.
            void EnableShardItems()
            {
                foreach (string prefabName in shardPrefabNames)
                {
                    GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                    if (prefab == null)
                    {
                        EpicLoot.LogError($"Could not find shardstone prefab '{prefabName}' to activate.");
                        continue;
                    }

                    prefab.SetActive(true);
                    prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = prefab;
                }
            }

            ItemManager.OnItemsRegistered += EnableShardItems;
        }

    }

}
