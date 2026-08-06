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
| **Pink** (Dodge) | PerfectDodgeGivesStamina | PerfectDodgeGivesStamina | PerfectDodgeGivesEitr | BlockAsDodgeAsBlock | DecreaseDodgeCost | ReduceFallDamage | DodgeBuff | PerfectDodgeGivesSpeed | PerfectDodge | RollCleanse |
| **Black** (Night) | IncreaseDamageDuringNighttime  | IncreaseDamageDuringNighttime | IncreaseDamageDuringNighttime  | NightBlocker | NightStaminaRegenIncrease | DamageReductionAtNight | AddKnivesSkill  | NightCarryWeight  | SummonBatWhenActivatingAdrenaline ☑️ | ModifyNoise  |
| **White**  (Day) | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | IncreaseDamageDuringDaytime | DayBlocker | DayDiscovery | DayArmor | DayStaminaRegen | DaySailingSpeed | DayHealthRegen | AddCrafterSkills |
| **Green** (Movement) | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | DamageIncreaseFromMovementPenalty | AnchoredBlock | IncreaseXPGainFromMovementPenalty | CarryWeightForMovementPenalty | StaminaIncreaseForMovementPenalty | ArmorFromMovementPenalty | AddMovementSkills | ModifyJumpStaminaUse |
| **Purple** (Eitr/Blood) | EitrLeech | EitrLeech | ModifyMagicFireRate | ElementalWarding | DartingThoughts | ConsumeEitrFirstForBloodCosts | EveryXPointsOfEitrIncreasesStamina | ReduceEitrCost | ConvertEitrCostToStaminaCost | RunningOnEmpty |
| **Grey** (Harvest) | IncreaseHarvestDamage | IncreaseHarvestDamage | IncreaseHarvestDamage | — :x: | IncreaseMiningDrop | AddFishingSkill | IncreaseTreeDrop | ReduceFishingStaminaCost | GainAdrenalineFromHarvesting | IncreaseHarvestXPGain |

## Dark shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **DarkRed** |  IncreaseMeleeSkills | IncreaseRangedSkills | AddBluntDamage | BloodBaseBlock | HeadHunter | Bloodrage | BloodDrinker | ReduceArmorIncreaseDamage | AdrenalineCharge | OffSetAttack |
| **DarkGreen** (Poison) | AddPoisonDamage | AddPoisonDamage | AddPoisonDamage | PhysToPoisonOnBlock | AddPoisonResistancePercentage | PhysToPoison | AddBlockingSkill | PoisonToTrueDamage | GainAdrenalineWhenApplyingPoison | IncreaseAllPoisonDamageDone |
| **DarkBlue** (Frost) | AddFrostDamage | AddFrostDamage | AddFrostDamage | PhysToFrostOnBlock | AddFrostResistancePercentage | PhysToFrost | AddElementalMagicSkill | IcyWeight | AdrenalineFrostWave | Warmth |
| **DarkPurple** (Blood) | ModifyAttackHealthUse | ModifyAttackHealthUse | ModifyAttackHealthUse | BloodStaggerBlock | KillsReduceNextBloodCost | ReflectDamage | BloodMagicLevelIncreasesHealthRegen | GainEitrWhenSacrificingHealth | GainAdrenalineWhenSacrificingHealth | AddBloodMagicSkill |
| **Golden** (Luck) | ChanceDoubleDamage | ChanceDoubleDamage | ChanceDoubleDamage | LuckyBlock | Inspiration | LuckyLoot | LuckWhileFishing | LuckyCraft | Luck | Riches |

## Light shards

| Shard | Melee Wpn | Ranged Wpn | Magic Wpn | Shield | Head | Chest | Legs | Shoulders | Trinket | Utility |
|---|---|---|---|---|---|---|---|---|---|---|
| **LightBlue** (Lightning) | AddLightningDamage | AddLightningDamage | AddLightningDamage | PhysToLightningOnBlock | AddLightningResistancePercentage | PhysToLightning | StormRider | Conduit | StormFury | ConvertPhysicalDamageToLightning |
| **LightGreen** | HealthGainPerXDamageDone | HealthGainPerXDamageDone | HealthGainPerXDamageDone | Warding | PotionEfficacy | Comfortable | AddPickaxesSkill | RestingHealthRegen | AdrenalineIncreasesHealthRegen | BountifulHarvest |
| **Peach** (Weight) | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | DamageBonusFromPlayerWeight | BurdenedBlock | GainMaxStaminaBasedOnPlayerMaxHealth | StaminaRegenBonusFromPlayerWeight | GainMaxCarryWeightFromRested | TravelLight | SailingSpeed | AddCarryWeight |

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

## Unique shards (uniform — one effect on any slot)

Same `UniformEffect` shape as the boss shards, and likewise outside the shield gap below. They differ in
where they come from and how they stack: uniques have no boss to kill for them, dropping instead from
elite creatures (`Tier3EliteMob`…`Tier8EliteMob`) and from Swamp-tier-and-above treasure/dungeon chests
via the `ShardUnique` item set in [`config/loottables.json`](config/loottables.json), always at Epic or
better. The early-game chests are deliberately excluded — an Epic unique out of a Meadows chest is a
bigger power spike than the drop rate suggests.

Because they have no per-color gate, their upgrade surcharge scales with the ladder instead: each
`ShardStoneUpgrade_{Firewalker,Stormcaller}_*` recipe in
[`config/shardstoneconversions.json`](config/shardstoneconversions.json) charges two blank runestones of
the rarity being upgraded *from* — `RunestoneEpic` on Epic → Legendary, `RunestoneLegendary` on
Legendary → Mythic — on top of the classic shards the step itself costs. Boss shards carry the
equivalent surcharge per-color as their own trophy, which is what makes leveling one gate on having
beaten that boss.

`ShardCategory.Unique` is exclusive, but exclusivity is enforced **per category**
([`ShardSocketManager.CheckExclusiveCategory`](src/ShardStones/ShardSocketManager.cs)) — so a unique shard
and a boss shard may be worn together, one of each, and never two uniques.

| Shard | Rarity | Effect (all slots) |
|---|---|---|
| **Firewalker** | Epic | Trailblazer ☑️ |
| **Stormcaller** | Epic | StrikeCausesLightning ☑️ |

## Implemented but unassigned effects

Seven effect ids are declared in [`MagicEffectType_Shards.cs`](src/Magic/MagicEffectType_Shards.cs) and have
behavior code checked in, but occupy **no slot in any shard above** and have no entry in any
[`config/overhauls/*/magiceffects.json`](config/overhauls/) either. That combination makes them completely
inert: [`ShardEffectDefinitions`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs) builds its
definition list by walking the shard grid, so an unassigned id gets no `MagicItemEffectDefinition` at all —
nothing can roll it, no shard can grant it, and `GetTotalActiveMagicEffectValue` returns 0 for every player.
All seven still have their `_display`/`_desc` strings in
[`localizations/English.json`](localizations/English.json), so assigning one to a grid slot is enough to
make it show up correctly in tooltips.

They are **not** equally close to working. Six are one config edit away; one also needs code restored.

| EffectType | Code | Hook status | To re-enable |
|---|---|---|---|
| **Wager** | [Wager.cs](src/Magic/MagicItemEffects/Shards/Wager.cs) | Live | Assign a slot |
| **Mercenary** | [Mercenary.cs](src/Magic/MagicItemEffects/Shards/Mercenary.cs) | Live | Assign a slot |
| **Coinplated** | [Coinplated.cs](src/Magic/MagicItemEffects/Shards/Coinplated.cs) | Live | Assign a slot |
| **ChanceToCritOnHit** | [ChanceToCritOnHit.cs](src/Magic/MagicItemEffects/Shards/ChanceToCritOnHit.cs) | Live | Assign a slot |
| **PerfectDodgeGivesHealth** | [PerfectDodgeEffects.cs:51](src/Magic/MagicItemEffects/Shards/PerfectDodgeEffects.cs#L51) | Live | Assign a slot |
| **StaminaReturnFromEitr** | [StaminaReturnFromEitr.cs](src/Magic/MagicItemEffects/Shards/StaminaReturnFromEitr.cs) | Live | Assign a slot |
| **BatteringRam** | [BatteringRam.cs](src/Magic/MagicItemEffects/Shards/BatteringRam.cs) | **Whole file commented out** | Uncomment, then assign a slot |

### Ready to assign — config only

The first four were vacated together when Golden's kit was re-themed from coins to luck and DarkRed's
Chest moved to `Bloodrage`. All four are cheaper to revive than the rest of this list: besides keeping
their code and their dispatcher call sites, they keep their per-effect `Config` blocks in
[`ShardEffectDefinitions.EffectConfigs`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs),
which sit dormant (`BuildDefinition` is only reached for effects the grid actually uses) until a slot
assignment brings them back with their tuning intact.

- **Wager** — stakes coins on each hit for flat bonus damage, refunded on a kill. Called from the
  `Character.Damage` dispatcher, both as an outgoing modifier and as the on-kill refund
  ([SharedCharacterDamagePatch.cs:34, :62](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L34)).
  Vacated when Golden's Head slot moved to `Inspiration`.
- **Mercenary** — spends coins per hit for a percentage damage bonus on a soft-capped curve. Called from
  [SharedCharacterDamagePatch.cs:33](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L33).
  Vacated when Golden's three weapon slots moved to `ChanceDoubleDamage`.
- **Coinplated** — commits a share of the purse to absorbing each incoming hit. Called from
  [SharedCharacterRpcDamagePatch.cs:48](src/Magic/MagicItemEffects/Helpers/SharedCharacterRpcDamagePatch.cs#L48).
  Vacated when Golden's Chest slot moved to `LuckyLoot`.
- **ChanceToCritOnHit** — flat proc chance to crit for 2x. Called from
  [SharedCharacterDamagePatch.cs:36](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L36).
  Vacated when DarkRed's Chest slot moved to `Bloodrage`. Note it is the same shape as
  `ChanceDoubleDamage` (Golden weapons) with a different intent, so reassigning it to a weapon slot
  risks a near-duplicate.
- **PerfectDodgeGivesHealth** — restores a % of max health on a perfect dodge. Already called from
  `SharedPerfectDodgeRewardPatch`, alongside its Stamina/Eitr/Speed siblings which *are* assigned (Pink's
  weapon and Shoulders slots). It is the only member of that family without a home.
- **StaminaReturnFromEitr** — refunds a % of spent eitr as stamina. Self-contained `Player.UseEitr` postfix,
  no dispatcher involvement. Never assigned to a slot at any point.

### Needs code restored as well

- **BatteringRam** — blunt damage from running into enemies, scaled by carried weight and speed. The file's
  own comment records why it was disabled: the per-frame `Player.Update` patch it needs was "mildly
  expensive" for an effect nothing granted. Peach's Shoulders slot moved to `TravelLight`.

## Known quirks, accepted deliberately

Recorded here so they read as decisions rather than oversights.

- **`ChanceDoubleDamage` and projectiles.** The effect is weapon-scoped — it reads the `MagicItem` of the
  weapon the shard is socketed into, via `MagicEffectsHelper.GetActiveWeapon`, so only that weapon procs.
  For bows and staves the hit resolves when the projectile lands, so the weapon is re-read at that
  moment; firing and then swapping weapons mid-flight reads the wrong one. `Executioner` solves this by
  stamping its multiplier onto the projectile's ZDO, which is the fix if it ever matters here.
- **`Bloodrage` scales chop and pickaxe damage.** `SE_Bloodrage.ModifyAttack` applies the bonus with
  `HitData.DamageTypes.Modify(float)`, which also scales `m_chop` and `m_pickaxe` — so raging speeds up
  tree-felling and mining slightly. Vanilla's own `SE_Stats.ModifyAttack` behaves identically and the
  ceiling is +25%, so a per-damage-type multiplier is more code than the quirk is worth.
- **`Bloodrage` can proc on an avoided hit.** It hangs off the `Character.RPC_Damage` postfix, and Harmony
  runs postfixes even when the dispatcher prefix cancels the method for `AvoidDamageTaken`.
  `DamageTakenGivesAdrenaline` has the identical behaviour today, so this matches its sibling rather than
  special-casing.
- **`LuckyLoot` needs a `CharacterDrop`.** The proc is rolled inside the `CharacterDrop.GenerateDropList`
  postfix, so a creature with no `CharacterDrop` component (or with `Ragdoll.m_dropItems` off) can never
  proc it — including the bonus magic-item half. See the header comment in
  [`LuckyLoot.cs`](src/Magic/MagicItemEffects/Shards/LuckyLoot.cs) for the full two-path timing diagram
  and why the decision has to travel through the ragdoll's ZDO.
- **`Inspiration` is the one effect whose grid value is not a percent.** Its 10/15/20/25/30 ramp is a count
  of raw skill-accumulator points, read with no `0.01f` scale. "Fixing" that for consistency would nerf
  the effect 100x; the warning is repeated at the top of
  [`Inspiration.cs`](src/Magic/MagicItemEffects/Shards/Inspiration.cs). Its proc chance is the percent,
  and lives in the effect's `Config` block so it can be retuned without a rebuild.

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
