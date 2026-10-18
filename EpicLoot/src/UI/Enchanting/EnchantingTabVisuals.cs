using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    /// <summary>
    /// Folder-tab motion for one enchanting tab: the active tab rises and lights its icon, an idle tab sits
    /// lower with its base hidden behind the panel. The active tab is the one TabHandler has made
    /// non-interactable.
    /// </summary>
    public class EnchantingTabVisuals : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform Plaque;
        public Image Icon;
        public TMP_Text Label;

        public float RaisedBy = 10f;
        public float HoverRaise = 3f;
        public float IconYActive = -31f;
        public float IconYIdle = -37f;
        public Color IconIdleColor = new Color(0.97f, 0.92f, 0.84f, 0.7f);
        public Color IconHoverColor = Color.white;
        public Color IconActiveColor = new Color(1f, 0.80f, 0.40f, 1f);
        public float LabelYActive = -72f;
        public float LabelYIdle = -78f;
        public Color LabelIdleColor = new Color(0.97f, 0.92f, 0.84f, 0.8f);
        public Color LabelHoverColor = Color.white;
        public Color LabelActiveColor = new Color(1f, 0.80f, 0.40f, 1f);
        public float FadeSpeed = 12f;

        public float Active01 => _active;
        public float Rise { get; private set; }

        private Selectable _selectable;
        private bool _hovered;
        private float _active;
        private float _hover;

        public void OnEnable()
        {
            _hovered = false;
            Snap();
        }

        public void OnDisable()
        {
            _hovered = false;
        }

        public void Update()
        {
            Step(Time.unscaledDeltaTime * FadeSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void Snap()
        {
            Step(1f);
        }

        private void Step(float maxDelta)
        {
            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }

            bool active = _selectable != null && !_selectable.IsInteractable();
            _active = Mathf.MoveTowards(_active, active ? 1f : 0f, maxDelta);
            _hover = Mathf.MoveTowards(_hover, _hovered && !active ? 1f : 0f, maxDelta);

            Rise = Mathf.Lerp(-RaisedBy + HoverRaise * _hover, 0f, _active);

            if (Plaque != null)
            {
                Plaque.anchoredPosition = new Vector2(Plaque.anchoredPosition.x, Rise);
            }

            if (Icon != null)
            {
                var iconRt = (RectTransform)Icon.transform;
                iconRt.anchoredPosition = new Vector2(iconRt.anchoredPosition.x,
                    Mathf.Lerp(IconYIdle + HoverRaise * _hover, IconYActive, _active));
                Icon.color = Color.Lerp(Color.Lerp(IconIdleColor, IconHoverColor, _hover), IconActiveColor, _active);
            }

            if (Label != null)
            {
                var labelRt = (RectTransform)Label.transform;
                labelRt.anchoredPosition = new Vector2(labelRt.anchoredPosition.x,
                    Mathf.Lerp(LabelYIdle + HoverRaise * _hover, LabelYActive, _active));
                Label.color = Color.Lerp(Color.Lerp(LabelIdleColor, LabelHoverColor, _hover), LabelActiveColor, _active);
            }
        }
    }
}
