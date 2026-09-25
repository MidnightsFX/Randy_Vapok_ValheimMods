using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Compile-time mirror of Project Auga's API (AugaAPI 1.6.0, Auga/API.cs in the Auga repo). Every body is a
// stub: when Auga is installed, its APIManager rewrites this assembly as it loads (this mod declares the
// randyknapp.mods.auga soft dependency, which is what opts it in) so that every reference to a type in the
// Auga namespace resolves to Auga.dll instead. Methods are matched by name, parameter types and return type
// and fields by name alone, so each signature here has to match Auga's exactly - a mismatch is left calling
// the stub, or reads Auga's field as the wrong type. Without Auga nothing is rewritten, the stubs run, and
// IsLoaded() answers false.
//
// Deliberately left out: CraftingControls / GetCraftingControls (unused, and CraftingControls needs
// gui_framework). Do not add types to the Auga namespace that Auga itself does not have.
namespace Auga
{
    [PublicAPI]
    internal static class API
    {
        public static string RedText = "#CD2121";
        public static string Red = "#AD1616";
        public static string Brown1 = "#EAE1D9";
        public static string Brown2 = "#D1C9C2";
        public static string Brown3 = "#A39689";
        public static string Brown4 = "#706457";
        public static string Brown5 = "#2E2620";
        public static string Brown6 = "#EAE1D9";
        public static string Brown7 = "#181410";
        public static string Blue = "#216388";
        public static string LightBlue = "#1AACEF";
        public static string Gold = "#B98A12";
        public static string BrightGold = "#EAA800";
        public static string BrightestGold = "#FFBF1B";
        public static string GoldDark = "#755608";
        public static string Green = "#1B9B37";

        public static bool IsLoaded() => false;

        // Fonts & Assets. The fonts are legacy UnityEngine.Font objects; the UI is TextMeshPro now.
        public static Font GetBoldFont() => null;
        public static Font GetSemiBoldFont() => null;
        public static Font GetRegularFont() => null;
        public static Sprite GetItemBackgroundSprite() => null;

        // Panels & Buttons
        public static GameObject Panel_Create(Transform parent, Vector2 size, string name, bool withCornerDecoration) => null;
        public static Button SmallButton_Create(Transform parent, string name, string labelText) => null;
        public static Button MediumButton_Create(Transform parent, string name, string labelText) => null;
        public static Button FancyButton_Create(Transform parent, string name, string labelText) => null;
        public static Button SettingsButton_Create(Transform parent, string name, string labelText) => null;
        public static Button DiamondButton_Create(Transform parent, string name, [CanBeNull] Sprite icon) => null;
        public static GameObject Divider_CreateSmall(Transform parent, string name, float width = -1) => null;
        public static Tuple<GameObject, GameObject> Divider_CreateMedium(Transform parent, string name, float width = -1) => null;
        public static Tuple<GameObject, GameObject> Divider_CreateLarge(Transform parent, string name, float width = -1) => null;
        public static void Button_SetTextColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, Color baseTextColor) { }
        public static void Button_OverrideTextColor(Button button, Color color) { }

        // Tooltips
        public static void Tooltip_MakeSimpleTooltip(GameObject obj) { }
        public static void Tooltip_MakeItemTooltip(GameObject obj, ItemDrop.ItemData item) { }
        public static void Tooltip_MakeFoodTooltip(GameObject obj, Player.Food food) { }
        public static void Tooltip_MakeStatusEffectTooltip(GameObject obj, StatusEffect statusEffect) { }
        public static void Tooltip_MakeSkillTooltip(GameObject obj, Skills.Skill skill) { }

        // Player Panel Tabs
        public static bool PlayerPanel_HasTab(string tabID) => false;
        public static PlayerPanelTabData PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected) => null;
        public static bool PlayerPanel_IsTabActive(GameObject tabButton) => false;
        public static Button PlayerPanel_GetTabButton(int index) => null;

        // Workbench Tabs
        public static bool Workbench_HasWorkbenchTab(string tabID) => false;
        public static WorkbenchTabData Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected) => null;
        public static WorkbenchTabData Workbench_AddVanillaWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected) => null;
        public static bool Workbench_IsTabActive(GameObject tabButton) => false;
        public static Button Workbench_GetCraftingTabButton() => null;
        public static Button Workbench_GetUpgradeTabButton() => null;
        public static GameObject Workbench_CreateNewResultsPanel() => null;

        // Tooltip Text Boxes. The Text overloads take a legacy UnityEngine.UI.Text; Auga's own text boxes are TMP.
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, Text t, object s, bool localize, bool overwrite) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, bool localize, bool overwrite) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, bool localize, bool overwrite) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize = true) { }
        public static void TooltipTextBox_AddLine(GameObject tooltipTextBoxGO, object a, object b, object parenthetical, bool localize, bool overwrite) { }
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize = true) { }
        public static void TooltipTextBox_AddUpgradeLine(GameObject tooltipTextBoxGO, object label, object value1, object value2, string color2, bool localize, bool overwrite) { }

        // Complex Tooltip
        public static void ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData> listener) { }
        public static void ComplexTooltip_AddFoodTooltipCreatedListener(Action<GameObject, Player.Food> listener) { }
        public static void ComplexTooltip_AddStatusEffectTooltipCreatedListener(Action<GameObject, StatusEffect> listener) { }
        public static void ComplexTooltip_AddSkillTooltipCreatedListener(Action<GameObject, Skills.Skill> listener) { }
        public static void ComplexTooltip_AddItemStatPreprocessor(Func<ItemDrop.ItemData, string, string, Tuple<string, string>> itemStatPreprocessor) { }
        public static void ComplexTooltip_ClearTextBoxes(GameObject complexTooltipGO) { }
        public static GameObject ComplexTooltip_AddTwoColumnTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddCenteredTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddUpgradeLabels(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddUpgradeTwoColumnTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddCheckBoxTextBox(GameObject complexTooltipGO) => null;
        public static GameObject ComplexTooltip_AddDivider(GameObject complexTooltipGO) => null;
        public static void ComplexTooltip_SetTopic(GameObject complexTooltipGO, string topic) { }
        public static void ComplexTooltip_SetSubtitle(GameObject complexTooltipGO, string topic) { }
        public static void ComplexTooltip_SetDescription(GameObject complexTooltipGO, string desc) { }
        public static void ComplexTooltip_SetIcon(GameObject complexTooltipGO, Sprite icon) { }
        public static void ComplexTooltip_EnableDescription(GameObject complexTooltipGO, bool enabled) { }
        public static string ComplexTooltip_GenerateItemSubtext(GameObject complexTooltipGO, ItemDrop.ItemData item) => string.Empty;
        public static void ComplexTooltip_SetItem(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1) { }
        public static void ComplexTooltip_SetItemNoTextBoxes(GameObject complexTooltipGO, ItemDrop.ItemData item, int quality = -1, int variant = -1) { }
        public static void ComplexTooltip_SetFood(GameObject complexTooltipGO, Player.Food food) { }
        public static void ComplexTooltip_SetStatusEffect(GameObject complexTooltipGO, StatusEffect statusEffect) { }
        public static void ComplexTooltip_SetSkill(GameObject complexTooltipGO, Skills.Skill skill) { }

        // Requirements Panel
        public static Image RequirementsPanel_GetIcon(GameObject requirementsPanelGO) => null;
        public static GameObject[] RequirementsPanel_RequirementList(GameObject requirementsPanelGO) => null;
        public static void RequirementsPanel_SetWires(GameObject requirementsPanelGO, RequirementWireState[] wireStates, bool canCraft) { }

        // Custom Variant Panel
        public static TMP_Text CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow) => null;
        public static void CustomVariantPanel_SetButtonLabel(string buttonLabel) { }
        public static void CustomVariantPanel_Disable() { }
    }
}
