#!/usr/bin/env python3
"""
markitdown_bridge.py — JSON-based bridge between the Avalonia GUI and the MarkItDown Python library.

Architecture choice (Python-subprocess-bridge over CLI-shell-out):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
We use a small Python helper script that the C# app invokes as an external
process. This gives us three advantages over shelling out to the `markitdown`
CLI directly:

1. Structured I/O — The script accepts a JSON command on stdin and writes a
   JSON result to stdout. This lets us pass complex options (Azure endpoints,
   OCR toggles, etc.) without fragile argument escaping, and receive structured
   responses (markdown text, error details, metadata) without scraping stderr.
2. Per-file error isolation — For batch jobs the C# side launches one process
   per file, so a crash in one file cannot take down the entire batch.
3. No intermediate temp files — The script returns the markdown content in the
   JSON response, so the GUI can show a live preview without first writing a
   .md file to disk and re-reading it.

Usage from C#:
   dotnet script markitdown_bridge.py < base64(input.json)
   or: echo <base64> | python markitdown_bridge.py
"""

import json
import sys
import base64
import os
import traceback
from pathlib import Path

# Fix Windows console encoding issues (emoji, non-ASCII chars in transcripts)
if sys.stdout.encoding and sys.stdout.encoding.lower() != 'utf-8':
    sys.stdout.reconfigure(encoding='utf-8')
if sys.stderr.encoding and sys.stderr.encoding.lower() != 'utf-8':
    sys.stderr.reconfigure(encoding='utf-8')


def convert_youtube(url: str, options: dict) -> dict:
    """
    Convert a YouTube URL to Markdown using youtube-transcript-api directly.
    Bypasses markitdown's broken YouTube implementation.
    """
    import re
    try:
        from youtube_transcript_api import YouTubeTranscriptApi
    except ImportError:
        return {
            "success": False,
            "error": "youtube-transcript-api is not installed. Run: pip install youtube-transcript-api",
            "error_type": "ImportError",
        }

    video_id = None
    match = re.search(r'(?:v=|\/)([0-9A-Za-z_-]{11})', url)
    if match:
        video_id = match.group(1)

    if not video_id:
        return {
            "success": False,
            "error": f"Could not extract video ID from URL: {url}",
            "error_type": "ValueError",
        }

    try:
        api = YouTubeTranscriptApi()
        transcript = api.fetch(video_id)
    except Exception as e:
        return {
            "success": False,
            "error": f"Could not fetch transcript: {e}",
            "error_type": type(e).__name__,
        }

    if not transcript:
        return {
            "success": False,
            "error": "No transcript available for this video.",
            "error_type": "ValueError",
        }

    lines = []
    for entry in transcript:
        text = entry.text if hasattr(entry, 'text') else entry.get('text', '')
        if text:
            lines.append(text)

    markdown = "\n\n".join(lines)

    return {
        "success": True,
        "markdown": markdown,
        "title": f"YouTube Transcript ({video_id})",
    }


def convert_file(file_path: str, options: dict) -> dict:
    """
    Convert a single file to Markdown using the MarkItDown library.
    Returns a dict suitable for JSON serialization.
    """
    from markitdown import MarkItDown

    kwargs = {}

    # Azure Document Intelligence
    if options.get("use_docintel") and options.get("docintel_endpoint"):
        kwargs["docintel_endpoint"] = options["docintel_endpoint"]

    # Azure Content Understanding
    if options.get("use_cu") and options.get("cu_endpoint"):
        kwargs["cu_endpoint"] = options["cu_endpoint"]
        if options.get("cu_analyzer"):
            kwargs["cu_analyzer_id"] = options["cu_analyzer"]

    # Build the converter
    md = MarkItDown(**kwargs)

    try:
        result = md.convert(file_path)
        return {
            "success": True,
            "markdown": result.text_content,
            "title": result.title or "",
        }
    except Exception as e:
        return {
            "success": False,
            "error": str(e),
            "error_type": type(e).__name__,
        }


def check_environment() -> dict:
    """Check if markitdown and its dependencies are available."""
    result = {
        "python_version": sys.version,
        "markitdown_available": False,
        "markitdown_version": None,
        "errors": [],
    }

    try:
        from markitdown import __version__ as md_version
        result["markitdown_available"] = True
        result["markitdown_version"] = md_version
    except ImportError:
        result["errors"].append("markitdown package is not installed. Run: pip install markitdown")

    # Check for optional extras
    extras = {
        "pdf": "pdfminer",
        "docx": "mammoth",
        "pptx": "pptx",
        "xlsx": "openpyxl",
        "audio": "pydub",
        "youtube": "youtube_transcript_api",
    }
    result["extras"] = {}
    for name, module in extras.items():
        try:
            __import__(module)
            result["extras"][name] = True
        except ImportError:
            result["extras"][name] = False

    try:
        from markitdown._markitdown import MarkItDown
        md = MarkItDown()
        supported = []
        # Get a sample of supported extensions from the registered converters
        result["supported_extensions"] = [
            ".pdf", ".docx", ".pptx", ".xlsx", ".xls",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".mp3", ".wav", ".m4a", ".flac",
            ".html", ".htm", ".csv", ".json", ".xml",
            ".zip", ".epub", ".msg",
            ".txt", ".md",
        ]
    except Exception as e:
        result["errors"].append(f"Error initializing markitdown: {e}")

    return result


def _handle_command(decoded: str) -> dict:
    """Parse decoded JSON string and dispatch to the appropriate handler."""
    command = json.loads(decoded)
    action = command.get("action", "convert")

    if action == "check":
        return check_environment()
    elif action == "convert":
        source = command.get("file_path", "")
        if not source:
            return {"success": False, "error": "No file_path provided"}
        if source.startswith(("http://", "https://", "ftp://")):
            if "youtube.com" in source or "youtu.be" in source:
                return convert_youtube(source, command.get("options", {}))
            return convert_file(source, command.get("options", {}))
        if not os.path.isfile(source):
            return {"success": False, "error": f"File not found: {source}"}
        return convert_file(source, command.get("options", {}))
    else:
        return {"success": False, "error": f"Unknown action: {action}"}


def main():
    """
    Accept a base64-encoded JSON command and return a JSON result.
    The command can come from two sources:
      1. First command-line argument (safer, eliminates pipe issues)
      2. stdin (fallback, for backward compatibility)
    """
    try:
        raw_input: str | None = None

        # Priority 1: command-line argument
        if len(sys.argv) > 1:
            raw_input = sys.argv[1]

        # Priority 2: stdin
        if raw_input is None:
            raw = sys.stdin.buffer.read()
            if raw:
                raw_input = raw.decode("utf-8").strip()

        if raw_input is None:
            result = check_environment()
        else:
            try:
                decoded = base64.b64decode(raw_input).decode("utf-8")
            except Exception:
                decoded = raw_input
            result = _handle_command(decoded)

        json.dump(result, sys.stdout, ensure_ascii=False, indent=2)
    except Exception as e:
        json.dump({
            "success": False,
            "error": f"Bridge error: {e}\n{traceback.format_exc()}",
        }, sys.stdout, ensure_ascii=False)


if __name__ == "__main__":
    main()
