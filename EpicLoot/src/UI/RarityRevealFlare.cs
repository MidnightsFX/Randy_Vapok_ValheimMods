using EpicLoot.Config;
using UnityEngine;

namespace EpicLoot;

public class RarityRevealFlare : MonoBehaviour
{
    private const float PunchPeak = 0.45f;

    // Indexed by tier, so slot 0 is the plain enchant every roll gets.
    private static readonly float[] PunchStartScale = { 0.94f, 0.90f, 0.87f, 0.84f };
    private static readonly float[] PunchDuration = { 0.22f, 0.26f, 0.30f, 0.34f };
    private static readonly float[] PunchOvershoot = { 1.02f, 1.05f, 1.08f, 1.12f };

    private RectTransform _frame;
    private int _tier;
    private float _elapsed;
    private bool _animate;

    public static void Play(RectTransform frame, int tier)
    {
        if (frame == null || ELConfig.EffectRarityFlareMode == null ||
            ELConfig.EffectRarityFlareMode.Value == EffectRarityFlare.Off)
        {
            return;
        }

        var flare = frame.GetComponent<RarityRevealFlare>();
        if (flare == null)
        {
            flare = frame.gameObject.AddComponent<RarityRevealFlare>();
        }

        flare.Begin(frame, tier);
    }

    private void Begin(RectTransform frame, int tier)
    {
        _frame = frame;
        _tier = Mathf.Clamp(tier, 0, MagicEffectRarity.MaxTier);
        _elapsed = 0f;
        _animate = ELConfig.EffectRarityFlareMode.Value == EffectRarityFlare.Animated;
        _frame.localScale = _animate ? Vector3.one * PunchStartScale[_tier] : Vector3.one;
    }

    private void LateUpdate()
    {
        if (!_animate || _frame == null)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;

        var duration = PunchDuration[_tier];
        if (_elapsed < duration)
        {
            var t = _elapsed / duration;
            var overshoot = PunchOvershoot[_tier];
            var scale = t < PunchPeak
                ? Mathf.Lerp(PunchStartScale[_tier], overshoot, t / PunchPeak)
                : Mathf.Lerp(overshoot, 1f, (t - PunchPeak) / (1f - PunchPeak));
            _frame.localScale = Vector3.one * scale;
        }
        else if (_frame.localScale != Vector3.one)
        {
            _frame.localScale = Vector3.one;
        }
    }
}
