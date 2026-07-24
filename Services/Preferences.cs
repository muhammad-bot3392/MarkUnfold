using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarkItDownGUI.Services;

public static class Preferences
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MarkUnfold",
        "prefs.json");

    private static PrefsData? _cache;

    static Preferences()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    }

    // ── Theme ────────────────────────────────────────────────────────────────

    public static string GetTheme()
    {
        var data = Load();
        return data.Theme;
    }

    public static void SetTheme(string theme)
    {
        var data = Load();
        data.Theme = theme;
        Save(data);
    }

    // ── Output folder ────────────────────────────────────────────────────────

    public static string? GetOutputFolder()
    {
        var data = Load();
        return data.OutputFolder;
    }

    public static void SetOutputFolder(string? path)
    {
        var data = Load();
        data.OutputFolder = path;
        Save(data);
    }

    // ── Azure Document Intelligence ──────────────────────────────────────────

    public static bool GetUseDocIntel()
    {
        var data = Load();
        return data.UseDocIntel;
    }

    public static void SetUseDocIntel(bool value)
    {
        var data = Load();
        data.UseDocIntel = value;
        Save(data);
    }

    public static string? GetDocIntelEndpoint()
    {
        var data = Load();
        return data.EncryptedDocIntelEndpoint is { Length: > 0 }
            ? Decrypt(data.EncryptedDocIntelEndpoint)
            : data.DocIntelEndpoint;
    }

    public static void SetDocIntelEndpoint(string? value)
    {
        var data = Load();
        if (string.IsNullOrWhiteSpace(value))
        {
            data.EncryptedDocIntelEndpoint = null;
            data.DocIntelEndpoint = null;
        }
        else
        {
            data.EncryptedDocIntelEndpoint = Encrypt(value);
            data.DocIntelEndpoint = null; // never store plaintext
        }
        Save(data);
    }

    // ── Azure Content Understanding ──────────────────────────────────────────

    public static bool GetUseContentUnderstanding()
    {
        var data = Load();
        return data.UseContentUnderstanding;
    }

    public static void SetUseContentUnderstanding(bool value)
    {
        var data = Load();
        data.UseContentUnderstanding = value;
        Save(data);
    }

    public static string? GetCuEndpoint()
    {
        var data = Load();
        return data.EncryptedCuEndpoint is { Length: > 0 }
            ? Decrypt(data.EncryptedCuEndpoint)
            : data.CuEndpoint;
    }

    public static void SetCuEndpoint(string? value)
    {
        var data = Load();
        if (string.IsNullOrWhiteSpace(value))
        {
            data.EncryptedCuEndpoint = null;
            data.CuEndpoint = null;
        }
        else
        {
            data.EncryptedCuEndpoint = Encrypt(value);
            data.CuEndpoint = null; // never store plaintext
        }
        Save(data);
    }

    public static string? GetCuAnalyzer()
    {
        var data = Load();
        return data.CuAnalyzer;
    }

    public static void SetCuAnalyzer(string? value)
    {
        var data = Load();
        data.CuAnalyzer = value;
        Save(data);
    }

    // ── History ──────────────────────────────────────────────────────────────

    public static List<HistoryEntryData> GetHistory()
    {
        var data = Load();
        return data.History;
    }

    public static void SaveHistory(List<HistoryEntryData> entries)
    {
        var data = Load();
        data.History = entries;
        Save(data);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private static PrefsData Load()
    {
        if (_cache is not null) return _cache;
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _cache = JsonSerializer.Deserialize<PrefsData>(json) ?? new PrefsData();
                return _cache;
            }
        }
        catch { /* ignore corrupt prefs */ }
        _cache = new PrefsData();
        return _cache;
    }

    private static void Save(PrefsData data)
    {
        _cache = data;
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        File.WriteAllText(FilePath, json);
    }

    private static string Encrypt(string plaintext)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var encrypted = System.Security.Cryptography.ProtectedData.Protect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return plaintext; // fallback: store plaintext if encryption fails
        }
    }

    private static string? Decrypt(string ciphertext)
    {
        try
        {
            var bytes = Convert.FromBase64String(ciphertext);
            var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    // ── Data classes ─────────────────────────────────────────────────────────

    private class PrefsData
    {
        public string Theme { get; set; } = "dark";
        public string? OutputFolder { get; set; }
        public bool UseDocIntel { get; set; }
        public string? DocIntelEndpoint { get; set; }
        public string? EncryptedDocIntelEndpoint { get; set; }
        public bool UseContentUnderstanding { get; set; }
        public string? CuEndpoint { get; set; }
        public string? EncryptedCuEndpoint { get; set; }
        public string? CuAnalyzer { get; set; }
        public List<HistoryEntryData> History { get; set; } = new();
    }
}

public class HistoryEntryData
{
    public string? SourceFile { get; set; }
    public string? OutputFile { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ConvertedAt { get; set; }
}
