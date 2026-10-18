using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot.Compendium;

public static class OffenseTextSection
{
    private const string BetterColor = "#8aff8aff";
    private const string WorseColor = "#ff8a8aff";
    private const string NeutralColor = "#c0c0c0ff";
    private const string Dash = "<color=#808080ff>-</color>";

    private class DamageRow
    {
        public string Key;
        public Func<HitData.DamageTypes, float> Get;
        public string AddEffect;
        public bool Physical;
        public bool Combat;
    }

    private static readonly DamageRow[] DamageRows =
    {
        new DamageRow { Key = "$inventory_damage", Get = x => x.m_damage, Combat = true },
        new DamageRow { Key = "$inventory_blunt", Get = x => x.m_blunt, AddEffect = MagicEffectType.AddBluntDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_slash", Get = x => x.m_slash, AddEffect = MagicEffectType.AddSlashingDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_pierce", Get = x => x.m_pierce, AddEffect = MagicEffectType.AddPiercingDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_fire", Get = x => x.m_fire, AddEffect = MagicEffectType.AddFireDamage, Combat = true },
        new DamageRow { Key = "$inventory_frost", Get = x => x.m_frost, AddEffect = MagicEffectType.AddFrostDamage, Combat = true },
        new DamageRow { Key = "$inventory_lightning", Get = x => x.m_lightning, AddEffect = MagicEffectType.AddLightningDamage, Combat = true },
        new DamageRow { Key = "$inventory_poison", Get = x => x.m_poison, AddEffect = MagicEffectType.AddPoisonDamage, Combat = true },
        new DamageRow { Key = "$inventory_spirit", Get = x => x.m_spirit, AddEffect = MagicEffectType.AddSpiritDamage, Combat = true },
        new DamageRow { Key = "$inventory_chop", Get = x => x.m_chop, AddEffect = MagicEffectType.AddSlashingDamage, Physical = true },
        new DamageRow { Key = "$inventory_pickaxe", Get = x => x.m_pickaxe, AddEffect = MagicEffectType.AddPiercingDamage, Physical = true },
    };

    private static readonly string[] WholeWeaponEffects =
    {
        MagicEffectType.ModifyDamage, MagicEffectType.ModifyDamageLowHealth, MagicEffectType.SpellSword,
        MagicEffectType.CoinHoarder, MagicEffectType.DodgeBuff, MagicEffectType.EitrInfusion,
        MagicEffectType.EitrImbueAttack, MagicEffectType.IncreaseDamageDuringDaytime,
        MagicEffectType.IncreaseDamageDuringNighttime, MagicEffectType.DamageBonusFromPlayerWeight,
        MagicEffectType.DamageIncreaseFromMovementPenalty, MagicEffectType.IncreaseHarvestDamage,
        MagicEffectType.ConvertPhysicalDamageToLightning, MagicEffectType.CoilingVenom, MagicEffectType.IcyWeight,
        MagicEffectType.ReduceArmorIncreaseDamage, MagicEffectType.NecroticFire,
    };

    public static IEnumerable<string> Covered
    {
        get
        {
            foreach (DamageRow row in DamageRows)
            {
                if (row.AddEffect != null)
                {
                    yield return row.AddEffect;
                }
            }

            foreach (string effect in WholeWeaponEffects)
            {
                yield return effect;
            }

            string[] others =
            {
                MagicEffectType.ModifyPhysicalDamage, MagicEffectType.ModifyElementalDamage, MagicEffectType.ModifyAttackSpeed,
                MagicEffectType.ModifyAttackSpeedLowHealth, MagicEffectType.ModifyStaggerDamage, MagicEffectType.ModifyStaggerDuration,
                MagicEffectType.ModifyBackstab, MagicEffectType.ModifyAttackStaminaUse, MagicEffectType.Bloodlust,
                MagicEffectType.ModifyAttackEitrUse, MagicEffectType.ConjuredArrows, MagicEffectType.ModifyAttackHealthUse,
                MagicEffectType.ModifyDrawStaminaUse, MagicEffectType.CloseQuarter, MagicEffectType.LifeSteal,
                MagicEffectType.LifeStealLowHealth,
            };
            foreach (string effect in others)
            {
                yield return effect;
            }
        }
    }

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        List<ItemDrop.ItemData> weapons = new();
        if (IsWeapon(player.m_rightItem))
        {
            weapons.Add(player.m_rightItem);
        }

        if (IsWeapon(player.m_leftItem))
        {
            weapons.Add(player.m_leftItem);
        }

        if (weapons.Count == 0)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_offense</size>");
        foreach (ItemDrop.ItemData weapon in weapons)
        {
            BuildWeapon(instance, player, weapon);
        }
    }

    public static bool IsWeapon(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Torch:
                return true;
            default:
                return false;
        }
    }

    private static void BuildWeapon(MagicPages instance, Player player, ItemDrop.ItemData weapon)
    {
        MagicItem magicItem = weapon.GetMagicItem();
        ItemDrop.ItemData otherWeapon = weapon == player.m_rightItem ? player.m_leftItem : player.m_rightItem;
        Func<ItemDrop.ItemData, bool> include = x => x != otherWeapon || !IsWeapon(otherWeapon);

        instance.MagicPagesTextArea.Add($"<size={MagicPages.MEDIUM_FONT_SIZE}>{weapon.GetDecoratedName()}</size>");
        MagicTextTable table = instance.MagicPagesTextArea.AddTable(
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_stat", Weight = 0.34f, Outline = true },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_base", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_epicloot", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_total", Weight = 0.22f, Alignment = TextAnchor.MiddleRight });

        AddDamageRows(player, table, weapon, magicItem, include);
        AddAttackSpeed(player, table);
        AddStagger(player, table, weapon, otherWeapon);
        AddLifeSteal(player, table, otherWeapon);
        AddBackstab(table, weapon, magicItem);
        AddStamina(player, table, weapon, magicItem);
        AddEitr(player, table, weapon, magicItem);
        AddHealth(player, table, weapon, magicItem);
        AddDrawStamina(player, table, weapon, magicItem);
        AddKnockback(player, table, weapon, magicItem);
        table.Finish();
    }

    private static void AddDamageRows(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem,
        Func<ItemDrop.ItemData, bool> include)
    {
        HitData.DamageTypes baseDamage = weapon.Clone().GetDamage();
        HitData.DamageTypes total = weapon.GetDamage();
        if (magicItem != null)
        {
            total.m_spirit += EitrImbueAttack.GetSpiritBonus(magicItem, total);
        }

        List<DamageRow> shown = new();
        foreach (DamageRow row in DamageRows)
        {
            if (row.Get(baseDamage) > 0.05f || row.Get(total) > 0.05f)
            {
                shown.Add(row);
            }
        }

        int combatRows = 0;
        float baseSum = 0f;
        float totalSum = 0f;
        foreach (DamageRow row in shown)
        {
            if (!row.Combat)
            {
                continue;
            }

            combatRows++;
            baseSum += row.Get(baseDamage);
            totalSum += row.Get(total);
        }

        foreach (DamageRow row in shown)
        {
            List<string> detail = new();
            if (row.AddEffect != null)
            {
                detail.AddRange(StatSourceLines.GetMagicSources(player, row.AddEffect, include));
            }

            detail.AddRange(StatSourceLines.GetMagicSources(player,
                row.Physical ? MagicEffectType.ModifyPhysicalDamage : MagicEffectType.ModifyElementalDamage, include));
            if (combatRows <= 1)
            {
                AddWholeWeaponSources(player, detail, include);
            }

            float baseValue = row.Get(baseDamage);
            float totalValue = row.Get(total);
            AddRow(table, row.Key, Whole(baseValue), totalValue - baseValue, Whole(totalValue), detail, x => Signed(x), true);
        }

        if (combatRows > 1)
        {
            List<string> detail = new();
            AddWholeWeaponSources(player, detail, include);
            AddRow(table, "$mod_epicloot_offense_totaldamage", Whole(baseSum), totalSum - baseSum, Whole(totalSum), detail,
                x => Signed(x), true);
        }
    }

    private static void AddWholeWeaponSources(Player player, List<string> detail, Func<ItemDrop.ItemData, bool> include)
    {
        foreach (string effectType in WholeWeaponEffects)
        {
            detail.AddRange(StatSourceLines.GetMagicSources(player, effectType, include));
        }
    }

    private static void AddAttackSpeed(Player player, MagicTextTable table)
    {
        double speed = 1.0;
        ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyAttackSpeed, effect =>
        {
            speed *= 1.0 + player.GetTotalActiveMagicEffectValue(effect, 0.01f);
        });

        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackSpeed));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackSpeedLowHealth));
        float total = (float)speed;
        AddRow(table, "$mod_epicloot_offense_attackspeed", Percent(1f), total - 1f, Percent(total), detail, SignedPercent, true);
    }

    private static void AddStagger(Player player, MagicTextTable table, ItemDrop.ItemData weapon, ItemDrop.ItemData otherWeapon)
    {
        ItemDrop.ItemData ignore = IsWeapon(otherWeapon) ? otherWeapon : null;
        Func<ItemDrop.ItemData, bool> include = x => x != ignore;

        float staggerDamage = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDamage, 0.01f, ignore);
        List<string> damageDetail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyStaggerDamage, include));
        AddRow(table, "$mod_epicloot_offense_staggerdamage", Percent(1f), staggerDamage, Percent(1f + staggerDamage),
            damageDetail, SignedPercent, true);

        float staggerDuration = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
        List<string> durationDetail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyStaggerDuration));
        AddRow(table, "$mod_epicloot_offense_staggerduration", Percent(1f), staggerDuration, Percent(1f + staggerDuration),
            durationDetail, SignedPercent, true);
    }

    private static void AddLifeSteal(Player player, MagicTextTable table, ItemDrop.ItemData otherWeapon)
    {
        ItemDrop.ItemData ignore = IsWeapon(otherWeapon) ? otherWeapon : null;
        Func<ItemDrop.ItemData, bool> include = x => x != ignore;
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.LifeSteal, include));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.LifeStealLowHealth, include));
        if (detail.Count == 0)
        {
            return;
        }

        float total = 0f;
        ModifyWithLowHealth.Apply(player, MagicEffectType.LifeSteal,
            effect => total += player.GetTotalActiveMagicEffectValue(effect, 0.01f, ignore));
        AddRow(table, "$mod_epicloot_offense_lifesteal", Percent(0f), total, Percent(total), detail, SignedPercent, true);
    }

    private static void AddBackstab(MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_backstabBonus;
        float total = baseValue * (1f + WeaponEffect(magicItem, MagicEffectType.ModifyBackstab));
        List<string> detail = new();
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.ModifyBackstab));
        AddRow(table, "$item_backstab", Times(baseValue), total - baseValue, Times(total), detail, x => Signed(x, "0.#") + "x", true);
    }

    private static void AddStamina(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackStamina;
        bool bloodlust = magicItem != null && magicItem.HasEffect(MagicEffectType.Bloodlust);
        if (baseValue <= 0f && !bloodlust)
        {
            return;
        }

        float total;
        if (bloodlust)
        {
            total = Bloodlust.GetBloodlustStamina();
        }
        else
        {
            total = (1f - WeaponEffect(magicItem, MagicEffectType.ModifyAttackStaminaUse)) * baseValue;
            if (magicItem != null && magicItem.HasEffect(MagicEffectType.SpellSword))
            {
                total = Spellsword.GetSpellswordAttackStamina(total);
            }
        }

        List<string> detail = new();
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.ModifyAttackStaminaUse));
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.Bloodlust));
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.SpellSword));
        AddRow(table, "$item_staminause", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    private static void AddEitr(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackEitr;
        bool spellsword = magicItem != null && magicItem.HasEffect(MagicEffectType.SpellSword);
        List<string> detail = new();
        float total;
        if (ConjuredArrows.TryGetShotCost(weapon, out float conjuredCost))
        {
            total = conjuredCost;
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ConjuredArrows));
        }
        else
        {
            if (baseValue <= 0f && !spellsword)
            {
                return;
            }

            float cost = baseValue;
            if (spellsword)
            {
                cost += Spellsword.GetAdditionalSpellswordAttackEitr(weapon.m_shared.m_attack.m_attackStamina);
            }

            total = (1f - WeaponEffect(magicItem, MagicEffectType.ModifyAttackEitrUse)) * cost;
            detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.ModifyAttackEitrUse));
            detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.SpellSword));
        }

        AddRow(table, "$item_eitruse", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    private static void AddHealth(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackHealth;
        bool bloodlust = magicItem != null && magicItem.HasEffect(MagicEffectType.Bloodlust);
        if (baseValue <= 0f && !bloodlust)
        {
            return;
        }

        float cost = bloodlust ? Bloodlust.GetBloodlustHealth(baseValue, weapon.m_shared.m_attack.m_attackStamina) : baseValue;
        float total = ModifyAttackCosts.GetEffectPercentage(WeaponEffect(magicItem, MagicEffectType.ModifyAttackHealthUse)) * cost;
        List<string> detail = new();
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.ModifyAttackHealthUse));
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.Bloodlust));
        AddRow(table, "$item_healthuse", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    private static void AddDrawStamina(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attack.m_drawStaminaDrain;
        if (baseValue <= 0f)
        {
            return;
        }

        float total = (1f - WeaponEffect(magicItem, MagicEffectType.ModifyDrawStaminaUse)) * baseValue;
        List<string> detail = new();
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.ModifyDrawStaminaUse));
        AddRow(table, "$item_staminahold", PerSecond(baseValue), total - baseValue, PerSecond(total), detail,
            x => Signed(x, "0.#"), false);
    }

    private static void AddKnockback(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attackForce;
        if (baseValue <= 0f)
        {
            return;
        }

        float total = magicItem != null ? CloseQuarter.GetKnockback(weapon, magicItem) : baseValue;
        List<string> detail = new();
        detail.AddRange(WeaponSources(magicItem, weapon, MagicEffectType.CloseQuarter));
        AddRow(table, "$item_knockback", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), true);
    }

    private static float WeaponEffect(MagicItem magicItem, string effectType) =>
        magicItem?.GetTotalEffectValue(effectType, 0.01f) ?? 0f;

    private static IEnumerable<string> WeaponSources(MagicItem magicItem, ItemDrop.ItemData weapon, string effectType)
    {
        if (magicItem == null)
        {
            yield break;
        }

        foreach (MagicItemEffect effect in magicItem.GetEffects(effectType, includeSocketed: true))
        {
            yield return StatSourceLines.Source(effectType,
                $"{MagicItem.GetEffectText(effect, weapon.GetRarity(), false)} ({weapon.GetDecoratedName()})");
        }
    }

    private static void AddRow(MagicTextTable table, string name, string baseText, float magicDelta, string totalText,
        List<string> detail, Func<float, string> formatDelta, bool higherIsBetter)
    {
        bool neutral = Mathf.Abs(magicDelta) < 0.005f;
        bool better = higherIsBetter ? magicDelta > 0f : magicDelta < 0f;
        string color = neutral ? NeutralColor : better ? BetterColor : WorseColor;
        string[] cells =
        {
            name,
            baseText,
            neutral ? Dash : formatDelta(magicDelta),
            $"<color={color}>{totalText}</color>",
        };
        table.AddRow(cells, detail, true);
    }

    private static string Whole(float value) => Mathf.RoundToInt(value).ToString();
    private static string Decimal(float value) => value.ToString("0.#");
    private static string Times(float value) => value.ToString("0.#") + "x";
    private static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)}%";
    private static string SignedPercent(float fraction) => Signed(fraction * 100f) + "%";

    private static string Signed(float value, string format = "0")
    {
        string text = Mathf.Abs(value).ToString(format);
        return value < 0f ? $"-{text}" : $"+{text}";
    }

    private static string PerSecond(float value) =>
        string.Format(Localization.instance.Localize("$mod_epicloot_vitals_persecond"), value.ToString("0.#"));
}
