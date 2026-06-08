using System;
using System.Collections.Generic;
using UnityEngine;

public class GameLogManager : MonoBehaviour
{
    public static GameLogManager Instance;

    public List<GameLogBase> logs = new List<GameLogBase>();

    public event Action<GameLogBase> Add_to_log;

    [Header("Settings")]
    public int maxLogs = 50;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddLog(string msg, Sprite icon = null)
    {
        GameLogBase new_log = new GameLogBase(msg, icon);

        logs.Insert(0, new_log);

        if (logs.Count > maxLogs)
            logs.RemoveAt(logs.Count - 1);

        Add_to_log?.Invoke(new_log);
    }
}