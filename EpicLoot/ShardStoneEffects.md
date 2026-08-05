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
| **DarkRed** |  IncreaseMeleeSkills | IncreaseRangedSkills | AddBluntDamage | BloodBaseBlock | HeadHunter | ChanceToCritOnHit | BloodDrinker | ReduceArmorIncreaseDamage | AdrenalineCharge | OffSetAttack |
| **DarkGreen** (Poison) | AddPoisonDamage | AddPoisonDamage | AddPoisonDamage | PhysToPoisonOnBlock | AddPoisonResistancePercentage | PhysToPoison | AddBlockingSkill | PoisonToTrueDamage | GainAdrenalineWhenApplyingPoison | IncreaseAllPoisonDamageDone |
| **DarkBlue** (Frost) | AddFrostDamage | AddFrostDamage | AddFrostDamage | PhysToFrostOnBlock | AddFrostResistancePercentage | PhysToFrost | AddElementalMagicSkill | IcyWeight | AdrenalineFrostWave | Warmth |
| **DarkPurple** (Blood) | ModifyAttackHealthUse | ModifyAttackHealthUse | ModifyAttackHealthUse | BloodStaggerBlock | KillsReduceNextBloodCost | ReflectDamage | BloodMagicLevelIncreasesHealthRegen | GainEitrWhenSacrificingHealth | GainAdrenalineWhenSacrificingHealth | AddBloodMagicSkill |
| **Golden** (Luck) | Mercenary | Mercenary | Mercenary | — :x: | Wager | Coinplated | LuckWhileFishing | LuckyCraft | Luck | Riches |

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

## Shield effects

Sixteen of the eighteen non-boss shards define a `Shield` effect. They fall into six implementation
families across **four different trigger mechanisms** — only the first three families fire on the block
itself.

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

### On-block resource spend for mitigation

Also fires from the `Humanoid.BlockAttack` postfix, but *spends* a resource instead of gaining one: the
value is a percentage of the player's **max** pool, and up to that much damage is paid off the hit at a
1:1 rate. Nothing is spent (and nothing mitigated) if the current pool can't cover the whole reduction.
Each drains its damage type in a fixed order and plays an FX only when the reduction exceeds 5.

| Shard | EffectType | Pool spent | Damage removed | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|---|---|
| **LightGreen** | Warding | Stamina | Physical (pierce → blunt → slash) | 4% | 8% | 12% | 16% | 20% |
| **Purple** | ElementalWarding | Eitr | Elemental (fire → frost → lightning) | 4% | 8% | 12% | 16% | 20% |

Implemented in [`Warding.UseMoreStaminaOnBlock`](src/Magic/MagicItemEffects/Shards/Warding.cs) and
[`ElementalWarding.UseEitrOnBlock`](src/Magic/MagicItemEffects/Shards/ElementalWarding.cs). They run
*after* the conversion family in the postfix, so a `PhysTo*OnBlock` shard on the same shield moves damage
onto an element first — which `ElementalWarding` can then pay off and `Warding` no longer sees.

### Base block power bonuses

**Not** an on-block trigger. These are a postfix on `ItemDrop.ItemData.GetBaseBlockPower`, so the bonus is
folded into the shield's block power *before* skill factor scales it and shows up in the tooltip total.
Each is a flat block-power grant per unit of some other resource, so the value is a per-increment rate
rather than a percentage.

| Shard | EffectType | Scales with | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|---|
| **Green** | AnchoredBlock | Movement penalty (per 1% of speed lost) | 0.25 | 0.5 | 0.75 | 1 | 1.25 |
| **Peach** | BurdenedBlock | Carried weight (per 50 over 300) | 0.25 | 0.5 | 0.75 | 1 | 1.25 |
| **DarkRed** | BloodBaseBlock | Nothing — flat grant, paid for in health | 3 | 6 | 9 | 12 | 15 |

Implemented in [`AnchoredBlock`](src/Magic/MagicItemEffects/Shards/AnchoredBlock.cs),
[`BurdenedBlock`](src/Magic/MagicItemEffects/Shards/BurdenedBlock.cs) and
[`BloodBaseBlock`](src/Magic/MagicItemEffects/Shards/BloodBaseBlock.cs), all called from the
`BlockBASEPowerPatch` postfix in the same helpers file as the block patch. `AnchoredBlock` reads
`PenaltyScaling.MovementPenalty`, which returns 0.01 per 1% of movement speed lost.

### Blood-cost blocking

The two `Blood*Block` shards charge health for their benefit: a `Humanoid.UpdateBlock` postfix fires once
at the *start* of each block (`m_blockTimer == 0`) and deals **5% of max health** as untyped true damage —
hard-coded, not rarity-scaled — routed through `player.Damage` as a `HitType.Self` hit so on-hit effects
still trigger. The DarkRed half buys the flat block power in the table above; the DarkPurple half buys
stagger mitigation, applied as a `Character.AddStaggerDamage` prefix that only bites while blocking.

| Shard | EffectType | Benefit | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|---|
| **DarkPurple** | BloodStaggerBlock | Stagger damage taken while blocking | -5% | -10% | -15% | -20% | -25% |

See [`BloodStaggerBlock`](src/Magic/MagicItemEffects/Shards/BloodStaggerBlock.cs). The self-damage block is
duplicated verbatim in both files, so socketing both shards charges the health cost **twice** per block.

### Skill-level grant

**Not** an on-block trigger. `BlockAsDodgeAsBlock` adds flat skill *levels* (not XP) to both Blocking and
Dodge, through the shared `Skills.GetSkillFactor` postfix in
[`AddSkillLevel`](src/Magic/MagicItemEffects/AddSkillLevel.cs) — the skill pair lives in
[`BlockAsDodgeAsBlock.BADAB`](src/Magic/MagicItemEffects/Shards/BlockAsDodgeAsBlock.cs). Because
`SkillIncrease` casts to `int`, only whole values count.

| Shard | EffectType | Magic | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| **Pink** | BlockAsDodgeAsBlock | +3 | +6 | +9 | +12 | +15 |

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

All three live-block families hang off one consolidated postfix on `Humanoid.BlockAttack` in
[`SharedHumanoidBlockAttackPatch`](src/Magic/MagicItemEffects/Helpers/SharedHumanoidBlockAttackPatch.cs),
which returns early unless the block actually succeeded, then calls each family's handler in a fixed order.
Effects keep their own guards inside their handlers, so ordering is mostly not load-bearing — the one
exception is that `IncomingPhysicalConversion` runs first, which changes what damage types the two
`*Warding` handlers find left on the hit.

The same file also owns `BlockBASEPowerPatch` (the `GetBaseBlockPower` postfix the three base-block-power
effects share). The remaining shield effects patch elsewhere: `Humanoid.UpdateBlock` and
`Character.AddStaggerDamage` for the blood costs, `Skills.GetSkillFactor` for `BlockAsDodgeAsBlock`, and
`Skills.RaiseSkill` for the day/night blocking-XP pair.

### Shards still needing a Shield effect

Two shards define all their other slots but omit `Shield`:

**Grey** and **Golden**

Socketing one of these into a shield is *permitted* but **inert** — the shard occupies the socket and the
tooltip says so, but grants nothing. See `ResolveCategory` / `GetShardEffect` in
[`Shards.cs`](src/ShardStones/Shards.cs), which intentionally returns no effect rather than substituting
one authored for a different kind of gear.

Neither shard has anything block-adjacent elsewhere in its own row to build on — Grey is harvest-themed and
Golden is luck-themed throughout. Block-adjacent effects that already exist on *other* shards are worth
keeping in mind when filling these two gaps, to avoid overlap: DarkGreen Legs `AddBlockingSkill`, Cyan
Utility `EitrShield`, and DarkPurple Chest `ReflectDamage`.

## Implemented but unassigned effects

Six effect ids are declared in [`MagicEffectType_Shards.cs`](src/Magic/MagicEffectType_Shards.cs) and have
behavior code checked in, but occupy **no slot in any shard above** and have no entry in any
[`config/overhauls/*/magiceffects.json`](config/overhauls/) either. That combination makes them completely
inert: [`ShardEffectDefinitions`](src/Magic/MagicItemEffects/Helpers/ShardEffectDefinitions.cs) builds its
definition list by walking the shard grid, so an unassigned id gets no `MagicItemEffectDefinition` at all —
nothing can roll it, no shard can grant it, and `GetTotalActiveMagicEffectValue` returns 0 for every player.
All six still have their `_display`/`_desc` strings in
[`localizations/English.json`](localizations/English.json), so assigning one to a grid slot is enough to
make it show up correctly in tooltips.

They are **not** equally close to working. Three are one config edit away; three also need code restored.

| EffectType | Code | Hook status | To re-enable |
|---|---|---|---|
| **ChanceDoubleDamage** | [ChanceDoubleDamage.cs](src/Magic/MagicItemEffects/Shards/ChanceDoubleDamage.cs) | Live | Assign a slot |
| **PerfectDodgeGivesHealth** | [PerfectDodgeEffects.cs:51](src/Magic/MagicItemEffects/Shards/PerfectDodgeEffects.cs#L51) | Live | Assign a slot |
| **StaminaReturnFromEitr** | [StaminaReturnFromEitr.cs](src/Magic/MagicItemEffects/Shards/StaminaReturnFromEitr.cs) | Live | Assign a slot |
| **Trailblazer** | [Trailblazer.cs](src/Magic/MagicItemEffects/Shards/Trailblazer.cs) | Patch live, **prefab registration removed** | Assign a slot **and** re-add `Trailblazer.RegisterVfxPrefab` |
| **StrikeCausesLightning** | [StrikeCausesLightning.cs](src/Magic/MagicItemEffects/Shards/StrikeCausesLightning.cs) | **No call site**, prefab registration removed | Assign a slot **and** re-add both the dispatcher call and `RegisterVisualPrefab` |
| **BatteringRam** | [BatteringRam.cs](src/Magic/MagicItemEffects/Shards/BatteringRam.cs) | **Whole file commented out** | Uncomment, then assign a slot |

### Ready to assign — config only

- **ChanceDoubleDamage** — flat proc chance to double a hit. Already called from the `Character.Damage`
  dispatcher ([SharedCharacterDamagePatch.cs:35](src/Magic/MagicItemEffects/Helpers/SharedCharacterDamagePatch.cs#L35)).
  Freed up when Golden's weapon slots moved to `Mercenary`. Note `ChanceToCritOnHit` (DarkRed Chest) is the
  same shape with a different intent, so re-using this one risks a near-duplicate on a weapon slot.
- **PerfectDodgeGivesHealth** — restores a % of max health on a perfect dodge. Already called from
  `SharedPerfectDodgeRewardPatch`, alongside its Stamina/Eitr/Speed siblings which *are* assigned (Pink's
  weapon and Shoulders slots). It is the only member of that family without a home.
- **StaminaReturnFromEitr** — refunds a % of spent eitr as stamina. Self-contained `Player.UseEitr` postfix,
  no dispatcher involvement. Never assigned to a slot at any point.

### Needs code restored as well

- **Trailblazer** — a burning trail laid while running. Its `Player.Update` postfix is still patched, but the
  whole effect is carried by the `EL_TrailblazerFire` prefab it spawns (`TrailblazerFire` does the damage),
  and `RegisterVfxPrefab` is no longer subscribed to `PrefabManager.OnPrefabsRegistered` — see the NOTE at
  [EpicLoot.cs:433](EpicLoot.cs#L433). Without that line it logs a missing-prefab warning once and does
  nothing, so this is not merely a cosmetic gap. Vacated when Orange's Legs slot moved to `Kindling`.
- **StrikeCausesLightning** — proc chance to call a lightning strike on the target. Furthest from working:
  `OnDamageDealt` exists but nothing calls it — `87a7a56f` ("unique effects rotated out") dropped the
  `SharedCharacterDamagePatch` line and the `RegisterVisualPrefab` subscription together, so a slot
  assignment alone would still do nothing.
  Vacated when LightBlue's Shoulders slot moved to `Conduit`.
- **BatteringRam** — blunt damage from running into enemies, scaled by carried weight and speed. The file's
  own comment records why it was disabled: the per-frame `Player.Update` patch it needs was "mildly
  expensive" for an effect nothing granted. Peach's Shoulders slot moved to `TravelLight`.

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
