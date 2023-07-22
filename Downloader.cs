using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace GoogleDriveXHardDiskSynchronizer;

public class Downloader
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private const double DataReserve = 1.5;

    public Downloader(IConfiguration configuration)
    {
        _client = new HttpClient();
        _configuration = configuration;
    }

    private async Task<string> LoginAndGetAccessToken(string username, string password)
    {
        string LoginUrl = _configuration["Urls:LoginUrl"];
        string ClientId = _configuration["ApiDetails:ClientId"];
        string ChannelId = _configuration["ApiDetails:ChannelId"];

        var requestContent = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("username", username),
        new KeyValuePair<string, string>("password", password),
        new KeyValuePair<string, string>("channelID", ChannelId)
    });

        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, LoginUrl)
        {
            Content = requestContent
        };

        requestMessage.Headers.Add("X-IBM-Client-Id", ClientId);

        var response = await _client.SendAsync(requestMessage);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(jsonResponse);

        return jObject["accessToken"].ToString();
    }


    private async Task<JObject> GetPackageSummary(string accessToken, string subscriberID)
    {
        string DashboardUrl = _configuration["Urls:DashboardUrl"] + subscriberID;
        string ClientId = _configuration["ApiDetails:ClientId"];

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, DashboardUrl);
        requestMessage.Headers.Add("X-IBM-Client-Id", ClientId);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(requestMessage);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        return JObject.Parse(jsonResponse);
    }

    public async Task CheckAndDownloadFilesAsync()
    {
        string? googleDriveFolder = _configuration["Folders:GoogleDriveFolder"];
        string? hardDiskFolder = _configuration["Folders:HardDiskFolder"];

        ArgumentNullException.ThrowIfNull(googleDriveFolder, nameof(googleDriveFolder));
        ArgumentNullException.ThrowIfNull(hardDiskFolder, nameof(hardDiskFolder));

        var googleDriveFiles = Directory.GetFiles(googleDriveFolder, "*.*", SearchOption.AllDirectories)
                                        .Select(Path.GetFileName);
        var hardDiskFiles = Directory.GetFiles(hardDiskFolder, "*.*", SearchOption.AllDirectories)
                                        .Select(Path.GetFileName);

        var missingFiles = googleDriveFiles.Except(hardDiskFiles).ToList();

        string accessToken = await LoginAndGetAccessToken(_configuration["LoginDetails:Username"], _configuration["LoginDetails:Password"]);
        var packageSummary = await GetPackageSummary(accessToken, _configuration["LoginDetails:SubscriberID"]);

        double limit = packageSummary["dataBundle"]["vas_data_summary"]["limit"].Value<double>();
        double used = packageSummary["dataBundle"]["vas_data_summary"]["used"].Value<double>();
        double availableData = limit - used - DataReserve;

        foreach (var file in missingFiles)
        {
            var files = Directory.GetFiles(googleDriveFolder, file, SearchOption.AllDirectories);
            if (files.Any())
            {
                var sourceFilePath = files.First();
                var fileInfo = new FileInfo(sourceFilePath);
                double fileSize = (double)fileInfo.Length / (1024 * 1024 * 1024); // File size in GB

                if (availableData >= fileSize)
                {
                    try
                    {
                        var destFilePath = Path.Combine(hardDiskFolder, file);
                        Console.WriteLine($"Copying: {file}");

                        // Copy with progress bar and speed calculation
                        using (FileStream sourceStream = File.Open(sourceFilePath, FileMode.Open))
                        {
                            using (FileStream destStream = File.Create(destFilePath))
                            {
                                var buffer = new byte[1024 * 1024]; // 1MB buffer
                                int bytesRead;
                                long totalRead = 0;
                                var watch = System.Diagnostics.Stopwatch.StartNew(); // start stopwatch

                                // read source file in chunks
                                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await destStream.WriteAsync(buffer, 0, bytesRead);
                                    totalRead += bytesRead;

                                    // calculate progress
                                    double progress = (double)totalRead / fileInfo.Length;

                                    // calculate speed
                                    watch.Stop();
                                    double speed = totalRead / (1024 * 1024 * watch.Elapsed.TotalSeconds); // Speed in MB/s

                                    // display progress bar and speed
                                    Console.Write("\r[{0}{1}] {2}%, {3:0.00} MB/s", new string('#', (int)(progress * 20)), new string(' ', 20 - (int)(progress * 20)), (int)(progress * 100), speed);

                                    watch.Start(); // restart stopwatch for next iteration
                                }
                            }
                        }

                        Console.WriteLine($"\nCopied: {file}");
                        availableData -= fileSize;

                        Console.WriteLine($"Available Data: {availableData}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to copy: {file}. Error: {e.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Not enough data available to download more files.");
                    break;
                }
            }
        }
    }

}
