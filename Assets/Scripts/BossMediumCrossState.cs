public sealed class BossMediumCrossState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.CrossSpread;
    protected override int Phase => 1;
}
