using TMPro;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class GamepadGlyph : MonoBehaviour
    {
        public string ZInputKey;

        private TMP_Text _text;

        public void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void OnEnable()
        {
            Refresh();
            ZInput.OnInputLayoutChanged += Refresh;
        }

        public void OnDisable()
        {
            ZInput.OnInputLayoutChanged -= Refresh;
        }

        public void Refresh()
        {
            Localize(_text, string.IsNullOrEmpty(ZInputKey) ? name : ZInputKey);
        }

        public static void Localize(TMP_Text text, string zinputKey)
        {
            if (text == null || ZInput.instance == null)
            {
                return;
            }

            text.text = Localization.instance.Localize(ZInput.instance.GetBoundKeyString(zinputKey, true));
        }

        public static void LocalizeHints(GameObject root)
        {
            foreach (UIGamePad pad in root.GetComponentsInChildren<UIGamePad>(true))
            {
                if (pad.m_hint == null || string.IsNullOrEmpty(pad.m_zinputKey))
                {
                    continue;
                }

                TMP_Text text = pad.m_hint.GetComponentInChildren<TMP_Text>(true);
                if (text != null && text.GetComponent<GamepadGlyph>() == null)
                {
                    Localize(text, pad.m_zinputKey);
                }
            }
        }
    }
}
