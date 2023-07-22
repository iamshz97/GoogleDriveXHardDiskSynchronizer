using GoogleDriveXHardDiskSynchronizer;
using Microsoft.Extensions.Configuration;
using System.IO;

var builder = new ConfigurationBuilder()
.SetBasePath(Directory.GetCurrentDirectory())
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfiguration configuration = builder.Build();

var downloader = new Downloader(configuration);
downloader.CheckAndDownloadFilesAsync().Wait();
