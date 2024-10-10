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
            Arguments = "-jar -Xmx2048m devious-client-launcher.jar --debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        (_, ProcessAsyncEnumerable stdOut, ProcessAsyncEnumerable stdError) = ProcessX.GetDualAsyncEnumerable(processStartInfo);

        var errorBuffer = new List<string>();
        var outputSubject = new Subject<string>();

        Observable<string> tradeReqObservable = outputSubject
            .Where(line => line.Contains("TRADEREQ", StringComparison.Ordinal))
            .Select(line => line.Replace("SEL? [Client] DEBUG injected-client - Chat message type TRADEREQ: ", string.Empty));

        var currentDate = DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
        currentDate = currentDate.Replace("/", "-").Replace(" ", "_");

        tradeReqObservable
            .Where(line => line.Contains("Packet", StringComparison.Ordinal))
            .Subscribe(async packet =>
            {
                await AppendToLogAsync($"packets_{currentDate}.log", packet).ConfigureAwait(false);
            });

        tradeReqObservable
            .Where(line => line.Contains("menuAction", StringComparison.Ordinal))
            .Subscribe(async menuAction =>
            {
                await AppendToLogAsync($"menuActions_{currentDate}.log", menuAction).ConfigureAwait(false);
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

    private static async Task AppendToLogAsync(string fileName, string message)
    {
        await using var sw = new StreamWriter(fileName, append: true);
        await sw.WriteLineAsync(message).ConfigureAwait(false);
    }
}