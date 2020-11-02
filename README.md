# OCRemixDownloader
 A downloader for OverClocked ReMix (OCRemix) songs

## Overview
OCRemixDownloader is a cross-platform command line tool to download the latest OCRemix (OverClocked ReMix) songs from the official website. The tool will remember the last downloaded song, and continue from that at next run. It has been developed using .Net Core in Visual Studio 2019 (C#).

## Installation
OCRemixDownloader is available as a .Net Core tool (supported by Windows, MacOS, Linux), that can be downloaded and used globally with the command line.

1. Install the latest .Net Core Runtime (minimum 3.1), https://dotnet.microsoft.com/download

2. In windows, open a Command Prompt or PowerShell window and execute:

   ```
   dotnet tool install -g dotnet-ocremixdownloader
   ```

3. You may need to close the previous window to continue. The command `ocremixdownloader` should then be able to run in any folder using the command prompt or PowerShell.

## Using OCRemixDownloader

Usage:
  ocremixdownloader [options]
Options:
  --output <PATH>    The folder where songs will be stored. (Required)
  --config <PATH>    The file (json) where settings and last downloaded song number will be stored. (Optional)
Example:
  ocremixdownloader --config "C:/Files/settings.json" --output "C:/Download/"

## Updating

In a Command Prompt or PowerShell, execute:

   ```
   dotnet tool update -g dotnet-ocremixdownloader
   ```

## Unstallation

In a Command Prompt or PowerShell, execute:

   ```
   dotnet tool uninstall -g dotnet-ocremixdownloader
   ```
