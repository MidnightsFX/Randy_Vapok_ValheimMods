using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ChanceToStaggerOnBlock {
        public static void StaggerOnBlock(Character attacker) 
        {
            var player = Player.m_localPlayer;

            float staggerChance = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaggerOnBlock, .01f);
            float staggerRoll = Random.Range(0f, 1f);

            if (staggerChance > staggerRoll) 
            {
                attacker.Stagger(Vector3.zero); // relative to player or attacker would be intentional choice. Parries are relative
                                                // to player in vanilla so that staggered enemies face player on parry.
                                                // making this stagger in place so aoe attacks leave attackers staggering in place
            }
        }
    }
}