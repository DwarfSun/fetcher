using System.CommandLine.Invocation;
using System.CommandLine;
using Fetcher.EventHandlers;
using Microsoft.Extensions.Hosting;
using Fetcher.Library;
using System.Text;
using System.Text.Json;
using Fetcher;

static class Program 
{
    public static Ticker? StatusUpdates;
    public static AzureBlobFile? AzureBlobFile;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        
        if (args.Length < 2)
        {
        #if DEBUG
            args = [
                "--url", "",
                "--path", "D:\\",
                "--threads", "8"
                ];
        #else
            Console.WriteLine("Usage: Fetcher.exe --url <url> [--threads <num> --path <path> --key <key>]");
            return 1;
        #endif
        }
        //Console.Clear();

        Task Downloader = Task.Run(async () => 
        {
            await DownloadWithResume(args);
        });

        var builder = Host.CreateDefaultBuilder(args)
            .UseRazorConsole<Fetcher.Components.Main>()
            ;
        var host = builder.Build();

        Task Display = host.RunAsync();

        await Downloader.WaitAsync(CancellationToken.None);
        
        await Task.Delay(StatusUpdates?.milliseconds ?? 1000);

        return 0;
    }

    static async Task DownloadWithResume(string[] args)
    {
        string? blobUri = null;
        string? localPath = null;
        string? accountKey = null;
        int threads = Environment.ProcessorCount * 32;
        bool debugLogEnabled = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLowerInvariant().Equals("--debug")) 
                debugLogEnabled = true;
            else if (i < args.Length - 1)
            switch (args[i].ToLowerInvariant())
            {
                case "--url":
                    blobUri = args[++i];
                    break;
                case "--path":
                    localPath = args[++i];
                    break;
                case "--key":
                    accountKey = args[++i];
                    break;
                case "--threads":
                    _ = int.TryParse(args[++i], out threads);
                    break;
                default:
                    break;
            }
        }

        StatusUpdates = new();

        AzureBlobFile = new (
            uri: new Uri(blobUri ?? throw new NullReferenceException()),
            localPath: localPath,
            accountKey: accountKey,
            threads: threads,
            writeDebugJson: debugLogEnabled
        );

        try 
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                Woke.PreventSleep();

            await AzureBlobFile.DownloadAsync();
        }
        catch (Exception e)
        {
            await File.WriteAllTextAsync(
                Path.Combine(Environment.CurrentDirectory, "Error", $"{DateTime.Now : yyyyMMddHHmm}", ".log"),
                JsonSerializer.Serialize(e, Global.JsonSerializerOptions));
        }
        finally
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                Woke.ResumeSleepHabits();
        }
    }
}

//