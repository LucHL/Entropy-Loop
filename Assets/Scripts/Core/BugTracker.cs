using System;
using System.IO;
using UnityEngine;

public static class BugTracker
{
    private static readonly object _lock = new();
    private static string logPath;
    private static bool initialized = false;

    public enum IssueLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public static void Initialize()
    {
        if (initialized)
            return;

        string folder = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        logPath = Path.Combine(folder, "bugtracker.log");
        initialized = true;
        Report(IssueLevel.Info, "BugTracker initialized.");
        Debug.Log("BugTracker initialized ! Log saved to: " + logPath);
    }

    public static void Report(IssueLevel level, string message)
    {
        if (!initialized)
            Initialize();

        lock (_lock)
        {
            string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}]: {message}\n";
            File.AppendAllText(logPath, log);
        }
    }

    public static void Error(string msg) => Report(IssueLevel.Error, msg);
    public static void Warning(string msg) => Report(IssueLevel.Warning, msg);
    public static void Info(string msg) => Report(IssueLevel.Info, msg);
    public static void Critical(string msg) => Report(IssueLevel.Critical, msg);
}
