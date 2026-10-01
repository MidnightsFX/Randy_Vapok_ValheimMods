// Only the Valheim members these two production handlers use. The common Unity,
// Player, StatusEffect and Harmony stand-ins come from Valheim.Testing.Doubles.
public sealed partial class Player
{
    public int ComfortLevel;
    public float ShardBonus;
    public int GetComfortLevel() => ComfortLevel;
    public float GetTotalActiveMagicEffectValue(string effect) =>
        effect == MagicEffectType.GainMaxCarryWeightFromRested ? ShardBonus : 0f;
}

public partial class StatusEffect
{
    public Player? m_character;
    public virtual void OnDestroy() { }
}

public sealed class SE_Rested : StatusEffect { }

public sealed class SEMan
{
    public static readonly int s_statusEffectRested = 1;
    public SE_Rested? ActiveRested;
    public bool HaveStatusEffect(int hash) => hash == s_statusEffectRested && ActiveRested != null;
    public StatusEffect? GetStatusEffect(int hash) =>
        hash == s_statusEffectRested ? ActiveRested : null;
}

public static class MagicEffectType
{
    public static readonly string GainMaxCarryWeightFromRested = nameof(GainMaxCarryWeightFromRested);
}
