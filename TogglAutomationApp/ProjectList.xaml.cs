using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for ProjectList.xaml
    /// </summary>
    public partial class ProjectList : Window
    {
        private WorkspaceProjectDto[] _project = [];
        public Action<WorkspaceProjectDto>? onClick;
        public ProjectList()
        {
            InitializeComponent();
        }

        public void SetProjectList(WorkspaceProjectDto[] projects)
        {
            _project = projects;
        }

        public void SetSearchTest(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
            {
                Hide();
                return;
            }
            var showList = _project.Where(project => { 
                foreach(char c in text)
                {
                    if (!project.Name.StartsWith(c)) return false;
                }
                return true;
            }).Take(8);

            if (!showList.Any())
            {
                Hide();
                return;
            }
            ListView.Items.Clear();
            foreach(var project in showList)
            {
                var item = new ProjectListViewItem();

                item.SetProject(project);
                item.MouseDown += (s, args) => {
                    if (args.LeftButton != MouseButtonState.Pressed) return;

                    onClick?.Invoke(project);
                };

                ListView.Items.Add(item);
            }
            Show();
        }
    }
}
