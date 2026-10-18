using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    /// <summary>
    /// A strip of wood laid over the panel's rim under the active tab, so the tab reads as part of the panel.
    /// The tabs themselves are drawn behind the panel; this is the only piece in front of it.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class EnchantingTabBridge : MonoBehaviour
    {
        public RectTransform Tabs;
        public float Inset = 4f;

        private Image _image;
        private RectTransform _rt;

        public void OnEnable()
        {
            Sync();
        }

        public void LateUpdate()
        {
            Sync();
        }

        public void Sync()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
                _rt = (RectTransform)transform;
            }

            EnchantingTabVisuals active = FindActive();
            if (active == null)
            {
                _image.enabled = false;
                return;
            }

            var tab = (RectTransform)active.transform;
            var parent = (RectTransform)_rt.parent;
            Vector3 tabCenter = parent.InverseTransformPoint(tab.TransformPoint(tab.rect.center.x, tab.rect.yMax, 0));
            Vector2 anchorOffset = Vector2.Scale(_rt.anchorMin - new Vector2(0.5f, 0.5f), parent.rect.size);

            _rt.anchoredPosition = new Vector2(tabCenter.x - anchorOffset.x, _rt.anchoredPosition.y);
            _rt.sizeDelta = new Vector2(tab.rect.width - Inset * 2f, _rt.sizeDelta.y);
            Color color = _image.color;
            color.a = active.Active01;
            _image.color = color;
            _image.enabled = true;
        }

        private EnchantingTabVisuals FindActive()
        {
            if (Tabs == null)
            {
                return null;
            }

            for (int i = 0; i < Tabs.childCount; i++)
            {
                Transform child = Tabs.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                Selectable selectable = child.GetComponent<Selectable>();
                if (selectable != null && !selectable.IsInteractable())
                {
                    return child.GetComponent<EnchantingTabVisuals>();
                }
            }

            return null;
        }
    }
}
