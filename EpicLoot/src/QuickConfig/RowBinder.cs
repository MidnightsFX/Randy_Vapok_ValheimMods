using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>
/// Binds one instantiated page prefab to the staged configuration. A row is a direct child of the
/// page's Column / Left / Right container (or of that container's ScrollRect content) whose GameObject
/// name is a registered key; the widget children are found by the names in README.md. Nothing here
/// positions anything: the column's VerticalLayoutGroup reflows when a conditional row is hidden.
/// </summary>
internal sealed class RowBinder {
    private static readonly string[] ContainerNames = { "Column", "Left", "Right" };
    private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);

    private sealed class BoundRow {
        internal Binding Binding;
        internal GameObject Root;
        internal CanvasGroup Group;
        internal Action Sync;
        internal bool ReadOnly;
    }

    private readonly GameObject page;
    private readonly StagedConfig staged;
    private readonly Action<string> onEdited;
    private readonly List<BoundRow> rows = new List<BoundRow>();

    /// <summary>True when the page holds a row the current player may only look at (JSON rows off-host).</summary>
    internal bool HasReadOnlyRows { get; private set; }

    internal RowBinder(GameObject page, StagedConfig staged, Action<string> onEdited) {
        this.page = page;
        this.staged = staged;
        this.onEdited = onEdited;
    }

    internal void Bind() {
        if (page == null) { return; }
        QuickConfigUi.LocalizeTexts(page);
        foreach (Transform container in RowContainers(page.transform)) {
            // Snapshot first: binding a Flags row clones children into the row, not the container,
            // but a static copy is cheap insurance against reflow during iteration.
            List<Transform> children = new List<Transform>();
            foreach (Transform child in container) { children.Add(child); }
            foreach (Transform child in children) { BindRow(child.gameObject); }
        }
    }

    /// <summary>Re-evaluates every row's visibility, enabled state and displayed value from the staged config.</summary>
    internal void Refresh() {
        foreach (BoundRow row in rows) {
            if (row.Root == null) { continue; }
            Binding binding = row.Binding;
            bool visible = binding.IsVisible(staged)
                && (binding.Scope != BindingScope.Json || staged.IsAvailable(binding.Key));
            if (row.Root.activeSelf != visible) { row.Root.SetActive(visible); }
            if (visible == false) { continue; }

            bool enabled = row.ReadOnly == false && binding.IsEnabled(staged);
            if (row.Group != null) {
                row.Group.interactable = enabled;
                row.Group.alpha = enabled ? 1f : 0.55f;
            }
            try {
                row.Sync?.Invoke();
            } catch (Exception e) {
                EpicLoot.LogWarning($"Quick Configure could not refresh row '{binding.Key}': {e.Message}");
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Finding rows
    // ------------------------------------------------------------------------------------------------

    private static IEnumerable<Transform> RowContainers(Transform pageRoot) {
        List<Transform> containers = new List<Transform>();
        foreach (string name in ContainerNames) {
            Transform column = QuickConfigUi.FindChild(pageRoot, name);
            if (column != null) { containers.Add(ContentOf(column)); }
        }
        if (containers.Count == 0) { containers.Add(pageRoot); }
        return containers;
    }

    // A column that scrolls holds its rows in the ScrollRect's content. Only the column itself or a
    // direct child is checked: a list row further down carries a ScrollRect of its own.
    private static Transform ContentOf(Transform column) {
        ScrollRect scroll = column.GetComponent<ScrollRect>();
        if (scroll == null) {
            foreach (Transform child in column) {
                scroll = child.GetComponent<ScrollRect>();
                if (scroll != null) { break; }
            }
        }
        return scroll != null && scroll.content != null ? scroll.content : column;
    }

    private void BindRow(GameObject root) {
        string key = root.name;
        Binding binding = QuickConfigBindings.Get(key);
        if (binding == null) {
            // Decorative rows keep their template name and are simply left alone.
            if (string.IsNullOrEmpty(key) || key.StartsWith("Row_", StringComparison.Ordinal)) { return; }
            if (warned.Add(key)) {
                EpicLoot.LogWarningForce($"Quick Configure page '{page.name}' has a row named '{key}' that matches no setting; the row is disabled.");
            }
            Disable(root);
            return;
        }

        TMP_Text label = QuickConfigUi.TextAt(root.transform, "Label");
        // An action row's button already carries the display name; its Label is side text that may
        // legitimately be empty, so only value rows get a fallback label.
        if (label != null && binding.Kind != BindingKind.Action && string.IsNullOrWhiteSpace(label.text)
            && string.IsNullOrEmpty(binding.DisplayName) == false) {
            label.text = QuickConfigureTool.L(binding.DisplayName);
        }
        AttachTooltip(root, binding);

        BoundRow row = new BoundRow {
            Binding = binding,
            Root = root,
            Group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>(),
            ReadOnly = binding.HostOnly && QuickConfigBindings.IsHost() == false
        };

        bool wired;
        try {
            wired = Wire(row);
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Quick Configure could not bind row '{key}': {e}");
            wired = false;
        }
        if (wired == false) {
            Disable(root);
            return;
        }
        if (row.ReadOnly) { HasReadOnlyRows = true; }
        rows.Add(row);
    }

    // List rows carry the tooltip on their Head (the label strip) so it does not pop over every
    // field in the list; a plain row carries it on its root. An inactive child named Tooltip with a
    // TMP_Text overrides the binding's text.
    private static void AttachTooltip(GameObject root, Binding binding) {
        string text = TooltipOverride(root.transform) ?? binding.TooltipText();
        GameObject target = root;
        if (IsListKind(binding.Kind)) {
            Transform head = QuickConfigUi.FindChild(root.transform, "Head");
            if (head != null) { target = head.gameObject; }
        }
        QuickConfigTooltip.Attach(target, text);
    }

    private static bool IsListKind(BindingKind kind) {
        return kind == BindingKind.BiomeCosts || kind == BindingKind.ItemCategories || kind == BindingKind.BiomeDrops
            || kind == BindingKind.EffectConfigs || kind == BindingKind.Bounties;
    }

    private static string TooltipOverride(Transform root) {
        TMP_Text text = QuickConfigUi.TextAt(root, "Tooltip");
        if (text == null || string.IsNullOrWhiteSpace(text.text)) { return null; }
        return QuickConfigureTool.L(text.text);
    }

    private static void Disable(GameObject root) {
        CanvasGroup group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.alpha = 0.4f;
    }

    private bool Wire(BoundRow row) {
        switch (row.Binding.Kind) {
            case BindingKind.Bool: return WireToggle(row);
            case BindingKind.Float:
            case BindingKind.Int: return WireNumber(row);
            case BindingKind.Enum:
            case BindingKind.Key:
            case BindingKind.String: return WireChoice(row);
            case BindingKind.Flags: return WireFlags(row);
            case BindingKind.Color: return WireColor(row);
            case BindingKind.RarityTable: return WireTextField(row);
            case BindingKind.BiomeCosts: return WireBiomeCosts(row);
            case BindingKind.ItemCategories: return WireItemCategories(row);
            case BindingKind.BiomeDrops: return WireBiomeDrops(row);
            case BindingKind.EffectConfigs: return WireEffectConfigs(row);
            case BindingKind.Bounties: return WireBounties(row);
            case BindingKind.Action: return WireButton(row);
            case BindingKind.Readout: return WireReadout(row);
            default: return false;
        }
    }

    private bool MissingWidget(BoundRow row, string widget) {
        if (warned.Add(row.Binding.Key + "/" + widget)) {
            EpicLoot.LogWarningForce($"Quick Configure row '{row.Binding.Key}' has no '{widget}' child; the row is disabled.");
        }
        return false;
    }

    private void Edited(string message) {
        onEdited?.Invoke(message);
    }

    // ------------------------------------------------------------------------------------------------
    //  Simple widgets
    // ------------------------------------------------------------------------------------------------

    private bool WireToggle(BoundRow row) {
        Toggle toggle = QuickConfigUi.FindComponent<Toggle>(row.Root.transform, "Toggle");
        if (toggle == null) { return MissingWidget(row, "Toggle"); }
        Binding binding = row.Binding;
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(value => {
            binding.Set(staged, value);
            Edited(null);
        });
        row.Sync = () => toggle.SetIsOnWithoutNotify(binding.Get(staged) is bool on && on);
        return true;
    }

    private bool WireNumber(BoundRow row) {
        Binding binding = row.Binding;
        Slider slider = QuickConfigUi.FindComponent<Slider>(row.Root.transform, "Slider");
        TMP_InputField value = QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Value")
            ?? QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Field");
        if (slider == null && value == null) { return MissingWidget(row, "Slider"); }

        bool whole = binding.Kind == BindingKind.Int
            || (binding.Step >= 1f && Math.Abs(binding.Step - Math.Round(binding.Step)) < 0.0001f);
        if (slider != null) {
            // Max first: setting a min above the old max would clamp the value for a frame.
            slider.maxValue = binding.Max;
            slider.minValue = binding.Min;
            slider.wholeNumbers = whole;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => {
                binding.Set(staged, v);
                Edited(null);
            });
        }
        if (value != null) {
            value.contentType = whole ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
            value.onEndEdit.RemoveAllListeners();
            value.onEndEdit.AddListener(text => {
                binding.Set(staged, text);
                Edited(null);
            });
        }
        row.Sync = () => {
            float current = Current(binding);
            if (slider != null) { slider.SetValueWithoutNotify(current); }
            if (value != null && value.isFocused == false) { value.SetTextWithoutNotify(Format(binding, current)); }
        };
        return true;
    }

    private float Current(Binding binding) {
        object raw = binding.Get(staged);
        return raw is int i ? i : QuickConfigBindings.ToFloat(raw, binding.Min);
    }

    private static string Format(Binding binding, float value) {
        return binding.Kind == BindingKind.Int
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : QuickConfigBindings.FormatFloat(value, binding.Step);
    }

    // Enum / key / restricted string: a Cycle button, or a Field with a Pick button; whichever the
    // template carries. Both may be present (Row_Color reuses this for its Field/Pick).
    private bool WireChoice(BoundRow row) {
        Binding binding = row.Binding;
        Button cycle = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Cycle");
        bool any = false;
        if (cycle != null) {
            WireCycle(row, cycle, binding);
            any = true;
        }
        if (WireFieldAndPick(row, binding)) { any = true; }
        return any || MissingWidget(row, "Cycle/Field");
    }

    private void WireCycle(BoundRow row, Button cycle, Binding binding) {
        TMP_Text caption = QuickConfigUi.CaptionOf(cycle);
        QuickConfigUi.Wire(cycle, () => {
            IList<string> options = binding.Options?.Invoke();
            if (options == null || options.Count == 0) { return; }
            int index = OptionIndex(binding, options);
            SetOption(binding, options, (index + 1) % options.Count);
            Edited(null);
        });
        row.Sync += () => {
            if (caption == null) { return; }
            IList<string> options = binding.Options?.Invoke();
            int index = options != null ? OptionIndex(binding, options) : -1;
            string text = index >= 0 && options != null ? options[index] : binding.Get(staged)?.ToString() ?? "";
            caption.text = QuickConfigureTool.L(text);
        };
    }

    // For an Int binding with options (the icon index) the value IS the index; otherwise the value is the option text.
    private int OptionIndex(Binding binding, IList<string> options) {
        if (binding.Kind == BindingKind.Int) {
            int index = QuickConfigBindings.ToInt(binding.Get(staged), 0);
            return Mathf.Clamp(index, 0, options.Count - 1);
        }
        string current = binding.Get(staged) as string ?? "";
        for (int i = 0; i < options.Count; i++) {
            if (string.Equals(options[i], current, StringComparison.OrdinalIgnoreCase)) { return i; }
        }
        return -1;
    }

    private void SetOption(Binding binding, IList<string> options, int index) {
        if (binding.Kind == BindingKind.Int) {
            binding.Set(staged, index);
        } else {
            binding.Set(staged, options[index]);
        }
    }

    private bool WireFieldAndPick(BoundRow row, Binding binding) {
        TMP_InputField field = QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Field");
        if (field == null) { return false; }
        Button pick = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Pick");
        TMP_Text status = QuickConfigUi.TextAt(row.Root.transform, "Status");

        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(text => {
            binding.Set(staged, text);
            Edited(null);
        });

        if (pick != null) {
            BindPicker(pick, binding.Options, binding.DisplayName, () => binding.Get(staged) as string, picked => {
                binding.Set(staged, picked);
                Edited(null);
            });
        }

        row.Sync += () => {
            if (field.isFocused == false) { field.SetTextWithoutNotify(binding.Get(staged) as string ?? ""); }
            if (status != null && binding.Validate != null) {
                string error = binding.Validate(staged);
                status.text = error ?? "";
            }
        };
        return true;
    }

    private bool WireTextField(BoundRow row) {
        return WireFieldAndPick(row, row.Binding) || MissingWidget(row, "Field");
    }

    private bool WireFlags(BoundRow row) {
        Binding binding = row.Binding;
        Transform flags = QuickConfigUi.FindChild(row.Root.transform, "Flags");
        Transform template = flags != null ? QuickConfigUi.FindChild(flags, "Flag") : null;
        if (template == null) { return MissingWidget(row, "Flags/Flag"); }
        template.gameObject.SetActive(false);

        IList<string> members = binding.Options?.Invoke() ?? new List<string>();
        List<(string Member, Toggle Toggle)> toggles = new List<(string, Toggle)>();
        foreach (string member in members) {
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent, false);
            clone.name = member;
            clone.SetActive(true);
            TMP_Text label = QuickConfigUi.TextAt(clone.transform, "Label") ?? clone.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = member; }
            Toggle toggle = clone.GetComponent<Toggle>() ?? clone.GetComponentInChildren<Toggle>(true);
            if (toggle == null) { continue; }
            string name = member;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(on => {
                HashSet<string> set = binding.Get(staged) as HashSet<string> ?? new HashSet<string>(StringComparer.Ordinal);
                if (on) { set.Add(name); } else { set.Remove(name); }
                binding.Set(staged, set);
                Edited(null);
            });
            toggles.Add((name, toggle));
        }

        row.Sync = () => {
            HashSet<string> set = binding.Get(staged) as HashSet<string> ?? new HashSet<string>(StringComparer.Ordinal);
            foreach ((string member, Toggle toggle) in toggles) {
                toggle.SetIsOnWithoutNotify(set.Contains(member));
            }
        };
        return true;
    }

    private bool WireColor(BoundRow row) {
        Binding binding = row.Binding;
        Image swatch = QuickConfigUi.FindComponent<Image>(row.Root.transform, "Swatch");
        bool fieldWired = WireFieldAndPick(row, binding);
        if (fieldWired == false && swatch == null) { return MissingWidget(row, "Field/Swatch"); }

        Button cycle = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Cycle");
        Binding icon = binding.IconKey != null ? QuickConfigBindings.Get(binding.IconKey) : null;
        if (cycle != null) {
            if (icon != null) {
                WireCycle(row, cycle, icon);
            } else {
                // The set-item colour has no icon index.
                cycle.gameObject.SetActive(false);
            }
        }
        if (binding.IconKey == null) {
            // Hex only: the name picker would offer names this entry cannot hold.
            Button pick = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Pick");
            if (pick != null) { pick.gameObject.SetActive(false); }
        }

        row.Sync += () => {
            if (swatch == null) { return; }
            Color? color = QuickConfigBindings.ParseColor(binding.Get(staged) as string);
            swatch.color = color ?? new Color(0.35f, 0.35f, 0.35f, 1f);
        };
        return true;
    }

    private bool WireButton(BoundRow row) {
        Binding binding = row.Binding;
        Button button = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Button")
            ?? row.Root.GetComponent<Button>()
            ?? row.Root.GetComponentInChildren<Button>(true);
        if (button == null) { return MissingWidget(row, "Button"); }
        TMP_Text caption = QuickConfigUi.CaptionOf(button);
        if (caption != null && string.IsNullOrWhiteSpace(caption.text)) { caption.text = QuickConfigureTool.L(binding.DisplayName); }
        QuickConfigUi.Wire(button, () => {
            string message = null;
            try {
                message = binding.Act?.Invoke(staged);
            } catch (Exception e) {
                EpicLoot.LogWarningForce($"Quick Configure action '{binding.Key}' failed: {e}");
                message = e.Message;
            }
            Edited(message);
        });
        return true;
    }

    private bool WireReadout(BoundRow row) {
        Binding binding = row.Binding;
        TMP_Text label = QuickConfigUi.TextAt(row.Root.transform, "Label") ?? row.Root.GetComponentInChildren<TMP_Text>(true);
        if (label == null) { return MissingWidget(row, "Label"); }
        row.Sync = () => {
            try { label.text = binding.Readout?.Invoke(staged) ?? ""; } catch (Exception e) { label.text = e.Message; }
        };
        return true;
    }

    // ------------------------------------------------------------------------------------------------
    //  List rows: one Item template under Items (a ScrollRect), cloned per entry
    // ------------------------------------------------------------------------------------------------

    // Clones an inactive template once per entry and rebuilds only when the entries changed: a slider
    // elsewhere on the panel refreshes every row, and rebuilding a list of input fields on every tick
    // would eat a field being typed into.
    private sealed class ClonedList {
        private readonly Transform template;
        private readonly List<GameObject> clones = new List<GameObject>();
        private string built;

        internal ClonedList(Transform template) {
            this.template = template;
            template.gameObject.SetActive(false);
        }

        internal void Rebuild<T>(IList<T> entries, string signature, Action<Transform, T> bind, GameObject styleRoot) {
            if (signature == built) { return; }
            // Committing one field (onEndEdit) changes the signature while the field the player clicked
            // into next has just taken focus; destroying the clones now would take that focus with it.
            // The clones already show what was typed, so the rebuild waits for the next refresh.
            if (FocusIsInside(template.parent)) { return; }
            built = signature;
            foreach (GameObject clone in clones) {
                if (clone != null) { UnityEngine.Object.Destroy(clone); }
            }
            clones.Clear();
            foreach (T entry in entries) {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent, false);
                clone.name = template.name;
                clone.SetActive(true);
                clones.Add(clone);
                bind(clone.transform, entry);
            }
            QuickConfigStyle.Apply(styleRoot);
        }
    }

    private static bool FocusIsInside(Transform root) {
        GameObject selected = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
            : null;
        return root != null && selected != null && selected.transform.IsChildOf(root);
    }

    // The Item template under the row's Items ScrollRect content, or null (and a logged warning).
    private Transform ItemTemplate(BoundRow row) {
        ScrollRect items = QuickConfigUi.FindComponent<ScrollRect>(row.Root.transform, "Items");
        Transform content = items != null && items.content != null ? items.content : QuickConfigUi.FindChild(row.Root.transform, "Items");
        Transform template = content != null ? QuickConfigUi.FindChild(content, "Item") : null;
        if (template == null) { MissingWidget(row, "Items/Item"); }
        return template;
    }

    private static Transform HeadOf(BoundRow row) {
        return QuickConfigUi.FindChild(row.Root.transform, "Head") ?? row.Root.transform;
    }

    private static TMP_InputField BindField(Transform parent, string name, string value, TMP_InputField.ContentType type, Action<string> commit) {
        TMP_InputField field = QuickConfigUi.FindComponent<TMP_InputField>(parent, name);
        if (field == null) { return null; }
        field.contentType = type;
        field.SetTextWithoutNotify(value ?? "");
        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(text => commit(text ?? ""));
        return field;
    }

    private static Button BindButton(Transform parent, string name, UnityAction onClick) {
        Button button = QuickConfigUi.FindComponent<Button>(parent, name);
        if (button != null) { QuickConfigUi.Wire(button, onClick); }
        return button;
    }

    // A picker button over an option list that may not exist (the item list exists only in a world);
    // a hidden button is clearer than one that opens an empty picker. Availability is decided once.
    private static void BindPicker(Button pick, Func<IList<string>> options, string title, Func<string> current, Action<string> picked) {
        if (pick == null) { return; }
        bool hasOptions = options != null && options() != null;
        pick.gameObject.SetActive(hasOptions);
        if (hasOptions) {
            QuickConfigUi.Wire(pick, () => QuickConfigPicker.Show(title, options(), current(), picked));
        }
    }

    private static string Caption(Button button, string text) {
        TMP_Text caption = QuickConfigUi.CaptionOf(button);
        if (caption != null) { caption.text = text ?? ""; }
        return text;
    }

    // Effect configs: every loaded effect's Config block; the shown effect is UI state on the row.
    // Keys are fixed (read-only Key field, no Add/Remove) except for open-key effects (Riches).
    private bool WireEffectConfigs(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button effectButton = QuickConfigUi.FindComponent<Button>(head, "Effect") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Effect");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        EffectConfigsValue Value() => binding.Get(staged) as EffectConfigsValue ?? new EffectConfigsValue();

        void Commit(EffectConfigsValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (effectButton != null) {
            QuickConfigUi.Wire(effectButton, () => {
                List<EffectConfigSet> sets = Value().PickerOrder();
                List<string> names = sets.Select(set => set.PickerName).ToList();
                string current = Value().Get(selected)?.PickerName;
                QuickConfigPicker.Show(binding.DisplayName, names, current, picked => {
                    int index = names.IndexOf(picked);
                    if (index >= 0) { selected = sets[index].Type; }
                    row.Sync?.Invoke();
                    EffectConfigSet chosen = Value().Get(selected);
                    if (chosen != null && chosen.Source == EffectSource.ReadOnly) {
                        Edited($"{chosen.DisplayName} is read-only: it is registered by another mod or synthesized in code, so its values live outside these files.");
                    }
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                EffectConfigsValue value = Value();
                EffectConfigSet set = value.Get(selected);
                if (set == null || set.OpenKeys == false || set.Source == EffectSource.ReadOnly) { return; }
                set.Entries.Add(new EffectConfigEntry { Key = "", Value = 1f });
                Commit(value);
            });
        }

        row.Sync = () => {
            EffectConfigsValue value = Value();
            if (selected == null || value.Get(selected) == null) { selected = value.PickerOrder().FirstOrDefault()?.Type; }
            EffectConfigSet set = value.Get(selected);
            Caption(effectButton, set?.PickerName ?? "");
            bool editable = set != null && set.Source != EffectSource.ReadOnly;
            bool open = editable && set.OpenKeys;
            if (add != null) { add.gameObject.SetActive(open); }
            List<EffectConfigEntry> entries = set?.Entries ?? new List<EffectConfigEntry>();
            string signature = $"{selected}|{editable}|{open}|" + string.Join("|", entries.Select(entry => $"{entry.Key}={entry.Value.ToString("R", CultureInfo.InvariantCulture)}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                QuickConfigTooltip.Attach(clone.gameObject, QuickConfigTooltip.Text(entry.Key, EffectConfigTables.KeyLabel(selected, entry.Key)));
                TMP_InputField keyField = BindField(clone, "Key", entry.Key, TMP_InputField.ContentType.Standard, text => {
                    entry.Key = text.Trim();
                    Commit(Value());
                });
                if (keyField != null) { keyField.interactable = open; }
                TMP_InputField valueField = BindField(clone, "Value", entry.Value.ToString("0.####", CultureInfo.InvariantCulture), TMP_InputField.ContentType.Standard, text => {
                    if (float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)) { entry.Value = parsed; }
                    Commit(Value());
                });
                if (valueField != null) { valueField.interactable = editable; }
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), open ? binding.Options : null, binding.DisplayName, () => entry.Key, picked => {
                    entry.Key = picked;
                    Commit(Value());
                });
                Button remove = QuickConfigUi.FindComponent<Button>(clone, "Remove");
                if (remove != null) {
                    remove.gameObject.SetActive(open);
                    if (open) {
                        QuickConfigUi.Wire(remove, () => {
                            EffectConfigsValue current = Value();
                            current.Get(selected)?.Entries.Remove(entry);
                            Commit(current);
                        });
                    }
                }
            }, row.Root);
        };
        return true;
    }

    // Bounty targets: every biome is staged; the shown biome is UI state on the row.
    private bool WireBounties(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button biomeButton = QuickConfigUi.FindComponent<Button>(head, "Biome") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Biome");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        BountiesValue Value() => binding.Get(staged) as BountiesValue ?? new BountiesValue();

        void Commit(BountiesValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (biomeButton != null) {
            QuickConfigUi.Wire(biomeButton, () => {
                List<string> biomes = Value().BiomeOrder.ToList();
                List<string> display = biomes.Select(QuickConfigBindings.BiomeDisplayName).ToList();
                QuickConfigPicker.Show(binding.DisplayName, display, QuickConfigBindings.BiomeDisplayName(selected), picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0) { selected = biomes[index]; }
                    row.Sync?.Invoke();
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                BountiesValue value = Value();
                List<BountyEntry> entries = value.Get(selected);
                if (entries == null) { return; }
                entries.Add(new BountyEntry { TargetID = "" });
                Commit(value);
            });
        }

        row.Sync = () => {
            BountiesValue value = Value();
            if (selected == null || value.Get(selected) == null) { selected = value.BiomeOrder.FirstOrDefault(); }
            Caption(biomeButton, QuickConfigBindings.BiomeDisplayName(selected));
            List<BountyEntry> entries = value.Get(selected) ?? new List<BountyEntry>();
            string signature = selected + "|" + string.Join("|", entries.Select(entry => $"{entry.TargetID}:{entry.RewardIron}:{entry.RewardGold}:{entry.RewardCoins}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                BindField(clone, "Target", entry.TargetID, TMP_InputField.ContentType.Standard, text => {
                    entry.TargetID = text.Trim();
                    Commit(Value());
                });
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), binding.Options, binding.DisplayName, () => entry.TargetID, picked => {
                    entry.TargetID = picked;
                    Commit(Value());
                });
                BindField(clone, "Iron", entry.RewardIron.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardIron = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardIron));
                    Commit(Value());
                });
                BindField(clone, "Gold", entry.RewardGold.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardGold = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardGold));
                    Commit(Value());
                });
                BindField(clone, "Coins", entry.RewardCoins.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardCoins = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardCoins));
                    Commit(Value());
                });
                BindButton(clone, "Remove", () => {
                    BountiesValue current = Value();
                    current.Get(selected)?.Remove(entry);
                    Commit(current);
                });
            }, row.Root);
        };
        return true;
    }

    // Treasure map costs: one fixed row per biome of the file (no Add/Remove), coins and forest tokens.
    private bool WireBiomeCosts(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);

        List<BiomeCostEntry> Entries() => binding.Get(staged) as List<BiomeCostEntry> ?? new List<BiomeCostEntry>();

        void Commit() {
            binding.Set(staged, Entries());
            Edited(null);
        }

        row.Sync = () => {
            List<BiomeCostEntry> entries = Entries();
            string signature = string.Join("|", entries.Select(entry => $"{entry.Biome}:{entry.Cost}:{entry.ForestTokens}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                TMP_Text name = QuickConfigUi.TextAt(clone, "Name");
                if (name != null) { name.text = QuickConfigBindings.BiomeDisplayName(entry.Biome); }
                BindField(clone, "Cost", entry.Cost.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.Cost = Mathf.Max(-1, QuickConfigBindings.ToInt(text, entry.Cost));
                    Commit();
                });
                BindField(clone, "Tokens", entry.ForestTokens.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.ForestTokens = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.ForestTokens));
                    Commit();
                });
            }, row.Root);
        };
        return true;
    }

    // Item categories: every category is staged; the one on show is UI state on the row.
    private bool WireItemCategories(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button category = QuickConfigUi.FindComponent<Button>(head, "Category") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Category");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        ItemCategoriesValue Value() => binding.Get(staged) as ItemCategoriesValue ?? new ItemCategoriesValue();

        void Commit(ItemCategoriesValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (category != null) {
            QuickConfigUi.Wire(category, () => {
                ItemCategoriesValue value = Value();
                QuickConfigPicker.Show(binding.DisplayName, value.Categories, selected, picked => {
                    selected = picked;
                    row.Sync?.Invoke();
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                ItemCategoriesValue value = Value();
                List<ItemByBossEntry> entries = value.Get(selected);
                if (entries == null) { return; }
                entries.Add(new ItemByBossEntry { Boss = "none", Item = "" });
                Commit(value);
            });
        }

        row.Sync = () => {
            ItemCategoriesValue value = Value();
            if (selected == null || value.Entries.ContainsKey(selected) == false) {
                selected = value.Categories.Count > 0 ? value.Categories[0] : null;
            }
            Caption(category, selected ?? "");
            List<ItemByBossEntry> entries = value.Get(selected) ?? new List<ItemByBossEntry>();
            string signature = selected + "|" + string.Join("|", entries.Select(entry => $"{entry.Boss}:{entry.Item}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                Button boss = QuickConfigUi.FindComponent<Button>(clone, "Boss");
                Caption(boss, entry.Boss);
                if (boss != null) {
                    QuickConfigUi.Wire(boss, () => QuickConfigPicker.Show("Boss key",
                        binding.KeyOptions?.Invoke(staged) ?? new List<string> { "none" }, entry.Boss, picked => {
                            entry.Boss = picked;
                            Commit(Value());
                        }));
                }
                BindField(clone, "Field", entry.Item, TMP_InputField.ContentType.Standard, text => {
                    entry.Item = text.Trim();
                    Commit(Value());
                });
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), binding.Options, binding.DisplayName, () => entry.Item, picked => {
                    entry.Item = picked;
                    Commit(Value());
                });
                BindButton(clone, "Remove", () => {
                    ItemCategoriesValue current = Value();
                    current.Get(selected)?.Remove(entry);
                    Commit(current);
                });
            }, row.Root);
        };
        return true;
    }

    // Biome drop tables: one fixed row per drop target of the selected biome (no Add/Remove); the
    // selected biome is UI state on the row.
    private bool WireBiomeDrops(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button biomeButton = QuickConfigUi.FindComponent<Button>(head, "Biome") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Biome");
        string selected = null;

        List<BiomeDropRow> Rows() => binding.Get(staged) as List<BiomeDropRow> ?? new List<BiomeDropRow>();

        // Rows arrive grouped in progression order, so the distinct biomes are already ordered.
        List<string> Groups(List<BiomeDropRow> rows) => rows.Select(r => r.Biome).Distinct().ToList();

        void Commit() {
            binding.Set(staged, Rows());
            Edited(null);
        }

        if (biomeButton != null) {
            QuickConfigUi.Wire(biomeButton, () => {
                List<string> groups = Groups(Rows());
                List<string> display = groups.Select(BiomeDropTables.GroupDisplayName).ToList();
                QuickConfigPicker.Show(binding.DisplayName, display, BiomeDropTables.GroupDisplayName(selected), picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0) { selected = groups[index]; }
                    row.Sync?.Invoke();
                });
            });
        }

        row.Sync = () => {
            List<BiomeDropRow> rows = Rows();
            List<string> groups = Groups(rows);
            if (selected == null || groups.Contains(selected) == false) { selected = groups.FirstOrDefault(); }
            Caption(biomeButton, BiomeDropTables.GroupDisplayName(selected));
            List<BiomeDropRow> entries = rows.Where(r => r.Biome == selected).ToList();
            string signature = selected + "|" + string.Join("|", entries.Select(entry => $"{entry.Id}:{entry.AmountText}:{entry.RarityText}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                TMP_Text name = QuickConfigUi.TextAt(clone, "Name");
                if (name != null) { name.text = entry.Label + (entry.Mixed ? " (mixed)" : ""); }
                BindField(clone, "Amount", entry.AmountText, TMP_InputField.ContentType.Standard, text => {
                    entry.AmountText = text.Trim();
                    Commit();
                });
                BindField(clone, "Rarity", entry.RarityText, TMP_InputField.ContentType.Standard, text => {
                    entry.RarityText = text.Trim();
                    Commit();
                });
            }, row.Root);
        };
        return true;
    }
}
