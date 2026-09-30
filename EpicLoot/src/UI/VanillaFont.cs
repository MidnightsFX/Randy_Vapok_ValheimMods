using EpicLoot.Compendium;
using TMPro;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class VanillaFont : MonoBehaviour
    {
        public MagicFontManager.TMP_FontOptions Font = MagicFontManager.TMP_FontOptions.AveriaSansLibreOutline;

        private bool _applied;

        public void OnEnable()
        {
            if (!_applied)
            {
                _applied = MagicFontManager.Apply(GetComponent<TMP_Text>(), Font);
            }
        }
    }
}
