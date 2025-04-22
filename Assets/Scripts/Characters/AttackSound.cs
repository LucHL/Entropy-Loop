using UnityEngine;

public class AttackSound : MonoBehaviour  // 👈 Vérifie que MonoBehaviour est bien là
{
    public AudioSource audioSource;
    public AudioClip swordSound;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(swordSound);
            }
        }
    }
}
