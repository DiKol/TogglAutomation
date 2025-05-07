using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TogglAutomationApp
{
    /// <summary>
    /// Interaction logic for ProjectListViewItem.xaml
    /// </summary>
    public partial class ProjectListViewItem : UserControl
    {
        public ProjectListViewItem()
        {
            InitializeComponent();
        }

        public void SetProject(WorkspaceProjectDto project)
        {
            ProjectColorEllipse.Fill = new SolidColorBrush(TimerWindow.ColorFromHex(project.Color));
            ProjectLabel.Content = project.Name;
        }
    }
}
