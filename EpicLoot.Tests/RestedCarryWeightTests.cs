using System;
using System.Reflection;
using EpicLoot.MagicItemEffects.Shards;
using Xunit;

[CollectionDefinition("Rested carry weight", DisableParallelization = true)]
public sealed class RestedCarryWeightCollection { }

[Collection("Rested carry weight")]
public sealed class RestedCarryWeightTests : IDisposable
{
    private static readonly Type Patch = typeof(GainMaxCarryWeightFromRested).Assembly
        .GetType("EpicLoot.SE_Rested_Patch", throwOnError: true)!;

    public RestedCarryWeightTests() => Reset();
    public void Dispose() => Reset();

    [Fact]
    public void RestedBonusUsesComfortAtRefreshAfterLeavingShelter()
    {
        var player = NewPlayer(3);
        var effect = Rested(player);
        var seman = new SEMan { ActiveRested = effect };
        InvokePatch("UpdateTTLPostfix", effect);

        Assert.Equal(315f, CarryLimit(player, seman));
        player.ComfortLevel = 1;
        Assert.Equal(315f, CarryLimit(player, seman));

        InvokePatch("UpdateTTLPostfix", effect); // Lower-comfort refresh must not lower the stored bonus.
        Assert.Equal(315f, CarryLimit(player, seman));
        player.ComfortLevel = 4;
        InvokePatch("UpdateTTLPostfix", effect);
        Assert.Equal(320f, CarryLimit(player, seman));
    }

    [Fact]
    public void ExpiryAndEquipmentRemovalRemoveTheBonus()
    {
        var player = NewPlayer(3);
        var effect = Rested(player);
        var seman = new SEMan { ActiveRested = effect };
        InvokePatch("UpdateTTLPostfix", effect);

        player.ShardBonus = 0;
        Assert.Equal(300f, CarryLimit(player, seman));
        player.ShardBonus = 5;
        Assert.Equal(315f, CarryLimit(player, seman));

        InvokePatch("StopPrefix", effect);
        seman.ActiveRested = null;
        Assert.Equal(300f, CarryLimit(player, seman));
    }

    [Fact]
    public void AnotherCharacterCannotInheritThePreviousCharactersComfort()
    {
        var first = NewPlayer(8);
        var oldEffect = Rested(first);
        InvokePatch("UpdateTTLPostfix", oldEffect);

        // SEMan.OnDestroy calls effect.OnDestroy, not Stop. A new local player
        // may therefore appear without the StopPrefix having run on the old one.
        var next = NewPlayer(2);
        var newEffect = Rested(next);
        var seman = new SEMan { ActiveRested = newEffect };
        Assert.Equal(300f, CarryLimit(next, seman));

        InvokePatch("UpdateTTLPostfix", newEffect);
        Assert.Equal(310f, CarryLimit(next, seman));

        // Cleanup from the old character must not erase the new effect's value.
        InvokePatch("OnDestroyPrefix", oldEffect);
        Assert.Equal(310f, CarryLimit(next, seman));
    }

    [Fact]
    public void DestroyingTheTrackedEffectClearsItsComfort()
    {
        var player = NewPlayer(3);
        var effect = Rested(player);
        var seman = new SEMan { ActiveRested = effect };
        InvokePatch("UpdateTTLPostfix", effect);
        Assert.Equal(315f, CarryLimit(player, seman));

        InvokePatch("OnDestroyPrefix", effect);
        Assert.Equal(300f, CarryLimit(player, seman));
    }

    [Fact]
    public void OtherPlayersRestedUpdateDoesNotChangeTheLocalBonus()
    {
        var local = NewPlayer(3);
        var localEffect = Rested(local);
        var seman = new SEMan { ActiveRested = localEffect };
        InvokePatch("UpdateTTLPostfix", localEffect);

        var other = new Player { ComfortLevel = 12, ShardBonus = 5 };
        InvokePatch("UpdateTTLPostfix", Rested(other));
        Assert.Equal(315f, CarryLimit(local, seman));
    }

    private static Player NewPlayer(int comfort)
    {
        var player = new Player { ComfortLevel = comfort, ShardBonus = 5 };
        Player.m_localPlayer = player;
        return player;
    }

    private static SE_Rested Rested(Player player) => new() { m_character = player };

    private static float CarryLimit(Player player, SEMan seman)
    {
        var limit = 300f;
        GainMaxCarryWeightFromRested.ModifyMaxCarryWeight(player, seman, ref limit);
        return limit;
    }

    private static void InvokePatch(string method, StatusEffect effect) =>
        Patch.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { effect });

    private static void Reset()
    {
        Player.m_localPlayer = null;
        GainMaxCarryWeightFromRested.RestedEffect = null!;
        GainMaxCarryWeightFromRested.RestedComfort = 0;
    }
}
