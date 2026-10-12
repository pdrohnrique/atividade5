using UnityEngine;

public enum BossAttackPattern
{
    AimedSingle, ThreeWay, Sweep,
    AimedBurst, CrossSpread, Rain,
    DashAndAim, DenseFan, Spiral
}

/// <summary>Animator state callback shared by the nine concrete boss patterns.</summary>
public abstract class BossBaseState : StateMachineBehaviour
{
    [SerializeField, Min(0.5f)] private float patternDuration = 4f;
    protected abstract BossAttackPattern Pattern { get; }
    protected abstract int Phase { get; }
    private float enteredAt;
    private bool advanceRequested;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        enteredAt = Time.time;
        advanceRequested = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        BossController boss = animator.GetComponent<BossController>();
        float elapsed = Time.time - enteredAt;
        if (boss != null)
        {
            boss.RunPattern(Pattern, elapsed);
            if (!advanceRequested && elapsed >= patternDuration)
            {
                advanceRequested = true;
                boss.AdvancePattern(Phase);
            }
        }
    }
}
