using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OCRemixDownloader
{
    class Program
    {
        private static readonly HttpClient DownloadClient = new HttpClient();
        private static readonly XmlSerializer RssSerializer = new XmlSerializer(typeof(RssRoot));
        private static readonly Regex DownloadLinkRegex = new Regex("<a href=\"(?<href>[^\"]+)\">Download from");
        private const string DownloadUrl = "http://ocremix.org/remix/OCR{0:D5}";

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                // Print usage information
                Console.WriteLine("ocremixdownloader 1.0.3:");
                Console.WriteLine("  Downloads OCRemix songs to a specified folder, remembering the last downloaded song.");
                Console.WriteLine("Usage:");
                Console.WriteLine("  ocremixdownloader [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --output <PATH>    The folder where songs will be stored. (Required)");
                Console.WriteLine("  --config <PATH>    The file (json) where settings and last downloaded song number will be stored. Will be created if it does not exist. (Optional)");
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
                Console.WriteLine("Missing parameter: --output");
                return;
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

            // Read the starting OCRemix song number from settings if possible, otherwise let user type in
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

            var latestSongNumber = await GetLatestSongNumberFromRss();
            if (!latestSongNumber.HasValue)
            {
                return;
            }

            if (latestSongNumber < settings.NextDownloadNumber)
            {
                Console.WriteLine("There are no new songs to download");
                return;
            }

            Console.WriteLine($"There are {latestSongNumber - settings.NextDownloadNumber + 1} new song(s) to attempt to download");

            // Begin downloading from the given remix number, and continue until we have reached the latest remix
            settings.NextDownloadNumber = await DownloadSongs(settings.NextDownloadNumber.Value, latestSongNumber.Value, parameters.OutputPath);

            if (parameters.ConfigPath != null)
            {
                Console.WriteLine($"Done. Will continue downloading from {settings.NextDownloadNumber} at next run");

                await WriteSettings(parameters.ConfigPath, settings);
            }
            else
            {
                Console.WriteLine("Done");
            }
        }

        private class Parameters
        {
            public string? ConfigPath { get; set; }
            public string? OutputPath { get; set; }
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
                IgnoreNullValues = true,
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
            var rssFeedResponse = await DownloadClient.GetAsync("https://ocremix.org/feeds/ten20/");
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

        private static async Task<int> DownloadSongs(int fromSongNr, int toSongNr, string outputPath)
        {
            var nextDownloadNumber = fromSongNr;

            // Begin downloading from the given remix number, and continue until we have reached the latest remix
            var currentAttemptNumber = fromSongNr;
            while (currentAttemptNumber <= toSongNr)
            {
                Console.Write($"{currentAttemptNumber} ");
                var success = false;

                // Read the OCRemix details page, to get all possible download links
                var remixPageUrl = string.Format(DownloadUrl, currentAttemptNumber);
                var pageResponse = await DownloadClient.GetAsync(remixPageUrl);
                if (pageResponse.IsSuccessStatusCode)
                {
                    Console.Write("pageok ");
                    var htmlContent = await pageResponse.Content.ReadAsStringAsync();

                    /* Try using the download links from HTML-page, look for links with text "Download from".
                       Try all mirrors until we find a working one */
                    foreach (Match? match in DownloadLinkRegex.Matches(htmlContent))
                    {
                        if (match == null) continue;

                        var downloadUrlHtmlEncoded = match.Groups["href"].Value; // Get url portion from link
                        var downloadUrl = System.Web.HttpUtility.HtmlDecode(downloadUrlHtmlEncoded); // Decode HTML encoded characters, like "&amp;" to "&"

                        // Try to download the remix
                        var downloadResponse = await DownloadClient.GetAsync(downloadUrl);
                        if (downloadResponse.IsSuccessStatusCode)
                        {
                            // Download was successful, get bytes
                            var downloadBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
                            Console.WriteLine($"downloadok {downloadUrl}");

                            // Get filename from URL
                            var uri = new Uri(downloadUrl);
                            var fileNameUrlEncoded = uri.Segments.Last();
                            var fileName = Uri.UnescapeDataString(fileNameUrlEncoded); // Decode URL escaped characters, like %20 to space

                            // Store remix bytes to file on disk
                            var filePath = Path.Combine(outputPath, fileName);
                            await File.WriteAllBytesAsync(filePath, downloadBytes);
                            success = true;
                            break; // Stop trying other mirrors
                        }

                        // Download failed, try next available mirror
                        Console.WriteLine($"downloadfail {downloadUrl}, statuscode: {(int)downloadResponse.StatusCode}");
                    }

                    if (!success)
                    {
                        // All download links failed, skip
                        Console.WriteLine("failed all download links, skipping remix");
                    }
                }
                else
                {
                    // Could not get the OCRemix details page for this remix number, skipping to next
                    Console.WriteLine($"pagefail, skipping remix, statuscode: {(int)pageResponse.StatusCode}");
                }

                currentAttemptNumber++;
                if (success)
                {
                    nextDownloadNumber = currentAttemptNumber; // Store the next remix to download in settings
                }
            }

            return nextDownloadNumber;
        }
    }
}
