using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;

namespace ControlBee.Services;

public sealed class ThreadHealthMonitor : IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger("ThreadHealth");
    private static ThreadHealthMonitor? _instance;
    private static readonly object StartLock = new();

    private readonly int _intervalMs;
    private readonly int _thresholdMs;
    private readonly Thread _thread;
    private readonly Process _process = Process.GetCurrentProcess();
    private volatile bool _running = true;

    private double _lastProcessCpu = -1;
    private double _lastSystemCpu = -1;

    public ThreadHealthMonitor(int intervalMs = 200, int thresholdMs = 500)
    {
        _intervalMs = intervalMs;
        _thresholdMs = thresholdMs;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "ThreadHealthMonitor",
            Priority = ThreadPriority.Highest,
        };
    }

    public static void EnsureStarted()
    {
        if (_instance != null)
            return;
        lock (StartLock)
        {
            if (_instance != null)
                return;
            _instance = new ThreadHealthMonitor();
            _instance.Start();
        }
    }

    public void Start()
    {
        _thread.Start();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out long idleTime,
        out long kernelTime,
        out long userTime
    );

    private void Run()
    {
        Logger.Info(
            $"ThreadHealthMonitor started (interval {_intervalMs}ms, threshold {_thresholdMs}ms)."
        );
        var cores = Environment.ProcessorCount;
        var stopwatch = Stopwatch.StartNew();
        var lastWall = stopwatch.ElapsedMilliseconds;
        _process.Refresh();
        var lastProcTime = _process.TotalProcessorTime;
        var haveSystemTimes = GetSystemTimes(
            out var lastIdle,
            out var lastKernel,
            out var lastUser
        );

        while (_running)
        {
            Thread.Sleep(_intervalMs);

            var nowWall = stopwatch.ElapsedMilliseconds;
            var elapsed = nowWall - lastWall;
            var overrun = elapsed - _intervalMs;

            _process.Refresh();
            var procTime = _process.TotalProcessorTime;
            var procDelta = (procTime - lastProcTime).TotalMilliseconds;
            var processCpu = elapsed > 0 ? procDelta / (elapsed * cores) * 100.0 : 0;

            var systemCpu = -1.0;
            if (haveSystemTimes && GetSystemTimes(out var idle, out var kernel, out var user))
            {
                var idleDelta = idle - lastIdle;
                var totalDelta = kernel - lastKernel + (user - lastUser);
                if (totalDelta > 0)
                    systemCpu = (1.0 - (double)idleDelta / totalDelta) * 100.0;
                lastIdle = idle;
                lastKernel = kernel;
                lastUser = user;
            }

            if (overrun > _thresholdMs)
            {
                var saturated = systemCpu >= 90.0 ? " [system CPU saturated]" : string.Empty;
                Logger.Warn(
                    $"Process stalled ~{overrun}ms (slept {_intervalMs}ms, actual {elapsed}ms). "
                        + $"CPU across stall: process {processCpu:F0}%, system {Format(systemCpu)}{saturated}. "
                        + $"Last CPU before stall: process {Format(_lastProcessCpu)}, system {Format(_lastSystemCpu)}."
                );
            }

            _lastProcessCpu = processCpu;
            _lastSystemCpu = systemCpu;
            lastWall = nowWall;
            lastProcTime = procTime;
        }
    }

    private static string Format(double cpu)
    {
        return cpu < 0 ? "n/a" : $"{cpu:F0}%";
    }

    public void Dispose()
    {
        _running = false;
        if (_thread.IsAlive)
            _thread.Join(_intervalMs * 2);
    }
}
