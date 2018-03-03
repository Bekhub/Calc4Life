﻿using System;
using System.Collections.Generic;
using System.Text;
using Calc4Life.Helpers;
using Calc4Life.Services.FormatServices;
using Prism.Commands;
using Prism.Navigation;

namespace Calc4Life.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        #region Declarations

        private INavigationService _navigationService;
        double sampleValue = 1234567.5555;

        #endregion
        #region Constructors

        public SettingsPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            _navigationService = navigationService;
            SetDefaultCommang = new DelegateCommand(SetDefaultExecute);
            Sample = FormatService.FormatResult(sampleValue);
        }

        #endregion
        #region Binding propeties

        bool _grouppingDigits;
        public bool GroupingDigits
        {
            get { return _grouppingDigits; }
            set
            {
                Settings.GrouppingDigits = value;
                SetProperty(ref _grouppingDigits, Settings.GrouppingDigits);
                Sample = FormatService.FormatResult(sampleValue);
            }
        }

        double _calcAccuracy;
        public double CalcAccuracy
        {
            get { return Settings.CalcAccuracy; }
            set
            {
                Settings.CalcAccuracy = value;
                SetProperty(ref _calcAccuracy, Settings.CalcAccuracy);
                Sample = FormatService.FormatResult(sampleValue);
            }
        }

        string _sample;
        public string Sample
        {
            get { return _sample; }
            set { SetProperty(ref _sample, value); }
        }

        bool _rounding;
        public bool Rounding
        {
            get { return _rounding; }
            set
            {
                Settings.Rounding = value;
                SetProperty(ref _rounding, Settings.Rounding);
                Sample = FormatService.FormatResult(sampleValue);
            }
        }

        //double _stepperMax;
        //public double StepperMax
        //{
        //    get { return CalcAccuracy + 1; }
        //}

        #endregion
        #region Commands

        public DelegateCommand SetDefaultCommang { get; set; }
        private void SetDefaultExecute()
        {
            Settings.GrouppingDigits = true;
            Settings.CalcAccuracy = 3.0;
            Settings.Rounding = false;

            GroupingDigits = Settings.GrouppingDigits;
            CalcAccuracy = Settings.CalcAccuracy;
            Rounding = Settings.Rounding;

            Sample = FormatService.FormatResult(sampleValue);
        }

        #endregion
        #region Navigation

        public override void OnNavigatedTo(NavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            GroupingDigits = Settings.GrouppingDigits;
            CalcAccuracy = Settings.CalcAccuracy;
            Rounding = Settings.Rounding;
        }

        #endregion
    }
}
