using EpicLoot.Config;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string EitrInfusion = nameof(EitrInfusion);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // EitrInfusion (4-piece Einherjar set bonus, NoRoll). A hotkey pours all of the wearer's eitr into the weapon: every
    // EitrPerSecond eitr becomes a second of infusion, and MinEitr is needed to start one. While infused, each weapon hit
    // adds the effect value as a percent of the weapon's highest damage type, as that same type, and natural eitr regen
    // is held off. Meads, Eitr Weaving parries and Eitr Leech still add eitr, and pressing the key again pours what has
    // come back into more time. The element is locked on the first press: Lightning, Frost or Burning on the wearer, or
    // a nearby fire, turns the bonus into that element for the whole infusion. Poison never counts.
    //
    // The timer and the regen hold run whatever the wearer does; the damage needs the 4-piece, i.e. the set sword in
    // hand. Spellsword's eitr cost is waived while infused (Spellsword checks IsInfused), since the eitr is already
    // spent.
    //
    // The bonus is added in ModifyDamage's GetDamage postfix, among the other added damage and before the multipliers,
    // so Modify Elemental/Physical Damage and the all-damage effects scale it, the weapon tooltip shows it, and it
    // reaches every weapon hit (EpicLoot's own bonus hits never read GetDamage). It is computed on the attacker and
    // travels inside the hit.
    public static class EitrInfusion
    {
        public enum Element
        {
            None,
            Fire,
            Frost,
            Lightning
        }

        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMinEitr = 20f;
        public const float DefaultEitrPerSecond = 4f;
        public const float DefaultMaxSeconds = 0f;      // 0 = no cap

        private const string MinEitrKey = "MinEitr";
        private const string EitrPerSecondKey = "EitrPerSecond";
        private const string MaxSecondsKey = "MaxSeconds";

        // Player-editable, so each is kept in a sane range; EitrPerSecond divides.
        private static float MinEitr => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, MinEitrKey, DefaultMinEitr, 1f, 1000f);
        private static float EitrPerSecond => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, EitrPerSecondKey,
            DefaultEitrPerSecond, 0.1f, 100f);
        private static float MaxSeconds => EffectConfig.GetClamped(MagicEffectType.EitrInfusion, MaxSecondsKey,
            DefaultMaxSeconds, 0f, 3600f);

        // The world's eitr cost modifier applies to the infusion as it does to spells.
        private static float EitrRate => Game.m_eitrRate > 0f ? Game.m_eitrRate : 1f;

        private const float MinTopUp = 0.5f;            // less eitr than this is not worth a press

        private const string IndicatorName = "EL_EitrInfusion";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private const string IconItem = "Eitr";
        private const string FallbackIconItem = "MeadEitrMinor";
        private const string TopUpSfx = "sfx_Potion_eitr_minor";

        private static StatusEffect _indicator;
        private static bool _iconMissingLogged;
        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();

        // Tooltip: "{0}% of the weapon's highest damage ... at least {1} Eitr ... a second for every {2} Eitr ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.EitrInfusion, value => new object[]
            {
                value, MinEitr, EitrPerSecond
            });
        }

        // "Eitr Infusion [T]" in the combat key hints, while a press would do something.
        private static readonly SetAbilityKeyHint InfuseHint = new SetAbilityKeyHint("EL_EitrInfusion",
            "$mod_epicloot_eitrinfusion_hint", () => CanShowHint(Player.m_localPlayer),
            () => ELConfig.EitrInfusionKey, () => ELConfig.EitrInfusionGamepadButton);

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static bool _hasSet;
        private static float _infusedUntil;
        private static Element _element;

        public static bool IsInfused => _owner != null && _owner == Player.m_localPlayer && Time.time < _infusedUntil;

        public static float SecondsLeft => IsInfused ? _infusedUntil - Time.time : 0f;

        public static Element LockedElement => _element;

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _hasSet = false;
            _infusedUntil = 0f;
            _element = Element.None;
        }

        private static bool CanShowHint(Player player)
        {
            if (player == null || player != _owner || !_hasSet || player.IsDead())
            {
                return false;
            }

            return IsInfused ? player.GetEitr() >= MinTopUp : player.GetEitr() >= MinEitr;
        }

        private static KeyCode Bound(BepInEx.Configuration.ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons (T is OpenEmote,
        // read by HandleRadialInput). With the full set worn the key belongs to the infusion.
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            _hasSet = player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float _);

            var key = Bound(ELConfig.EitrInfusionKey);
            var button = Bound(ELConfig.EitrInfusionGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = KeyDown(button);
            if (!keyDown && !buttonDown || !_hasSet || !player.TakeInput() || Hud.InRadial() || player.IsDead())
            {
                return;
            }

            if (keyDown)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
            }

            TryInfuse(player);
        }

        // Keys whose vanilla action is read in the fixed step (attack, block, jump -- mostly a concern for the gamepad
        // button) have already been read by the time Player.Update runs, so they are held down here too.
        private static void OnLocalPlayerFixedUpdate(Player player)
        {
            var key = Bound(ELConfig.EitrInfusionKey);
            var button = Bound(ELConfig.EitrInfusionGamepadButton);
            bool keyHeld = KeyHeld(key);
            bool buttonHeld = KeyHeld(button);
            if (!keyHeld && !buttonHeld || !_hasSet || player != _owner || !player.TakeInput())
            {
                return;
            }

            if (keyHeld)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: false);
            }
            if (buttonHeld)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: false);
            }
        }

        private static void TryInfuse(Player player)
        {
            if (!player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float _))
            {
                return;
            }

            bool infused = IsInfused;
            float eitr = player.GetEitr();
            if (!infused && eitr < MinEitr || infused && eitr < MinTopUp)
            {
                Hud.instance?.EitrBarEmptyFlash();
                if (!infused)
                {
                    player.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize("$mod_epicloot_eitrinfusion_noeitr", MinEitr.ToString("0")));
                }
                return;
            }

            float now = Time.time;
            if (!infused)
            {
                _element = LockElement(player);
                _infusedUntil = now;
            }

            float perSecond = EitrPerSecond * EitrRate;
            float seconds = eitr / perSecond;
            float cap = MaxSeconds;
            if (cap > 0f)
            {
                // Only what fits under the cap is poured; the rest stays in the pool.
                seconds = Mathf.Clamp(now + cap - _infusedUntil, 0f, seconds);
                if (infused && seconds <= 0f)
                {
                    return;     // already full
                }
            }
            _infusedUntil += seconds;

            // Taken from m_eitr directly, as Frostwalker and the Eitr Barrier do: UseEitr's EpicLoot patches would treat
            // the pour as a spell cast (HealthOnEitrUse, EitrUseGivesAdrenaline).
            player.m_eitr = Mathf.Max(0f, eitr - seconds * perSecond);
            PauseEitrRegen(player);

            if (infused)
            {
                SpawnNetworkedFx(TopUpSfx, player.transform.position, player.transform.rotation);
            }
            // Also on a top-up, in case something cleared the indicator while the infusion ran on.
            ShowIndicator(player);

            EpicLoot.Log($"[EitrInfusion] {(infused ? "Topped up" : "Started")} with {seconds * perSecond:0.#} eitr: +{seconds:0.#} s " +
                $"({SecondsLeft:0.#} s left), element {_element}.");
        }

        // The rare, deliberate statuses outrank the ambient campfire; Poison never counts.
        private static Element LockElement(Player player)
        {
            var seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectLightning))
            {
                return Element.Lightning;
            }
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectFrost))
            {
                return Element.Frost;
            }
            if (seMan.HaveStatusEffect(SEMan.s_statusEffectBurning) || seMan.HaveStatusEffect(SEMan.s_statusEffectCampFire))
            {
                return Element.Fire;
            }
            return Element.None;
        }

        // Holds natural regen off the way a spell cast does (RPC_UseEitr restarts the same timer), without spending any.
        private static void PauseEitrRegen(Player player)
        {
            player.m_eitrRegenTimer = Mathf.Max(player.m_eitrRegenTimer, player.m_eitrRegenDelay);
        }

        // Called from ModifyDamage.ApplyMagicDamageModifiers, after the added damage and before the multipliers.
        public static void AddInfusedDamage(Player player, ItemDrop.ItemData item, ref HitData.DamageTypes damages)
        {
            if (player == null || !IsInfused || player != _owner || item == null || item != player.GetCurrentWeapon() ||
                !player.HasActiveMagicEffect(MagicEffectType.EitrInfusion, out float value, 0.01f))
            {
                return;
            }

            // The highest of the damage types a hit can carry (chop and pickaxe are tool damage); ties go to the
            // first in this order. Vanilla's GetMajorityDamageType never looks at blunt, so it is not used.
            float best = damages.m_blunt;
            int kind = 0;
            Consider(damages.m_slash, 1, ref best, ref kind);
            Consider(damages.m_pierce, 2, ref best, ref kind);
            Consider(damages.m_fire, 3, ref best, ref kind);
            Consider(damages.m_frost, 4, ref best, ref kind);
            Consider(damages.m_lightning, 5, ref best, ref kind);
            Consider(damages.m_poison, 6, ref best, ref kind);
            Consider(damages.m_spirit, 7, ref best, ref kind);
            if (best <= 0f)
            {
                return;
            }

            float bonus = best * value;
            switch (_element)
            {
                case Element.Fire:
                    damages.m_fire += bonus;
                    return;
                case Element.Frost:
                    damages.m_frost += bonus;
                    return;
                case Element.Lightning:
                    damages.m_lightning += bonus;
                    return;
            }

            switch (kind)
            {
                case 0: damages.m_blunt += bonus; break;
                case 1: damages.m_slash += bonus; break;
                case 2: damages.m_pierce += bonus; break;
                case 3: damages.m_fire += bonus; break;
                case 4: damages.m_frost += bonus; break;
                case 5: damages.m_lightning += bonus; break;
                case 6: damages.m_poison += bonus; break;
                case 7: damages.m_spirit += bonus; break;
            }
        }

        private static void Consider(float amount, int index, ref float best, ref int kind)
        {
            if (amount > best)
            {
                best = amount;
                kind = index;
            }
        }

        // AddStatusEffect clones the prototype; the copy's name carries the locked element for the HUD.
        private static void ShowIndicator(Player player)
        {
            SEMan seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(IndicatorHash))
            {
                return;
            }

            StatusEffect indicator = GetOrCreateIndicator();
            if (indicator == null)
            {
                return;
            }

            StatusEffect added = seMan.AddStatusEffect(indicator);
            if (added != null)
            {
                added.m_name = IndicatorToken(_element);
            }
        }

        private static string IndicatorToken(Element element)
        {
            switch (element)
            {
                case Element.Fire: return "$mod_epicloot_se_eitrinfusion_fire";
                case Element.Frost: return "$mod_epicloot_se_eitrinfusion_frost";
                case Element.Lightning: return "$mod_epicloot_se_eitrinfusion_lightning";
                default: return "$mod_epicloot_se_eitrinfusion";
            }
        }

        // A null icon would render as an invisible HUD entry, so a missing icon logs once and shows nothing. The eitr
        // mead's own start effects (attached glow and sound, both networked) play on the wearer when it is added, and
        // vanilla removes them when it ends.
        private static StatusEffect GetOrCreateIndicator()
        {
            if (_indicator != null)
            {
                return _indicator;
            }

            ObjectDB objectDB = ObjectDB.instance;
            Sprite icon = objectDB?.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon() ??
                objectDB?.GetItemPrefab(FallbackIconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[EitrInfusion] could not find the '{IconItem}' or '{FallbackIconItem}' icon; the HUD indicator will not display.");
                }
                return null;
            }

            SE_EitrInfusion se = ScriptableObject.CreateInstance<SE_EitrInfusion>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_se_eitrinfusion";
            se.m_icon = icon;
            se.m_ttl = 0f;      // never expires on its own; IsDone removes it

            var startEffects = new List<EffectList.EffectData>();
            AddEffect(startEffects, "vfx_Potion_eitr_minor", attach: true);
            AddEffect(startEffects, "sfx_Potion_eitr_minor", attach: false);
            if (startEffects.Count > 0)
            {
                se.m_startEffects = new EffectList { m_effectPrefabs = startEffects.ToArray() };
            }

            _indicator = se;
            return _indicator;
        }

        private static void AddEffect(List<EffectList.EffectData> effects, string name, bool attach)
        {
            var prefab = FindFxPrefab(name);
            if (prefab != null)
            {
                effects.Add(new EffectList.EffectData { m_prefab = prefab, m_enabled = true, m_attach = attach });
            }
        }

        private static void SpawnNetworkedFx(string name, Vector3 position, Quaternion rotation)
        {
            var prefab = FindFxPrefab(name);
            if (prefab != null)
            {
                Object.Instantiate(prefab, position, rotation);
            }
        }

        private static GameObject FindFxPrefab(string name)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && MissingFxLogged.Add(name))
            {
                EpicLoot.LogWarning($"[EitrInfusion] could not find the '{name}' prefab; playing without it.");
            }
            return prefab;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    OnLocalPlayerUpdate(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
        private static class PlayerController_FixedUpdate_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(PlayerController __instance)
            {
                var player = __instance.m_character;
                if (player != null && player == Player.m_localPlayer)
                {
                    OnLocalPlayerFixedUpdate(player);
                }
            }
        }

        // Just before vanilla's regen step, every update while infused.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
        private static class Player_UpdateStats_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (IsInfused && ReferenceEquals(__instance, _owner))
                {
                    PauseEitrRegen(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
        private static class KeyHints_UpdateHints_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHints __instance) => InfuseHint.Refresh(__instance);
        }
    }

    // HUD entry while infused: the seconds left as its icon text, and the locked element in its name.
    public class SE_EitrInfusion : StatusEffect
    {
        public override string GetIconText()
        {
            return StatusEffect.GetTimeString(EitrInfusion.SecondsLeft);
        }

        public override bool IsDone()
        {
            return !EitrInfusion.IsInfused;
        }
    }
}
