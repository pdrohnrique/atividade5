public sealed class BossHighThreeWayState : BossBaseState
{
    protected override BossAttackPattern Pattern => BossAttackPattern.ThreeWay;
    protected override int Phase => 0;
}
