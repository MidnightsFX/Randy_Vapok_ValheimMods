using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Firebrand = nameof(Firebrand);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Firebrand (4-piece Surtr's Brand set bonus, NoRoll). With a two-handed axe or sword in hand (battleaxes,
    // Berserkir axes and greatswords), the third swing of every primary combo erupts in Brenna's fire nova: her
    // fx_fireskeleton_nova visual and skeleton_firenova_aoe blast, placed where she places it, just ahead of the hips.
    // Everything hostile within Radius takes the effect value as a percentage of the weapon's damage, all of it fire.
    // Every secondary attack erupts too, at SecondaryPower of that.
    //
    // It fires as the third swing comes out, hit or miss, as vanilla advances the combo either way. For battleaxes and
    // greatswords that swing is the finisher; Berserkir axes have a fourth swing, which does not erupt. Swings are told
    // apart by animation (the weapon's primary vs secondary attack) rather than Humanoid.m_currentAttackIsSecondary,
    // which attacks other sets start directly can leave stale.
    //
    // The damage is the weapon's own (GetDamage: enchantments, the set's fire imbue, upgrades and world level), not
    // counting the x2 vanilla gives the finishing swing itself. The Aoe then applies the weapon-skill factor, and
    // vanilla turns the fire into burning over time. The nova is a bonus hit (see HitSource): crits, lifesteal,
    // Executioner and the other strike effects come from the swing, not from it.
    //
    // Both prefabs are vanilla networked prefabs already in ZNetScene, so the attacking client spawns each once and
    // everyone sees the nova. Only the Aoe's owner deals its damage, and the visual's TimedDestruction removes it for all.
    [HarmonyPatch]
    public static class Firebrand
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultRadius = 3f;          // metres; Brenna's own blast radius
        public const float DefaultSecondaryPower = 0.5f; // the secondary attack's share of the full nova's damage
        public const float DefaultPushForce = 40f;      // Brenna's own knockback

        private const string RadiusKey = "Radius";
        private const string SecondaryPowerKey = "SecondaryPower";
        private const string PushForceKey = "PushForce";

        // Player-editable. The visual does not follow a retuned Radius: its particle systems use Local scaling, so
        // its shockwave ring stays at about 4.5 m whatever the blast covers.
        private static float Radius => EffectConfig.GetClamped(MagicEffectType.Firebrand, RadiusKey, DefaultRadius, 0.5f, 10f);
        private static float SecondaryPower => EffectConfig.GetClamped(MagicEffectType.Firebrand, SecondaryPowerKey, DefaultSecondaryPower, 0f, 2f);
        private static float PushForce => EffectConfig.GetClamped(MagicEffectType.Firebrand, PushForceKey, DefaultPushForce, 0f, 200f);

        private const string NovaFxPrefab = "fx_fireskeleton_nova";
        private const string NovaAoePrefab = "skeleton_firenova_aoe";

        // Brenna's attack spawns its blast at her hips, raised and pushed forward a little; a player's hips sit about
        // a metre up.
        private const float NovaHeight = 1f;
        private const float NovaForward = 0.7f;

        // The third swing of a combo, as Attack.m_currentAttackCainLevel counts them (from 0).
        private const int ThirdSwing = 2;

        // The swing that last erupted. Berserkir swings three and four carry two Hit events, so DoMeleeAttack runs
        // twice for them; every swing is a fresh Attack clone, so the reference tells a repeat apart. Held weakly, so
        // the last swing does not keep a logged-out player's objects alive.
        private static readonly System.WeakReference<Attack> LastNovaAttack = new System.WeakReference<Attack>(null);
        private static readonly HashSet<string> MissingPrefabLogged = new HashSet<string>();

        // Tooltip: "... within {1}m for {0}% of your weapon's damage. Your secondary attack erupts for {2}%."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Firebrand, value => new object[] { value, GetRadius(), value * SecondaryPower });
        }

        // Rounded to the centimetre so the tooltip shows the radius the blast actually uses.
        private static float GetRadius()
        {
            return Mathf.Round(Radius * 100f) / 100f;
        }

        public static bool IsFirebrandWeapon(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.SharedData shared = item?.m_shared;
            return shared != null &&
                shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon &&
                (shared.m_skillType == Skills.SkillType.Axes || shared.m_skillType == Skills.SkillType.Swords);
        }

        // A postfix on the melee swing itself: it runs only when a swing really triggers (past vanilla's stagger and
        // ammo checks), only on the attacking client, and after the swing's own hits. Every two-handed axe and sword
        // attack is a melee swing, so this sees all of them.
        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        [HarmonyPostfix]
        private static void Attack_DoMeleeAttack_Postfix(Attack __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_character != player || player.m_currentAttack != __instance ||
                (LastNovaAttack.TryGetTarget(out Attack last) && last == __instance))
            {
                return;
            }

            if (!player.HasActiveMagicEffect(MagicEffectType.Firebrand, out float value) || value <= 0f)
            {
                return;
            }

            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (!IsFirebrandWeapon(weapon))
            {
                return;
            }

            float power = GetPower(__instance, weapon);
            if (power <= 0f)
            {
                return;
            }

            LastNovaAttack.SetTarget(__instance);
            Erupt(player, weapon, value / 100f * power);
        }

        // 1 for the third swing of the primary combo, SecondaryPower for the secondary attack, 0 for anything else.
        private static float GetPower(Attack attack, ItemDrop.ItemData weapon)
        {
            string animation = attack.m_attackAnimation;
            if (string.IsNullOrEmpty(animation))
            {
                return 0f;
            }

            ItemDrop.ItemData.SharedData shared = weapon.m_shared;
            if (shared.m_attack != null && animation == shared.m_attack.m_attackAnimation)
            {
                return attack.m_attackChainLevels > ThirdSwing && attack.m_currentAttackCainLevel == ThirdSwing ? 1f : 0f;
            }

            if (shared.m_secondaryAttack != null && animation == shared.m_secondaryAttack.m_attackAnimation)
            {
                return SecondaryPower;
            }

            return 0f;
        }

        private static void Erupt(Player player, ItemDrop.ItemData weapon, float damageFraction)
        {
            float fire = damageFraction * weapon.GetDamage().EpicLootGetTotalDamage();
            if (fire <= 0f)
            {
                return;
            }

            Transform playerTransform = player.transform;
            Vector3 position = playerTransform.position + Vector3.up * NovaHeight + playerTransform.forward * NovaForward;
            Quaternion rotation = playerTransform.rotation;

            GameObject fxPrefab = FindPrefab(NovaFxPrefab);
            if (fxPrefab != null)
            {
                Object.Instantiate(fxPrefab, position, rotation);
            }

            GameObject aoePrefab = FindPrefab(NovaAoePrefab);
            if (aoePrefab == null)
            {
                return;
            }

            GameObject spawned = Object.Instantiate(aoePrefab, position, rotation);
            Aoe aoe = spawned.GetComponent<Aoe>();
            if (aoe == null)
            {
                if (MissingPrefabLogged.Add(NovaAoePrefab + ".Aoe"))
                {
                    EpicLoot.LogError($"[Firebrand] the '{NovaAoePrefab}' prefab has no Aoe; the nova deals no damage.");
                }
                ZNetScene.instance.Destroy(spawned);
                return;
            }

            // Set directly rather than through Aoe.Setup, which would add the weapon's upgrade and world-level bonuses
            // a second time: GetDamage above already carries both. The prefab's own m_hitOwner would burn the wearer.
            aoe.m_owner = player;
            aoe.m_hitOwner = false;

            // PvP: the blast hits what the wearer's own swing could. With PvP off that is enemies only; with it on,
            // friendlies as well, and vanilla's RPC_Damage then spares any player whose own PvP is off. Every player
            // shares one m_name ("Human"), so m_hitSame has to follow too, or the same-kind filter drops them all.
            bool pvp = player.IsPVPEnabled();
            aoe.m_hitFriendly = pvp;
            aoe.m_hitSame = pvp;

            // The weapon's skill: OnHit multiplies the damage by its skill factor, and a hit raises it.
            aoe.m_skill = weapon.m_shared.m_skillType;
            aoe.m_attackForce = PushForce;
            aoe.m_radius = Radius;
            aoe.m_damage = new HitData.DamageTypes { m_fire = fire };

            // Damage the effect deals on its own, not a weapon strike (see HitSource).
            HitSource.MarkBonusSource(spawned);
        }

        private static GameObject FindPrefab(string name)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && MissingPrefabLogged.Add(name))
            {
                EpicLoot.LogError($"[Firebrand] could not find the '{name}' prefab; the nova will not work as expected.");
            }
            return prefab;
        }
    }
}
