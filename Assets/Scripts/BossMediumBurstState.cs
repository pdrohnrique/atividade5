public sealed class BossMediumBurstState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.AimedBurst;
    protected override int Phase => 1;
}
