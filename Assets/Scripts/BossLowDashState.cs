public sealed class BossLowDashState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.DashAndAim;
    protected override int Phase => 2;
}
