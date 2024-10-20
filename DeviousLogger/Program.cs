using Cysharp.Diagnostics;
using R3;

namespace DeviousLogger;

internal static class Program
{
    private static async Task Main()
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "java",
            Arguments = "-jar -Xmx2048m devious-client-launcher-1.0.2.jar --debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        (_, ProcessAsyncEnumerable stdOut, ProcessAsyncEnumerable stdError) = ProcessX.GetDualAsyncEnumerable(processStartInfo);

        var errorBuffer = new List<string>();
        var outputSubject = new Subject<string>();

        outputSubject
            .Where(line => !line.Contains("DEBUG", StringComparison.Ordinal))
            .Select(line => line.AppendCurrentDateTime(DateTime.Now))
            .Subscribe(async line =>
            {
                string? logFileName = null;

                var currentDate = DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                currentDate = currentDate.Replace("/", "-").Replace(" ", "_");

                if (line.Contains("Packet", StringComparison.Ordinal))
                    logFileName = $"packets_{currentDate}.log";
                else if (line.Contains("menuAction", StringComparison.Ordinal))
                    logFileName = $"menuActions_{currentDate}.log";

                if (logFileName is not null)
                    await AppendToLogAsync(logFileName, line).ConfigureAwait(false);
            });

        var consumeStdOut = Task.Run(async () =>
        {
            await foreach (var item in stdOut)
            {
                Console.WriteLine("[STDOUT]: " + item);
                outputSubject.OnNext(item);
            }
        });

        var consumeStdError = Task.Run(async () =>
        {
            await foreach (var item in stdError)
            {
                Console.WriteLine("[STDERROR]: " + item);
                errorBuffer.Add(item);
            }
        });

        try
        {
            await Task.WhenAll(consumeStdOut, consumeStdError).ConfigureAwait(false);
        }
        catch (ProcessErrorException ex)
        {
            Console.WriteLine("[ERROR] ExitCode: " + ex.ExitCode);
            Console.WriteLine(string.Join(Environment.NewLine, errorBuffer));
        }
        finally
        {
            outputSubject.OnCompleted();
        }
    }

    private static async Task AppendToLogAsync(string? fileName, string message)
    {
        if (fileName is not null)
        {
            await using var sw = new StreamWriter(fileName, append: true);
            await sw.WriteLineAsync(message).ConfigureAwait(false);
        }
    }

    private static string AppendCurrentDateTime(this string input, DateTime dateTime)
        => $"{dateTime:yyyy-MM-dd HH:mm:ss} {input}";
}