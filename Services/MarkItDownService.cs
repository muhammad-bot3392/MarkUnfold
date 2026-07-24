using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MarkItDownGUI.Services;

/// <summary>
/// Structured response from the Python bridge script.
/// </summary>
public class BridgeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("markdown")]
    public string? Markdown { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_type")]
    public string? ErrorType { get; set; }

    [JsonPropertyName("python_version")]
    public string? PythonVersion { get; set; }

    [JsonPropertyName("markitdown_available")]
    public bool MarkitdownAvailable { get; set; }

    [JsonPropertyName("markitdown_version")]
    public string? MarkitdownVersion { get; set; }

    [JsonPropertyName("supported_extensions")]
    public List<string>? SupportedExtensions { get; set; }

    [JsonPropertyName("extras")]
    public Dictionary<string, bool>? Extras { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Service that invokes the MarkItDown Python bridge to convert files.
/// Communicates via base64-encoded JSON over stdin/stdout.
/// </summary>
public class MarkItDownService
{
    private readonly string _bridgePath;
    private readonly string _pythonExe;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public MarkItDownService()
    {
        _bridgePath = PythonSetupManager.GetBridgePath();
        _pythonExe = PythonSetupManager.GetPythonPath() ?? "python";
    }

    /// <summary>
    /// Check if Python and markitdown are available.
    /// </summary>
    public async Task<BridgeResponse> CheckEnvironmentAsync()
    {
        var json = JsonSerializer.Serialize(new { action = "check" });
        return await SendCommandAsync(json);
    }

    /// <summary>
    /// Convert a single file to Markdown.
    /// </summary>
    public async Task<BridgeResponse> ConvertFileAsync(
        string filePath,
        ConversionOptions? options = null)
    {
        var cmd = new
        {
            action = "convert",
            file_path = filePath,
            options = options?.ToDictionary() ?? new Dictionary<string, object>()
        };
        var json = JsonSerializer.Serialize(cmd);
        return await SendCommandAsync(json);
    }

    private async Task<BridgeResponse> SendCommandAsync(string json)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        // Pass the base64 payload as a command-line argument (eliminates pipe/encoding issues)
        psi.ArgumentList.Add(_bridgePath);
        psi.ArgumentList.Add(base64);

        try
        {
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            using var process = new Process { StartInfo = psi };
            process.Start();

            // Read stdout (the bridge writes its JSON result there)
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(output))
            {
                return new BridgeResponse
                {
                    Success = false,
                    Error = $"No output from bridge. Stderr: {error}"
                };
            }

            try
            {
                var result = JsonSerializer.Deserialize<BridgeResponse>(output, JsonOptions);
                return result ?? new BridgeResponse
                {
                    Success = false,
                    Error = "Failed to parse bridge output"
                };
            }
            catch (JsonException ex)
            {
                return new BridgeResponse
                {
                    Success = false,
                    Error = $"JSON parse error: {ex.Message}. Raw output: {output}"
                };
            }
        }
        catch (Exception ex)
        {
            return new BridgeResponse
            {
                Success = false,
                Error = $"Process error: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Options that map to flags the MarkItDown Python API accepts.
/// </summary>
public class ConversionOptions
{
    public bool UseDocIntel { get; set; }
    public string? DocIntelEndpoint { get; set; }
    public bool UseContentUnderstanding { get; set; }
    public string? CUEndpoint { get; set; }
    public string? CUAnalyzer { get; set; }

    public Dictionary<string, object> ToDictionary()
    {
        var d = new Dictionary<string, object>();
        if (UseDocIntel && !string.IsNullOrWhiteSpace(DocIntelEndpoint))
        {
            d["use_docintel"] = true;
            d["docintel_endpoint"] = DocIntelEndpoint;
        }
        if (UseContentUnderstanding && !string.IsNullOrWhiteSpace(CUEndpoint))
        {
            d["use_cu"] = true;
            d["cu_endpoint"] = CUEndpoint;
            if (!string.IsNullOrWhiteSpace(CUAnalyzer))
                d["cu_analyzer"] = CUAnalyzer;
        }
        return d;
    }
}
