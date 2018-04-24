﻿using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using Calc4Life.Services.RepositoryServices;
using System.Collections.ObjectModel;
using Calc4Life.Models;
using Xamarin.Forms;
using Prism.Services;
using System.Diagnostics;
using Calc4Life.Services.PurchasingServices;
using Plugin.InAppBilling.Abstractions;

namespace Calc4Life.ViewModels
{
    public class ConstantsPageViewModel : ViewModelBase
    {
        #region Declarations

        IConstantsRepositoryService _constantRepository;
        IPageDialogService _dialogService;
        PurchasingService _purchasingService;
        ObservableCollection<Constant> _constants;
        Constant _selectedConstant;

        #endregion

        #region Constructors
        public ConstantsPageViewModel(INavigationService navigationService,
            IConstantsRepositoryService constantsRepository,
            IPageDialogService dialogService,
            PurchasingService purchasingService)
           : base(navigationService)
        {
            Title = "Constants";

            //_constantRepository = constantsRepository;
            _constantRepository = App.Database;
            _purchasingService = purchasingService;
            _dialogService = dialogService;

            NavigateToAddCommand = new DelegateCommand(NavigateToAddExecute);
            NavigateToEditCommand = new DelegateCommand(NavigateToEditExecute, ActionCanExecute);
            NavigateToCalcCommand = new DelegateCommand(NavigateToCalcExecute, ActionCanExecute);
            DeleteCommand = new DelegateCommand(DeleteExecute, ActionCanExecute);

            MessagingCenter.Subscribe<ConstantsRepositoryServiceFake>(this, "deleted", ConstantDeleted);
        }

        #endregion

        #region Commands

        public DelegateCommand NavigateToAddCommand { get; }
        private async void NavigateToAddExecute()
        {
            bool isPurchased = _purchasingService.IsPurchasedItemSaved("constants_unblocked");

            if (Constants.Count < 3)
                await NavigationService.NavigateAsync("EditConstPage", null, false, true);
            else
            {
                if (isPurchased)
                    await NavigationService.NavigateAsync("EditConstPage", null, false, true);
                else
                {
                    //string title = "Purchasing";
                    //string message = $"Do You want to buy the item?\r\n Proceeding?";
                    //var answer = await _dialogService.DisplayAlertAsync(title, message, "Yes", "No");

                    //if (answer == true)
                    //    await NavigationService.NavigateAsync("OptionsPage?selectedTab=SettingsPage", null, false, true);
                    bool success;
                    try
                    {
                        success = await _purchasingService.PurchaseConsumableItem("constants_unblocked", "payload");
                        if (success)
                            App.Current.Properties["constants_unblocked"] = "constants_unblocked";
                    }
                    catch (InAppBillingPurchaseException)
                    { }
                    catch (Exception)
                    {}
                }
            }
        }

        public DelegateCommand NavigateToEditCommand { get; }
        private async void NavigateToEditExecute()
        {

            var par = new NavigationParameters();
            par.Add("edit", SelectedConstant);
            await NavigationService.NavigateAsync("EditConstPage", par, false, true);

        }

        public DelegateCommand DeleteCommand { get; }
        public async void DeleteExecute()
        {
            string title = "WARNING!";
            string message = $"Your are about deleting \"{SelectedConstant.Name}\" \r\n\r\nDelete?";
            var answer = await _dialogService.DisplayAlertAsync(title, message, "Yes", "No");

            if (answer == true)
            {
                await _constantRepository.DeleteAsync(SelectedConstant);
                Constants.Remove(SelectedConstant);
            }
        }

        public DelegateCommand NavigateToCalcCommand { get; }
        private async void NavigateToCalcExecute()
        {
            var navigationParams = new NavigationParameters();
            navigationParams.Add("const", SelectedConstant);
            await NavigationService.GoBackAsync(navigationParams);
        }
        private bool ActionCanExecute() => (SelectedConstant != null);

        #endregion

        #region Navigation
        public async override void OnNavigatingTo(NavigationParameters parameters)
        {
            var list = await _constantRepository.GetItemsAsync();
            Constants = new ObservableCollection<Constant>(list);
        }

        #endregion

        #region Bindable properties

        public ObservableCollection<Constant> Constants
        {
            get { return _constants; }
            set { SetProperty(ref _constants, value); }
        }

        public Constant SelectedConstant
        {
            get { return _selectedConstant; }
            set
            {
                SetProperty(ref _selectedConstant, value);

                DeleteCommand.RaiseCanExecuteChanged();
                NavigateToEditCommand.RaiseCanExecuteChanged();
                NavigateToCalcCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Messages actions
        /// <summary>
        /// message "deleted" from IConstRepositoryService
        /// </summary>
        private void ConstantDeleted(ConstantsRepositoryServiceFake sender)
        {
            Constants.Remove(SelectedConstant);
            SelectedConstant = null;

            //_dialogService.DisplayAlertAsync("SUCCESS!", "Constant deleted", "Close");
        }

        #endregion

    }
}
