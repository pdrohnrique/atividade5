public sealed class BossMediumRainState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.Rain;
    protected override int Phase => 1;
}
