using System.Collections.Generic;
using System.Text;
using EpicLoot.Config;
using UnityEngine;

namespace EpicLoot;

public class TemperData
{
    private const string SUCCESS_GREEN = "#22bb33";
    public const string FAIL_RED = "#CC0000";
    private const string CRIT_SUCCESS_GOLD = "#FFD700";

    private static float BASE_CHANCE => Mathf.Clamp01(ELConfig.TemperBaseChance.Value);
    private static float OVER_TEMPERED_DECREMENT => Mathf.Clamp01(ELConfig.TemperDecrement.Value);
    public static bool CAN_DESTROY_ITEM => ELConfig.TemperDestroysItem.Value;
    public static float DESTROY_CHANCE => Mathf.Clamp01(ELConfig.TemperChanceToDestroy.Value);

    private readonly ItemDrop.ItemData itemData;
    private readonly MagicItem magicItem;
    private readonly MagicItemEffect selectedEffect;
    private MagicItemEffectDefinition selectedDefinition;
    public readonly MagicItemEffectDefinition.ValueDef selectedValues;

    private readonly string rarityColor;

    private readonly MagicItem _tempItem;
    private float _tempValue;
    private float _tempIncrement;
    private float _tempUpdatedValue;
    private MagicItemEffect _tempEffect;
    
    public readonly float probability;
    public readonly bool success;
    private readonly bool critical;
    private readonly int indexOfEffect;
    
    public TemperData(ItemDrop.ItemData itemData, string effectType)
    {
        this.itemData = itemData;
        
        magicItem = itemData.GetMagicItem();
        selectedEffect = magicItem.GetEffects(effectType)[0];
        selectedDefinition = MagicItemEffectDefinitions.Get(effectType);
        selectedValues = selectedDefinition.GetValuesForRarity(magicItem.Rarity);
        rarityColor = EpicLoot.GetRarityColor(magicItem.Rarity);
        
        probability = CalculateProbability();
        success = UnityEngine.Random.value <= probability;
        critical = UnityEngine.Random.value <= GetCriticalSuccessChance();
        
        _tempItem = new MagicItem
        {
            Version = magicItem.Version, 
            Rarity = magicItem.Rarity, 
            TypeNameOverride = magicItem.TypeNameOverride,
            AugmentedEffectIndex = magicItem.AugmentedEffectIndex,
            AugmentedEffectIndices = magicItem.AugmentedEffectIndices,
            TemperedEffectIndices = magicItem.TemperedEffectIndices,
            DisplayName = magicItem.DisplayName,
            LegendaryID = magicItem.LegendaryID,
            SetID = magicItem.SetID,
            IsUnidentified = magicItem.IsUnidentified
        };
        
        for (int i = 0; i < magicItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = magicItem.Effects[i];
            MagicItemEffect newValue = new MagicItemEffect(
                effect.EffectType,
                effect.EffectValue);
                
            if (newValue.EffectType == effectType)
            {
                MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
                MagicItemEffectDefinition.ValueDef values = def.GetValuesForRarity(magicItem.Rarity);
                _tempValue = newValue.EffectValue;
                _tempIncrement = values.Increment;
                newValue.EffectValue += values.Increment;
                _tempUpdatedValue = newValue.EffectValue;
                _tempEffect = newValue;
                indexOfEffect = i;
            }
            _tempItem.Effects.Add(newValue);
        }
    }
    public void OnSuccess()
    {
        if (critical)
        {
            _tempEffect.EffectValue += _tempIncrement;
            _tempUpdatedValue = _tempEffect.EffectValue;
        }
        _tempItem.TemperedEffectIndices.Add(indexOfEffect);
        itemData.SaveMagicItem(_tempItem);
        UpdateLog(critical ? CRIT_SUCCESS_GOLD : SUCCESS_GREEN);

    }
    public void OnFail()
    {
        _tempItem.Effects.Clear();

        string effectToReduce = SelectWeightedEffect(magicItem.Effects);
        for (int i = 0; i < magicItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = magicItem.Effects[i];
            MagicItemEffect newValue = new MagicItemEffect(
                effect.EffectType,
                effect.EffectValue);

            if (effect.EffectType == effectToReduce)
            {
                MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
                MagicItemEffectDefinition.ValueDef values = def.GetValuesForRarity(magicItem.Rarity);
                selectedDefinition = def;
                _tempValue = newValue.EffectValue;
                _tempIncrement = values.Increment;
                newValue.EffectValue -= values.Increment;
                _tempUpdatedValue = newValue.EffectValue;
                _tempEffect = newValue;
            }

            _tempItem.Effects.Add(newValue);
        }
        
        itemData.SaveMagicItem(_tempItem);
        UpdateLog(FAIL_RED);
    }
    private void UpdateLog(string color)
    {
        string localizedText = Localization.instance.Localize(selectedDefinition.DisplayText);
        string message = string.Format(localizedText, $"{_tempValue} → {_tempUpdatedValue}");
        TemperPanel.Instance.UpdateLog($"<color={color}>{message}</color>");
    }
    private string SelectWeightedEffect(List<MagicItemEffect> effects)
    {
        float maxInclusive = 0.0f;
        for (int i = 0; i < effects.Count; ++i)
        {
            MagicItemEffect effect = effects[i];
            MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
            MagicItemEffectDefinition.ValueDef values = def.GetValuesForRarity(magicItem.Rarity);
            if (values == null) continue;
            float num = effect.EffectValue / values.MaxValue;
            maxInclusive += num;
        }
        float random = UnityEngine.Random.Range(0.0f, maxInclusive);
        float value = 0.0f;
        for (int i = 0; i < effects.Count; ++i)
        {
            MagicItemEffect effect = effects[i];
            MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
            MagicItemEffectDefinition.ValueDef values = def.GetValuesForRarity(magicItem.Rarity);
            if (values == null) continue;
            float num = effect.EffectValue / values.MaxValue;
            value += num;
            if (value >= random)
            {
                return effect.EffectType;
            }
        }

        return effects[effects.Count - 1].EffectType;
    }
    public string GetTooltip()
    {
        bool showRange = ZInput.GetKey(KeyCode.LeftShift) || 
                         ZInput.GetKey(KeyCode.RightShift) || 
                         ZInput.GetButton("JoyLStick") ||
                         ZInput.GetButton("JoyRStick");

        StringBuilder sb = new StringBuilder();
        
        sb.Append($"<color={rarityColor}>{itemData.GetDisplayName()}\n\n");
        for (int i = 0; i < _tempItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = _tempItem.Effects[i];
            string pip = _tempItem.GetMagicEffectPip(i);
            MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
            string localizedDisplayText = Localization.instance.Localize(def.DisplayText);

            string result;
            if (effect.EffectType == selectedEffect.EffectType)
            {
                result = string.Format(localizedDisplayText,
                    $"{effect.EffectValue - selectedValues.Increment} <color={SUCCESS_GREEN}>→ {effect.EffectValue}</color>");
            }
            else
            {
                result = string.Format(localizedDisplayText, effect.EffectValue);
            }
            if (showRange)
            {
                MagicItemEffectDefinition.ValueDef values = def.GetValuesForRarity(magicItem.Rarity);
                if (values != null && !Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $"\n[{values.MinValue}-{values.MaxValue}]";
                }
            }
            
            sb.AppendLine($"{pip} {result}");
        }
        sb.Append("</color>");
        return sb.ToString();
    }
    private float CalculateProbability()
    {
        float overTemperedModifier = 0f;
        if (selectedEffect.EffectValue > selectedValues.MaxValue)
        {
            float difference = selectedEffect.EffectValue - selectedValues.MaxValue;
            float increments = difference / selectedValues.Increment;
            overTemperedModifier = increments * OVER_TEMPERED_DECREMENT;
        }
        return 1 - Mathf.Clamp01(selectedEffect.EffectValue / selectedValues.MaxValue - (BASE_CHANCE - overTemperedModifier));
    }
    public float GetCriticalSuccessChance() => Mathf.Clamp01(1 - selectedEffect.EffectValue / selectedValues.MaxValue);

    public MagicItem GetUpdatedMagicItem() => _tempItem;
}