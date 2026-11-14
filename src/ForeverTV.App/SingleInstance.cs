using System.IO.Pipes;
using System.Text;

namespace ForeverTV.App;

/// <summary>
/// One ForeverTV per Windows user. A second start hands its arguments (a video link) to the running one over a
/// named pipe and exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    /// <summary>The argument the updater restarts the app with.</summary>
    public const string AfterUpdate = "--after-update";

    private static readonly string Name = "ForeverTV-" + Environment.UserName;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>Null when another instance runs; the arguments were then passed to it.</summary>
    public static SingleInstance? TryStart(string[] args)
    {
        var mutex = new Mutex(true, @"Local\" + Name, out var first);
        if (first) return new SingleInstance(mutex);
        // The build that an update has just put in place starts while the old one is still exiting: wait for it.
        if (args.Contains(AfterUpdate))
        {
            try
            {
                if (mutex.WaitOne(TimeSpan.FromSeconds(20))) return new SingleInstance(mutex);
            }
            catch (AbandonedMutexException)
            {
                return new SingleInstance(mutex);
            }
        }
        mutex.Dispose();
        try
        {
            using var pipe = new NamedPipeClientStream(".", Name, PipeDirection.Out);
            pipe.Connect(3000);
            using var writer = new StreamWriter(pipe, Encoding.UTF8);
            writer.WriteLine(string.Join("\u001f", args));
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or UnauthorizedAccessException)
        {
            // The running instance is closing; nothing to hand over.
        }
        return null;
    }

    /// <summary>Calls back (on a pool thread) with the arguments of every later start.</summary>
    public void Listen(Action<string[]> received)
    {
        _ = Task.Run(async () =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = new NamedPipeServerStream(Name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(_stop.Token);
                    using var reader = new StreamReader(pipe, Encoding.UTF8);
                    var line = await reader.ReadLineAsync(_stop.Token);
                    if (line is not null) received(line.Split('\u001f', StringSplitOptions.RemoveEmptyEntries));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (IOException)
                {
                    // A client that gave up; wait for the next one.
                }
            }
        });
    }

    public void Dispose()
    {
        _stop.Cancel();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
