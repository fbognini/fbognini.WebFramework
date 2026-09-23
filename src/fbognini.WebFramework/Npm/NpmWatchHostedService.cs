using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Npm
{
    internal sealed class NpmWatchHostedService : IHostedService, IDisposable
    {
        private const int MaxRestarts = 5;

        private readonly bool _enabled;
        private readonly ILogger<NpmWatchHostedService> _logger;
        private readonly string _path;
        private readonly KillOnCloseJobObject? _jobObject;

        private Process? _process;
        private int _restarts;
        private bool _stopping;

        public NpmWatchHostedService(bool enabled, ILogger<NpmWatchHostedService> logger, string path)
        {
            _enabled = enabled;
            _logger = logger;
            _path = path;

            if (enabled)
            {
                _jobObject = KillOnCloseJobObject.Create();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_enabled)
            {
                StartProcess();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping = true;
            KillProcess();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _stopping = true;
            KillProcess();

            _jobObject?.Dispose();
        }

        private void StartProcess()
        {
            var process = new Process();
            process.StartInfo.FileName = Path.Join(Directory.GetCurrentDirectory(), "node_modules/.bin/sass" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ""));
            process.StartInfo.Arguments = $"--watch {_path}";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogInformation(args.Data);
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    _logger.LogError(args.Data);
            };

            process.Exited += HandleProcessExit;

            _process = process;

            process.Start();

            if (_jobObject is not null && !_jobObject.TryAssign(process))
            {
                _logger.LogWarning("Can't assign npm watch to the job object, it may outlive this process");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _logger.LogInformation("Started NPM watch");
        }

        private void KillProcess()
        {
            var process = Interlocked.Exchange(ref _process, null);
            if (process is null)
            {
                return;
            }

            try
            {
                process.Exited -= HandleProcessExit;

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Can't kill npm watch");
            }
            finally
            {
                process.Dispose();
            }
        }

        private async void HandleProcessExit(object? sender, EventArgs args)
        {
            if (_stopping)
            {
                return;
            }

            var process = Interlocked.Exchange(ref _process, null);
            process?.Dispose();

            if (Interlocked.Increment(ref _restarts) > MaxRestarts)
            {
                _logger.LogError("npm watch exited {Restarts} times, giving up", MaxRestarts);
                return;
            }

            _logger.LogWarning("npm watch exited, restarting in 1 second.");

            await Task.Delay(1000);

            if (_stopping)
            {
                return;
            }

            try
            {
                StartProcess();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Can't restart npm watch");
            }
        }
    }
}
