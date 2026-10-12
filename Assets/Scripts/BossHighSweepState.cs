public sealed class BossHighSweepState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.Sweep;
    protected override int Phase => 0;
}
