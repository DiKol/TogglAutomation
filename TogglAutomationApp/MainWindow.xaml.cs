using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Net.WebSockets;
using TogglAutomationAppStarter;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using CollectionViewMVVM.ViewModels;

namespace TogglAutomationApp
{
    public partial class MainWindow
    {
        private TrackWindow.TrackWindow? TimerWindow = null;

        private static string SaveFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toggl Automation", "save.txt");
        private NotifyIcon? _notifyIcon;
        private static string? Version = VersionFile.GetVersion();
        private WebSocket? _socket;
        public MainWindow()
        {
            InitializeComponent();

            Closing += (_, _) =>
            {
                Console.WriteLine("Closing");
                TimerWindow?.Close();
                _socket?.Dispose();
            };
            Loaded += (_, _) =>
            {
                if(Version == null)
                {
                    #if !RELEASE
                    Version = "debug";
                    #else
                    MessageBox.Show("Could not get the version of the app please try restarting...");
                    Close();
                    return;
                    #endif
                }

             


            };
        }

        private void OnWsError(string message)
        {
            try
            {
                _socket?.Dispose();
            }
            catch { }

            try
            {
                MessageBox.Show(message);
                Dispatcher.Invoke(() => Close());
            }
            catch
            {

            }
        }

        private void WebSocketMessage(WebSocketMessage message)
        {
            if (_socket == null) return;

            var type = message.Type;
            Console.WriteLine("Message type: {0}", type);

            if (type == "msgBox")
            {
                if (message.Data.ValueKind != JsonValueKind.String) return;

                var text = message.Data.GetString();

                _ = Task.Run(() => MessageBox.Show(text));
                return;
            }
            else if (type == "select")
            {
                if (message.Data.ValueKind != JsonValueKind.Object) return;
                var inCallInfo = message.Data.Deserialize<ProjectSelectInfo>();
                if(inCallInfo == null) return;

                _ = Dispatcher.Invoke(async () =>
                {
                    var result = await DialogWindow.Choose(this, inCallInfo);
                    await _socket.Send("select", result);
                });
            }
            else if (type == "light")
            {
                if (message.Data.ValueKind != JsonValueKind.Object) return;
                var data = JsonSerializer.Deserialize<JsonObject>(message.Data.GetRawText());
                if (data == null) return;

                var color = data["color"]!.GetValue<string>();
                var pusle = data["pulse"]!.GetValue<bool>();
            }
            else if(type == "toast")
            {
                if (message.Data.ValueKind != JsonValueKind.Array) return;
                var elements = message.Data.Deserialize<ToastElement[]>();
                if (elements == null) return;

                ToastNotification.Show(elements);
            }
        }


        private void StartWs(Guid sessionId)
        {
            _socket = new WebSocket(sessionId, Version!, WebSocketMessage, OnWsError);
        }

        #region Login
        private async void LoginButtonClock(object sender, RoutedEventArgs e)
        {
            var email = EmailTb.Text;
            var password = PasswordTb.Text;
            var extension = ExtensionTb.Text;
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please fill in the email");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in the password");
                return;
            }

            if (string.IsNullOrEmpty(extension))
            {
                MessageBox.Show("Please fill in your extension");
                return;
            }
            
            await TryAuth(email, password, extension, RememberCb.IsChecked == true);
        }
        private class SessionIdEndpoint
        {
            public Guid SessionId { get; set; }
        }
        private async Task TryAuth(string email, string password, string extension, bool remember)
        {
            Dispatcher.Invoke(() => IsEnabled = false);

            TimerWindow = new TrackWindow.TrackWindow() { DataContext = new MainViewModel() };
            TimerWindow.Show();
            Hide();
            ShowInTaskbar = false;
            return;
            try
            {
                using var client = new HttpClient();
                Guid sessionId;
                try
                {
/*#if DEBUG
                    const string url = "http://localhost:5006/session/create";
#else*/
                    const string url = "https://luka.getremail.com:8213/session/create";
//#endif



                    using var response = await client.PutAsJsonAsync(url,
                        new
                        {
                            email,
                            password,
                            extension
                        });
                    if (response.StatusCode != HttpStatusCode.OK) throw new Exception();
                    var responseObject = await response.Content.ReadFromJsonAsync<SessionIdEndpoint>();
                    if (responseObject == null)
                    {
                        MessageBox.Show("Wrong email/password try again");
                        return;
                    }

                    sessionId = responseObject.SessionId;
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Wrong email/password try again\n{ex.Message}");
                    return;
                }

                try
                {
                    if (!remember)
                    {
                        if (File.Exists(SaveFile))
                        {
                            File.Delete(SaveFile);
                        }
                    }
                    else
                    {
                        File.WriteAllLines(SaveFile, [email, password, extension]);
                    }
                }
                catch { 
                    Console.WriteLine("e1 continue");
                }

                Hide();
                ShowInTaskbar = false;
                _notifyIcon = new NotifyIcon
                {
                    Text = "Toggl Task Automator",
                    Visible = true
                };
                _notifyIcon.ContextMenuStrip = new ContextMenuStrip()
                {
                    Items =
                    {
                        new ToolStripMenuItem("Sign out", null, (_, _) =>
                        {
                            _notifyIcon.Dispose();
                            _notifyIcon = null;
                            ShowInTaskbar = true;
                            EmailTb.Text = "";
                            PasswordTb.Text = "";
                            RememberCb.IsChecked = false;
                            Close();
                        }),
                        new ToolStripMenuItem("Exit", null, (_, _) => Close())
                    }
                };
                _notifyIcon.Icon = SystemIcons.Application;
                Console.WriteLine("Starting ws");
                StartWs(sessionId);

                TimerWindow = new TrackWindow.TrackWindow() { DataContext = new MainViewModel() };
                TimerWindow.Show();

                Console.WriteLine("t");
            }
            finally
            {
                Dispatcher.Invoke(() => IsEnabled = true);
            }
        }
        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(SaveFile)) return;

            var lines = await File.ReadAllLinesAsync(SaveFile);
            if (lines.Length != 3) return;

            var email = lines[0];
            var password = lines[1];
            var extension = lines[2];
            await TryAuth(email, password, extension, true);
        }
        #endregion
    }
}