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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CollectionViewMVVM.Views
{
    /// <summary>
    /// Interaction logic for EmployeeListingView.xaml
    /// </summary>
    public partial class EmployeeListingView : UserControl
    {
        public EmployeeListingView()
        {
            InitializeComponent();
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                ProjectNameLabel.Content = (item.DataContext as EmployeeViewModel).Name;
            }
        }
    }
}
