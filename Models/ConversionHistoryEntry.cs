using System;

namespace MarkItDownGUI.Models;

public class ConversionHistoryEntry
{
    public string SourceFile { get; set; } = "";
    public string OutputFile { get; set; } = "";
    public DateTime ConvertedAt { get; set; } = DateTime.Now;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string ConvertedAtFormatted => ConvertedAt.ToString("yyyy-MM-dd HH:mm");
}
