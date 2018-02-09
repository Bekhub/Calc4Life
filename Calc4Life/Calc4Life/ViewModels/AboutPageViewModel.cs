﻿using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Navigation;


namespace Calc4Life.ViewModels
{
	public class AboutPageViewModel : ViewModelBase
    {
        private INavigationService _navigationService;

        public AboutPageViewModel(INavigationService navigationService) :base(navigationService)
        {
            _navigationService = navigationService;
        }
    }
}