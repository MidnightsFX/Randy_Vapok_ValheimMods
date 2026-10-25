namespace EpicLoot.Compendium;

public class MagicTextInfo(string topic, bool showSearchBar = true) : TextsDialog.TextInfo(topic, "")
{
    public readonly bool ShowSearchBar = showSearchBar;
    public virtual void Build(MagicPages instance) { }

    /// <summary>Whether the page's table rows can be narrowed to one item type.</summary>
    public virtual bool HasItemTypeFilter => false;

    /// <summary>Whether the row tagged <paramref name="tag"/> applies to items of <paramref name="type"/>.</summary>
    public virtual bool AllowsItemType(object tag, ItemTypeOption type) => true;
}
