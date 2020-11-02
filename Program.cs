using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OCRemixDownloader
{
    class Program
    {
        private static readonly HttpClient DownloadClient = new HttpClient();
        private static readonly Regex DownloadLinkRegex = new Regex("<a href=\"(?<href>[^\"]+)\">Download from");

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            // Get download path from parameters
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: OCRemixDownloader.exe <download_folder_path>");
                return;
            }
            var downloadFolderPath = args[0];
            if (!Directory.Exists(downloadFolderPath))
            {
                Console.WriteLine($"Download folder path does not exist: {downloadFolderPath}");
                return;
            }

            // Read settings json from same folder as app
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            var settingsContent = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<SettingsModel>(settingsContent);

            // Read the starting OCRemix song number from settings if possible
            int nextDownloadNumber;
            if (settings.NextDownloadNumber.HasValue)
            {
                nextDownloadNumber = settings.NextDownloadNumber.Value;
            }
            else
            {
                // Let user decide on first release number, since missing in settings
                Console.Write("Please input OC ReMix song number to begin downloading from (e.g 3745): ");
                var input = Console.ReadLine();
                if (!int.TryParse(input, out nextDownloadNumber))
                {
                    Console.WriteLine("Input not valid number");
                    return;
                }
            }

            /* Begin downloading from the given remix number, and continue until we have had 5 consecutive errors (404-NotFound usually)
               which indicates that we have reached the latest remix */
            var currentAttemptNumber = nextDownloadNumber;
            var consecutiveErrors = 0;
            while (consecutiveErrors < 5)
            {
                Console.Write($"{currentAttemptNumber} ");
                var success = false;

                // Read the OCRemix details page, to get all possible download links
                var remixPageUrl = string.Format(settings.DownloadUrl, currentAttemptNumber); // The remix page URL format is taken from settings.json
                var pageResponse = await DownloadClient.GetAsync(remixPageUrl);
                if (pageResponse.IsSuccessStatusCode)
                {
                    Console.Write("pageok ");
                    var htmlContent = await pageResponse.Content.ReadAsStringAsync();

                    /* Try using the download links from HTML-page, look for links with text "Download from".
                       Try all mirrors until we find a working one */
                    foreach (Match match in DownloadLinkRegex.Matches(htmlContent))
                    {
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
                            var filePath = Path.Combine(downloadFolderPath, fileName);
                            File.WriteAllBytes(filePath, downloadBytes);
                            success = true;
                            break; // Stop trying other mirrors
                        }
                        else
                        {
                            // Download failed, try next available mirror
                            Console.WriteLine($"downloadfail {downloadUrl}, statuscode: {(int)downloadResponse.StatusCode}");
                        }
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
                    consecutiveErrors = 0; // Reset error counter on success
                    nextDownloadNumber = currentAttemptNumber; // Store the next remix to download in settings
                }
                else
                {
                    consecutiveErrors++;
                }
            }

            Console.WriteLine($"Too many consecutive errors, will start downloading from {nextDownloadNumber} next time");

            // Save settings for next run
            settings.NextDownloadNumber = nextDownloadNumber;

            var serializerOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };
            settingsContent = JsonSerializer.Serialize(settings, serializerOptions);
            File.WriteAllText(settingsPath, settingsContent);
        }
    }
}
