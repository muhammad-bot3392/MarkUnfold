# Build Instructions

## Prerequisites

- .NET 9.0 SDK or later
- Windows 10/11 (for publishing as win-x64)
- Python 3.10+ (for building/testing the bridge script)

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

## Code Signing

For production distribution, sign the executable with an EV code-signing certificate:

1. Obtain an EV code-signing certificate from a trusted CA (e.g., DigiCert, Sectigo, GlobalSign)
2. Sign the executable using SignTool:

```bash
signtool sign /fd SHA256 /a /tr http://timestamp.digicert.com /td SHA256 MarkItDownGUI.exe
```

3. Verify the signature:

```bash
signtool verify /pa MarkItDownGUI.exe
```

**Note:** Without code signing, users will see Windows SmartScreen warnings when running the app. Open-source projects can obtain free or discounted code-signing certificates through:
- [Open Source Security Foundation (OpenSSF)](https://openssf.org/)
- [Microsoft Developer Security](https://developer.microsoft.com/)

## CI/CD

See [.github/workflows/build.yml](.github/workflows/build.yml) for the automated build pipeline.

## Dependencies

The app requires Python 3.10+ and the `markitdown` package with its extras. On first run or via the Settings panel, the app can automatically set up these dependencies.

Manual setup:

```bash
pip install "markitdown[all]"
```

Required extras:
- `pdf` — pdfminer.six
- `docx` — mammoth
- `pptx` — python-pptx
- `xlsx` — openpyxl
- `audio` — pydub
- `youtube` — youtube-transcript-api
