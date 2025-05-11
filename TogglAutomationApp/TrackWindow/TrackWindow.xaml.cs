using CollectionViewMVVM.ViewModels;
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
using System.Windows.Shapes;

namespace TogglAutomationApp.TrackWindow
{
    /// <summary>
    /// Interaction logic for TrackWindow.xaml
    /// </summary>
    public partial class TrackWindow : Window
    {
        public TrackWindow()
        {
            InitializeComponent();

            MouseDown += (e, args) =>
            {
                if (args.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            Topmost = true;
           
        }
    }
}
