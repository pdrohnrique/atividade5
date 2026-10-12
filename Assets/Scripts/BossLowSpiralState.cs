public sealed class BossLowSpiralState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.Spiral;
    protected override int Phase => 2;
}
