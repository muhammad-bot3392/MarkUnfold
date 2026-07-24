using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarkItDownGUI.Models;
using MarkItDownGUI.Services;

namespace MarkItDownGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MarkItDownService _service = new();

    // ── Navigation ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _selectedTab; // 0 = Convert, 1 = History, 2 = Settings

    // ── File list (Convert tab) ─────────────────────────────────────────────

    public ObservableCollection<FileItem> Files { get; } = new();

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private int _totalFiles;

    private int _completedFiles;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _youtubeUrl = "";

    [ObservableProperty]
    private string _outputFolder = "";

    [ObservableProperty]
    private string _selectedMarkdown = "";

    [ObservableProperty]
    private FileItem? _selectedFile;

    // ── Settings ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _useDocIntel;

    [ObservableProperty]
    private string _docIntelEndpoint = "";

    [ObservableProperty]
    private bool _useContentUnderstanding;

    [ObservableProperty]
    private string _cuEndpoint = "";

    [ObservableProperty]
    private string _cuAnalyzer = "";

    // ── History ─────────────────────────────────────────────────────────────

    public ObservableCollection<ConversionHistoryEntry> History { get; } = new();

    // ── Log / Console ───────────────────────────────────────────────────────

    public ObservableCollection<string> LogLines { get; } = new();

    // ── Supported file types info ───────────────────────────────────────────

    public string SupportedTypes => string.Join(", ", new[]
    {
        "PDF", "DOCX", "PPTX", "XLSX", "XLS",
        "Images (JPG, PNG, GIF, BMP, WebP)",
        "Audio (MP3, WAV, M4A, FLAC)",
        "HTML", "CSV", "JSON", "XML",
        "ZIP", "EPUB", "MSG", "TXT",
        "YouTube URLs"
    });

    // ── Constructor ─────────────────────────────────────────────────────────

    public MainViewModel()
    {
        // Load saved preferences
        var savedFolder = Preferences.GetOutputFolder();
        OutputFolder = savedFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
             "MarkUnfold Converted");

        var savedTheme = Preferences.GetTheme();
        IsDarkTheme = savedTheme == "dark";

        UseDocIntel = Preferences.GetUseDocIntel();
        DocIntelEndpoint = Preferences.GetDocIntelEndpoint() ?? "";
        UseContentUnderstanding = Preferences.GetUseContentUnderstanding();
        CuEndpoint = Preferences.GetCuEndpoint() ?? "";
        CuAnalyzer = Preferences.GetCuAnalyzer() ?? "";

        // Load saved history
        var savedHistory = Preferences.GetHistory();
        foreach (var entry in savedHistory)
        {
            History.Add(new ConversionHistoryEntry
            {
                SourceFile = entry.SourceFile ?? "",
                OutputFile = entry.OutputFile ?? "",
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage,
                ConvertedAt = entry.ConvertedAt,
            });
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        Preferences.SetTheme(value ? "dark" : "light");
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = value
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        }
    }

    partial void OnSelectedFileChanged(FileItem? value)
    {
        SelectedMarkdown = value?.MarkdownContent ?? "";
    }

    partial void OnSelectedMarkdownChanged(string value)
    {
    }

    partial void OnOutputFolderChanged(string value)
    {
        Preferences.SetOutputFolder(value);
    }

    partial void OnUseDocIntelChanged(bool value)
    {
        Preferences.SetUseDocIntel(value);
    }

    partial void OnDocIntelEndpointChanged(string value)
    {
        Preferences.SetDocIntelEndpoint(value);
    }

    partial void OnUseContentUnderstandingChanged(bool value)
    {
        Preferences.SetUseContentUnderstanding(value);
    }

    partial void OnCuEndpointChanged(string value)
    {
        Preferences.SetCuEndpoint(value);
    }

    partial void OnCuAnalyzerChanged(string value)
    {
        Preferences.SetCuAnalyzer(value);
    }

    [RelayCommand]
    private void SelectTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var idx))
            SelectedTab = idx;
    }

    // ── Commands ────────────────────────────────────────────────────────────

    public void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!Files.Any(f => f.FilePath == path))
            {
                Files.Add(new FileItem(path));
            }
        }
        TotalFiles = Files.Count;
    }

    [RelayCommand]
    private void SelectFile(FileItem item)
    {
        SelectedFile = item;
    }

    [RelayCommand]
    private void RemoveFile(FileItem item)
    {
        Files.Remove(item);
        TotalFiles = Files.Count;
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        TotalFiles = 0;
        _completedFiles = 0;
        ProgressPercent = 0;
        LogLines.Clear();
        SelectedFile = null;
        SelectedMarkdown = "";
    }

    [RelayCommand]
    private async Task BrowseFiles()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files to convert",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("All Supported Documents")
                {
                    Patterns = new[]
                    {
                        "*.pdf", "*.docx", "*.pptx", "*.xlsx", "*.xls",
                        "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp",
                        "*.mp3", "*.wav", "*.m4a", "*.flac",
                        "*.html", "*.htm", "*.csv", "*.json", "*.xml",
                        "*.zip", "*.epub", "*.msg", "*.txt", "*.md",
                    }
                },
                FilePickerFileTypes.All,
            }
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is not null)
            {
                AddFiles([path]);
            }
        }
    }

    [RelayCommand]
    private void AddYouTubeUrl()
    {
        var url = YoutubeUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        if (!Files.Any(f => f.FilePath == url))
        {
            Files.Add(new FileItem(url));
        }
        YoutubeUrl = "";
        TotalFiles = Files.Count;
    }

    [RelayCommand]
    private async Task BrowseOutputFolder()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output folder",
        });

        if (folder.Count > 0)
        {
            var path = folder[0].TryGetLocalPath();
            if (path is not null)
                OutputFolder = path;
        }
    }

    [RelayCommand]
    private async Task ConvertAll()
    {
        if (IsConverting || Files.Count == 0) return;

        IsConverting = true;
        _completedFiles = 0;
        ProgressPercent = 0;
        LogLines.Clear();

        try
        {
            await ProcessConversions();
        }
        finally
        {
            IsConverting = false;
        }
    }

    private async Task ProcessConversions()
    {
        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(OutputFolder);
        }
        catch (Exception ex)
        {
            LogLines.Add($"  ✕ Invalid output folder: {ex.Message}");
            return;
        }

        var options = new ConversionOptions
        {
            UseDocIntel = UseDocIntel,
            DocIntelEndpoint = DocIntelEndpoint,
            UseContentUnderstanding = UseContentUnderstanding,
            CUEndpoint = CuEndpoint,
            CUAnalyzer = CuAnalyzer,
        };

        // Process each file sequentially
        foreach (var file in Files.ToList())
        {
            file.Status = FileStatus.Converting;
            LogLines.Add($"Converting: {file.FileName}");

            try
            {
                var result = await _service.ConvertFileAsync(file.FilePath, options);

                if (result.Success)
                {
                    var markdown = result.Markdown ?? "";
                    file.MarkdownContent = markdown;

                    var baseName = !string.IsNullOrWhiteSpace(result.Title)
                        ? SanitizeFileName(result.Title)
                        : SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                    if (string.IsNullOrWhiteSpace(baseName))
                        baseName = "youtube_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var outputName = baseName + ".md";
                    var outputPath = Path.Combine(OutputFolder, outputName);

                    var counter = 1;
                    while (File.Exists(outputPath))
                    {
                        outputPath = Path.Combine(
                            OutputFolder,
                            $"{baseName}_{counter++}.md");
                    }

                    await File.WriteAllTextAsync(outputPath, markdown);
                    file.OutputPath = outputPath;
                    file.Status = FileStatus.Done;
                    SelectedFile = file;

                    LogLines.Add($"  ✓ Saved: {outputPath}");
                }
                else
                {
                    var err = result.Error ?? "Unknown error";
                    file.Status = FileStatus.Failed;
                    file.ErrorMessage = err;
                    LogLines.Add($"  ✕ Failed: {err}");
                }
            }
            catch (Exception ex)
            {
                file.Status = FileStatus.Failed;
                file.ErrorMessage = ex.Message;
                LogLines.Add($"  ✕ Exception: {ex.Message}");
            }

            _completedFiles++;
            ProgressPercent = TotalFiles > 0 ? (double)_completedFiles / TotalFiles * 100 : 0;
        }

        // Add to history
        foreach (var file in Files)
        {
            if (file.Status == FileStatus.Done)
            {
                History.Insert(0, new ConversionHistoryEntry
                {
                    SourceFile = file.FilePath,
                    OutputFile = file.OutputPath,
                    Success = true,
                    ConvertedAt = DateTime.Now,
                });
            }
            else if (file.Status == FileStatus.Failed)
            {
                History.Insert(0, new ConversionHistoryEntry
                {
                    SourceFile = file.FilePath,
                    Success = false,
                    ErrorMessage = file.ErrorMessage,
                    ConvertedAt = DateTime.Now,
                });
            }
        }

        // Persist history
        var historyData = new System.Collections.Generic.List<HistoryEntryData>();
        foreach (var entry in History)
        {
            historyData.Add(new HistoryEntryData
            {
                SourceFile = entry.SourceFile,
                OutputFile = entry.OutputFile,
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage,
                ConvertedAt = entry.ConvertedAt,
            });
        }
        Preferences.SaveHistory(historyData);

        LogLines.Add($"Done. {_completedFiles}/{TotalFiles} files completed.");
    }

    [RelayCommand]
    private void OpenOutputFolder(string? path = null)
    {
        var target = path ?? OutputFolder;
        if (Directory.Exists(target))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
                Verb = "open",
            });
        }
    }

    [RelayCommand]
    private void OpenFile(string? path)
    {
        if (path is not null && File.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open",
            });
        }
    }

    [RelayCommand]
    private async Task CopyMarkdown(string? markdown)
    {
        if (!string.IsNullOrWhiteSpace(markdown))
        {
            var topLevel = GetTopLevel();
            if (topLevel?.Clipboard is { } clipboard)
            {
                var transfer = new Avalonia.Input.DataTransfer();
                var item = Avalonia.Input.DataTransferItem.CreateText(markdown);
                transfer.Add(item);
                await clipboard.SetDataAsync(transfer);
            }
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
        Preferences.SaveHistory(new System.Collections.Generic.List<HistoryEntryData>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Trim().Length > 100 ? sanitized.Trim()[..100] : sanitized.Trim();
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public void OnFilesDropped(string[] paths)
    {
        AddFiles(paths.Where(File.Exists));
    }
}
