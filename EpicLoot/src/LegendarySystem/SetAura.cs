using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EpicLoot.LegendarySystem
{
    public static class SetAura
    {
        private const string ZdoKey = "el-aura";

        private class AuraState
        {
            public string SetID = "";
            public readonly List<GameObject> Instances = new List<GameObject>();
        }

        private static readonly ConditionalWeakTable<Player, AuraState> States = new ConditionalWeakTable<Player, AuraState>();
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();

        public static void Publish(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.m_nview == null ||
                !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = "";
            foreach (LegendarySetProgress progress in SetBonusEvaluator.GetEquippedSetProgress(player))
            {
                if (progress.IsFull && progress.Set.FullSetFx?.Count > 0)
                {
                    setID = progress.Set.ID;
                    break;
                }
            }

            if (zdo.GetString(ZdoKey) != setID)
            {
                zdo.Set(ZdoKey, setID);
            }

            Refresh(player);
        }

        public static void Refresh(Player player)
        {
            if (player == null || player.m_nview == null || !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = zdo.GetString(ZdoKey);
            AuraState state = States.GetOrCreateValue(player);
            if (state.SetID == setID)
            {
                return;
            }

            foreach (GameObject instance in state.Instances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            state.Instances.Clear();
            state.SetID = setID;

            if (string.IsNullOrEmpty(setID) || !UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo set) ||
                set.FullSetFx == null)
            {
                return;
            }

            foreach (string fx in set.FullSetFx)
            {
                GameObject instance = Attach(player, fx);
                if (instance != null)
                {
                    state.Instances.Add(instance);
                }
            }
        }

        private static GameObject Attach(Player player, string fx)
        {
            // "Prefab/child/path" takes a child of the prefab, for effects that only exist inside a creature's rig.
            int slash = fx.IndexOf('/');
            string prefabName = slash > 0 ? fx.Substring(0, slash) : fx;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            Transform child = prefab != null && slash > 0 ? prefab.transform.Find(fx.Substring(slash + 1)) : null;
            GameObject source = slash > 0 ? child?.gameObject : prefab;
            if (source == null)
            {
                if (MissingLogged.Add(fx))
                {
                    EpicLoot.LogWarning($"[SetAura] could not find '{fx}'; the set aura shows without it.");
                }

                return null;
            }

            GameObject instance;
            ZNetView.m_forceDisableInit = true;
            try
            {
                // Each client builds its own copy from the wearer's ZDO, so the prefab's ZNetView must not
                // register this one as a networked object.
                instance = Object.Instantiate(source, player.GetCenterPoint(), player.transform.rotation, player.transform);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            instance.name = $"EL_SetAura_{fx}";

            // Vanilla effects made to end on their own would take the aura with them.
            foreach (TimedDestruction timed in instance.GetComponentsInChildren<TimedDestruction>(true))
            {
                Object.Destroy(timed);
            }

            return instance;
        }
    }
}
