using System;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using EpicLoot.Config;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compatibility;

/// <summary>
/// Item Favorite Framework integration. The player marks items (and inventory slots) as favorites in
/// that mod; here they are left out of every enchanting-table list that destroys or consumes the item,
/// and get a star wherever the tables list items.
///
/// Soft in every direction: no assembly reference, only a soft BepInDependency for load order. The
/// framework's API is bound once, to typed delegates, the first time it is needed. When the mod is
/// absent every check answers "not favorited" and no star is drawn.
/// </summary>
public static class ItemFavorites
{
    private const string PluginGuid = "MidnightsFX.ItemFavoriteFramework";
    private const string ApiTypeName = "ItemFavoriteFramework.API, ItemFavoriteFramework";
    private const string StarName = "EL_FavoriteStar";

    // Bits of the framework's GetMarks.
    private const int MarkItemFavorite = 1;
    private const int MarkSlotFavorite = 2;

    private static bool _resolved;
    private static bool _reportedFailure;
    private static Func<ItemDrop.ItemData, bool> _isProtected;
    private static Func<ItemDrop.ItemData, int> _getMarks;
    private static Func<Sprite> _getFavoriteIcon;
    private static Func<Color> _getFavoriteColor;
    private static Func<Color> _getFavoritedSlotColor;

    /// <summary>True when Item Favorite Framework is installed and its API bound.</summary>
    public static bool IsLoaded
    {
        get
        {
            Resolve();
            return _isProtected != null;
        }
    }

    /// <summary>
    /// The player favorited this item, or it sits in a favorited slot of their inventory, and they
    /// want Epic Loot to respect that. Use it to keep an item out of anything that destroys or
    /// consumes it, both when building a list and again when the action runs.
    /// </summary>
    public static bool IsProtected(ItemDrop.ItemData item)
    {
        if (item == null || !ELConfig.RespectItemFavorites.Value)
        {
            return false;
        }

        Resolve();
        if (_isProtected == null)
        {
            return false;
        }

        try
        {
            return _isProtected(item);
        }
        catch (Exception e)
        {
            // Fail closed: when favorites cannot be checked, hide the item rather than risk
            // destroying one the player marked to keep.
            ReportFailure(e);
            return true;
        }
    }

    /// <summary>
    /// Shows or hides the favorite star on an item icon: gold for a favorited item, the slot colour
    /// for an item only protected by its slot. Call on every refresh, including for empty rows (null
    /// item), because list rows are pooled and would otherwise keep a star from their previous item.
    /// </summary>
    public static void UpdateStar(Image icon, ItemDrop.ItemData item)
    {
        if (icon == null)
        {
            return;
        }

        Transform existing = icon.transform.Find(StarName);
        int marks = item != null && ELConfig.ShowFavoriteStars.Value ? GetMarks(item) : 0;
        if ((marks & (MarkItemFavorite | MarkSlotFavorite)) == 0)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Image star = existing != null ? existing.GetComponent<Image>() : CreateStar(icon);
        if (star == null)
        {
            return;
        }

        star.color = (marks & MarkItemFavorite) != 0 ? _getFavoriteColor() : _getFavoritedSlotColor();
        star.gameObject.SetActive(true);
    }

    private static int GetMarks(ItemDrop.ItemData item)
    {
        Resolve();
        if (_getMarks == null)
        {
            return 0;
        }

        try
        {
            return _getMarks(item);
        }
        catch (Exception e)
        {
            ReportFailure(e);
            return 0;
        }
    }

    // In the icon's top-left corner, sized by anchors so it follows whatever size the row gives the
    // icon. A child of the icon, so it draws above it and hides with it.
    private static Image CreateStar(Image icon)
    {
        Sprite sprite = _getFavoriteIcon?.Invoke();
        if (sprite == null)
        {
            return null;
        }

        GameObject go = new GameObject(StarName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(icon.transform, false);
        rect.anchorMin = new Vector2(0f, 0.55f);
        rect.anchorMax = new Vector2(0.45f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image star = go.GetComponent<Image>();
        star.sprite = sprite;
        star.preserveAspect = true;
        star.raycastTarget = false;
        return star;
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        if (!Chainloader.PluginInfos.ContainsKey(PluginGuid))
        {
            return;
        }

        try
        {
            Type api = Type.GetType(ApiTypeName, throwOnError: false);
            int apiVersion = api == null ? 0 : Bind<Func<int>>(api, "GetApiVersion")?.Invoke() ?? 0;
            if (apiVersion < 1)
            {
                EpicLoot.LogWarningForce("Item Favorite Framework is installed but its API was not found; favorites are ignored.");
                return;
            }

            _getMarks = Bind<Func<ItemDrop.ItemData, int>>(api, "GetMarks");
            _getFavoriteIcon = Bind<Func<Sprite>>(api, "GetFavoriteIcon");
            _getFavoriteColor = Bind<Func<Color>>(api, "GetFavoriteColor");
            _getFavoritedSlotColor = Bind<Func<Color>>(api, "GetFavoritedSlotColor");
            // Last: IsLoaded keys off it, so a partial bind never reads as loaded.
            Func<ItemDrop.ItemData, bool> isProtected = Bind<Func<ItemDrop.ItemData, bool>>(api, "IsProtected");
            if (isProtected == null || _getMarks == null || _getFavoriteIcon == null || _getFavoriteColor == null ||
                _getFavoritedSlotColor == null)
            {
                _getMarks = null;
                EpicLoot.LogWarningForce("Item Favorite Framework's API is missing an endpoint Epic Loot uses; favorites are ignored.");
                return;
            }

            _isProtected = isProtected;
            EpicLoot.Log($"Item Favorite Framework found (API {apiVersion}); favorited items are kept out of destructive enchanting lists.");
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not bind Item Favorite Framework's API; favorites are ignored: {e.Message}");
        }
    }

    // Matches the delegate's exact signature, so a renamed or changed endpoint binds to nothing
    // instead of throwing at the call site.
    private static T Bind<T>(Type api, string name) where T : class
    {
        MethodInfo invoke = typeof(T).GetMethod("Invoke");
        Type[] parameters = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
        MethodInfo method = api.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
        if (method == null || method.ReturnType != invoke.ReturnType)
        {
            return null;
        }

        return Delegate.CreateDelegate(typeof(T), method, throwOnBindFailure: false) as T;
    }

    private static void ReportFailure(Exception e)
    {
        if (_reportedFailure)
        {
            return;
        }

        _reportedFailure = true;
        EpicLoot.LogErrorForce($"Item Favorite Framework threw while checking an item; items it cannot check are treated as favorited: {e}");
    }
}
