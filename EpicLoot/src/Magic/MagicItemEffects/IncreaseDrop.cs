using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public abstract class IncreaseDrop
{
    // Rounding slack, so ten breaks at 0.1 make a whole drop rather than 0.9999999 of one.
    private const float BankRounding = 0.001f;

    public string MagicEffect { get; set; }
    public string ZDOVar { get; set; }

    // Fractions of a drop each player has banked, by player ID. Every break adds the effect's value and each whole
    // one banked falls as a drop, so a value below 1 pays out every few breaks. This lives on the machine that owns
    // the broken object, where the drop is made: that machine does not own the attacker's ZDO and may not be theirs.
    // A player whose breaks land on two machines banks on both, so a fraction can take longer to pay out.
    private readonly Dictionary<long, float> _banked = new Dictionary<long, float>();

    public void DoPrefix(HitData hit)
    {
        if (hit != null)
        {
            Player player = Player.m_localPlayer;
            if (player != null &&
                MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, player.GetCurrentWeapon(),
                    MagicEffect, out float effectValue) &&
                player.m_nview.GetZDO().GetFloat(ZDOVar) != effectValue)
            {
                player.m_nview.GetZDO().Set(ZDOVar, effectValue);
            }
        }
    }

    public void TryDropExtraItems(Character character, DropTable dropTable, Vector3 objPosition)
    {
        if (character is Player player)
        {
            float effectValue = player.m_nview.GetZDO().GetFloat(ZDOVar);

            // An empty table is not a break that drops anything: a full log, which only splits into halves.
            if (effectValue > 0f && dropTable.m_drops.Count > 0)
            {
                int drops = BankBreak(player.GetPlayerID(), effectValue);
                if (drops > 0)
                {
                    DropExtraItems(dropTable.GetDropList(drops), objPosition);
                }
            }
        }
    }

    // Adds one break's value to the player's bank, and returns the whole drops taken out of it.
    private int BankBreak(long playerId, float effectValue)
    {
        _banked.TryGetValue(playerId, out float banked);
        banked += effectValue;
        int drops = Mathf.FloorToInt(banked + BankRounding);
        _banked[playerId] = Mathf.Max(0f, banked - drops);
        return drops;
    }

    public void DropExtraItems(List<GameObject> dropList, Vector3 objPosition)
    {
        Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
        Vector3 position = objPosition + Vector3.up + new Vector3(vector.x, 0, vector.y);
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);

        for (int i = 0; i < dropList.Count; i++)
        {
            GameObject drop = dropList[i];
            ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(drop, position, rotation));
        }
    }
}
