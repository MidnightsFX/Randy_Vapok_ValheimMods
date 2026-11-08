using EpicLoot.General;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public enum StaffRole
    {
        Damage,
        Summon,
        Support
    }

    public static class StaffRoles
    {
        private const int MaxSpawnDepth = 4;

        private static readonly Dictionary<ItemDrop.ItemData.SharedData, HashSet<StaffRole>> Cache =
            new Dictionary<ItemDrop.ItemData.SharedData, HashSet<StaffRole>>();

        public static bool IsStaff(ItemDrop.ItemData item)
        {
            Skills.SkillType skill = item.m_shared.m_skillType;
            return skill == Skills.SkillType.BloodMagic || skill == Skills.SkillType.ElementalMagic;
        }

        public static bool HasAnyRole(ItemDrop.ItemData item, List<StaffRole> roles)
        {
            HashSet<StaffRole> itemRoles = GetRoles(item);
            foreach (StaffRole role in roles)
            {
                if (itemRoles.Contains(role))
                {
                    return true;
                }
            }

            return false;
        }

        public static HashSet<StaffRole> GetRoles(ItemDrop.ItemData item)
        {
            if (!Cache.TryGetValue(item.m_shared, out HashSet<StaffRole> roles))
            {
                roles = Classify(item.m_shared);
                Cache[item.m_shared] = roles;
            }

            return roles;
        }

        // Vanilla sets up a spawn-on-hit projectile without hit data, so it keeps its prefab damage instead of
        // the weapon's. On a weapon with no damage of its own (the Orb of Ahri's returning orb) that prefab
        // damage is the weapon's real damage.
        public static bool CarriesOwnDamage(Character owner, HitData hitData, ItemDrop.ItemData item)
        {
            return hitData == null && item?.m_shared != null && owner != null && owner == Player.m_localPlayer &&
                IsStaff(item) && item.m_shared.m_damages.EpicLootGetTotalDamage() <= 0f;
        }

        private static HashSet<StaffRole> Classify(ItemDrop.ItemData.SharedData shared)
        {
            var roles = new HashSet<StaffRole>();
            if (shared.m_damages.EpicLootGetTotalDamage() > 0f)
            {
                roles.Add(StaffRole.Damage);
            }

            var visited = new HashSet<GameObject>();
            Inspect(shared.m_attack?.m_attackProjectile, false, true, 0, visited, roles);
            Inspect(shared.m_secondaryAttack?.m_attackProjectile, false, true, 0, visited, roles);

            if (roles.Count == 0)
            {
                roles.Add(StaffRole.Support);
            }

            return roles;
        }

        // Mirrors how vanilla finds the object to set up: the attack's own IProjectile, or for a spawn-on-hit
        // object the first IProjectile anywhere in its children.
        private static void Inspect(GameObject prefab, bool spawned, bool receivesHitData, int depth,
            HashSet<GameObject> visited, HashSet<StaffRole> roles)
        {
            if (prefab == null || depth > MaxSpawnDepth || !visited.Add(prefab))
            {
                return;
            }

            IProjectile target = spawned ? prefab.GetComponentInChildren<IProjectile>() : prefab.GetComponent<IProjectile>();
            switch (target)
            {
                case SpawnAbility:
                    roles.Add(StaffRole.Summon);
                    break;
                case Aoe aoe:
                    if (!receivesHitData && aoe.m_hitEnemy && aoe.m_damage.EpicLootGetTotalDamage() > 0f)
                    {
                        roles.Add(StaffRole.Damage);
                    }
                    break;
                case Projectile projectile:
                    if (!receivesHitData && projectile.m_damage.EpicLootGetTotalDamage() > 0f)
                    {
                        roles.Add(StaffRole.Damage);
                    }
                    Inspect(projectile.m_spawnOnHit, true, projectile.m_projectilesInheritHitData, depth + 1, visited, roles);
                    break;
            }
        }
    }
}
