using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MarkItDownGUI.Services;

/// <summary>
/// Manages Python runtime discovery and setup for the bridge.
/// Search order:
///   1. App-local python/ directory (bundled)
///   2. System Python (python, python3, py)
///   3. If none found, user is prompted to install Python
/// </summary>
public static class PythonSetupManager
{
    private static readonly string AppLocalPythonPath = Path.Combine(
        AppContext.BaseDirectory, "python", "python.exe");

    private static readonly string AppLocalPipPath = Path.Combine(
        AppContext.BaseDirectory, "python", "Scripts", "pip.exe");

    private static readonly string AppLocalRequirementsPath = Path.Combine(
        AppContext.BaseDirectory, "python", "requirements.txt");

    private static string? _cachedPythonPath;

    /// <summary>
    /// Resolve the Python executable path.
    /// </summary>
    public static string? GetPythonPath()
    {
        if (_cachedPythonPath is not null)
            return _cachedPythonPath;

        if (File.Exists(AppLocalPythonPath))
        {
            _cachedPythonPath = AppLocalPythonPath;
            return _cachedPythonPath;
        }

        foreach (var candidate in new[] { "python", "python3", "py" })
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    ArgumentList = { "--version" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (proc is not null)
                {
                    proc.WaitForExit(2000);
                    if (proc.ExitCode == 0)
                    {
                        _cachedPythonPath = candidate;
                        return _cachedPythonPath;
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    /// <summary>
    /// True if Python and markitdown are available and ready.
    /// </summary>
    public static bool IsEnvironmentReady()
    {
        var python = GetPythonPath();
        if (python is null) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = python,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("import markitdown; print('OK')");
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(5000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Install markitdown and dependencies into the resolved Python environment.
    /// </summary>
    public static bool InstallMarkItDown()
    {
        var python = GetPythonPath();
        if (python is null) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = python,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add("pip");
            psi.ArgumentList.Add("install");
            psi.ArgumentList.Add("--quiet");
            psi.ArgumentList.Add("markitdown[all]");

            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(120000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the MarkItDown bridge script path.
    /// </summary>
    public static string GetBridgePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var local = Path.Combine(baseDir, "markitdown_bridge.py");
        if (File.Exists(local))
            return local;

        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "markitdown_bridge.py");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return local;
    }

    /// <summary>
    /// Invalidate cache (e.g., after user installs Python).
    /// </summary>
    public static void InvalidateCache()
    {
        _cachedPythonPath = null;
    }
}
