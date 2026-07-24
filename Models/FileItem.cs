using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MarkItDownGUI.Models;

public enum FileStatus
{
    Queued,
    Converting,
    Done,
    Failed,
}

public partial class FileItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private FileStatus _status = FileStatus.Queued;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _markdownContent = "";

    [ObservableProperty]
    private string? _errorMessage;

    public string Extension => Path.GetExtension(FilePath).ToLowerInvariant();

    public FileItem(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }
}
