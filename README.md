# MarkUnfold GUI

A modern desktop application for converting documents to Markdown using [MarkItDown](https://github.com/microsoft/markitdown) by Microsoft.

**MarkUnfold** provides a graphical interface to the powerful MarkItDown library, making it easy to convert PDFs, Word documents, PowerPoint presentations, Excel files, images, audio files, YouTube URLs, and more into clean Markdown.

![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-blue?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9.0-blue?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

## Features

- **Batch Conversion** — Convert multiple files at once with live progress tracking
- **Drag & Drop** — Drag files directly onto the app to add them to the conversion queue
- **YouTube Support** — Convert YouTube video transcripts to Markdown
- **Document Preview** — Preview converted Markdown before saving
- **Dark/Light Theme** — Toggle between dark and light mode
- **Azure Integration** — Optional Document Intelligence and Content Understanding for enhanced conversion
- **Conversion History** — Track past conversions with timestamps
- **Configurable Output** — Choose your own output directory with file name collision handling

## Screenshots

*(Screenshots coming soon)*

## Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 9.0 Runtime** (included in self-contained build)
- **Python 3.10+** — The app bundles Python automatically on first run

## Installation

### Option 1: Download Release (Recommended)

Download the latest release from the [Releases page](https://github.com/mohammad-bot3390/MarkUnfold/releases).

1. Extract the zip to any folder
2. Run `MarkUnfold.exe`
3. On first launch, the app will automatically download and set up the Python runtime and MarkItDown dependencies

### Option 2: Build from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/mohammad-bot3390/MarkUnfold.git
   cd MarkUnfold/MarkItDownGUI
   ```

2. Build the application:
   ```bash
   dotnet build --configuration Release
   ```

3. Run:
   ```bash
   dotnet run --project MarkItDownGUI.csproj
   ```

## Usage

1. **Add files** — Use the "Browse Files" button or drag files onto the drop zone
2. **Add YouTube URLs** — Paste a YouTube URL and click "Add"
3. **Set output folder** — Click "Change" to select your output directory
4. **Convert** — Click "Convert All" and watch the progress
5. **Preview** — Click any completed file to preview its Markdown content
6. **Open** — Click the folder icon to open the output folder, or the file icon to open the file directly

## Supported Formats

| Category | Formats |
|----------|---------|
| Documents | PDF, DOCX, PPTX, XLS, XLSX |
| Images | JPG, JPEG, PNG, GIF, BMP, WebP |
| Audio | MP3, WAV, M4A, FLAC |
| Text/Web | HTML, CSV, JSON, XML |
| Archives | ZIP |
| E-books | EPUB |
| Email | MSG |
| Misc | TXT, MD |
| Web | YouTube URLs |

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    MarkUnfold GUI (Avalonia .NET)            │
│  ┌──────────────┐  ┌─────────────┐  ┌──────────────────┐   │
│  │ ViewModels   │  │   Views     │  │   Services       │   │
│  │ (MVVM)       │◄─│  (.axaml)   │  │ (Logic Layer)    │   │
│  └──────────────┘  └─────────────┘  └────────┬─────────┘   │
└──────────────────────────────────────────────┼─────────────┘
                                               │
                                               ▼
┌─────────────────────────────────────────────────────────────┐
│              markitdown_bridge.py (Python Subprocess)       │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    MarkItDown Library                   ││
│  │   (PDF, Word, Excel, PowerPoint, Audio, Images, etc.)  ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

The GUI communicates with the Python `markitdown` library via a bridge script (`markitdown_bridge.py`) running as a subprocess. This architecture provides:

- **Structured I/O** — JSON-based request/response protocol
- **Per-file error isolation** — One process per file; crashes don't affect the batch
- **No intermediate temp files** — Markdown content is returned directly via JSON

## Configuration

Settings are stored in `%APPDATA%\MarkItDownGUI\prefs.json`:

- Theme (dark/light)
- Output folder
- Azure Document Intelligence settings
- Azure Content Understanding settings
- Conversion history

## Why a GUI for MarkItDown?

MarkItDown is a powerful CLI tool, but many users prefer a visual interface for batch operations, file browsing, and previewing results. MarkUnfold wraps MarkItDown's capabilities in a modern desktop experience.

## License

This project is a derivative work based on [MarkItDown](https://github.com/microsoft/markitdown) by Microsoft Corporation.

- **MarkItDown**: MIT License © Microsoft Corporation
- **MarkUnfold GUI**: MIT License — see [LICENSE-MARKITDOWNGUI.md](LICENSE-MARKITDOWNGUI.md)

## Acknowledgements

- [MarkItDown](https://github.com/microsoft/markitdown) — Microsoft's document-to-markdown converter
- [Avalonia UI](https://avaloniaui.net/) — Cross-platform XAML framework
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) — MVVM helpers

## Support

For issues specific to this GUI, please use [GitHub Issues](https://github.com/mohammad-bot3390/MarkUnfold/issues).

For MarkItDown library issues, visit the [upstream repository](https://github.com/microsoft/markitdown/issues).
