using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Reads and writes one versioned JSON save. A backup is retained before the
/// active file is replaced, and loading falls back to that backup if needed.
/// </summary>
public static class SaveSystem
{
    private const string SaveFileName = "mistwood_save.json";

    /// <summary>
    /// In the Editor this resolves to the Unity project's Saves folder. In a
    /// player build it resolves to a Saves folder beside the executable.
    /// This deliberately avoids Application.persistentDataPath/AppData.
    /// </summary>
    public static string SaveDirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Saves"));
    public static string SaveFilePath => Path.Combine(SaveDirectoryPath, SaveFileName);
    public static string BackupFilePath => SaveFilePath + ".bak";
    private static string TemporaryFilePath => SaveFilePath + ".tmp";
    private static string LegacySaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    private static string LegacyBackupFilePath => LegacySaveFilePath + ".bak";

    public static bool HasSaveFile => File.Exists(SaveFilePath) ||
                                      File.Exists(BackupFilePath) ||
                                      File.Exists(LegacySaveFilePath) ||
                                      File.Exists(LegacyBackupFilePath);

    public static GameSaveData Load()
    {
        if (TryRead(SaveFilePath, out GameSaveData data))
        {
            data.Normalize();
            return data;
        }

        if (TryRead(BackupFilePath, out data))
        {
            Debug.LogWarning("Main save could not be loaded. The backup save was used.");
            data.Normalize();
            return data;
        }

        if (TryMigrateLegacySave(out data))
        {
            return data;
        }

        return GameSaveData.CreateDefault();
    }

    public static bool Save(GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("Cannot save: GameSaveData is null.");
            return false;
        }

        try
        {
            data.Normalize();
            data.lastSavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TemporaryFilePath, json, new UTF8Encoding(false));

            if (File.Exists(SaveFilePath))
            {
                ReplaceSaveWithTemporaryFile();
            }
            else
            {
                File.Move(TemporaryFilePath, SaveFilePath);
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to save game: " + exception.Message);
            TryDeleteTemporaryFile();
            return false;
        }
    }

    public static void DeleteAll()
    {
        TryDelete(SaveFilePath);
        TryDelete(BackupFilePath);
        TryDelete(TemporaryFilePath);
        TryDelete(LegacySaveFilePath);
        TryDelete(LegacyBackupFilePath);
    }

    private static bool TryMigrateLegacySave(out GameSaveData data)
    {
        bool loaded = TryRead(LegacySaveFilePath, out data) ||
                      TryRead(LegacyBackupFilePath, out data);
        if (!loaded) return false;

        data.Normalize();
        if (Save(data))
        {
            TryDelete(LegacySaveFilePath);
            TryDelete(LegacyBackupFilePath);
            Debug.Log("Save migrated to: " + SaveFilePath);
        }
        else
        {
            Debug.LogWarning("The old save was loaded, but could not be migrated to: " + SaveFilePath);
        }

        return true;
    }

    private static bool TryRead(string path, out GameSaveData data)
    {
        data = null;
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return false;
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load save '" + path + "': " + exception.Message);
            data = null;
            return false;
        }
    }

    private static void ReplaceSaveWithTemporaryFile()
    {
        try
        {
            File.Replace(TemporaryFilePath, SaveFilePath, BackupFilePath);
        }
        catch (Exception)
        {
            // File.Replace is unavailable on some Unity targets. Keep the same
            // backup behavior while using broadly supported file operations.
            File.Copy(SaveFilePath, BackupFilePath, true);
            File.Delete(SaveFilePath);
            File.Move(TemporaryFilePath, SaveFilePath);
        }
    }

    private static void TryDeleteTemporaryFile()
    {
        TryDelete(TemporaryFilePath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to delete save file '" + path + "': " + exception.Message);
        }
    }
}
