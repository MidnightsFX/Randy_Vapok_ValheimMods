using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public static class GrapplingHook
{
    // Named flight time rather than range on purpose: gravity pulls on the shot for the whole flight, so
    // a longer lifetime only adds its full percentage in reach on level or downhill shots. Aiming up gains
    // less (+100% flight time is about +71% straight up), so calling it range would overpromise.
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    [HarmonyPostfix]
    public static void Projectile_Setup_Postfix(Projectile __instance, Character owner)
    {
        if (owner == null || owner != Player.m_localPlayer || !IsGrapplingShot(__instance))
        {
            return;
        }

        float flightTime = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyGrappleFlightTime, 0.01f);
        if (flightTime > 0f)
        {
            __instance.m_ttl *= 1f + flightTime;
        }
    }

    // Vanilla's reload block is sized for the unmodified flight, and m_grappling only takes over once the
    // hook attaches, so hold the reload off until the longer flight lands or expires. This has to run
    // after ProjectileAttackTriggered, which writes m_blockReload after the projectile is already set up.
    [HarmonyPatch(typeof(Attack), "ProjectileAttackTriggered")]
    [HarmonyPostfix]
    public static void Attack_ProjectileAttackTriggered_Postfix(Attack __instance)
    {
        if (__instance.m_character is not Player player || player != Player.m_localPlayer ||
            __instance.m_attackProjectile == null ||
            !__instance.m_attackProjectile.TryGetComponent(out Projectile projectile) || !IsGrapplingShot(projectile))
        {
            return;
        }

        float flightTime = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyGrappleFlightTime, 0.01f);
        if (flightTime > 0f)
        {
            player.m_blockReload = Mathf.Max(player.m_blockReload, projectile.m_ttl * (1f + flightTime));
        }
    }

    [HarmonyPatch(typeof(GrapplingPoint), nameof(GrapplingPoint.Activate))]
    [HarmonyPostfix]
    public static void GrapplingPoint_Activate_Postfix(GrapplingPoint __instance, Character character)
    {
        if (character == null || character != Player.m_localPlayer)
        {
            return;
        }

        Player player = Player.m_localPlayer;

        float pullSpeed = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyGrapplePullSpeed, 0.01f);
        if (pullSpeed > 0f)
        {
            __instance.m_pullForce *= 1f + pullSpeed;
        }

        // Update breaks the line the moment it is longer than m_maxLength, so the rope has to grow with
        // everything that lets the shot land farther away, or a long shot snaps as soon as it attaches.
        float flightTime = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyGrappleFlightTime, 0.01f);
        float projectileSpeed = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyProjectileSpeed, 0.01f);
        if (flightTime > 0f || projectileSpeed > 0f)
        {
            __instance.m_maxLength *= (1f + flightTime) * (1f + projectileSpeed);
        }
    }

    private static bool IsGrapplingShot(Projectile projectile)
    {
        return projectile.m_spawnOnHit != null && projectile.m_spawnOnHit.GetComponent<GrapplingPoint>() != null;
    }
}
