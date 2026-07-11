using UnityEngine;
using UnityEngine.Events;

public class BeatManager : MonoBehaviour
{
    [SerializeField] private float bpm;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Interval[] intervals;

    private void Update()
    {
        foreach (Interval i in intervals)
        {
            float sampledTime = audioSource.timeSamples / (audioSource.clip.frequency * i.GetIntervalLengh(bpm));
            i.CheckForNewInterval(sampledTime);
        }
    }
}

[System.Serializable]
public class Interval
{
    [SerializeField] private float steps;
    [SerializeField] private GameObject gameObject;
    private int lastInterval;

    public float GetIntervalLengh(float bpm)
    {
        return 60f / (bpm * steps);
    }

    public void CheckForNewInterval(float interval)
    {
        if (Mathf.FloorToInt(interval) != lastInterval)
        {
            lastInterval = Mathf.FloorToInt(interval);
            gameObject.GetComponent<PulseToTheBeat>().Pulse();
        }
    }
}
