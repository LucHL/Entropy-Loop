using System;
using System.IO;
using UnityEngine;

public static class BugTracker
{
    private static readonly object _lock = new();
    private static string logPath;
    private static bool initialized = false;

    public enum Level
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
        Write(Level.Info, "BugTracker initialized.");
    }

    public static void Write(Level level, string message)
    {
        if (!initialized)
            Initialize();

        lock (_lock)
        {
            string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}]: {message}\n";
            File.AppendAllText(logPath, log);
        }
    }

    public static void NewEntity(string entityName)
    {
        string msg = "New entity '" + entityName + "' created.";
        Write(Level.Info, msg);
    }

    public static void Error(string msg) => Write(Level.Error, msg);
    public static void Warning(string msg) => Write(Level.Warning, msg);
    public static void Info(string msg) => Write(Level.Info, msg);
    public static void Critical(string msg) => Write(Level.Critical, msg);
}
