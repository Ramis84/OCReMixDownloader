# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build
dotnet run -- [args]
dotnet pack          # produces nupkg/ for tool distribution
dotnet publish -c Release
```

There are no automated tests or linting configurations in this project.

## Architecture

**OCReMixDownloader** is a cross-platform .NET 8 CLI tool (global tool: `ocremixdownloader`) that downloads video game music remixes from ocremix.org. It supports individual song downloads, ranges, concurrent downloads with mirror load balancing, MD5 hash verification, and torrent file downloads.

### Core files

- [Program.cs](OCReMixDownloader/Program.cs) — All download logic: HTTP handling, HTML scraping (via HtmlAgilityPack), RSS feed parsing, concurrent task management, mirror selection, and MD5 verification. This single 600+ line file contains the main application flow.
- [Parameters.cs](OCReMixDownloader/Parameters.cs) — CLI argument model (config path, output path, from/to range, thread count, flags).
- [SettingsModel.cs](OCReMixDownloader/SettingsModel.cs) — Persistent state stored as JSON: next song number, last torrent date, set of downloaded filenames.
- [SourceGenerationContext.cs](OCReMixDownloader/SourceGenerationContext.cs) — JSON source generation context for zero-allocation serialization.
- [RssModel.cs](OCReMixDownloader/RssModel.cs) — XML deserialization models for the ocremix.org RSS 2.0 feed.
- [EnumerableExtensions.cs](OCReMixDownloader/EnumerableExtensions.cs) — Shuffle extension used to randomize mirror order.

### Key design patterns

**Concurrent downloads with host-aware load balancing:** Uses `ConcurrentQueue<T>`, `ConcurrentDictionary<T, U>`, and `Task.WhenAll`. A `HostStatistics` record tracks active requests, completed requests, and last start time per mirror host. Mirrors are prioritized by fewest active requests → fewest completed → oldest last start.

**Mirror failover with hash verification:** Each song page lists multiple download mirrors. The tool scrapes the page for mirrors and an expected MD5 hash, shuffles mirrors, then tries each in order — falling back to the next mirror if hash verification fails. `--ignoreHashErrors` skips hash validation.

**Persistent state:** Settings are read/written as JSON to a user-specified config file. On first run without a config, the tool interactively prompts for the starting song number.

**Source generation:** Both JSON (`System.Text.Json` source gen) and XML (`Microsoft.XmlSerializer.Generator`) use compile-time source generation for performance and AOT compatibility.

### Dependencies

- `HtmlAgilityPack` — HTML parsing for scraping download links from song pages and torrent pages.
- `Microsoft.XmlSerializer.Generator` — Source-generated XML serialization for RSS feed.

### Build configuration

- `net8.0`, nullable reference types enabled
- Release build treats all warnings as errors
- Packaged as `dotnet-ocremixdownloader` global tool (command: `ocremixdownloader`)
- Output nupkg goes to `./nupkg/`
