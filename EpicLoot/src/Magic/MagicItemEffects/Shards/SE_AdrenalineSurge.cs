using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // "Adrenaline Surge" -- the timed health-regen buff granted when the local player's adrenaline fills
    // (see AdrenalineIncreasesHealthRegen). RegenBonus is a fraction (e.g. 0.40 at Mythic) stamped on the
    // live instance by that effect, so the buff always reflects the rarity of the shard that granted it and
    // a re-proc mid-buff can raise or lower it.
    //
    // ModifyHealthRegen is re-queried every tick (SEMan.ModifyHealthRegen), so the bonus tracks the live
    // value. Vanilla adds (multiplier - 1) when a multiplier is > 1 (SE_Stats.ModifyHealthRegen); here we add
    // the bonus directly to the base multiplier of 1 the Player seeds each tick, giving +RegenBonus.
    //
    // The buff does not stack: AdrenalineIncreasesHealthRegen restamps and refreshes the single instance
    // rather than adding a second one, so the standard remaining-duration icon text is all it needs.
    public class SE_AdrenalineSurge : StatusEffect
    {
        public float RegenBonus;

        public override void ModifyHealthRegen(ref float regenMultiplier) => regenMultiplier += RegenBonus;

        // Returned with the label left as a token (the callers localize), matching SE_QueenEverflow.
        public override string GetTooltipString()
        {
            return $"$se_healthregen: <color=orange>+{Mathf.RoundToInt(RegenBonus * 100f)}%</color>";
        }
    }
}
