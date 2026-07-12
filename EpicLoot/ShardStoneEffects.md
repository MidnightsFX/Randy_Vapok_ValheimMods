# ShardStone Effects by Slot

All data is sourced from [`config/shardstones.json`](config/shardstones.json), keyed by shard color → slot → effect.
Standard shards (Core/Dark/Light) define one effect per broad slot; Boss shards use a single **uniform** effect that
applies to any socket. Effect names are the internal `EffectType` ids, and each scales by rarity (Magic → Rare → Epic →
Legendary → Mythic).

## Core shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Head | Chest | Legs | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|
| **Red** (Vitality) | LifeSteal | LifeSteal | LifeSteal | ModifyHealthRegen | IncreaseHealth | PercentHealth | DamageTakenGivesAdrenaline | AddHealthRegen |
| **Yellow** (Stamina) | ModifyAttackStaminaUse | ModifyDrawStaminaUse | StaminaReturnFromEitr | PercentStamina | ModifyStaminaRegen | IncreaseStamina | UseAdrenalineAsStamina | ModifySprintStaminaUse |
| **Cyan** (Eitr) | EitrImbueAttack | EitrImbueAttack | ModifyAttackEitrUse | PercentEitr | IncreaseEitr | ModifyEitrRegen | EitrUseGivesAdrenaline | EitrShield |
| **Orange** (Fire) | AddFireDamage | AddFireDamage | AddFireDamage | AddFireResistancePercentage | PhysToFire | Stampede | BurningAdrenaline | IncreaseHeatResistance |
| **Pink** (Dodge) | PerfectDodgeGivesHealth | PerfectDodgeGivesStamina | PerfectDodgeGivesEitr | DecreaseDodgeCost | ReduceFallDamage | DodgeBuff | PerfectDodge | RollCleanse |
| **Black** (Night) | IncreaseDamageDuringNighttime | IncreaseDamageDuringNighttime | IncreaseDamageDuringNighttime | NightStaminaRegenIncrease | DamageReductionAtNight | AddKnivesSkill | SummonBatWhenActivatingAdrenaline | ModifyNoise |
| **White** (Day) | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | DayDiscovery | DayArmor | DayStaminaRegen | DayHealthRegen | AddCrafterSkills |
| **Green** (Movement) | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | IncreaseXPGainFromMovementPenalty | CarryWeightForMovementPenalty | StaminaIncreaseForMovementPenalty | AddMovementSkills | ModifyJumpStaminaUse |
| **Purple** (Eitr/Blood) | EitrLeech | EitrLeech | ModifyMagicFireRate | DartingThoughts | ConsumeEitrFirstForBloodCosts | EveryXPointsOfEitrIncreasesStamina | ConvertEitrCostToStaminaCost | RunningOnEmpty |
| **Grey** (Harvest) | IncreaseHarvestDamage | IncreaseHarvestDamage | IncreaseHarvestDamage | IncreaseMiningDrop | AddFishingSkill | IncreaseTreeDrop | GainAdrenalineFromHarvesting | IncreaseHarvestXPGain |

## Dark shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Head | Chest | Legs | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|
| **DarkRed** | IncreaseMeleeSkills | AddBowsSkill | AddBluntDamage | HeadHunter | ChanceToCritOnHit | BulkUp | DecreaseForsakenCooldown | OffSetAttack |
| **DarkGreen** (Poison) | AddPoisonDamage | AddPoisonDamage | AddPoisonDamage | AddPoisonResistancePercentage | PhysToPoison | AddBlockingSkill | GainAdrenalineWhenApplyingPoison | IncreaseAllPoisonDamageDone |
| **DarkBlue** (Frost) | AddFrostDamage | AddFrostDamage | AddFrostDamage | AddFrostResistancePercentage | PhysToFrost | AddElementalMagicSkill | AdrenalineIncreasesFrostDamage | Warmth |
| **DarkPurple** (Blood) | ModifyAttackHealthUse | ModifyAttackHealthUse | ModifyAttackHealthUse | KillsReduceNextBloodCost | ReflectDamage | BloodMagicLevelIncreasesHealthRegen | GainAdrenalineWhenSacrificingHealth | AddBloodMagicSkill |
| **Golden** (Luck) | ChanceDoubleDamage | ChanceDoubleDamage | ChanceDoubleDamage | QuickLearner | SpendCoinsToIncreaseDamage | LuckWhileFishing | Luck | Riches |

## Light shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Head | Chest | Legs | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|
| **LightBlue** (Lightning) | AddLightningDamage | AddLightningDamage | AddLightningDamage | AddLightningResistancePercentage | PhysToLightning | StormRider | IncreaseAdrenalineGainDuringStorm | ConvertPhysicalDamageToLightning |
| **LightGreen** | HealthGainPerXDamageDone | HealthGainPerXDamageDone | HealthGainPerXDamageDone | PotionEfficacy | Comfortable | AddPickaxesSkill | AdrenalineIncreasesHealthRegen | BountifulHarvest |
| **Peach** (Weight) | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | GainMaxStaminaBasedOnPlayerMaxHealth | StaminaRegenBonusFromPlayerWeight | FeatherFall | SailingSpeed | AddCarryWeight |

## Boss shards (uniform — one effect on any slot)

| Shard | Rarity | Effect (all slots) |
|---|---|---|
| **Eikthyr** | Rare | ShockingCharge |
| **Elder** | Rare | ForestsAid |
| **Bonemass** | Epic | PoisonCoating |
| **Moder** | Epic | IcyRetribution |
| **Yagluth** | Legendary | MeteorSummoner |
| **Queen** | Legendary | EitrSiphon |
| **Fader** | Mythic | LastFire |

## Notes on slot resolution

Slot resolution happens at socket time in [`Shards.GetShardEffect` / `ResolveCategory`](src/ShardStones/Shards.cs).

- The config only defines the **broad group** keys above (`MeleeWeapon`, `RangedWeapon`, `MagicWeapon`, `Head`,
  `Chest`, `Legs`, `Trinket`, `Utility`).
- `ResolveCategory` first maps a host item to a *fine* type (Swords, Bows, Bucklers, etc.), then falls back to its
  group — so, e.g., a sword and a club both pick up the `MeleeWeapon` effect since no fine-type effects are defined.
- `Shoulders`, `Shield`, and the fine weapon/shield slots exist in the `ShardSlotCategory` enum but currently have no
  shard effects assigned.
