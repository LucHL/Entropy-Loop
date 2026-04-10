using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    private Units units;

    void Awake()
    {
        units = GetComponentInParent<Units>();
    }

    public void DealDamage()
    {
        units.Attack();
    }

    public void DesapearAfterDeath()
    {
        units.DesapearAfterDeath();
    }
}
