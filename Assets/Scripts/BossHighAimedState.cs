public sealed class BossHighAimedState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.AimedSingle;
    protected override int Phase => 0;
}
