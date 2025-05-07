using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TogglAutomationApp;

public partial class DialogWindow : Window
{
    private static DialogWindow? _currentWindow;
    private readonly TaskCompletionSource<ReplyObject> _completionSource = new();
    public ProjectSelectInfo ProjectSelectInfo = new();
    public class ReplyObject
    {
        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("option")]
        public int Option { get; set; }

        public ReplyObject(int option, int? pId)
        {
            ProjectId = pId;
            Option = option;
        }
    }
    private static readonly ReplyObject DoNothingReply = new(2, null);
    public DialogWindow()
    {
        InitializeComponent();
        Loaded += (s, args) => 
        {
            SetBorderColorsAndText(ProjectSelectInfo.InCall, ForCallBorder, ForCallLabel);
            SetBorderColorsAndText(ProjectSelectInfo.BeforeCall, BeforeCallBorder, BeforeCallLabel);

            SetBorderActions(BeforeCallBorder, new ReplyObject(0, ProjectSelectInfo.BeforeCall?.ProjectId));
            SetBorderActions(ForCallBorder, new ReplyObject(1, ProjectSelectInfo.InCall?.ProjectId));
            SetBorderActions(DoNothingBorder, DoNothingReply);
        };

        Closing += (_, _) => _completionSource.TrySetResult(DoNothingReply);
    }

    private void SetBorderActions(Border border, ReplyObject value)
    {
        if (!border.IsEnabled) return;

        border.MouseEnter += (_, _) => 
        {
            Cursor = System.Windows.Input.Cursors.Hand;
            border.Opacity = 0.7;
        };
        border.MouseLeave += (_, _) =>
        {
            Cursor = System.Windows.Input.Cursors.Arrow;
            border.Opacity = 1;
        };

        border.MouseUp += (s, args) =>
        {
            if(args.LeftButton == System.Windows.Input.MouseButtonState.Released)
            {
                SetResult(value);
            }
        };
    }

    private void SetBorderColorsAndText(Project? project, Border border, Label label)
    {
        string color = project?.Color ?? "#808080";
        var colorInstance = (Color)ColorConverter.ConvertFromString(color);
        var brush = new SolidColorBrush(colorInstance);
        border.BorderBrush = brush;
        label.Foreground = brush;
        label.Content = project?.Name ?? "";
        border.IsEnabled = project != null;
    }

    public static Task<ReplyObject> Choose(Window owner, ProjectSelectInfo projectSelectInfo)
    {
        _currentWindow?.SetResult(DoNothingReply);
        _currentWindow = new DialogWindow
        {
            Owner = owner,
            Topmost = true,
            ProjectSelectInfo = projectSelectInfo
        };
        _currentWindow.Show();
        return _currentWindow._completionSource.Task;
    }

    private void SetResult(ReplyObject result)
    {
        _completionSource.TrySetResult(result);
        Close();
    }
}