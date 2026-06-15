using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Dev-only convenience: when the API starts under the debugger, also bring up the
/// Vite frontend dev server (if it isn't already running) and open the browser to
/// it, so a single F5 launches the whole stack. The Vite server proxies "/api" to
/// this API, so the opened URL is the app. Disable with Frontend:AutoLaunch=false.
/// </summary>
public class FrontendDevServerLauncher : IHostedService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<FrontendDevServerLauncher> _logger;
    private Process? _process;

    public FrontendDevServerLauncher(
        IConfiguration config, IHostEnvironment env, ILogger<FrontendDevServerLauncher> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.GetValue("Frontend:AutoLaunch", true))
        {
            return Task.CompletedTask;
        }

        var url = _config.GetValue<string>("Frontend:DevUrl") ?? "http://localhost:5273";
        var port = new Uri(url).Port;

        // Don't block startup — set everything up on a background task.
        _ = Task.Run(async () =>
        {
            try
            {
                var ready = await IsListeningAsync(port);
                if (ready)
                {
                    _logger.LogInformation("Frontend dev server already running on {Url}.", url);
                }
                else
                {
                    var root = FindRepoRoot();
                    if (root is null)
                    {
                        _logger.LogWarning("Could not locate the frontend (package.json) — skipping auto-launch.");
                        return;
                    }
                    StartViteServer(root);
                    // Vite's first cold start (esbuild dep optimization) can take a while.
                    ready = await WaitUntilListeningAsync(port, TimeSpan.FromSeconds(90));
                }

                if (ready)
                {
                    OpenBrowser(url);
                }
                else
                {
                    _logger.LogWarning(
                        "Frontend dev server did not become reachable on {Url}. Check the [vite] output above " +
                        "(run 'npm install' in {Root} if a dependency is missing), then open it manually.",
                        url, FindRepoRoot() ?? "the repo root");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Frontend dev-server auto-launch failed (non-fatal).");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // best-effort; a surviving dev server is simply reused next run
        }
        return Task.CompletedTask;
    }

    private void StartViteServer(string workingDirectory)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "npm",
            Arguments = isWindows ? "/c npm run dev" : "run dev",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        _process = Process.Start(psi);
        if (_process is null)
        {
            _logger.LogWarning("Failed to start the Vite dev server process.");
            return;
        }

        // Surface Vite's own output (ready banner / errors) in the API logs so a
        // failed cold start is diagnosable instead of silent.
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) _logger.LogInformation("[vite] {Line}", e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _logger.LogWarning("[vite] {Line}", e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _logger.LogInformation("Started Vite frontend dev server (npm run dev) in {Dir}.", workingDirectory);
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "package.json"))
                && File.Exists(Path.Combine(dir.FullName, "vite.config.ts")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static async Task<bool> IsListeningAsync(int port)
    {
        // Probe both loopback stacks — Vite often binds IPv6 (::1) on Windows, and
        // checking only 127.0.0.1 would miss it and trigger a duplicate spawn.
        foreach (var host in new[] { "127.0.0.1", "::1" })
        {
            try
            {
                using var client = new TcpClient();
                var connect = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connect, Task.Delay(800));
                if (completed == connect && !connect.IsFaulted && client.Connected)
                {
                    return true;
                }
            }
            catch
            {
                // try the next stack
            }
        }
        return false;
    }

    private static async Task<bool> WaitUntilListeningAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsListeningAsync(port)) return true;
            await Task.Delay(500);
        }
        return false;
    }

    private void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.LogInformation("Opened browser at {Url}.", url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open the browser automatically; open {Url} manually.", url);
        }
    }
}
