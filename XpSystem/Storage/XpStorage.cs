using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace XpSystem.Storage;

internal static class XpStorage
{
    private static readonly ConcurrentDictionary<string, long> Values = new(StringComparer.Ordinal);
    private static readonly object SaveLock = new();

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    private static string _filePath;

    internal static void Initialize(string directory, string fileName)
    {
        _filePath = Path.Combine(directory, fileName);
        Values.Clear();

        try
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(_filePath))
            {
                LogManager.Info($"[XpSystem] XP data file not found, a new one will be created: {_filePath}");
                return;
            }

            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            Dictionary<string, long> loaded = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            if (loaded == null)
                return;

            foreach (KeyValuePair<string, long> entry in loaded.Where(entry => !string.IsNullOrWhiteSpace(entry.Key))) Values[entry.Key] = Math.Max(0L, entry.Value);

            LogManager.Info($"[XpSystem] XP data loaded: {Values.Count} records ({_filePath}).");
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to load the XP data: {ex.Message}");
        }
    }

    internal static long Get(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0L;

        return Values.TryGetValue(userId, out long value) ? value : 0L;
    }

    internal static void Set(string userId, long value)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        Values[userId] = Math.Max(0L, value);
    }

    internal static long Add(string userId, long amount)
    {
        return string.IsNullOrWhiteSpace(userId) ? 0L : Values.AddOrUpdate(userId, Math.Max(0L, amount), (_, current) => Math.Max(0L, current + amount));
    }

    internal static IReadOnlyDictionary<string, long> GetAll()
    {
        return new Dictionary<string, long>(Values, StringComparer.Ordinal);
    }

    internal static void Save()
    {
        lock (SaveLock)
        {
            string path = _filePath;
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                Dictionary<string, long> snapshot = new(Values, StringComparer.Ordinal);
                string json = JsonSerializer.Serialize(snapshot, WriteOptions);
                string temporaryPath = path + ".tmp";

                File.WriteAllText(temporaryPath, json);

                if (File.Exists(path))
                    try
                    {
                        File.Replace(temporaryPath, path, path + ".bak");
                    }
                    catch
                    {
                        File.Copy(temporaryPath, path, true);
                        File.Delete(temporaryPath);
                    }
                else
                    File.Move(temporaryPath, path);

                LogManager.Debug($"[XpSystem] XP data saved: {snapshot.Count} records.");
            }
            catch (Exception ex)
            {
                LogManager.Error($"[XpSystem] Failed to save the XP data: {ex.Message}");
            }
        }
    }

    internal static void SaveInBackground()
    {
        Task.Run(Save);
    }

    internal static void Unload()
    {
        lock (SaveLock)
        {
            Values.Clear();
            _filePath = null;
        }
    }
}