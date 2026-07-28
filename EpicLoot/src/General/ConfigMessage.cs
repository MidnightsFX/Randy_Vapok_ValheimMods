using EpicLoot.Config;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Main menu panel offering to replace base configs the player has edited with the current defaults.
/// Uses its own prefab rather than the vanilla UnifiedPopup, which is shared with world deletion,
/// disconnect notices and the like and cannot be resized without affecting all of them.
///
/// Prefab layout (Assets/EpicLoot/Prefabs/UI/ConfigMessage): Title, Content, AcceptButton/Text,
/// DenyButton/Text, InputBlocker. The button labels are baked $el_menu_yes / $el_menu_no tokens
/// resolved by the prefab's own Localize component; Title and Content are blank and filled here.
/// </summary>
public sealed class ConfigMessage : MonoBehaviour
{
    public Text TitleText { get; private set; }
    public Text ContentText { get; private set; }

    public Button AcceptButton { get; private set; }
    public Button DenyButton { get; private set; }

    private void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        TitleText = transform.Find("Title").GetComponent<Text>();
        ContentText = transform.Find("Content").GetComponent<Text>();

        AcceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        DenyButton = transform.Find("DenyButton").GetComponent<Button>();

        if (EpicLoot.HasAuga)
        {
            ApplyAugaUI();
        }

        AcceptButton.onClick.AddListener(OnAcceptClick);
        DenyButton.onClick.AddListener(OnDenyClick);
    }

    private void ApplyAugaUI()
    {
        EpicLootAuga.ReplaceBackground(gameObject, withCornerDecoration: true);
        EpicLootAuga.FixFonts(gameObject);

        EpicLootAuga.ReplaceButton(AcceptButton);
        EpicLootAuga.ReplaceButton(DenyButton);
    }

    /// <summary>
    /// Fills in the already-localized title and body. Safe to call after the prefab's Localize
    /// component has run: Localization.Localize only substitutes $tokens, so text without them
    /// passes through untouched.
    /// </summary>
    public void SetMessage(string title, string content)
    {
        if (TitleText != null)
        {
            TitleText.text = title;
        }

        if (ContentText != null)
        {
            ContentText.text = content;
        }
    }

    public void OnAcceptClick()
    {
        ConfigVersionManager.BackupAndResetOutdatedConfigs();
        Close();
    }

    public void OnDenyClick()
    {
        ConfigVersionManager.DeclineOutdatedConfigs();
        Close();
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
