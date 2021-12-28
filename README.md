# OCReMixDownloader

<a href="https://ocremix.org/"><img align="right" src="https://ramis84.github.io/OCReMixDownloader/ocremix_88x31_icon.png" alt="OverClocked ReMix - Video Game Music Community" title="OverClocked ReMix - Video Game Music Community" /></a>

A cross-platform command line tool/script to download the latest OC ReMix (OverClocked ReMix) songs from the [official website](https://ocremix.org/). The tool will (optionally) remember the last downloaded song, and continue from that at next run. Visit the [GitHub repository](https://github.com/Ramis84/OCReMixDownloader) for source code, and to contribute.

**NOTE:** If you want to download the entire back catalogue of songs, please check the [torrent page](https://ocremix.org/torrents) at OC ReMix instead to save on time/bandwidth, and continue from there. 

![Screenshot of OCReMixDownloader](https://ramis84.github.io/OCReMixDownloader/screenshot1.png "Screenshot of OCReMixDownloader")

## Using OCReMixDownloader
```
Usage:
  ocremixdownloader [options]
Options:
  --output <PATH>    (Required) The folder where songs will be stored.
  --config <PATH>    (Optional) The file (json) where settings and last downloaded song number will be stored. Will be created if it does not exist.
  --threads <COUNT>  (Optional) Number of concurrent downloads (default: 1).
  --includeTorrents  (Optional) Downloads torrents files as well, both Collections and Albums (only the .torrent, needs to be added to torrent client manually).
Example:
  ocremixdownloader --config "C:/Files/settings.json" --output "C:/Download/"
```

On first run (when the specified config file does not exist yet or does not contain the latest downloaded song number), you will get to input the song number to start from.

## Installation

### Prerequisites

Before installing OCReMixDownloader, make sure that the latest [.Net Runtime (minimum 6.0)](https://dotnet.microsoft.com/download) is installed (available on Windows/Linux & macOS). To check your currently installed .Net version, run the following command in a Command Line window (Windows: Command Prompt/PowerShell, Linux/macOS: Terminal), and verify that the major version (first digit) is at least 6:

   ```
   dotnet --version
   ```

### Installing

OCReMixDownloader is available as a .Net Tool (supported by Windows, macOS, Linux), that can be downloaded and used globally with the command line.

1. Open a Command Line window (Windows: Command Prompt/PowerShell, Linux/macOS: Terminal) and execute:

   ```
   dotnet tool install -g dotnet-ocremixdownloader
   ```

2. You may need to close the previous window to continue. The command `ocremixdownloader` should then be able to run in any folder.

3. Later. you can update or uninstall the tool using the following commands:

   ```
   dotnet tool update -g dotnet-ocremixdownloader
   ```
   ```
   dotnet tool uninstall -g dotnet-ocremixdownloader
   ```
   
### Run without installing

You may also download the latest binaries from the [releases page](https://github.com/Ramis84/OCReMixDownloader/releases), and run manually in Windows/Linux or macOS using the command line.

### Run/compile from source

The application has been developed using .Net 6 (C#), and can be compiled/run using Visual Studio Code or just the .Net CLI (included in the [.Net 6.0 SDK](https://dotnet.microsoft.com/download)). Download the [GitHub repository](https://github.com/Ramis84/OCReMixDownloader) and execute the terminal command `dotnet run` to run from source, or `dotnet build` to build binaries.

## Automation / scheduling

In order to automatically download the newest songs periodically (e.g daily), you need to schedule the command to run at a specific interval. In Windows, the easiest way is to use the [Task Scheduler](https://en.wikipedia.org/wiki/Windows_Task_Scheduler) app included in Windows. In Linux & macOS, use [cron](https://en.wikipedia.org/wiki/Cron).
