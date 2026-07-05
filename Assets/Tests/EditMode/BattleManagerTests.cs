using NUnit.Framework;
using KingdomWar.Game.Battle;

public class BattleManagerTests
{
    [Test]
    public void BattleStatus_Enum_Exists()
    {
        Assert.That(typeof(BattleStatus).IsEnum, Is.True);
    }

    [Test]
    public void BattleStatus_HasWaiting()
    {
        Assert.That(System.Enum.IsDefined(typeof(BattleStatus), BattleStatus.Waiting), Is.True);
    }

    [Test]
    public void BattleStatus_HasFighting()
    {
        Assert.That(System.Enum.IsDefined(typeof(BattleStatus), BattleStatus.Fighting), Is.True);
    }

    [Test]
    public void UnitState_Enum_Exists()
    {
        Assert.That(typeof(UnitState).IsEnum, Is.True);
    }

    [Test]
    public void UnitState_HasIdle()
    {
        Assert.That(System.Enum.IsDefined(typeof(UnitState), UnitState.Idle), Is.True);
    }

    [Test]
    public void UnitState_HasDead()
    {
        Assert.That(System.Enum.IsDefined(typeof(UnitState), UnitState.Dead), Is.True);
    }
}
