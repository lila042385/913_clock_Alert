// v1.02 20260619 08:20 - Silent single instance with Named Pipe IPC
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PortableAlarmClock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexName = "PortableAlarmClock-SingleInstance-Mutex";
        private const string PipeName = "PortableAlarmClock-SingleInstance-Pipe";
        public static AlarmManager? AlarmManagerInstance { get; private set; }
        private CancellationTokenSource? _pipeCts;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Single Instance Check
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Signal existing instance to show window, then exit silently
                NotifyExistingInstance();
                Shutdown();
                return;
            }

            // 2. Base Directory and Write Permission Check
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!HasWritePermission(baseDir))
            {
                MessageBox.Show($"This application requires write permission to its running folder.{Environment.NewLine}Path: {baseDir}", 
                                "Write Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // 3. Initialize Logger & AlarmManager
            Logger.Initialize(baseDir);
            Logger.Info("App started.");

            ThemeManager.Initialize();

            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            AlarmManagerInstance = new AlarmManager(baseDir);
            AlarmManagerInstance.LoadAlarms();

            // 4. Start Named Pipe listener for second-instance signals
            StartPipeListener();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("App exiting.");
            _pipeCts?.Cancel();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Sends a signal to the existing instance via Named Pipe to bring it to the foreground.
        /// </summary>
        private static void NotifyExistingInstance()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(2000); // 2 second timeout
                using var writer = new StreamWriter(client);
                writer.Write("SHOW");
                writer.Flush();
            }
            catch
            {
                // If pipe connection fails, just exit silently
            }
        }

        /// <summary>
        /// Starts a background Named Pipe server that listens for signals from second instances.
        /// </summary>
        private void StartPipeListener()
        {
            _pipeCts = new CancellationTokenSource();
            var token = _pipeCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                        await server.WaitForConnectionAsync(token);

                        using var reader = new StreamReader(server);
                        string? message = await reader.ReadToEndAsync();

                        if (message == "SHOW")
                        {
                            // Bring main window to foreground on UI thread
                            Dispatcher.Invoke(() =>
                            {
                                var mainWindow = MainWindow;
                                if (mainWindow != null)
                                {
                                    mainWindow.Show();
                                    mainWindow.WindowState = WindowState.Normal;
                                    mainWindow.Activate();
                                    Logger.Info("Brought window to foreground via second instance signal.");
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Pipe listener error.", ex);
                        // Brief delay before retry
                        await Task.Delay(500, token);
                    }
                }
            }, token);

            Logger.Info("Named pipe listener started for single instance IPC.");
        }

        private bool HasWritePermission(string directoryPath)
        {
            try
            {
                string testFile = Path.Combine(directoryPath, Guid.NewGuid().ToString() + ".tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled UI Exception occurred.", e.Exception);
            MessageBox.Show($"An unexpected error occurred:{Environment.NewLine}{e.Exception.Message}", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app crash if possible, but logs are safe
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Error("Unhandled Domain Exception occurred.", ex);
            }
            else
            {
                Logger.Error($"Unhandled Domain Exception occurred (not an exception object): {e.ExceptionObject}");
            }
        }
    }
}
// v1.02 20260619 08:20 - Silent single instance with Named Pipe IPC


