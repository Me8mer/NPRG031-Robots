using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class BuildSerializer
{
    // Editor saves to <repo-root>/RobotBuilds, builds save to persistentDataPath
    public static string SavesDirectory
    {
        get
        {
#if UNITY_EDITOR
            string assets = Application.dataPath;                       // <repo-root>/<Project>/Assets
            var projectDir = Directory.GetParent(assets);               // <repo-root>/<Project>
            var repoRoot = projectDir?.Parent ?? projectDir;          // <repo-root>
            return Path.Combine(repoRoot.FullName, "RobotBuilds");
#else
            return Path.Combine(Application.persistentDataPath, "RobotBuilds");
#endif
        }
    }

    public struct SaveResult
    {
        public string path;
        public string fileName;
        public string originalFileName;
        public bool renamed;
    }

    public static SaveResult SaveUnique(RobotBuildData data)
    {
        if (!Directory.Exists(SavesDirectory))
            Directory.CreateDirectory(SavesDirectory);

        string baseSafe = MakeSafeFileName(data.robotName);
        string requested = baseSafe + ".json";
        string unique = GetUniqueFileName(SavesDirectory, requested);

        data.savedAtIso = DateTime.UtcNow.ToString("o");

        string fullPath = Path.Combine(SavesDirectory, unique);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(fullPath, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        return new SaveResult
        {
            path = fullPath,
            fileName = unique,
            originalFileName = requested,
            renamed = !string.Equals(unique, requested, StringComparison.OrdinalIgnoreCase)
        };
    }

    // Overwrite an existing file exactly. Use in edit mode.
    public static string SaveExact(RobotBuildData data, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) throw new ArgumentException("fullPath is empty");
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        data.savedAtIso = DateTime.UtcNow.ToString("o");

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(fullPath, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        return fullPath;
    }

    // Return all build file paths sorted by last write time desc
    public static List<string> ListBuildFiles()
    {
        if (!Directory.Exists(SavesDirectory)) return new List<string>();
        return Directory.EnumerateFiles(SavesDirectory, "*.json", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToList();
    }

    public static RobotBuildData Load(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return null;
        try
        {
            string json = File.ReadAllText(fullPath);
            var data = JsonUtility.FromJson<RobotBuildData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load build: {fullPath}\n{ex}");
            return null;
        }
    }

    // ---------- helpers ----------
    private static string GetUniqueFileName(string dir, string fileName)
    {
        var existing = new HashSet<string>(
            Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                     .Select(Path.GetFileName),
            StringComparer.OrdinalIgnoreCase
        );
        if (!existing.Contains(fileName)) return fileName;

        string name = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        for (int i = 2; i < 10000; i++)
        {
            string candidate = $"{name} ({i}){ext}";
            if (!existing.Contains(candidate)) return candidate;
        }
        return $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}{ext}";
    }

    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "Unnamed" : cleaned;
    }
}
