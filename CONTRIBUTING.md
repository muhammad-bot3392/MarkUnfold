# Contributing to MarkUnfold

Thank you for your interest in contributing to MarkUnfold! This is a community-driven open-source project and we welcome contributions of all kinds.

## How to Contribute

### Reporting Bugs

1. Check existing [Issues](https://github.com/mohammad-bot3390/MarkUnfold/issues) to avoid duplicates
2. Create a new issue with:
   - A clear, descriptive title
   - Steps to reproduce the bug
   - Expected behavior vs actual behavior
   - Screenshots if applicable
   - Your environment (OS, .NET runtime version, Python version)

### Suggesting Features

1. Open a new issue with the `enhancement` label
2. Describe the feature and why it's useful
3. Include any mockups or design ideas

### Submitting Code

1. Fork the repository
2. Create a feature branch:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. Make your changes
4. Ensure the solution builds:
   ```bash
   dotnet build MarkItDownGUI/MarkItDownGUI.csproj
   ```
5. Commit your changes:
   ```bash
   git commit -m "Add: descriptive message of your change"
   ```
6. Push to your fork:
   ```bash
   git push origin feature/my-new-feature
   ```
7. Open a Pull Request

### Code Style

- C#: Follow the `.editorconfig` rules in the repository
- XAML: 2-space indentation
- Python (`markitdown_bridge.py`): 4-space indentation
- Write meaningful commit messages
- Keep PRs small and focused

## Project Structure

```
MarkItDownGUI/
├── App.axaml              # Global styles, themes, resources
├── ViewModels/            # MVVM ViewModels (CommunityToolkit.Mvvm)
├── Views/                 # Avalonia views (.axaml)
├── Services/              # Business logic (MarkItDownService, Preferences)
├── Models/                # Data models (FileItem, ConversionHistoryEntry)
├── Converters/            # IValueConverters
├── Assets/                # Icons, logos
├── markitdown_bridge.py   # Python bridge script
├── python/                # Bundled Python runtime and requirements
└── MarkItDownGUI.csproj   # Project file

MarkItDownGUI.Tests/
└── xUnit test project
```

## Development Setup

1. Install .NET 9.0 SDK
2. Clone the repo:
   ```bash
   git clone https://github.com/mohammad-bot3390/MarkUnfold.git
   cd MarkUnfold
   ```
3. Build and run:
   ```bash
   dotnet run --project MarkItDownGUI/MarkItDownGUI.csproj
   ```
4. On first run, the app will prompt you to install Python + markitdown dependencies (or you can pre-install):
   ```bash
   pip install "markitdown[all]"
   ```
