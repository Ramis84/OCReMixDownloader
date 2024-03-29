# OCReMixDownloader

<a href="https://ocremix.org/"><img align="right" src="https://ramis84.github.io/OCReMixDownloader/ocremix_88x31_icon.png" alt="OverClocked ReMix - Video Game Music Community" title="OverClocked ReMix - Video Game Music Community" /></a>

A cross-platform command line tool/script to download the latest OC ReMix (OverClocked ReMix) songs from the [official website](https://ocremix.org/). The tool will (optionally) remember the last downloaded ReMix, and continues from there at next run. Visit the [GitHub repository](https://github.com/Ramis84/OCReMixDownloader) for source code, and to contribute.

**NOTE:** If you want to download the entire back catalogue of songs, please check the [torrent page](https://ocremix.org/torrents) at OC ReMix instead to save on time/bandwidth, and continue from there. 

![Screenshot of OCReMixDownloader](https://ramis84.github.io/OCReMixDownloader/screenshot1.png "Screenshot of OCReMixDownloader")

## Features
1. Verifies the computed MD5-hash of each downloaded song with the hash taken from the OC ReMix website, and will download another mirror if the hash does not match.
1. (Optionally) remembers the last downloaded ReMix, and continues from there at next run.
1. (Optionally) multithreaded/concurrent downloads (limited by parameter).
1. (Optionally) downloads the latest .torrent-files as well (needs to manually be added to torrent client afterwards).
1. Will distribute load on all available mirrors.
1. In case of any problems downloading a ReMix, individual log files for each song will be written to the output folder.

## Using OCReMixDownloader
```
Usage:
  ocremixdownloader [option]... [songNr]...
Options:
  (NOTE: At least one parameter are required to run.)
  --output <PATH>    (Optional) The path (absolute/relative) where songs will be stored (default: The current folder/working directory).
  --config <PATH>    (Optional) The path (absolute/relative) of file (json) where settings and last downloaded song number will be stored. Will be created if it does not exist.
  --from <SONG_NR>   (Optional) The first song nr to download. If not set, and not available in config file, user will be asked to input it during startup.
  --to <SONG_NR>     (Optional) The last song nr to download. If not set, all songs including the latest one will be downloaded.
  --threads <COUNT>  (Optional) Number of concurrent downloads (default: 1).
  --includeTorrents  (Optional) Downloads torrents files as well, both Collections and Albums (only the .torrent, needs to be added to torrent client manually).
  --ignoreHashErrors (Optional) If the hash of a download differs from the reference on the page, the program will just give a warning and keep the download anyway.
Example:
  ocremixdownloader 2352 3751
  ocremixdownloader --config "C:/Files/settings.json" --output "C:/Download/"
```

On first run (when the specified config file does not exist yet or does not contain the latest downloaded song number), you will get to input the song number to start from.

## Installation

### Prerequisites

Before installing OCReMixDownloader, make sure that the latest [.Net Runtime (minimum 8.0)](https://dotnet.microsoft.com/download) is installed (available on Windows/Linux & macOS). To check your currently installed .Net version, run the following command in a Command Line window (Windows: Command Prompt/PowerShell, Linux/macOS: Terminal), and verify that the major version (first digit) is at least 8:

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

The application has been developed using .Net 8 (C#), and can be compiled/run using Visual Studio Code or just the .Net CLI (included in the [.Net 8.0 SDK](https://dotnet.microsoft.com/download)). Download the [GitHub repository](https://github.com/Ramis84/OCReMixDownloader) and execute the terminal command `dotnet run` to run from source, or `dotnet build` to build binaries.

## Automation / scheduling

In order to automatically download the newest songs periodically (e.g daily), you need to schedule the command to run at a specific interval. In Windows, the easiest way is to use the [Task Scheduler](https://en.wikipedia.org/wiki/Windows_Task_Scheduler) app included in Windows. In Linux & macOS, use [cron](https://en.wikipedia.org/wiki/Cron).
