using System;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static TickManager instance;

    [Header("Tick Settings")]
    [Tooltip("Durée d'un tick en secondes (ex: 0.5s = 2 ticks par seconde)")]
    public float tickInterval = 0.5f;

    public event Action OnTick;
    private float timer;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (GameLoopManager.instance != null && !GameLoopManager.instance.isGameRunning)
            return;

        timer += Time.deltaTime;

        if (timer >= tickInterval) {
            timer -= tickInterval;
            OnTick?.Invoke();
        }
    }
}
