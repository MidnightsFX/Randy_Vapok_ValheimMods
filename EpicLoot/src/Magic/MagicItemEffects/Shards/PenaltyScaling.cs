using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Shared 0..1 scaling inputs for the weight / movement-penalty shards (Peach "Logistics" and Green
    // "Movement"). Both families turn a normally-undesirable stat -- a full pack, or a slow heavy loadout
    // -- into a reward, so each effect multiplies its shard value by one of these factors. Values are low
    // power / tunable; the reference constant below is the only knob for the movement-penalty curve.
    public static class PenaltyScaling
    {
        // Equipment movement penalty (as a positive fraction) treated as "fully committed" to a heavy
        // loadout, i.e. where MovementPenaltyFactor reaches 1. ~20% speed loss is roughly a full set of
        // the heaviest armor.
        public const float MovementPenaltyReference = 0.20f;

        // How loaded the player's pack is: 0 (empty) .. 1 (at or over the carry cap).
        public static float WeightFactor(Player player)
        {
            if (player == null)
            {
                return 0f;
            }

            var maxCarry = Mathf.Max(1f, player.GetMaxCarryWeight());
            return Mathf.Clamp01(player.GetInventory().GetTotalWeight() / maxCarry);
        }

        // The raw movement-speed penalty from equipped gear as a positive fraction (e.g. 0.15 == -15%
        // speed). Reads the unmodified equipment modifier (index 0) so EpicLoot's own speed bonuses on
        // GetEquipmentMovementModifier don't mask the gear's intrinsic penalty.
        public static float MovementPenalty(Player player)
        {
            return player == null ? 0f : Mathf.Max(0f, -player.GetEquipmentModifier(0));
        }

        // MovementPenalty normalized to 0..1 against MovementPenaltyReference, for effects that scale a
        // shard value by "how heavy" the loadout is.
        public static float MovementPenaltyFactor(Player player)
        {
            return Mathf.Clamp01(MovementPenalty(player) / MovementPenaltyReference);
        }
    }
}
