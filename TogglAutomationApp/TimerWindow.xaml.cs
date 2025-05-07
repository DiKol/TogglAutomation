using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TogglAutomationApp
{
    /// <summary>
    /// Interaction logic for TimerWindow.xaml
    /// </summary>
    public partial class TimerWindow : Window
    {
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static TimeZoneInfo AmsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        private CurrentProjectInfoDto? CurrentProject = null;
        private long WorkspaceId = 0;
        private HttpClient HttpClient { get; }
        private WorkspaceProjectDto[] Projects { get; set; } = [];
        private CancellationTokenSource CancelRefershSource = new CancellationTokenSource();
        public bool IsTextBoxFocused = false;
        public ProjectList ProjectListView = new ProjectList();
        public TimerWindow(string email, string password)
        {
            InitializeComponent();
            MouseDown += (e, args) =>
            {
                if (args.LeftButton == MouseButtonState.Pressed) DragMove();
            };
            Closing += (s, args) => CancelRefershSource.Cancel();

            HttpClient = new()
            {
                BaseAddress = new Uri("https://api.track.toggl.com")
            };
            HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{password}")));

            Loaded += OnLoaded;

            SearchTextBox.GotFocus += (s, args) => {
                IsTextBoxFocused = true;
                ProjectListView.SetSearchTest(SearchTextBox.Text);
            };

            SearchTextBox.LostFocus += (s, args) => {
                IsTextBoxFocused = false;
                ProjectListView.SetSearchTest("");
            };

            SearchTextBox.TextChanged += (s, args) =>
            {
                var search = SearchTextBox.Text;
                Console.WriteLine($"Setting {search}");
                ProjectListView.SetSearchTest(search);
            };

            ProjectListView.onClick = (project) => StartProject(project);

            this.LocationChanged += (s, e) => {
                //KeepWindowOnTop(); // Ensures it stays on top after being moved
                Topmost = true;

                ProjectListView.Left = Left + 20;
                ProjectListView.Top = Top + Height;
                ProjectListView.Topmost = true;
            };
        }

        private void KeepWindowOnTop()
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private class CurrentProjectInfoDto
        {
            [JsonPropertyName("project_id")]
            public long ProjectId { get; set; }

            public DateTime Start { get; set; }

            public long Id { get; set; }
        }

        string FormatTime(int x) => x < 10 ? $"0{x}" : x.ToString();    

        private void SetProjectInfo(CurrentProjectInfoDto? project, bool fromCheckup = false)
        {
            if(project == null)
            {
                TimerLabel.Content = "";
            }
            else
            {
                DateTime amsterdamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AmsterdamTimeZone);
                Console.WriteLine("=====");
                Console.WriteLine(amsterdamTime);
                Console.WriteLine(project.Start);
                int totalSecInt = (int)(amsterdamTime - TimeSpan.FromHours(6) - project.Start).TotalSeconds;
                var hours = totalSecInt / 3600;
                var mins = (totalSecInt % 3600) / 60;
                var secs = totalSecInt % 60;
                string label = $"{FormatTime(mins)}:{FormatTime(secs)}";
                if(hours > 0)
                {
                    label = FormatTime(hours) + ":" + label;
                }
                TimerLabel.Content = label;
            }

            WorkspaceProjectDto? projectInfo = project == null ? null : Projects.FirstOrDefault(x => x.Id == project.ProjectId);

            ProjectNameLabel.Content = projectInfo?.Name ?? "No Project";
            Brush brush;
            if(projectInfo != null) brush = new SolidColorBrush(ColorFromHex(projectInfo.Color));
            else brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            

            ProjectColorEllipse.Fill = brush;
            ProjectNameLabel.Foreground = brush;


            StartStopButton.Content = projectInfo == null ? "Start" : "Stop";
            CurrentProject = project;
            if (!fromCheckup && project == null)
            {
                //ProjectListCombobox.SelectedIndex = -1;
            }
        }

        private async Task RefreshTask()
        {
            
            try
            {
                while (!CancelRefershSource.IsCancellationRequested)
                {
                    try
                    {
                        var currentTimer = await HttpClient.GetFromJsonAsync<CurrentProjectInfoDto>("api/v9/me/time_entries/current");
                        Dispatcher.Invoke(() => SetProjectInfo(currentTimer, true));
                    }
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine("Failed to refresh " + ex.ToString());
                    }

                    await Task.Delay(1000, CancelRefershSource.Token);
                }
            }
            catch(TaskCanceledException)
            {
                return;
            }
            catch(Exception)
            {
                MessageBox.Show("Refresh timer crashed please restart the app");
                Environment.Exit(0);
            }
        }

        public static Color ColorFromHex(string hex)
        {
            System.Drawing.Color projectColor = System.Drawing.ColorTranslator.FromHtml(hex);
            return Color.FromArgb(255, projectColor.R, projectColor.G, projectColor.B);
        }

        private void PopulateCombobox()
        {
            ProjectListView.SetProjectList(Projects);
        }

        private class WorkspaceRequestDto { public long Id { get; set; } }
        private async void OnLoaded(object _, RoutedEventArgs _1)
        {
            try
            {
                var workspaces = await HttpClient.GetFromJsonAsync<WorkspaceRequestDto[]>("api/v9/me/workspaces");
                if (workspaces == null || workspaces.Length == 0) throw new Exception();


                WorkspaceId = workspaces[0].Id;

                Projects = (await HttpClient.GetFromJsonAsync<WorkspaceProjectDto[]>($"api/v9/workspaces/{WorkspaceId}/projects"))!.Where(x => x.Active && !x.Archived).ToArray();
                if (Projects == null) throw new Exception();

                Dispatcher.Invoke(PopulateCombobox);
                RefreshTask();
            }
            catch
            {
                MessageBox.Show("Startup of the timer part failed please restart the app");
                Environment.Exit(0);
            }
        }

        async void StartProject(WorkspaceProjectDto projectDto)
        {
            var response = await HttpClient.PostAsJsonAsync($"api/v9/workspaces/{WorkspaceId}/time_entries", new
            {
                created_with = "Toggl Windows App Bar",
                duration = -1,
                project_id = projectDto.Id,
                start = DateTime.UtcNow,
                workspace_id = WorkspaceId
            });
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Failed to start the timer: " + await response.Content.ReadAsStringAsync());
            }
            else
            {
                var currentProjectInfo = await response.Content.ReadFromJsonAsync<CurrentProjectInfoDto>();
                SetProjectInfo(currentProjectInfo);
            }
        }

        private async void StartStopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (CurrentProject == null)
                {
                    return;
                }
                else
                {
                    var request = new HttpRequestMessage(HttpMethod.Patch, $"api/v9/workspaces/{WorkspaceId}/time_entries/{CurrentProject.Id}/stop");
                    var response = await HttpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        SetProjectInfo(null);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("An error occured " + ex.Message);
            }
        }
    }
}

public class WorkspaceProjectDto
{
    public long Id { get; set; }
    public bool Active { get; set; }
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
    public bool Archived { get; set; } = false;
}