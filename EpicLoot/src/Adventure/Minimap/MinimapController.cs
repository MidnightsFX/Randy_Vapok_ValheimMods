using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.Biomes;
using UnityEngine;

namespace EpicLoot.Adventure;

[RequireComponent(typeof(Minimap))]
public class MinimapController : MonoBehaviour
{
    private static readonly Queue<PinJob> MinimapPinQueue = new();

    private Minimap _minimap;

    public const float AreaScale = 2.1f;

    public static readonly Dictionary<Tuple<int, Heightmap.Biome>, AreaPinInfo> TreasureMapPins = new();
    public static readonly Dictionary<string, AreaPinInfo> BountyPins = new();
    public static bool DebugMode;

    private static AdventurePinFilter _bountyPinFilter;
    private static AdventurePinFilter _treasurePinFilter;

    public virtual void Awake()
    {
        _minimap = GetComponent<Minimap>();

        if (!_minimap.m_icons.Exists(x => x.m_name == EpicLoot.TreasureMapPinType))
        {
            _minimap.m_icons.Add(new Minimap.SpriteData
            {
                m_name = EpicLoot.TreasureMapPinType,
                m_icon = EpicAssets.MapIconTreasureMap
            });
        }

        if (!_minimap.m_icons.Exists(x => x.m_name == EpicLoot.BountyPinType))
        {
            _minimap.m_icons.Add(new Minimap.SpriteData
            {
                m_name = EpicLoot.BountyPinType,
                m_icon = EpicAssets.MapIconBounty
            });
        }
    }

    private void Start()
    {
        EnsureVisibleIconTypeCapacity();
        SetupPinFilters();
        RefreshAdventurePinFilters();
    }

    public virtual void Update()
    {
        if (Player.m_localPlayer == null)
        {
            // Do not perform operations without access to adventure data
            return;
        }

        while (MinimapPinQueue.Any())
        {
            ProcessMinimapPinTask(MinimapPinQueue.Dequeue());
        }
    }

    private void OnDestroy()
    {
        MinimapPinQueue.Clear();
        TreasureMapPins.Clear();
        BountyPins.Clear();

        _bountyPinFilter?.Destroy();
        _treasurePinFilter?.Destroy();
        _bountyPinFilter = null;
        _treasurePinFilter = null;
    }

    private void EnsureVisibleIconTypeCapacity()
    {
        int required = (int)EpicLoot.TreasureMapPinType + 1;

        if (_minimap.m_visibleIconTypes != null && _minimap.m_visibleIconTypes.Length >= required)
        {
            return;
        }

        bool[] resized = new bool[required];

        for (int index = 0; index < resized.Length; ++index)
        {
            resized[index] = _minimap.m_visibleIconTypes == null ||
                             index >= _minimap.m_visibleIconTypes.Length ||
                             _minimap.m_visibleIconTypes[index];
        }

        _minimap.m_visibleIconTypes = resized;
    }

    private void SetupPinFilters()
    {
        if (_minimap.m_selectedIcon3 == null || _minimap.m_selectedIcon4 == null)
        {
            EpicLoot.LogError("Could not find the minimap pin icon row, adventure pins cannot be filtered!");
            return;
        }

        _bountyPinFilter = new AdventurePinFilter(_minimap, EpicLoot.BountyPinType, EpicAssets.MapIconBounty,
            "$mod_epicloot_merchant_bounties");
        _treasurePinFilter = new AdventurePinFilter(_minimap, EpicLoot.TreasureMapPinType, EpicAssets.MapIconTreasureMap,
            "$mod_epicloot_merchant_treasuremaps");

        GrowIconPanel(2);
    }

    private void GrowIconPanel(int addedIcons)
    {
        if (_minimap.m_selectedIcon0.transform.parent is not RectTransform firstIcon ||
            _minimap.m_selectedIcon1.transform.parent is not RectTransform secondIcon ||
            firstIcon.parent is not RectTransform panel || panel.parent == null)
        {
            return;
        }

        float growth = Mathf.Abs(secondIcon.anchoredPosition.y - firstIcon.anchoredPosition.y) * addedIcons;
        if (growth <= 0f)
        {
            return;
        }

        float panelTop = panel.localPosition.y + panel.rect.yMax;
        float panelCenterX = panel.localPosition.x + panel.rect.center.x;
        float panelWidth = panel.rect.width;

        panel.sizeDelta += new Vector2(0f, growth);
        panel.anchoredPosition += new Vector2(0f, growth * (1f - panel.pivot.y));

        foreach (RectTransform sibling in panel.parent.Cast<Transform>().OfType<RectTransform>())
        {
            if (sibling == panel || sibling.rect.width > panelWidth * 2f)
            {
                continue;
            }

            if (Mathf.Abs(sibling.localPosition.x + sibling.rect.center.x - panelCenterX) > panelWidth * 0.5f)
            {
                continue;
            }

            if (sibling.localPosition.y + sibling.rect.yMin < panelTop)
            {
                continue;
            }

            // Nothing names the death/boss panel or the d-pad hint, so they are picked out by sitting above the
            // icon panel in its own narrow column. Re-check this if the vanilla map layout changes.
            sibling.localPosition += new Vector3(0f, growth, 0f);
        }
    }

    /// <summary>
    /// When not connected to a world, the filters stay active. When connected and Adventure Mode is disabled, they are hidden.
    /// </summary>
    public static void RefreshAdventurePinFilters()
    {
        bool show = ShowAdventurePinFilters();

        _bountyPinFilter?.SetActive(show);
        _treasurePinFilter?.SetActive(show);

        PinJob pinJob = new PinJob
        {
            Task = MinimapPinQueueTask.RefreshAll
        };

        AddPinJobToQueue(pinJob);
    }

    public static void OnPinFilterToggled(Minimap.PinType type)
    {
        if (type == EpicLoot.BountyPinType)
        {
            ToggleBounties(ShowAdventureBountyPins());
        }
        else if (type == EpicLoot.TreasureMapPinType)
        {
            ToggleTreasureMaps(ShowAdventureTreasurePins());
        }
    }

    public static bool IsAdventurePinType(Minimap.PinType type)
    {
        return type == EpicLoot.BountyPinType || type == EpicLoot.TreasureMapPinType;
    }

    private static bool ShowAdventureBountyPins()
    {
        return _bountyPinFilter == null || _bountyPinFilter.Visible;
    }

    private static bool ShowAdventureTreasurePins()
    {
        return _treasurePinFilter == null || _treasurePinFilter.Visible;
    }

    private static bool ShowAdventurePinFilters()
    {
        // TODO: add more configuration options to hide minimap buttons as needed
        // Will need to ensure places this is used maintian logic if changed.
        return EpicLoot.IsAdventureModeEnabled();
    }

    private static void ToggleBounties(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        RefreshBounties(show);
    }

    private static void RefreshBounties(bool show)
    {
        if (ShowAdventurePinFilters() && show)
        {
            AdventureSaveData adventureSaveData = Player.m_localPlayer.GetAdventureSaveData();
            if (adventureSaveData == null) return;
            List<BountyInfo> currentBounties = adventureSaveData.GetInProgressBounties();

            foreach (BountyInfo bounty in currentBounties)
            {
                string key = bounty.ID;
                if (!BountyPins.ContainsKey(key))
                {
                    AreaPinInfo pinInfo = new AreaPinInfo
                    {
                        Position = bounty.Position + bounty.MinimapCircleOffset,
                        Type = EpicLoot.BountyPinType,
                        Name = Localization.instance.Localize("$mod_epicloot_bounties_minimappin", AdventureDataManager.GetBountyName(bounty))
                    };

                    PinJob pinJob = new PinJob
                    {
                        Task = MinimapPinQueueTask.AddBountyPin,
                        DebugMode = DebugMode,
                        BountyPin = new KeyValuePair<string, AreaPinInfo>(key, pinInfo)
                    };

                    AddPinJobToQueue(pinJob);
                }
            }
        }
        else
        {
            foreach (KeyValuePair<string, AreaPinInfo> pinEntry in BountyPins)
            {
                PinJob pinJob = new PinJob()
                {
                    Task = MinimapPinQueueTask.RemoveBountyPin,
                    DebugMode = DebugMode,
                    BountyPin = new KeyValuePair<string, AreaPinInfo>(pinEntry.Key, pinEntry.Value)
                };
                AddPinJobToQueue(pinJob);
            }
        }
    }
    private static void ToggleTreasureMaps(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        RefreshTreasureMaps(show);
    }

    private static void RefreshTreasureMaps(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (ShowAdventurePinFilters() && show)
        {
            AdventureSaveData adventureSaveData = Player.m_localPlayer.GetAdventureSaveData();
            if (adventureSaveData == null)
            {
                return;
            }

            List<TreasureMapChestInfo> unfoundTreasureChests = adventureSaveData.GetUnfoundTreasureChests();

            foreach (TreasureMapChestInfo chestInfo in unfoundTreasureChests)
            {
                Tuple<int, Heightmap.Biome> key = new Tuple<int, Heightmap.Biome>(chestInfo.Interval, chestInfo.Biome);
                if (!TreasureMapPins.ContainsKey(key))
                {
                    AreaPinInfo pinInfo = new AreaPinInfo
                    {
                        Position = chestInfo.Position + chestInfo.MinimapCircleOffset,
                        Type = EpicLoot.TreasureMapPinType,
                        Name = Localization.instance.Localize("$mod_epicloot_treasurechest_minimappin",
                            Localization.instance.Localize(BiomeDataManager.GetLocalizationToken(chestInfo.Biome)),
                            (chestInfo.Interval + 1).ToString())
                    };

                    PinJob pinJob = new PinJob
                    {
                        Task = MinimapPinQueueTask.AddTreasurePin,
                        DebugMode = DebugMode,
                        TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, pinInfo)
                    };

                    AddPinJobToQueue(pinJob);
                }
            }
        }
        else
        {
            foreach (KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo> pinEntry in TreasureMapPins)
            {
                PinJob pinJob = new PinJob()
                {
                    Task = MinimapPinQueueTask.RemoveTreasurePin,
                    DebugMode = DebugMode,
                    TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(pinEntry.Key, pinEntry.Value)
                };

                AddPinJobToQueue(pinJob);
            }
        }
    }

    public static void AddPinJobToQueue(PinJob pinJob)
    {
        if (pinJob != null)
        {
            MinimapPinQueue.Enqueue(pinJob);
        }
    }

    private void ProcessMinimapPinTask(PinJob pinJob)
    {
        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                if (ShowAdventureBountyPins())
                {
                    AddPin(pinJob);
                }
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                if (ShowAdventureTreasurePins())
                {
                    AddPin(pinJob);
                }
                break;
            case MinimapPinQueueTask.RemoveTreasurePin:
                RemovePin(pinJob.TreasurePin.Value);
                TreasureMapPins.Remove(pinJob.TreasurePin.Key);
                break;
            case MinimapPinQueueTask.RemoveBountyPin:
                RemovePin(pinJob.BountyPin.Value);
                BountyPins.Remove(pinJob.BountyPin.Key);
                break;
            case MinimapPinQueueTask.RefreshAll:
                RefreshPins();
                break;
        }
    }

    private void AddPin(PinJob pinJob)
    {
        AreaPinInfo newPin = null;
        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                newPin = pinJob.BountyPin.Value;
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                newPin = pinJob.TreasurePin.Value;
                break;
        }

        if (newPin == null)
        {
            return;
        }

        //Add Area Pin
        newPin.Area = _minimap.AddPin(newPin.Position, Minimap.PinType.EventArea, string.Empty, false, false);
        newPin.Area.m_worldSize = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * AreaScale;

        //Add Pin
        newPin.Pin = _minimap.AddPin(newPin.Position, newPin.Type, newPin.Name, false, false);

        //Add Debug Pin
        if (pinJob.DebugMode)
        {
            newPin.DebugPin = _minimap.AddPin(newPin.Position, Minimap.PinType.Icon3,
                $"{newPin.Position.x:0.0}, {newPin.Position.z:0.0}", false, false);
        }

        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                BountyPins[pinJob.BountyPin.Key] = pinJob.BountyPin.Value;
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                TreasureMapPins[pinJob.TreasurePin.Key] = pinJob.TreasurePin.Value;
                break;
        }
    }

    private void RemovePin(AreaPinInfo pinEntry)
    {
        _minimap.RemovePin(pinEntry.Pin);
        _minimap.RemovePin(pinEntry.Area);

        if (pinEntry.DebugPin != null)
        {
            _minimap.RemovePin(pinEntry.DebugPin);
        }
    }

    private void RefreshPins()
    {
        ToggleBounties(ShowAdventureBountyPins());
        ToggleTreasureMaps(ShowAdventureTreasurePins());
    }
}