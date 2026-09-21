using System.Linq;
using EpicLoot.Compendium;
using EpicLoot.Config;
using TMPro;
using UnityEngine;

namespace EpicLoot
{
    public static class MagicEffectRarity
    {
        public const string LinkIdPrefix = "elrare";
        public const int MaxTier = 3;

        // Escapes, not literals: a CP1252 round-trip corrupted the pip glyphs in
        // EpicLoot.GetMagicEffectPip once already.
        //
        // Tried in order against the real tooltip font, because guessing loses: U+2726 renders on the
        // Enchant panel (legacy Text, Valheim UI font) and as an empty box in the item tooltip, whose
        // TMP asset resolves through a different fallback chain. U+25C6 is last and is not a guess --
        // GetMagicEffectPip already draws it on every effect line of that same tooltip.
        private static readonly char[] GlyphCandidates = { '\u2605', '\u2726', '\u25C8', '\u25C6' };

        private static char _tierGlyph = '\u25C6';
        private static bool _glyphResolved;

        public static void ResolveGlyph(TMP_FontAsset font)
        {
            if (_glyphResolved || font == null)
            {
                return;
            }

            _glyphResolved = true;

            var legacyFont = MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre);

            foreach (var candidate in GlyphCandidates)
            {
                // searchFallbacks/tryAddCharacter both true: a dynamic atlas reports a glyph as missing
                // until something asks it to add it, which is exactly what drawing the text would do.
                if (!font.HasCharacter(candidate, true, true))
                {
                    continue;
                }

                // The Enchant/Augment panels draw the same marker through legacy UnityEngine.UI.Text,
                // which resolves glyphs against its own Font rather than the TMP fallback chain. A
                // candidate has to clear both or the panel boxes whatever the tooltip settled on.
                if (legacyFont != null && !legacyFont.HasCharacter(candidate))
                {
                    continue;
                }

                _tierGlyph = candidate;
                break;
            }

            EpicLoot.LogWarningForce($"[flare] rarity marker U+{(int)_tierGlyph:X4} on font '{font.name}'");
        }

        private static readonly float[] TierRatioCeilings = { 0.35f, 0.125f, 0.0625f };
        private static readonly string[] TierColors = { "#8FD8FF", "#C79BFF", "#FFD24A" };

        private static float _anchorWeight = float.NaN;
        private static float[] _rungsBelowAnchor = new float[0];

        public static void InvalidateAnchor()
        {
            _anchorWeight = float.NaN;
        }

        private static void EnsureAnchor()
        {
            if (!float.IsNaN(_anchorWeight))
            {
                return;
            }

            var weights = MagicItemEffectDefinitions.AllDefinitions.Values
                .Select(x => x.SelectionWeight)
                .Where(x => x > 0f)
                .OrderBy(x => x)
                .ToList();

            _anchorWeight = weights.Count > 0 ? weights[weights.Count / 2] : 0f;
            _rungsBelowAnchor = weights.Where(x => x < _anchorWeight).Distinct().OrderBy(x => x).ToArray();
        }

        // Two independent reads of the same weight, and the lower wins.
        //
        // Rank alone cheapens the top tier: the legendary/minimal configs use only four distinct
        // weights, so their lowest rung holds 22+ effects. Ratio alone is just as bad the other way --
        // it assumes a long tail, and hands out no stars at all once a config's spread is narrow.
        // Taking the min means a tier has to be earned on both counts, so a config with no genuine
        // outlier (legendary) simply has no three-star effects rather than inventing some.
        private static int GetRankTier(float weight)
        {
            for (var i = 0; i < _rungsBelowAnchor.Length && i < MaxTier; i++)
            {
                if (Mathf.Approximately(weight, _rungsBelowAnchor[i]))
                {
                    return MaxTier - i;
                }
            }

            return 0;
        }

        private static int GetRatioTier(float weight)
        {
            var ratio = weight / _anchorWeight;
            for (var i = TierRatioCeilings.Length - 1; i >= 0; i--)
            {
                if (ratio < TierRatioCeilings[i])
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public static int GetTier(MagicItemEffectDefinition effectDef)
        {
            if (effectDef == null || effectDef.SelectionWeight <= 0f)
            {
                return 0;
            }

            EnsureAnchor();
            if (_anchorWeight <= 0f)
            {
                return 0;
            }

            return Mathf.Min(GetRankTier(effectDef.SelectionWeight), GetRatioTier(effectDef.SelectionWeight));
        }

        public static string Decorate(MagicItemEffectDefinition effectDef, string effectText, bool allowAnimation)
        {
            return Decorate(GetTier(effectDef), effectText, allowAnimation);
        }

        // TryGetValue rather than MagicItemEffectDefinitions.Get: Get warns and fabricates a stand-in
        // definition for an unknown type, and this runs per effect per tooltip frame.
        public static string Decorate(string effectType, string effectText, bool allowAnimation)
        {
            MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effectType, out var effectDef);
            return Decorate(effectDef, effectText, allowAnimation);
        }

        // allowAnimation is false for any surface backed by UnityEngine.UI.Text (the Enchant and Augment
        // panels): legacy Text has no <link> tag and prints it verbatim instead of ignoring it.
        public static string Decorate(int tier, string effectText, bool allowAnimation)
        {
            if (tier <= 0 || tier > MaxTier || ELConfig.EffectRarityFlareMode == null ||
                ELConfig.EffectRarityFlareMode.Value == EffectRarityFlare.Off)
            {
                return effectText;
            }

            var marker = $"<color={TierColors[tier - 1]}>{new string(_tierGlyph, tier)}</color>";
            var decorated = $"{effectText} {marker}";

            if (!allowAnimation || ELConfig.EffectRarityFlareMode.Value != EffectRarityFlare.Animated)
            {
                return decorated;
            }

            return $"<link=\"{LinkIdPrefix}{tier}\">{decorated}</link>";
        }
    }
}
