using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SelectableTextColor : MonoBehaviour
    {
        private Color _defaultColor = Color.white;
        public Color DisabledColor = Color.grey;
        private Selectable _selectable;
        private TMP_Text _text;

        public void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _text = GetComponentInChildren<TMP_Text>();
            _defaultColor = _text.color;
        }

        public void Update()
        {
            _text.color = _selectable.IsInteractable() ? _defaultColor : DisabledColor;
        }
    }
}
