public sealed class BossLowFanState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.DenseFan;
    protected override int Phase => 2;
}
