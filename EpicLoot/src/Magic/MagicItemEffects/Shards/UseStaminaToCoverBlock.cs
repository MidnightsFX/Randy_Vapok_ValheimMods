using System;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class UseStaminaToCoverBlock 
    {
        public static void UseMoreStaminaOnBlock(Humanoid __instance, HitData hit) 
        {
            var player = Player.m_localPlayer;

            float magicEffectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.UseMoreStaminaOnBlock, .01f);
            float stamBlockPool = player.GetMaxStamina() * magicEffectValue;

            float blockDamageTaken = hit.GetTotalDamage();

            float reduction = Math.Min(blockDamageTaken, magicEffectValue);

            if (player.GetStamina() < reduction) return;

            player.UseStamina(reduction); // use stam up to % of max stamina
            hit.ApplyModifier(Math.Max(0f, blockDamageTaken - reduction) / blockDamageTaken);
        }
    }
}
