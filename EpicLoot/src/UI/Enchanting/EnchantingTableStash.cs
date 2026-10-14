using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class StashEntry : IListElement
    {
        public string PrefabName;
        public ItemDrop.ItemData Item;

        public List<string> GetEffectNames() => new List<string>();
        public string GetEnchantName() => string.Empty;
        public ItemDrop.ItemData GetItem() => Item;
        public int GetMax() => Item?.m_stack ?? 0;
        public string GetDisplayNameSuffix() => string.Empty;
    }

    /// <summary>
    /// The enchanting table's material storage: a count per item kind, kept on the table's ZDO by its owner.
    /// Every change is a delta sent to the owner, who clamps it to what the stash holds and to the capacity,
    /// writes the result and answers with what was actually applied. Until the answer and the ZDO sync
    /// arrive, the delta is shown as pending so the table's own view stays immediate.
    /// </summary>
    public class EnchantingTableStash
    {
        private const string ZdoKey = "el.et.stash";
        private const int SerializationVersion = 1;
        private const string ChangeRpc = "el.StashChange";
        private const string ChangeResultRpc = "el.StashChangeResult";
        private const float RequestTimeoutSeconds = 10f;

        private static int _lastRequestId;

        private class Request
        {
            public int Id;
            public Dictionary<string, int> Deltas;
            public Action<Dictionary<string, int>> OnApplied;
            public float SentAt;
            public bool Confirmed;
            public uint ConfirmedRevision;
        }

        public event Action OnChanged;

        private readonly EnchantingTable _table;
        private readonly ZNetView _nview;
        private readonly Dictionary<string, int> _synced = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _view = new Dictionary<string, int>();
        private readonly List<Request> _requests = new List<Request>();
        private readonly Dictionary<string, ItemDrop.ItemData> _snapshots = new Dictionary<string, ItemDrop.ItemData>();
        private readonly Dictionary<ItemDrop.ItemData, string> _snapshotPrefabs = new Dictionary<ItemDrop.ItemData, string>();
        private uint _syncedRevision;
        private bool _everSynced;

        public EnchantingTableStash(EnchantingTable table, ZNetView nview)
        {
            _table = table;
            _nview = nview;
            _nview.Register<ZPackage>(ChangeRpc, RPC_StashChange);
            _nview.Register<ZPackage>(ChangeResultRpc, RPC_StashChangeResult);
            Sync();
        }

        public bool IsAvailable => _table != null && _table.IsFeatureAvailable(EnchantingFeature.Storage);
        public bool CanDeposit => IsAvailable && _table.IsFeatureUnlocked(EnchantingFeature.Storage);

        /// <summary>How many of each kind the table holds; 0 means no limit.</summary>
        public int Capacity
        {
            get
            {
                if (!CanDeposit)
                {
                    return 0;
                }

                float value = _table.GetFeatureCurrentValue(EnchantingFeature.Storage).Item1;
                return value > 0 ? Mathf.RoundToInt(value) : 0;
            }
        }

        public int TotalCount
        {
            get
            {
                int total = 0;
                foreach (int count in _view.Values)
                {
                    total += count;
                }

                return total;
            }
        }

        public int GetCount(string prefabName)
        {
            return prefabName != null && _view.TryGetValue(prefabName, out int count) ? count : 0;
        }

        public int GetCountBySharedName(string sharedName)
        {
            return StashCatalog.TryGetBySharedName(sharedName, out StashItemKind kind) ? GetCount(kind.PrefabName) : 0;
        }

        public IEnumerable<ItemDrop.ItemData> GetSnapshotItems()
        {
            return _snapshots.Values;
        }

        public bool TryGetSnapshotPrefab(ItemDrop.ItemData item, out string prefabName)
        {
            prefabName = null;
            return item != null && _snapshotPrefabs.TryGetValue(item, out prefabName);
        }

        public int GetRoom(string prefabName)
        {
            if (!CanDeposit)
            {
                return 0;
            }

            int capacity = Capacity;
            return capacity > 0 ? Mathf.Max(0, capacity - GetCount(prefabName)) : int.MaxValue;
        }

        public void Update()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            if (!_everSynced || _nview.GetZDO().DataRevision != _syncedRevision)
            {
                Sync();
            }

            if (_requests.RemoveAll(x => !x.Confirmed && Time.time - x.SentAt > RequestTimeoutSeconds) > 0)
            {
                RebuildView();
            }
        }

        public void DepositAll()
        {
            Player player = Player.m_localPlayer;
            Inventory inventory = player != null ? player.GetInventory() : null;
            if (inventory == null || !CanDeposit)
            {
                return;
            }

            Dictionary<string, int> deltas = new Dictionary<string, int>();
            int moved = 0;
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (!StashCatalog.IsStashable(item))
                {
                    continue;
                }

                string prefabName = StashCatalog.GetPrefabName(item);
                deltas.TryGetValue(prefabName, out int alreadyMoving);
                int take = Mathf.Min(item.m_stack, GetRoom(prefabName) - alreadyMoving);
                if (take <= 0)
                {
                    continue;
                }

                inventory.RemoveItem(item, take);
                deltas[prefabName] = alreadyMoving + take;
                moved += take;
            }

            if (moved == 0)
            {
                player.Message(MessageHud.MessageType.Center, "$mod_epicloot_storage_nothingtodeposit");
                return;
            }

            Send(deltas, applied => RefundUnapplied(deltas, applied));
            player.Message(MessageHud.MessageType.TopLeft,
                Localization.instance.Localize("$mod_epicloot_storage_deposited", moved.ToString()));
        }

        public void Withdraw(string prefabName, int amount)
        {
            Player player = Player.m_localPlayer;
            Inventory inventory = player != null ? player.GetInventory() : null;
            if (inventory == null || !StashCatalog.TryGetByPrefab(prefabName, out StashItemKind kind))
            {
                return;
            }

            ItemDrop.ItemData template = kind.Prefab.GetComponent<ItemDrop>().m_itemData;
            amount = Mathf.Min(amount, GetCount(prefabName), template.m_shared.m_maxStackSize);
            if (amount <= 0)
            {
                return;
            }

            if (!inventory.CanAddItem(kind.Prefab, amount))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_noroom");
                return;
            }

            Send(new Dictionary<string, int> { { prefabName, -amount } }, applied =>
            {
                int taken = applied.TryGetValue(prefabName, out int delta) ? -delta : 0;
                if (taken > 0)
                {
                    InventoryManagement.Instance.GiveItem(kind.CreateItem(taken));
                }
            });
        }

        /// <summary>Spends up to <paramref name="amount"/> as a crafting cost; returns how many were taken.</summary>
        public int Take(string prefabName, int amount)
        {
            int take = Mathf.Min(amount, GetCount(prefabName));
            if (take <= 0)
            {
                return 0;
            }

            Send(new Dictionary<string, int> { { prefabName, -take } }, null);
            return take;
        }

        public int TakeBySharedName(string sharedName, int amount)
        {
            return StashCatalog.TryGetBySharedName(sharedName, out StashItemKind kind) ? Take(kind.PrefabName, amount) : 0;
        }

        public static Dictionary<string, int> ReadCounts(ZDO zdo)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            string data = zdo?.GetString(ZdoKey, string.Empty);
            if (string.IsNullOrEmpty(data))
            {
                return counts;
            }

            try
            {
                ZPackage package = new ZPackage(data);
                package.ReadInt();
                ReadCounts(package, counts);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EpicLoot] Could not read the enchanting table's storage: {e.Message}");
            }

            return counts;
        }

        private static void ReadCounts(ZPackage package, Dictionary<string, int> into)
        {
            int count = package.ReadInt();
            for (int i = 0; i < count; ++i)
            {
                string name = package.ReadString();
                int amount = package.ReadInt();
                into[name] = amount;
            }
        }

        private static void WriteCounts(ZPackage package, Dictionary<string, int> counts)
        {
            package.Write(counts.Count);
            foreach (KeyValuePair<string, int> pair in counts)
            {
                package.Write(pair.Key);
                package.Write(pair.Value);
            }
        }

        private static void WriteCounts(ZDO zdo, Dictionary<string, int> counts)
        {
            ZPackage package = new ZPackage();
            package.Write(SerializationVersion);
            WriteCounts(package, counts);
            zdo.Set(ZdoKey, package.GetBase64());
        }

        private void Send(Dictionary<string, int> deltas, Action<Dictionary<string, int>> onApplied)
        {
            if (deltas.Count == 0 || _nview == null || !_nview.IsValid())
            {
                return;
            }

            Request request = new Request
            {
                Id = ++_lastRequestId,
                Deltas = new Dictionary<string, int>(deltas),
                OnApplied = onApplied,
                SentAt = Time.time
            };
            _requests.Add(request);

            ZPackage package = new ZPackage();
            package.Write(request.Id);
            WriteCounts(package, deltas);
            _nview.InvokeRPC(ChangeRpc, package);

            RebuildView();
        }

        private void RPC_StashChange(long sender, ZPackage package)
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }

            int requestId = package.ReadInt();
            Dictionary<string, int> deltas = new Dictionary<string, int>();
            ReadCounts(package, deltas);

            ZDO zdo = _nview.GetZDO();
            Dictionary<string, int> counts = ReadCounts(zdo);
            Dictionary<string, int> applied = new Dictionary<string, int>();
            int capacity = Capacity;
            bool canDeposit = CanDeposit;
            foreach (KeyValuePair<string, int> delta in deltas)
            {
                counts.TryGetValue(delta.Key, out int current);
                int change;
                if (delta.Value > 0)
                {
                    change = !canDeposit ? 0 : capacity > 0 ? Mathf.Min(delta.Value, Mathf.Max(0, capacity - current)) : delta.Value;
                }
                else
                {
                    change = -Mathf.Min(-delta.Value, current);
                }

                int next = current + change;
                if (next > 0)
                {
                    counts[delta.Key] = next;
                }
                else
                {
                    counts.Remove(delta.Key);
                }

                applied[delta.Key] = change;
            }

            WriteCounts(zdo, counts);

            ZPackage reply = new ZPackage();
            reply.Write(requestId);
            WriteCounts(reply, applied);
            _nview.InvokeRPC(sender, ChangeResultRpc, reply);

            Sync();
        }

        private void RPC_StashChangeResult(long sender, ZPackage package)
        {
            int requestId = package.ReadInt();
            Request request = _requests.Find(x => x.Id == requestId);
            if (request == null)
            {
                return;
            }

            Dictionary<string, int> applied = new Dictionary<string, int>();
            ReadCounts(package, applied);
            request.Deltas = applied;
            request.Confirmed = true;
            request.ConfirmedRevision = _nview.GetZDO().DataRevision;
            if (_nview.IsOwner())
            {
                _requests.Remove(request);
            }

            Sync();
            request.OnApplied?.Invoke(applied);
        }

        // A confirmed delta stays in the view until a ZDO sync newer than the confirmation arrives, since the
        // owner wrote the ZDO before answering; the owner itself reads its own write straight away.
        private void Sync()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            ZDO zdo = _nview.GetZDO();
            _syncedRevision = zdo.DataRevision;
            _everSynced = true;
            _synced.Clear();
            foreach (KeyValuePair<string, int> pair in ReadCounts(zdo))
            {
                _synced[pair.Key] = pair.Value;
            }

            bool owner = _nview.IsOwner();
            _requests.RemoveAll(x => x.Confirmed && (owner || x.ConfirmedRevision < _syncedRevision));
            RebuildView();
        }

        private void RebuildView()
        {
            _view.Clear();
            foreach (KeyValuePair<string, int> pair in _synced)
            {
                _view[pair.Key] = pair.Value;
            }

            foreach (Request request in _requests)
            {
                foreach (KeyValuePair<string, int> delta in request.Deltas)
                {
                    _view.TryGetValue(delta.Key, out int current);
                    _view[delta.Key] = Mathf.Max(0, current + delta.Value);
                }
            }

            foreach (string prefabName in new List<string>(_snapshots.Keys))
            {
                if (GetCount(prefabName) <= 0)
                {
                    _snapshotPrefabs.Remove(_snapshots[prefabName]);
                    _snapshots.Remove(prefabName);
                }
            }

            foreach (KeyValuePair<string, int> pair in _view)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (_snapshots.TryGetValue(pair.Key, out ItemDrop.ItemData snapshot))
                {
                    snapshot.m_stack = pair.Value;
                }
                else if (StashCatalog.TryGetByPrefab(pair.Key, out StashItemKind kind))
                {
                    snapshot = kind.CreateItem(pair.Value);
                    _snapshots[pair.Key] = snapshot;
                    _snapshotPrefabs[snapshot] = pair.Key;
                }
            }

            OnChanged?.Invoke();
        }

        private static void RefundUnapplied(Dictionary<string, int> requested, Dictionary<string, int> applied)
        {
            foreach (KeyValuePair<string, int> pair in requested)
            {
                applied.TryGetValue(pair.Key, out int accepted);
                int refund = pair.Value - accepted;
                if (refund <= 0 || !StashCatalog.TryGetByPrefab(pair.Key, out StashItemKind kind))
                {
                    continue;
                }

                InventoryManagement.Instance.GiveItem(kind.CreateItem(refund));
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mod_epicloot_storage_full", kind.SharedName));
            }
        }
    }
}
