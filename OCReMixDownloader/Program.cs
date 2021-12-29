﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace OCReMixDownloader
{
    class Program
    {
        private static readonly XmlSerializer RssSerializer = new XmlSerializer(typeof(RssRoot));
        private static readonly Regex Md5HashRegex = new Regex("<strong>MD5 Checksum: </strong>(?<md5>[^\"]+)</li>");
        private static readonly Regex DownloadLinkRegex = new Regex("<a href=\"(?<href>[^\"]+)\">Download from");
        private const string RssUrl = "https://ocremix.org/feeds/ten20/";
        private const string DownloadUrl = "https://ocremix.org/remix/OCR{0:D5}";
        private const string TorrentBaseUrl = "https://bt.ocremix.org/";
        private const string TorrentLinksPageUrl = "https://bt.ocremix.org/index.php?order=date&sort=descending";

        private static readonly HttpClient DownloadClient =
            new HttpClient(
                new HttpClientHandler
                {
                    AllowAutoRedirect = true
                });

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Print usage information
                var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                Console.WriteLine($"ocremixdownloader {version}:");
                Console.WriteLine("  Downloads OCReMix songs to a specified folder, remembering the last downloaded song.");
                Console.WriteLine("Usage:");
                Console.WriteLine("  ocremixdownloader [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --output <PATH>    (Optional) The folder where songs will be stored (default: The current folder/working directory).");
                Console.WriteLine("  --config <PATH>    (Optional) The file (json) where settings and last downloaded song number will be stored. Will be created if it does not exist.");
                Console.WriteLine("  --threads <COUNT>  (Optional) Number of concurrent downloads (default: 1).");
                Console.WriteLine("  --includeTorrents  (Optional) Downloads torrents files as well, both Collections and Albums (only the .torrent, needs to be added to torrent client manually).");
                Console.WriteLine("Example:");
                Console.WriteLine("  ocremixdownloader --config \"C:/Files/settings.json\" --output \"C:/Download/\"");
                return;
            }

            // Read parameters from command line
            var parameters = ReadParameters(args);
            if (parameters == null) return;

            // Check required parameters
            if (parameters.OutputPath == null)
            {
                parameters.OutputPath = ".";
            }
            if (!Directory.Exists(parameters.OutputPath))
            {
                Console.WriteLine($"Output folder path does not exist: {parameters.OutputPath}");
                return;
            }

            // Check optional parameters
            if (parameters.ConfigPath == null)
            {
                Console.WriteLine("WARNING: --config option omitted, will not remember the last downloaded song.");
            }

            // Read config file (if available)
            var settings = await ReadSettings(parameters.ConfigPath);
            if (settings == null)
            {
                Console.WriteLine($"Error: Could not load settings from config path, check permissions: {parameters.ConfigPath}");
                return;
            }

            // Read the starting OCReMix song number from settings if possible, otherwise let user type in
            if (!settings.NextDownloadNumber.HasValue)
            {
                // Let user decide on first release number, since missing in settings
                Console.Write("Please input OC ReMix song number to begin downloading from (e.g 3745): ");
                var input = Console.ReadLine();
                if (!int.TryParse(input, out var nextDownloadNumber))
                {
                    Console.WriteLine("Input not valid number");
                    return;
                }

                settings.NextDownloadNumber = nextDownloadNumber;
            }

            // If including torrents, read the starting torrent date from settings if possible, otherwise let user type in
            if (parameters.IncludeTorrents && !settings.LastTorrentDate.HasValue)
            {
                // Let user decide on starting torrent date, since missing in settings
                Console.Write("Please input torrent date to begin downloading from (in format \"2020-01-01\"). Leave empty to download everything: ");
                var input = Console.ReadLine();
                DateTime lastTorrentDate;
                if (string.IsNullOrWhiteSpace(input))
                {
                    // Download from the beginning
                    lastTorrentDate = DateTime.MinValue;
                }
                else if (!DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out lastTorrentDate))
                {
                    Console.WriteLine("Input not valid date");
                    return;
                }

                settings.LastTorrentDate = lastTorrentDate;
            }

            var latestSongNumber = await GetLatestSongNumberFromRss();
            if (latestSongNumber.HasValue)
            {
                if (latestSongNumber < settings.NextDownloadNumber)
                {
                    Console.WriteLine("There are no new ReMixes to download");
                }
                else
                {
                    Console.WriteLine($"There are {latestSongNumber - settings.NextDownloadNumber + 1} new ReMix(es) to attempt to download");

                    // Begin downloading from the given ReMix number, and continue until we have reached the latest one
                    settings.NextDownloadNumber = await DownloadSongs(settings.NextDownloadNumber.Value, latestSongNumber.Value, parameters.OutputPath, parameters.Threads);
                }
            }

            if (parameters.IncludeTorrents)
            {
                await DownloadTorrents(settings, parameters.OutputPath, parameters.Threads);
            }

            if (parameters.ConfigPath != null)
            {
                await WriteSettings(parameters.ConfigPath, settings);
            }
        }

        private static Parameters? ReadParameters(string[] args)
        {
            // Read parameters from command line
            var parameters = new Parameters();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--config" when i + 1 < args.Length:
                        parameters.ConfigPath = args[++i];
                        break;
                    case "--output" when i + 1 < args.Length:
                        parameters.OutputPath = args[++i];
                        break;
                    case "--threads" when i + 1 < args.Length:
                        if (!int.TryParse(args[++i], out var threads) || threads < 1)
                        {
                            Console.WriteLine($"Invalid number of threads: {args[i]}");
                            return null;
                        }
                        parameters.Threads = threads;
                        break;
                    case "--includeTorrents":
                        parameters.IncludeTorrents = true;
                        break;
                    default:
                        Console.WriteLine($"Invalid parameter: {args[i]}");
                        return null;
                }
            }

            return parameters;
        }

        private static async Task<SettingsModel?> ReadSettings(string? configPath)
        {
            if (configPath != null && File.Exists(configPath))
            {
                try
                {
                    var settingsContent = await File.ReadAllTextAsync(configPath);
                    return JsonSerializer.Deserialize<SettingsModel>(settingsContent) ?? new SettingsModel();
                }
                catch
                {
                    return null;
                }
            }

            // Use empty settings
            return new SettingsModel();
        }

        private static async Task WriteSettings(string configPath, SettingsModel settings)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var settingsContent = JsonSerializer.Serialize(settings, serializerOptions);

            try
            {
                await File.WriteAllTextAsync(configPath, settingsContent);
            }
            catch
            {
                Console.WriteLine($"Error: Could not save settings to config path, check permissions: {configPath}");
            }
        }

        private static async Task<int?> GetLatestSongNumberFromRss()
        {
            // Get RSS feed, to read latest song number
            var rssFeedResponse = await DownloadClient.GetAsync(RssUrl);
            if (!rssFeedResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: Could not get RSS feed. StatusCode: {rssFeedResponse.StatusCode}");
                return null;
            }

            var rssFeedXml = await rssFeedResponse.Content.ReadAsStreamAsync();
            var rssFeed = (RssRoot?)RssSerializer.Deserialize(rssFeedXml);
            var latestSongRss = rssFeed?.Channel?.Items?.FirstOrDefault();
            if (latestSongRss?.Link == null)
            {
                Console.WriteLine("Error: Could not read latest song number from RSS, invalid format");
                return null;
            }

            var latestSongNumberString = Regex.Match(latestSongRss.Link, @"\d+").Value; // Extract song number from URL
            var latestSongNumber = int.Parse(latestSongNumberString);
            return latestSongNumber;
        }

        private record HostStatistics(int HostActiveRequestCount, int CompletedRequestCount);

        private static async Task<int> DownloadSongs(int fromSongNr, int toSongNr, string outputPath, int threadCount)
        {
            if (fromSongNr > toSongNr)
            {
                return fromSongNr;
            }
            
            using var md5 = System.Security.Cryptography.MD5.Create();

            // Keep track of statistics of hosts to choose best mirrors
            var statisticsByHostname = new ConcurrentDictionary<string, HostStatistics>();

            // Begin downloading from the given ReMix number, and continue until we have reached the latest one
            var songNumbersQueue = new ConcurrentQueue<int>(Enumerable.Range(fromSongNr, toSongNr - fromSongNr + 1));
            var completedSongNrs = new ConcurrentBag<int>();
            var threadNumbers = Enumerable.Range(1, threadCount);
            await Task.WhenAll(threadNumbers
                .Select(async threadNumber =>
                {
                    while (songNumbersQueue.TryDequeue(out var songNr))
                    {
                        var success = false;
                        var songLogMessages = new List<string>();

                        // Read the OCReMix details page, to get all possible download links
                        var remixPageUrl = string.Format(DownloadUrl, songNr);
                        var pageResponse = await DownloadClient.GetAsync(remixPageUrl);
                        if (pageResponse.IsSuccessStatusCode)
                        {
                            // Log page success
                            var pageSuccessMessage = $"ReMix page loaded successfully ({remixPageUrl})";
                            songLogMessages.Add(pageSuccessMessage);

                            var htmlContent = await pageResponse.Content.ReadAsStringAsync();
                            var md5Hash = Md5HashRegex.Match(htmlContent).Groups["md5"].Value.ToLower();
                            if (string.IsNullOrWhiteSpace(md5Hash))
                            {
                                var warningMessage = $"Warning: MD5 hash was not found on page ({remixPageUrl}). Skipping verification";
                                songLogMessages.Add(warningMessage);
                                Console.WriteLine($"{songNr} {warningMessage}");
                            }

                            /* Try using the download links from HTML-page, look for links with text "Download from".
                               Try all mirrors until we find a working one
                               Shuffle the order mirrors are tried, to distribute load on all mirrors */
                            var songDownloadMirrorUris = DownloadLinkRegex.Matches(htmlContent)
                                .Cast<Match>()
                                .Where(x => x != null)
                                .Select(x => x.Groups["href"].Value) // Get url portion from link
                                .Select(x => System.Web.HttpUtility.HtmlDecode(x)) // Decode HTML encoded characters, like "&amp;" to "&"
                                .Shuffle() // Randomize order of mirrors, to distribute load a bit
                                .Select(x => new Uri(x))
                                .Select(x => new
                                {
                                    Uri = x,
                                    HostStatistics = statisticsByHostname.TryGetValue(x.Host, out var hostStatistics) ? hostStatistics : new HostStatistics(0, 0)
                                })
                                .OrderBy(x => x.HostStatistics.HostActiveRequestCount) // Prefer hosts with least number of active requests
                                .ThenBy(x => x.HostStatistics.CompletedRequestCount) // For hosts with same number of active requests, prefer hosts with least number of completed requests
                                .Select(x => x.Uri)
                                .ToList();

                            // Log number of mirrors
                            var allMirrorHosts = songDownloadMirrorUris.Select(x => x.Host);
                            var mirrorsCountMessage = $"{songDownloadMirrorUris.Count} download mirrors found on page ({string.Join(", ", allMirrorHosts)})";
                            songLogMessages.Add(mirrorsCountMessage);

                            foreach (var songDownloadMirrorUri in songDownloadMirrorUris)
                            {
                                // Get filename of mp3 from URL
                                var songFileNameUrlEncoded = songDownloadMirrorUri.Segments.Last();
                                var songFileName =
                                    Uri.UnescapeDataString(
                                        songFileNameUrlEncoded); // Decode URL escaped characters, like %20 to space

                                // Request about to start.
                                // Increase the number of active request for this host by one
                                var hostName = songDownloadMirrorUri.Host;
                                statisticsByHostname.AddOrUpdate(
                                    hostName,
                                    new HostStatistics(1, 0),
                                    (_, oldStatistics) => oldStatistics with
                                    {
                                        HostActiveRequestCount = oldStatistics.HostActiveRequestCount + 1
                                    });

                                // Try to download the ReMix
                                HttpResponseMessage downloadResponse;
                                try
                                {
                                    downloadResponse = await DownloadClient.GetAsync(songDownloadMirrorUri);
                                }
                                finally
                                {
                                    // Request finished.
                                    // Decrease the number of active request for this host by one, and increase completed by one instead
                                    statisticsByHostname.AddOrUpdate(
                                        hostName,
                                        new HostStatistics(1, 0),
                                        (_, oldStatistics) => oldStatistics with
                                        {
                                            HostActiveRequestCount = oldStatistics.HostActiveRequestCount - 1,
                                            CompletedRequestCount = oldStatistics.CompletedRequestCount + 1
                                        });
                                }

                                if (downloadResponse.IsSuccessStatusCode)
                                {
                                    // Download was successful, get bytes
                                    var downloadBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

                                    // Verify MD5 hash of file with hash on page, if available
                                    if (!string.IsNullOrWhiteSpace(md5Hash))
                                    {
                                        var md5HashComputedBytes = md5.ComputeHash(downloadBytes);
                                        var md5HashComputed = Convert.ToHexString(md5HashComputedBytes).ToLower();
                                        if (md5HashComputed != md5Hash)
                                        {
                                            // MD5 not matching, try next mirror
                                            var warningMessage = $"Skipping mirror, MD5 hash failure: {songDownloadMirrorUri}, Computed: {md5HashComputed}, Reference: {md5Hash}";
                                            songLogMessages.Add(warningMessage);
                                            Console.WriteLine($"{songNr} {warningMessage}");
                                            continue;
                                        }
                                    }

                                    // Store ReMix bytes to file on disk
                                    var filePath = Path.Combine(outputPath, songFileName);
                                    await File.WriteAllBytesAsync(filePath, downloadBytes);
                                    success = true;
                                    Console.WriteLine($"{songNr} OK: {songDownloadMirrorUri}");
                                    break; // Stop trying other mirrors
                                }

                                // Download failed, try next available mirror
                                var downloadLinkFailedWarningMessage = $"Skipping mirror, download link failed: {songDownloadMirrorUri}, HTTP StatusCode: {(int)downloadResponse.StatusCode}";
                                songLogMessages.Add(downloadLinkFailedWarningMessage);
                                Console.WriteLine($"{songNr} {downloadLinkFailedWarningMessage}");
                            }

                            if (!success)
                            {
                                // All download links failed, skip
                                var errorMessage = $"Failed: All download mirrors failed, skipping ReMix";
                                songLogMessages.Add(errorMessage);
                                Console.WriteLine($"{songNr} {errorMessage}");
                            }
                        }
                        else
                        {
                            // Could not get the OCReMix details page for this song number, skipping to next
                            var errorMessage = $"Failed: ReMix page could not be loaded ({remixPageUrl}), skipping ReMix. HTTP StatusCode: {(int)pageResponse.StatusCode}";
                            songLogMessages.Add(errorMessage);
                            Console.WriteLine($"{songNr} {errorMessage}");
                        }

                        if (!success)
                        {
                            // Write log file in output path for current song to indicate error
                            var songLogFilePath = Path.Combine(outputPath, $"{songNr}_failure.log");
                            await File.WriteAllLinesAsync(songLogFilePath, songLogMessages);
                        }

                        completedSongNrs.Add(songNr);
                    }
                }));

            // Store the next song number to download in settings
            var nextDownloadNr = completedSongNrs.Count > 0
                ? completedSongNrs.Max() + 1
                : fromSongNr;
            return nextDownloadNr;
        }

        private class TorrentToDownload
        {
            public DateTime TimeStamp { get; }
            public Uri Uri { get; }
            public string FileName { get; }

            public TorrentToDownload(DateTime timeStamp, Uri uri, string fileName)
            {
                TimeStamp = timeStamp;
                Uri = uri;
                FileName = fileName;
            }
        }

        private static async Task DownloadTorrents(SettingsModel settings, string outputPath, int threadCount)
        {
            var oldLastTorrentDate = settings.LastTorrentDate!.Value;

            // Load torrent links page
            var pageResponse = await DownloadClient.GetAsync(TorrentLinksPageUrl);
            if (!pageResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: Could not load torrent links page {TorrentLinksPageUrl}, StatusCode: {(int)pageResponse.StatusCode}");
                return;
            }

            // Parse HTML DOM
            var pageContent = await pageResponse.Content.ReadAsStringAsync();
            var pageDecoded = WebUtility.HtmlDecode(pageContent);
            var pageHtml = new HtmlDocument();

            // Fetch all table rows in torrent list
            pageHtml.LoadHtml(pageDecoded);
            var rows = pageHtml.DocumentNode.SelectNodes("//table[@class='trkInner']/tr[td and position() < last()]");
            if (rows == null)
            {
                Console.WriteLine("Error: Invalid format of torrent page");
                return;
            }

            // Go through all the latest torrents, until we reach a torrent we have already downloaded
            var torrentsToDownload = new ConcurrentStack<TorrentToDownload>();
            foreach (var row in rows)
            {
                // Read timestamp
                var timeStampCell = row.SelectSingleNode("td[contains(@class, 'colAdded')]");
                if (timeStampCell == null)
                {
                    Console.WriteLine("Error: Invalid format of torrent page, row without torrent timestamp information");
                    return;
                }

                if (!DateTime.TryParse(timeStampCell.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStamp))
                {
                    Console.WriteLine($"Error: Invalid format of torrent date: {timeStampCell.InnerText}");
                    return;
                }

                // Read torrent URL
                var torrentLinkCell = row.SelectSingleNode("td[contains(@class, 'colName')]/a");
                if (torrentLinkCell == null)
                {
                    Console.WriteLine("Error: Invalid format of torrent page, row without torrent link information");
                    return;
                }

                var torrentLinkAttribute = torrentLinkCell.Attributes["href"];
                if (torrentLinkAttribute == null)
                {
                    Console.WriteLine($"Error: Invalid format of torrent page, torrent link without url: {torrentLinkCell.OuterHtml}");
                    return;
                }

                // Get filename, check if we have downloaded this torrent before
                var torrentUri = new Uri(new Uri(TorrentBaseUrl), torrentLinkAttribute.Value);
                var fileNameUrlEncoded = torrentUri.Segments.Last();
                var fileName = Uri.UnescapeDataString(fileNameUrlEncoded); // Decode URL escaped characters, like %20 to space
                if (timeStamp < oldLastTorrentDate ||
                    (timeStamp == oldLastTorrentDate && settings.LastTorrentFiles != null && settings.LastTorrentFiles.Contains(fileName)))
                {
                    // No more new torrents, check no further
                    break;
                }

                // Queue it for download
                torrentsToDownload.Push(new TorrentToDownload(timeStamp, torrentUri, fileName));
            }

            if (torrentsToDownload.Count == 0)
            {
                Console.WriteLine("There are no new torrents to download");
                return;
            }
            
            Console.WriteLine($"There are {torrentsToDownload.Count} new torrent(s) to attempt to download");

            // Download all new torrents, oldest first
            var successfulDownloads = new ConcurrentBag<TorrentToDownload>();
            var threadNumbers = Enumerable.Range(1, threadCount);
            await Task.WhenAll(threadNumbers
                .Select(async threadNumber =>
                {
                    while (torrentsToDownload.TryPop(out var torrentToDownload))
                    {
                        var downloadResponse = await DownloadClient.GetAsync(torrentToDownload.Uri);
                        if (downloadResponse.IsSuccessStatusCode)
                        {
                            // Download was successful, get bytes
                            var downloadBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

                            var filePath = Path.Combine(outputPath, torrentToDownload.FileName!);
                            await File.WriteAllBytesAsync(filePath, downloadBytes);

                            successfulDownloads.Add(torrentToDownload);

                            Console.WriteLine($"{torrentToDownload.TimeStamp:yyyy-MM-dd} OK: {torrentToDownload.Uri}");
                        }
                        else
                        {
                            // Download failed, skip
                            Console.WriteLine(
                                $"{torrentToDownload.TimeStamp:yyyy-MM-dd} Error: Could not download {torrentToDownload.Uri}, StatusCode: {(int) downloadResponse.StatusCode}");
                        }
                    }
                }));

            // Store the latest torrent date in the settings, and all filenames in that date which have already been downloaded
            var successfulDownloadsInLatestDate = successfulDownloads
                .GroupBy(x => x.TimeStamp)
                .OrderByDescending(x => x.Key)
                .FirstOrDefault();
            if (successfulDownloadsInLatestDate != null)
            {
                var timestamp = successfulDownloadsInLatestDate.Key;
                if (timestamp > settings.LastTorrentDate)
                {
                    settings.LastTorrentDate = timestamp;
                    settings.LastTorrentFiles = new List<string>();
                }

                settings.LastTorrentFiles ??= new List<string>();
                settings.LastTorrentFiles.AddRange(successfulDownloadsInLatestDate.Select(x => x.FileName));
            }
        }
    }
}