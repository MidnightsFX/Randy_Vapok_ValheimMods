using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    // Local-only copies of vanilla fx prefabs, networked or not, seen by this client alone. ZNetView.m_forceDisableInit
    // keeps a networked prefab from creating a ZDO (as vanilla's build ghost does), and removing its ZNetView makes
    // its TimedDestruction fall back to a plain Destroy. Particle systems mostly simulate in Local scaling, which
    // ignores the root's scale, so a scaled copy switches them to Hierarchy.
    internal static class LocalFx
    {
        internal static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f,
            Transform parent = null, float lifetime = 10f)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject fx;
            ZNetView.m_forceDisableInit = true;
            try
            {
                fx = Object.Instantiate(prefab, position, rotation, parent);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            foreach (var sync in fx.GetComponentsInChildren<ZSyncTransform>(true))
            {
                Object.DestroyImmediate(sync);
            }
            foreach (var nview in fx.GetComponentsInChildren<ZNetView>(true))
            {
                Object.DestroyImmediate(nview);
            }

            if (!Mathf.Approximately(scale, 1f))
            {
                fx.transform.localScale *= scale;
                foreach (var particles in fx.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
            }

            // A fallback, should the copy have no TimedDestruction of its own.
            if (lifetime > 0f)
            {
                Object.Destroy(fx, lifetime);
            }
            return fx;
        }
    }
}
