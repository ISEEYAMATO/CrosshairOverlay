using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Shell;

namespace CrosshairOverlay
{
    public partial class App : Application
    {
        public static string[] PendingArgs { get; set; } = Array.Empty<string>();

        private static Mutex? _mutex;
        private static readonly string PipeName = "CrosshairOverlay_IPC";
        private Thread? _pipeThread;
        private bool _ownsMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Single-instance handling
            _mutex = new Mutex(true, "CrosshairOverlay_SingleInstance", out _ownsMutex);
            if (!_ownsMutex)
            {
                // Another instance is running: send command and exit
                try
                {
                    using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    client.Connect(2000);
                    using var w = new StreamWriter(client) { AutoFlush = true };
                    w.WriteLine(string.Join(" ", e.Args));
                }
                catch { }
                Shutdown();
                return;
            }

            // Start IPC server
            _pipeThread = new Thread(PipeServerLoop) { IsBackground = true };
            _pipeThread.Start();

            PendingArgs = e.Args;

            // Jump list
            BuildJumpList();
        }

        private void PipeServerLoop()
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    server.WaitForConnection();
                    using var r = new StreamReader(server);
                    string? line = r.ReadLine();
                    if (line != null)
                    {
                        Dispatcher.Invoke(() => HandleArgs(line.Split(' ')));
                    }
                }
                catch { }
            }
        }

        public static void HandleArgs(string[] args)
        {
            var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            var mw = Current?.MainWindow as MainWindow;
            switch (cmd)
            {
                case "--toggle":
                    mw?.ToggleVisibility();
                    break;
                case "--settings":
                    mw?.OpenSettings();
                    break;
                case "--exit":
                    Current?.Shutdown();
                    break;
                case "--show":
                    if (mw != null) { mw.Settings.Visible = true; mw.ApplySettings(); }
                    break;
                case "--hide":
                    if (mw != null) { mw.Settings.Visible = false; mw.ApplySettings(); }
                    break;
            }
        }

        private void BuildJumpList()
        {
            var jl = new JumpList();
            jl.JumpItems.Add(new JumpTask
            {
                Title = "Toggle Crosshair",
                Arguments = "--toggle",
                Description = "Show or hide the crosshair overlay"
            });
            jl.JumpItems.Add(new JumpTask
            {
                Title = "Settings",
                Arguments = "--settings",
                Description = "Open crosshair settings"
            });
            jl.JumpItems.Add(new JumpTask
            {
                Title = "Exit",
                Arguments = "--exit",
                Description = "Exit the application"
            });
            JumpList.SetJumpList(this, jl);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
            base.OnExit(e);
        }
    }
}
