using EpicLoot_UnityLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.CraftingV2
{
    public static class EnchantingUIAugaFixup
    {
        private static readonly HashSet<GameObject> _hasBeenFixedUp = new HashSet<GameObject>();

        public static void AugaFixup(EnchantingTableUI ui)
        {
            if (!EpicLoot.HasAuga)
                return;

            if (_hasBeenFixedUp.Contains(ui.gameObject))
                return;

            var root = ui.Root;
            EpicLootAuga.ReplaceBackground(root, true);

            foreach (var tabData in ui.TabHandler.m_tabs)
            {
                var newButton = EpicLootAuga.ReplaceVerticalLargeTab(tabData.m_button);
                tabData.m_button = newButton;
            }

            foreach (var panelBase in ui.Panels)
            {
                EpicLootAuga.FixFonts(panelBase.gameObject);

                //var dividerParts = Auga.API.Divider_CreateLarge(panelBase.transform, "TitleDivider");
                //var dividerRT = (RectTransform)dividerParts.Item1.transform;
                //dividerRT.SetSiblingIndex(0);
                //dividerRT.anchoredPosition = new Vector2(0, 295);
                //dividerRT.sizeDelta = new Vector2(910, 40);
                //Object.Destroy(dividerParts.Item2.GetComponent<ContentSizeFitter>());
                //var contentRT = (RectTransform)dividerParts.Item2.transform;
                //contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);

                panelBase.MainButton = EpicLootAuga.ReplaceButtonFancy(panelBase.MainButton, false, true);
                
                if (panelBase.AvailableItems != null)
                    AugaFixupMultiselectPrefab(panelBase.AvailableItems.ElementPrefab.gameObject);

                var existingListElements = panelBase.GetComponentsInChildren<MultiSelectItemListElement>();
                foreach (var multiSelectItemListElement in existingListElements)
                {
                    AugaFixupMultiselectPrefab(multiSelectItemListElement.gameObject);
                }

                var bottomRowHints = (RectTransform)panelBase.transform.Find("GamepadHints/BottomRow");
                if (bottomRowHints)
                    bottomRowHints.anchoredPosition = new Vector2(bottomRowHints.anchoredPosition.x, 8);

                foreach (var scrollbar in panelBase.GetComponentsInChildren<Scrollbar>())
                {
                    EpicLootAuga.FixupScrollbar(scrollbar);
                }

                if (panelBase is SacrificeUI sacrificeUI)
                {
                    AugaFixupMultiselectPrefab(sacrificeUI.SacrificeProducts.ElementPrefab.gameObject);
                }
                else if (panelBase is ConvertUI convertUI)
                {
                    AugaFixupMultiselectPrefab(convertUI.Products.ElementPrefab.gameObject);
                    AugaFixupMultiselectPrefab(convertUI.CostList.ElementPrefab.gameObject);

                    var modeButtonContainer = (RectTransform)convertUI.ModeButtons[0].transform.parent;
                    modeButtonContainer.anchoredPosition = new Vector2(-20, modeButtonContainer.anchoredPosition.y);
                }
                else if (panelBase is EnchantUI enchantUI)
                {
                    AugaFixupMultiselectPrefab(enchantUI.CostList.ElementPrefab.gameObject);
                }
                else if (panelBase is AugmentUI augmentUI)
                {
                    AugaFixupMultiselectPrefab(augmentUI.CostList.ElementPrefab.gameObject);
                }
            }

            _hasBeenFixedUp.Add(ui.gameObject);
        }

        public static void AugaFixupMultiselectPrefab(GameObject prefab)
        {
            if (!EpicLoot.HasAuga || _hasBeenFixedUp.Contains(prefab))
                return;

            EpicLootAuga.FixItemBG(prefab);
            EpicLootAuga.FixListElementColors(prefab);
            EpicLootAuga.FixFonts(prefab);

            _hasBeenFixedUp.Add(prefab);
        }
    }
}
