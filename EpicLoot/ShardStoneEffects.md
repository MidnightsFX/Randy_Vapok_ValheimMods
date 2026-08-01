# ShardStone Effects by Slot

All data is sourced from [`config/shardstones.json`](config/shardstones.json), keyed by shard color → slot → effect.
Standard shards (Core/Dark/Light) define one effect per broad slot; Boss shards use a single **uniform** effect that
applies to any socket. Effect names are the internal `EffectType` ids, and each scales by rarity (Magic → Rare → Epic →
Legendary → Mythic).

## Core shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **Red** (Vitality) ☑️ | LifeGainOnHit | LifeGainOnHit | HealthOnEitrUse | LifeGainOnBlock | ModifyHealthRegen | PercentHealth | IncreaseHealth | BulkUp | DamageTakenGivesAdrenaline | AddHealthRegen |
| **Yellow** (Stamina) ☑️ | ModifyAttackStaminaUse | ModifyDrawStaminaUse | EnergeticEitr | StaminaGainOnBlock | ModifyStaminaRegen | PercentStamina | IncreaseStamina | StaminaOnKill | UseAdrenalineAsStamina | ModifySprintStaminaUse |
| **Cyan** (Eitr) | EitrImbueAttack | EitrImbueAttack | ModifyAttackEitrUse | EitrGainOnBlock | PercentEitr | IncreaseEitr | ModifyEitrRegen | HeartyEitr | EitrUseGivesAdrenaline | EitrShield |
| **Orange** (Fire) | AddFireDamage | AddFireDamage | AddFireDamage | PhysToFireOnBlock | AddFireResistancePercentage | PhysToFire | Kindling | BurningSpeed | BurningAdrenaline | IncreaseHeatResistance |
| **Pink** (Dodge) | PerfectDodgeGivesStamina | PerfectDodgeGivesStamina | PerfectDodgeGivesEitr | — :x: | DecreaseDodgeCost | ReduceFallDamage | DodgeBuff | PerfectDodgeGivesSpeed | PerfectDodge | RollCleanse |
| **Black** (Night) | IncreaseDamageDuringNighttime  | IncreaseDamageDuringNighttime | IncreaseDamageDuringNighttime  | NightBlocker | NightStaminaRegenIncrease | DamageReductionAtNight | AddKnivesSkill  | NightCarryWeight  | SummonBatWhenActivatingAdrenaline :x: | ModifyNoise  |
| **White**  (Day) | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | DayBlocker | DayDiscovery | DayArmor | DayStaminaRegen | DaySailingSpeed | DayHealthRegen | AddCrafterSkills |
| **Green** (Movement) | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | — :x: | IncreaseXPGainFromMovementPenalty | CarryWeightForMovementPenalty | StaminaIncreaseForMovementPenalty | ArmorFromMovementPenalty | AddMovementSkills | ModifyJumpStaminaUse |
| **Purple** (Eitr/Blood) | EitrLeech | EitrLeech | ModifyMagicFireRate | — :x: | DartingThoughts | ConsumeEitrFirstForBloodCosts | EveryXPointsOfEitrIncreasesStamina | ReduceEitrCost | ConvertEitrCostToStaminaCost | RunningOnEmpty |
| **Grey** (Harvest) | IncreaseHarvestDamage | IncreaseHarvestDamage | IncreaseHarvestDamage | — :x: | IncreaseMiningDrop | AddFishingSkill | IncreaseTreeDrop | ReduceFishingStaminaCost | GainAdrenalineFromHarvesting | IncreaseHarvestXPGain |

## Dark shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **DarkRed** |  IncreaseMeleeSkills | IncreaseRangedSkills | AddBluntDamage | — :x: | HeadHunter | ChanceToCritOnHit | BloodDrinker | ReduceArmorIncreaseDamage | AdrenalineCharge | OffSetAttack |
| **DarkGreen** (Poison) | AddPoisonDamage | AddPoisonDamage | AddPoisonDamage | PhysToPoisonOnBlock | AddPoisonResistancePercentage | PhysToPoison | AddBlockingSkill | PoisonToTrueDamage | GainAdrenalineWhenApplyingPoison | IncreaseAllPoisonDamageDone |
| **DarkBlue** (Frost) | AddFrostDamage | AddFrostDamage | AddFrostDamage | PhysToFrostOnBlock | AddFrostResistancePercentage | PhysToFrost | AddElementalMagicSkill | IcyWeight | AdrenalineFrostWave | Warmth |
| **DarkPurple** (Blood) | ModifyAttackHealthUse | ModifyAttackHealthUse | ModifyAttackHealthUse | — :x: | KillsReduceNextBloodCost | ReflectDamage | BloodMagicLevelIncreasesHealthRegen | GainEitrWhenSacrificingHealth | GainAdrenalineWhenSacrificingHealth | AddBloodMagicSkill |
| **Golden** (Luck) | Mercenary | Mercenary | Mercenary | — :x: | Wager | Coinplated | LuckWhileFishing | LuckyCraft | Luck | Riches |

## Light shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **LightBlue** (Lightning) | AddLightningDamage | AddLightningDamage | AddLightningDamage | PhysToLightningOnBlock | AddLightningResistancePercentage | PhysToLightning | StormRider | Conduit | StormFury | ConvertPhysicalDamageToLightning |
| **LightGreen** | HealthGainPerXDamageDone | HealthGainPerXDamageDone | HealthGainPerXDamageDone | — :x: | PotionEfficacy | Comfortable | AddPickaxesSkill | RestingHealthRegen | AdrenalineIncreasesHealthRegen | BountifulHarvest |
| **Peach** (Weight) | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | — :x: | GainMaxStaminaBasedOnPlayerMaxHealth | StaminaRegenBonusFromPlayerWeight | GainMaxCarryWeightFromRested | TravelLight | SailingSpeed | AddCarryWeight |

## Boss shards (uniform — one effect on any slot)

Boss shards use `UniformEffect`, so they already work in a shield socket — they just grant their signature
effect rather than anything shield-specific. They are not part of the shield gap below.

| Shard | Rarity | Effect (all slots) |
|---|---|---|
| **Eikthyr** | Rare | ShockingCharge ☑️ |
| **Elder** | Rare | ForestsAid ☑️ |
| **Bonemass** | Epic | CorpseRot ☑️ |
| **Moder** | Epic | IcyRetribution ☑️|
| **Yagluth** | Legendary | MeteorSummoner ☑️ |
| **Queen** | Legendary | Everflow ☑️ |
| **Fader** | Mythic | NecroticFire ☑️|

## Shield effects

Nine of the eighteen non-boss shards define a `Shield` effect. They fall into three implementation
families, and — importantly — **two different trigger mechanisms**.

### On-block resource gain

Fires from the `Humanoid.BlockAttack` postfix. Flat resource restored per successful block.

| Shard | EffectType | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| **Red** | LifeGainOnBlock | 1 | 1.5 | 2 | 2.5 | 3 |
| **Yellow** | StaminaGainOnBlock | 1 | 1.5 | 2 | 2.5 | 3 |
| **Cyan** | EitrGainOnBlock | 1 | 1.5 | 2 | 2.5 | 3 |

Implemented in [`GainOnBlockResource.GainOnBlock`](src/Magic/MagicItemEffects/Shards/GainOnBlock.cs).

### On-block incoming damage conversion

Fires from the same postfix. Moves a percentage of incoming **physical** damage onto an element *before*
resistances apply — so pairing one with the matching resistance shard (that shard's Head slot) turns raw
physical into damage the player resists. Values are percentages.

| Shard | EffectType | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| **Orange** | PhysToFireOnBlock | 4% | 6% | 8% | 10% | 12% |
| **DarkBlue** | PhysToFrostOnBlock | 4% | 6% | 8% | 10% | 12% |
| **DarkGreen** | PhysToPoisonOnBlock | 4% | 6% | 8% | 10% | 12% |
| **LightBlue** | PhysToLightningOnBlock | 4% | 6% | 8% | 10% | 12% |

Implemented in [`IncomingPhysicalConversion.ModifyIncoming`](src/Magic/MagicItemEffects/Shards/DamageConversionEffects.cs),
gated behind the `IsBlocked` flag — these stack additively on top of the always-on `PhysTo*` Chest effects,
and the combined total is clamped so never more physical is converted than the hit actually contains.

### Blocking-skill XP

**Not** an on-block trigger. These are a `Skills.RaiseSkill` prefix filtered to `SkillType.Blocking` — a
passive multiplier on all Blocking XP gained during the relevant time of day. Values are percentages.

| Shard | EffectType | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| **White** | DayBlocker | 2% | 4% | 6% | 8% | 10% |
| **Black** | NightBlocker | 10% | 15% | 20% | 25% | 30% |

Implemented in [`IncreasedXPGainFromBlockDayNight`](src/Magic/MagicItemEffects/Shards/IncreasedXPGainFromBlockDayNight.cs).
The in-file comment flags the mechanism as provisional — it may move to a true on-block trigger if future
interactions call for it.

### Where the on-block effects are hooked

Both live-block families hang off one consolidated postfix on `Humanoid.BlockAttack` in
[`SharedHumanoidBlockAttackPatch`](src/Magic/MagicItemEffects/Helpers/SharedHumanoidBlockAttackPatch.cs),
which returns early unless the block actually succeeded, then calls each family's handler in a fixed order.
Effects keep their own guards inside their handlers, so ordering there is not load-bearing.

### Shards still needing a Shield effect

Nine shards define all their other slots but omit `Shield`:

**Pink**, **Green**, **Purple**, **Grey**, **DarkRed**, **DarkPurple**, **Golden**, **LightGreen**, **Peach**

Socketing one of these into a shield is *permitted* but **inert** — the shard occupies the socket and the
tooltip says so, but grants nothing. See `ResolveCategory` / `GetShardEffect` in
[`Shards.cs`](src/ShardStones/Shards.cs), which intentionally returns no effect rather than substituting
one authored for a different kind of gear.

Block-adjacent effects already exist on *other* slots of some of these shards and are worth keeping in mind
when filling the gaps, to avoid overlap: DarkGreen Legs `AddBlockingSkill`, Cyan Utility `EitrShield`, and
DarkPurple Chest `ReflectDamage`.

## Notes on slot resolution

Slot resolution happens at socket time in [`Shards.GetShardEffect` / `ResolveCategory`](src/ShardStones/Shards.cs).

- The config only defines the **broad group** keys above (`MeleeWeapon`, `RangedWeapon`, `MagicWeapon`,
  `Shield`, `Head`, `Chest`, `Legs`, `Shoulders`, `Trinket`, `Utility`).
- `ResolveCategory` first maps a host item to a *fine* type (Swords, Bows, Bucklers, etc.), then falls back to its
  group — so, e.g., a sword and a club both pick up the `MeleeWeapon` effect since no fine-type effects are defined.
- The three fine shield slots (`Bucklers`, `RoundShields`, `TowerShields`) all fall back to the `Shield`
  group, and no shard defines an effect for any of them — every shield of a given shard gets the same
  effect regardless of subtype.
- The fine type itself comes from [`ItemTypeClassifier`](src/GatedItemType/ItemTypeClassifier.cs), the mod-wide
  answer to "which `iteminfo.json` type is this item?" — the item's configured entry when it has one, else a
  raw-field heuristic. `ItemInfoTypeToSlot` is only the shard-specific mapping over that shared vocabulary.
- An item that cannot be classified at all (unlisted *and* unrecognizable) yields **no slot**: the shard sits in
  the socket inert rather than being handed some other slot's effect.
