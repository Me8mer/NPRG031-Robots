using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles saving and loading of <see cref="RobotBuildData"/> objects to disk.
/// 
/// In Editor: saves under <repo-root>/RobotBuilds  
/// In Build: saves under <see cref="Application.persistentDataPath"/>/RobotBuilds
/// </summary>
public static class BuildSerializer
{
    /// <summary>
    /// Path where builds are saved.
    /// Editor: repo root,  
    /// Runtime: persistentDataPath.
    /// </summary>
    public static string SavesDirectory
    {
        get
        {
#if UNITY_EDITOR
            string assets = Application.dataPath;                       // <repo-root>/<Project>/Assets
            var projectDir = Directory.GetParent(assets);               // <repo-root>/<Project>
            var repoRoot = projectDir?.Parent ?? projectDir;            // <repo-root>
            return Path.Combine(repoRoot.FullName, "RobotBuilds");
#else
            return Path.Combine(Application.persistentDataPath, "RobotBuilds");
#endif
        }
    }

    /// <summary>Result of a Save operation, with metadata about uniqueness/renaming.</summary>
    public struct SaveResult
    {
        public string path;
        public string fileName;
        public string originalFileName;
        public bool renamed;
    }

    /// <summary>
    /// Saves build to a unique filename. If a file already exists, it adds (2), (3), etc.
    /// </summary>
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

    /// <summary>
    /// Overwrites an existing build file exactly.
    /// Used in edit mode when updating an existing save.
    /// </summary>
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

    /// <summary>
    /// Returns all build file paths, sorted by last modified time (newest first).
    /// </summary>
    public static List<string> ListBuildFiles()
    {
        if (!Directory.Exists(SavesDirectory)) return new List<string>();
        return Directory.EnumerateFiles(SavesDirectory, "*.json", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToList();
    }

    /// <summary>
    /// Loads a <see cref="RobotBuildData"/> from a file path.
    /// Returns null on failure.
    /// </summary>
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

    // ---------- Helpers ----------

    /// <summary>
    /// Generates a unique file name if <paramref name="fileName"/> already exists.
    /// Adds (2), (3), etc., or falls back to timestamp if too many.
    /// </summary>
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

    /// <summary>
    /// Sanitizes a string into a safe file name by replacing invalid characters with underscores.
    /// </summary>
    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "Unnamed" : cleaned;
    }
}
