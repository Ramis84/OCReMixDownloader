# OCRemixDownloader

A cross-platform command line tool to download the latest OCRemix (OverClocked ReMix) songs from the official website. The tool will (optionally) remember the last downloaded song, and continue from that at next run.

**NOTE:** If you want to download the entire back catalogue of songs, please check the [torrent page](https://ocremix.org/torrents) at ocremix instead to save on time/bandwidth.

## Using OCRemixDownloader
```
Usage:
  ocremixdownloader [options]
Options:
  --output <PATH>    The folder where songs will be stored. (Required)
  --config <PATH>    The file (json) where settings and last downloaded song number will be stored. Will be created if it does not exist. (Optional)
Example:
  ocremixdownloader --config "C:/Files/settings.json" --output "C:/Download/"
```

On first run (when the specified config file does not exist yet or does not contain the latest downloaded song number), you will get to input the song number to start from.

## Installation

### Prerequisites

Before installing OCRemixDownloader, make sure that the latest [.Net Runtime (minimum 5.0)](https://dotnet.microsoft.com/download) is installed (available on Windows/Linux & MacOS)

### Installing as a NuGet package

OCRemixDownloader is available as a .Net Tool (supported by Windows, MacOS, Linux), that can be downloaded and used globally with the command line.

1. Open a Command Line window (Windows: Command Prompt/PowerShell or Windows Terminal, Linux/macOS: Terminal) and execute:

   ```
   dotnet tool install -g dotnet-ocremixdownloader
   ```

2. You may need to close the previous window to continue. The command `ocremixdownloader` should then be able to run in any folder using the command prompt or PowerShell.

3. Later. you can update or uninstall the tool using the following commands:

   ```
   dotnet tool update -g dotnet-ocremixdownloader
   ```
   ```
   dotnet tool uninstall -g dotnet-ocremixdownloader
   ```
   
### Run without installing

You may also download the latest binaries from the releases-page, and run manually in Windows/Linux or MacOS using the command line.

- https://github.com/Ramis84/OCRemixDownloader/releases

### Run/compile from source

The application has been developed using .Net 5 (C#), and can be compiled/run using Visual Studio Code or just the .Net CLI (included in the [.Net 5.0 SDK](https://dotnet.microsoft.com/download)). Download the repository and execute the terminal command `dotnet run` to run from source, or `dotnet build` to build binaries.
