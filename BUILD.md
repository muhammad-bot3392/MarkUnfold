# Build Instructions

## Prerequisites

- .NET 9.0 SDK or later
- Windows 10/11 (for publishing as win-x64)

## Quick Build

```bash
dotnet build MarkItDownGUI/MarkItDownGUI.csproj
```

## Release Build

```bash
dotnet build MarkItDownGUI/MarkItDownGUI.csproj -c Release
```

Output will be in `MarkItDownGUI/bin/Release/net9.0/win-x64/`.

## Single-File Publish

The project is configured with `<PublishSingleFile>true</PublishSingleFile>` which bundles the .NET runtime and all dependencies into a single executable.

```bash
dotnet publish MarkItDownGUI/MarkItDownGUI.csproj -c Release
```

Output will be in `MarkItDownGUI/bin/Release/net9.0/win-x64/publish/`.

## Packaging a Release (with bundled Python)

To create a fully offline release zip:

1. Download [Python 3.11 embeddable zip](https://www.python.org/downloads/windows/) (Windows embeddable package)
2. Extract it into `MarkItDownGUI/python/` so you have:
   ```
   MarkItDownGUI/python/python.exe
   MarkItDownGUI/python/Scripts/
   MarkItDownGUI/python/Lib/
   ```
3. Install markitdown into the bundled Python:
   ```bash
   MarkItDownGUI/python/python.exe -m pip install --quiet -r MarkItDownGUI/python/requirements.txt
   ```
4. Build the C# app:
   ```bash
   dotnet publish MarkItDownGUI/MarkItDownGUI.csproj -c Release
   ```
5. Zip the contents of the `publish/` folder — the `python/` directory will be included automatically

## Code Signing

For production distribution, sign the executable with an EV code-signing certificate:

1. Obtain an EV code-signing certificate from a trusted CA (e.g., DigiCert, Sectigo, GlobalSign)
2. Sign the executable using SignTool:

```bash
signtool sign /fd SHA256 /a /tr http://timestamp.digicert.com /td SHA256 MarkUnfold.exe
```

3. Verify the signature:

```bash
signtool verify /pa MarkUnfold.exe
```

**Note:** Without code signing, users will see Windows SmartScreen warnings when running the app. Open-source projects can obtain free or discounted code-signing certificates through:
- [Open Source Security Foundation (OpenSSF)](https://openssf.org/)
- [Microsoft Developer Security](https://developer.microsoft.com/)

## CI/CD

See [.github/workflows/build.yml](.github/workflows/build.yml) for the automated build pipeline.

## Dependencies

The release package includes:
- .NET 9.0 runtime (self-contained)
- Python 3.11 embeddable runtime
- markitdown[all] with all extras (pdf, docx, pptx, xlsx, audio, youtube)

Required extras:
- `pdf` — pdfminer.six
- `docx` — mammoth
- `pptx` — python-pptx
- `xlsx` — openpyxl
- `audio` — pydub
- `youtube` — youtube-transcript-api
