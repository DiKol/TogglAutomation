using System;
using System.Collections.Generic;
using System.Text;

namespace CollectionViewMVVM.ViewModels
{
    public class EmployeeViewModel : ViewModelBase
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public EmployeeViewModel(string name)
        {
            Name = name;
            
        }
    }
}
