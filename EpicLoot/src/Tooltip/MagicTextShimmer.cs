using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EpicLoot;

public class MagicTextShimmer : MonoBehaviour
{
    private const float SheenSpeed = 44f;
    private const float SheenCore = 2f;
    private const float SheenWidth = 6f;
    // Character units, not seconds. The dark stretch it buys is
    // (SheenGap - 2 * SheenWidth + 1) / SheenSpeed seconds -- currently 1.0 -- so it has to be
    // retuned alongside SheenSpeed. Because all three are constants that rest is the same on a
    // two-word effect and a long one; only the sweep scales with length.
    private const float SheenGap = 56f;
    private const float SheenStrength = 1f;
    private const float SheenTierFloor = 1f;

    private struct FlareRange
    {
        public int Start;
        public int Length;
        public float Weight;
    }

    private TMP_Text _text;
    private TMP_MeshInfo[] _pristine;
    private readonly List<FlareRange> _ranges = new List<FlareRange>();
    private bool _dirty = true;

    public static void Ensure(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        MagicEffectRarity.ResolveGlyph(text.font);

        if (text.GetComponent<MagicTextShimmer>() == null)
        {
            text.gameObject.AddComponent<MagicTextShimmer>();
        }
    }

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _dirty = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        _ranges.Clear();
        _pristine = null;
    }

    private void OnTextChanged(UnityEngine.Object changed)
    {
        if (ReferenceEquals(changed, _text))
        {
            _dirty = true;
        }
    }

    private void LateUpdate()
    {
        if (_text == null)
        {
            return;
        }

        if (_dirty)
        {
            Rebuild();
        }

        if (_pristine != null && _ranges.Count > 0)
        {
            Animate();
        }
    }

    // Deliberately no ForceMeshUpdate here. TEXT_CHANGED_EVENT fires after TMP has already generated
    // the mesh, so textInfo is current; forcing a regenerate would re-raise the event and re-dirty us
    // every frame. Before the first generation characterCount is 0, so we stay dirty and retry.
    private void Rebuild()
    {
        _ranges.Clear();
        _pristine = null;

        var textInfo = _text.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
        {
            return;
        }

        _dirty = false;

        for (var i = 0; i < textInfo.linkCount; i++)
        {
            var link = textInfo.linkInfo[i];
            var id = link.GetLinkID();
            if (string.IsNullOrEmpty(id) || !id.StartsWith(MagicEffectRarity.LinkIdPrefix))
            {
                continue;
            }

            if (!int.TryParse(id.Substring(MagicEffectRarity.LinkIdPrefix.Length), out var tier) || tier <= 0)
            {
                continue;
            }

            _ranges.Add(new FlareRange
            {
                Start = link.linkTextfirstCharacterIndex,
                Length = link.linkTextLength,
                Weight = Mathf.Clamp01(tier / (float)MagicEffectRarity.MaxTier)
            });
        }

        if (_ranges.Count > 0)
        {
            _pristine = textInfo.CopyMeshInfoVertexData();
        }
    }

    private void Animate()
    {
        var textInfo = _text.textInfo;
        if (textInfo == null || textInfo.meshInfo == null || _pristine.Length != textInfo.meshInfo.Length)
        {
            _dirty = true;
            return;
        }

        var time = Time.unscaledTime;
        var touched = false;

        foreach (var range in _ranges)
        {
            var end = Mathf.Min(range.Start + range.Length, textInfo.characterCount);
            var sheenHead = Mathf.Repeat(time * SheenSpeed, range.Length + SheenGap) - SheenWidth;
            var tierScale = SheenStrength * (SheenTierFloor + (1f - SheenTierFloor) * range.Weight);

            for (var c = range.Start; c < end; c++)
            {
                var charInfo = textInfo.characterInfo[c];
                if (!charInfo.isVisible || charInfo.materialReferenceIndex >= _pristine.Length)
                {
                    continue;
                }

                var mat = charInfo.materialReferenceIndex;
                var vi = charInfo.vertexIndex;
                var source = _pristine[mat].colors32;
                var target = textInfo.meshInfo[mat].colors32;
                if (source == null || target == null || vi + 3 >= source.Length || vi + 3 >= target.Length)
                {
                    continue;
                }

                var distance = Mathf.Abs(sheenHead - (c - range.Start));
                var sheen = Mathf.Clamp01((SheenWidth - distance) / Mathf.Max(SheenWidth - SheenCore, 0.001f))
                            * tierScale;

                for (var v = 0; v < 4; v++)
                {
                    var baseColor = source[vi + v];
                    target[vi + v] = Color32.Lerp(baseColor, new Color32(255, 255, 255, baseColor.a), sheen);
                }

                touched = true;
            }
        }

        if (touched)
        {
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
