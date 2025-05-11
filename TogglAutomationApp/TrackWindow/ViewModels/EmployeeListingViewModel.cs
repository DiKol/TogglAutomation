using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Data;

namespace CollectionViewMVVM.ViewModels
{
    public class EmployeeListingViewModel : ViewModelBase
    {
        private readonly List<EmployeeViewModel> _employeeViewModels;

        public ICollectionView EmployeesCollectionView { get; }

        private string _employeesFilter = string.Empty;
        public string EmployeesFilter
        {
            get
            {
                return _employeesFilter;
            }
            set
            {
                _employeesFilter = value;
                OnPropertyChanged(nameof(EmployeesFilter));
                EmployeesCollectionView.Refresh();
            }
        }

        public EmployeeListingViewModel()
        {
            _employeeViewModels = new List<EmployeeViewModel>();

            foreach (EmployeeViewModel employeeViewModel in GetEmployeeViewModels())
            {
                _employeeViewModels.Add(employeeViewModel);
            }

            EmployeesCollectionView = CollectionViewSource.GetDefaultView(_employeeViewModels);

            EmployeesCollectionView.Filter = FilterEmployees;
            EmployeesCollectionView.SortDescriptions.Add(new SortDescription(nameof(EmployeeViewModel.Name), ListSortDirection.Ascending));
        }

        private bool FilterEmployees(object obj)
        {
            if(obj is EmployeeViewModel employeeViewModel)
            {
                return employeeViewModel.Name.StartsWith(EmployeesFilter, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        private IEnumerable<EmployeeViewModel> GetEmployeeViewModels()
        {
            yield return new EmployeeViewModel("Sean");
            yield return new EmployeeViewModel("Joe");
            yield return new EmployeeViewModel("Abby");
            yield return new EmployeeViewModel("Frank");
            yield return new EmployeeViewModel("Kate");
            yield return new EmployeeViewModel("Adam");
            yield return new EmployeeViewModel("Mary");
            yield return new EmployeeViewModel("Tony");
        }
    }
}
