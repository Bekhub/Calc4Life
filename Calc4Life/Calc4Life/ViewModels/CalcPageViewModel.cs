using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Calc4Life.Services;
using System.Globalization;
using Calc4Life.Models;
using Prism.Services;
using Calc4Life.Services.OperationServices;
using Calc4Life.Services.FormatServices;
using Calc4Life.Helpers;
using Calc4Life.Services.RepositoryServices;
using Xamarin.Forms;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using Plugin.Vibrate;
using Calc4Life.Services.PurchasingServices;


namespace Calc4Life.ViewModels
{
    public class CalcPageViewModel : ViewModelBase
    {
        #region Declarations
        List<Constant> Constants;
        const int maxFiguresNumber = 13; // максимальное число ВВОДИМЫХ ЦИФР С УЧЕТОМ ДЕСЯТИЧНОГО ЗНАКА (один знак зарезервирован под возможный МИНУС)
        const int VIBRATE_DURATION = 50;
        string decimalSeparator; // десятичный знак числа

        //flags
        bool canBackSpace; // флаг - возможно ли редактирование дисплея кнопкой BackSpace
        bool canChangeSign; //флаг - возможно ли изменение знака числа
        bool mustClearDisplay; // флаг - необходимо ли очистить дисплей перед вводом

        //current values
        decimal? registerOperand; // текущий операнд
        decimal? registerMemory; // значение ячейки памяти


        //services
        IPageDialogService _dialogService;
        IBinaryOperationService _binaryOperation;
        FormatService _formatService;
        DedicationService _dedicationService;
        IConstantsRepositoryService _constantsRepository;
        ConstantsPurchasingService _purchasingService;

        #endregion

        #region Constructors

        public CalcPageViewModel(INavigationService navigationService,
            IPageDialogService dialogService,
            IBinaryOperationService binaryOperationService,
            FormatService formatService,
            DedicationService dedicationService,
            ConstantsPurchasingService purchasingService)
            : base(navigationService)
        {
            _dialogService = dialogService;
            _binaryOperation = binaryOperationService;
            _formatService = formatService;
            _dedicationService = dedicationService;
            _constantsRepository = App.Database;
            _purchasingService = purchasingService;

            //defaults
            Title = "Calculator for Life";
            Display = "0";
            canBackSpace = true;
            canChangeSign = true;
            mustClearDisplay = false;

            DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            ConstCommand = new DelegateCommand(ConstCommandExecute);
            OptionsCommand = new DelegateCommand(OptionsCommandExecute);
            EnterFiguresCommand = new DelegateCommand<string>(EnterFiguresExecute);
            BackSpaceCommand = new DelegateCommand(BackSpaceExecute);
            EnterOperatorCommand = new DelegateCommand<string>(EnterOperatorExecute);
            CalcCommand = new DelegateCommand(CalcExecute);
            SignCommand = new DelegateCommand(SignExecute);
            MemoryCommand = new DelegateCommand<string>(MemoryExecute);
            AddConstantCommand = new DelegateCommand(AddConstExecute);
            ClearCommand = new DelegateCommand(ClearExecute);
            NaigateToDedicationCommand = new DelegateCommand(NavigateToDedicationExecute);

            //подписываемся на событие изменения настроек калькулятора, для того чтобы отформатировать Display 
            //на основе новых настроек
            MessagingCenter.Subscribe<SettingsPageViewModel>(this, AppConstants.SETTINGS_CHANGED_MESSAGE, (settingsVm) => UpdateDisplayText());


            var propertyChangedObservable = Observable.FromEventPattern<PropertyChangedEventArgs>(this, nameof(PropertyChanged));
            var displayChangedObservale = propertyChangedObservable.Where(r => r.EventArgs.PropertyName == nameof(Display)).Select(d => Display);

            var con = new ConstantSuggestionService();
            var obs = con.SuggestionsObservable(displayChangedObservale);
            obs.Subscribe(l => SuggestionConstants = new ObservableCollection<Constant>(l));
          
            //displayChangedObservale.ObserveOn(SynchronizationContext.Current)
            //    .DistinctUntilChanged()
            //    .Subscribe(async s =>
            //   {
            //       if (!isConstantSuggestionsUpdated)
            //       {
            //           _constants = await _constantsRepository.GetItemsAsync();
            //           _constants.ForEach(c => { if (c.Name.Count() > 27) c.Name = c.Name.Substring(0, 27) + "..."; });
            //           isConstantSuggestionsUpdated = true;
            //       }
            //       SuggestionConstants = new ObservableCollection<Constant>(_constants?.Where(c => c.Value.ToString().StartsWith(s)));
            //   });
        }
        #endregion

        #region Bindable Properties
        private List<Constant> _constants;
        private ObservableCollection<Constant> _suggestionConstants;
        public ObservableCollection<Constant> SuggestionConstants
        {
            get => _suggestionConstants;
            set => SetProperty(ref _suggestionConstants, value);
        }
        private Constant _selectedSuggestionConstant;
        public Constant SelectedSuggestionConstant
        {
            get => _selectedSuggestionConstant;
            set
            {
                if (SetProperty(ref _selectedSuggestionConstant, value))
                    SetSuggestedConstant(value);

            }
        }
        string _Display;
        public string Display
        {
            get { return _Display; }
            set { SetProperty(ref _Display, value); }
        }

        string _Expression;
        public string Expression
        {
            get { return _Expression; }
            set { SetProperty(ref _Expression, value); }
        }

        string _Memory;
        public string Memory
        {
            get { return _Memory; }
            set { SetProperty(ref _Memory, value); }
        }

        bool _IsMemoryVisible;
        public bool IsMemoryVisible
        {
            get { return _IsMemoryVisible; }
            set { SetProperty(ref _IsMemoryVisible, value); }
        }

        public string DecimalSeparator
        {
            get { return decimalSeparator; }
            set { SetProperty(ref decimalSeparator, value); }
        }

        bool _IsRounding;
        public bool IsRounding
        {
            get { return Settings.Rounding; }
            set { SetProperty(ref _IsRounding, value); }
        }
        string _dedication;
        public string Dedication
        {
            get { return _dedication; }
            set { SetProperty(ref _dedication, value); }
        }

        #endregion

        #region Commands
        private void SetSuggestedConstant(Constant selectedConstant)
        {
            if (selectedConstant == null) return;
            //TODO вывести в отдельную процедуру, данный код повторяется в нескольких местах
            //2 
            registerOperand = selectedConstant.Value;

            //2. отражаем на дисплее
            Display = _formatService.FormatInput(registerOperand.Value);
            //Expression = _binaryOperation.GetOperationExpression();

            //3. назначаем операнд в операцию
            _binaryOperation.SetOperand(new Operand(registerOperand.Value, selectedConstant.Name));
            Expression = _binaryOperation.GetOperationExpression();

            //4. Устанавливаем флаги
            canBackSpace = false;
            mustClearDisplay = true;
        }
        public DelegateCommand ConstCommand { get; }
        private async void ConstCommandExecute()
        {
            await NavigationService.NavigateAsync("ConstantsPage", null, false, false);
        }

        public DelegateCommand OptionsCommand { get; }
        private async void OptionsCommandExecute()
        {
            await NavigationService.NavigateAsync("OptionsPage?selectedTab=SettingsPage", null, false, true);
        }
        #region EditDisplayCommands


        public DelegateCommand<string> EnterFiguresCommand { get; }
        private void EnterFiguresExecute(string par)
        {
            VibrateButton();
            canChangeSign = true;
            //1 усли в операции определен оператор (значит идет ввод второго операнда), очищаем дисплей
            if (mustClearDisplay) Display = String.Empty;

            //2 выводим на дисплей, значения вводимые с кнопок
            Display = GetNewDisplayText(Display, par);

            //3 запоминаем в регистре операнда
            registerOperand = decimal.Parse(Display, CultureInfo.CurrentCulture);

            //4. назначаем операнд в операцию
            _binaryOperation.SetOperand(new Operand(registerOperand.Value, null));

            //5 очищаем строку выражения
            //Expression = GetNewExpression();
            Expression = _binaryOperation.GetOperationExpression();

            //6 устанавливаем флаги
            mustClearDisplay = false;
            canBackSpace = true;
        }

        public DelegateCommand BackSpaceCommand { get; }
        private void BackSpaceExecute()
        {
            if (!canBackSpace) return;

            string currentDisplayText = Display;

            if (currentDisplayText == "0") return;
            VibrateButton();
            int i = currentDisplayText.Length - 1;
            currentDisplayText = currentDisplayText.Remove(i);

            if (i == 1 & currentDisplayText == "-")
                currentDisplayText = "0";
            if (i == 0)
                currentDisplayText = "0";

            Display = currentDisplayText;
            // запоминаем в регистре операнда
            registerOperand = decimal.Parse(Display, CultureInfo.CurrentCulture);

            _binaryOperation.SetOperand(new Operand(registerOperand.Value, null));

            Expression = _binaryOperation.GetOperationExpression();
        }

        public DelegateCommand SignCommand { get; }
        private void SignExecute() //todo: пересмотреть 
        {
            if (canChangeSign == false) return;
            if (registerOperand == 0) return;
            VibrateButton();
            string curDisplay = Display;

            if (Display.StartsWith("-"))
            {
                Display = "";
                curDisplay = curDisplay.Remove(0, 1);
                Display = curDisplay;
            }
            else
            {
                Display = "";
                curDisplay = curDisplay.Insert(0, "-");
                Display = curDisplay;
            }

            // запоминаем в регистре операнда
            registerOperand = decimal.Parse(Display, CultureInfo.CurrentCulture);

            _binaryOperation.SetOperand(new Operand(registerOperand.Value, null));

            Expression = _binaryOperation.GetOperationExpression();
        }

        public DelegateCommand ClearCommand { get; }
        private void ClearExecute()
        {
            VibrateButton();
            registerOperand = null;
            Display = "0";
            _binaryOperation.Clear();
            Expression = _binaryOperation.GetOperationExpression();
        }

        #endregion

        public DelegateCommand AddConstantCommand { get; }
        private async void AddConstExecute()
        {
            string wantMessage = $"Do you want to save {(decimal.Parse(Display, CultureInfo.CurrentCulture)).ToString()} as constant";
            var answer = await _dialogService.DisplayAlertAsync("", wantMessage, "Yes", "No");
            if (answer == true)
            {
                var par = new NavigationParameters();
                par.Add("value", Display);
                if (Constants.Count < AppConstants.MAX_CONSTANTS_NUMBER)
                    await NavigationService.NavigateAsync("EditConstPage", par, false, true);
                else
                {
                    if (Settings.ConstProductPurchased)
                        await NavigationService.NavigateAsync("EditConstPage", null, false, true);
                    else
                    {
                        bool purchased = await _purchasingService.PurchaseNonConsumableItem(AppConstants.CONSTANTS_PPODUCT_ID, "payload");

                        //string title, message;
                        //if (purchased)
                        //{
                        //    title = "Congratulations!";
                        //    message = " You succefully purchase the product";
                        //    await NavigationService.NavigateAsync("EditConstPage", par, false, true);
                        //}
                        //else
                        //{
                        //    title = "Something has gone wrong";
                        //    message = "Please, try it later ";
                        //}
                        //await _dialogService.DisplayAlertAsync(title, message, "OK");
                    }
                }
            }
        }

        public DelegateCommand<string> EnterOperatorCommand { get; }
        private void EnterOperatorExecute(string par) // Plus Minus Multiplication Division Discount
        {
            if (_binaryOperation.Operand1 == null) return;
            VibrateButton();
            //1. устанавливаем флаги
            mustClearDisplay = true;
            canBackSpace = false;
            canChangeSign = false;

            //2. форматируем дисплей
            Display = _formatService.FormatInput(registerOperand.Value);

            //3. 
            if (_binaryOperation.IsReadyForCalc() == false)
            {
                _binaryOperation.SetOperator(par);
            }
            else if (_binaryOperation.IsReadyForCalc() == true)
            {
                //1. произвести вычисление
                registerOperand = _binaryOperation.GetResult();

                //2. вывести результат на дисплей
                Display = _formatService.FormatInput(registerOperand.Value);

                //3. очистить операцию
                _binaryOperation.Clear();

                //4. первому операнду присвоить значение, равное результату операции
                _binaryOperation.SetOperand(new Operand(registerOperand.Value, null));

                //5. назначить следующий оператор в операцию
                _binaryOperation.SetOperator(par);
            }

            Expression = _binaryOperation.GetOperationExpression();
        }

        public DelegateCommand CalcCommand { get; }
        private void CalcExecute()
        {
            //1. производим вычисление ИЛИ выходим
            if (_binaryOperation.IsReadyForCalc()) //операция готова к вычислению
            {
                VibrateButton();
                //1. произвести вычисление
                try
                {
                    registerOperand = _binaryOperation.GetResult();

                    //2. вывести результат на дисплей
                    Display = _formatService.FormatResult(registerOperand.Value);
                    Expression = _binaryOperation.GetOperationExpression();

                    //3. очистить операцию
                    _binaryOperation.Clear();

                    //4. устанавливаем первый операнд равный результату вычисления
                    _binaryOperation.SetOperand(new Operand(registerOperand.Value, null));

                }
                catch (DivideByZeroException ex)
                {
                    Display = ex.Message;
                    //3. очистить операцию
                    _binaryOperation.Clear();
                    Expression = _binaryOperation.GetOperationExpression();
                }
                //5. устанавливаем флаги
                canBackSpace = false;
                canChangeSign = false;
                mustClearDisplay = true;

            }
            //else
            //{
            //    //1.
            //    _binaryOperation.SetOperand(decimal.Parse(Display, CultureInfo.CurrentCulture));
            //    //2
            //    decimal? result = _binaryOperation.Result();

            //    //2. вывести результат на дисплей
            //    Display = result.ToString();

            //    //3. очистить операцию
            //    _binaryOperation.Clear();

            //    //4. устанавливаем первый операнд равный результату вычисления
            //    _binaryOperation.SetOperand(decimal.Parse(Display, CultureInfo.CurrentCulture));

            //    //5. устанавливаем флаги
            //    isBackSpaceApplicable = false;
            //    mustClearDisplay = true;
            //}
        }

        public DelegateCommand<string> MemoryCommand { get; }
        private void MemoryExecute(string par)
        {
            VibrateButton();
            switch (par)
            {
                case "Add":
                    if (registerOperand == null) return;

                    registerMemory = registerMemory == null ? registerOperand : registerMemory + registerOperand;
                    Memory = _formatService.FormatInput(registerMemory.Value);
                    IsMemoryVisible = true;
                    break;
                case "Clear":
                    registerMemory = null;
                    Memory = null;
                    IsMemoryVisible = false;
                    break;
                case "Read":
                    if (registerMemory == null) return;

                    _binaryOperation.SetOperand(new Operand(registerMemory.Value, null));

                    registerOperand = registerMemory;

                    Display = _formatService.FormatInput(registerMemory.Value);

                    Expression = _binaryOperation.GetOperationExpression();

                    canBackSpace = false;
                    mustClearDisplay = true;
                    break;
            }
        }

        public DelegateCommand NaigateToDedicationCommand { get; }
        private async void NavigateToDedicationExecute()
        {
            if (_dedicationService.GetDedicationName(Display) != null)
            {
                var navParams = new NavigationParameters();
                navParams.Add("code", Display);
                await NavigationService.NavigateAsync("DedicationPage", navParams, false, true);
            }
        }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationParameters parameters)
        {
            #region Work with constant
            if (parameters.Count != 0)
            {
                //1. получаем параметр
                decimal curConstValue = ((Constant)parameters["const"]).Value;
                string curConstName = ((Constant)parameters["const"]).Name;

                //2 
                registerOperand = curConstValue;

                //2. отражаем на дисплее
                Display = _formatService.FormatInput(registerOperand.Value);

                //3. назначаем операнд в операцию
                _binaryOperation.SetOperand(new Operand(registerOperand.Value, curConstName));
                Expression = _binaryOperation.GetOperationExpression();

                //4. Устанавливаем флаги
                canBackSpace = false;
                mustClearDisplay = true;
            }
            #endregion
            #region Resore purchasing
            Constants = await App.Database.GetItemsAsync();
            {
                if (Settings.ConstProductPurchased==false)
                {
                    bool purchased = await _purchasingService.IsItemPurchased(AppConstants.CONSTANTS_PPODUCT_ID);

                    string title, message;
                    if (purchased)
                    {
                        title = "Congratulations!";
                        message = " You succefully restore your purchase";
                        await _dialogService.DisplayAlertAsync(title, message, "OK");
                    }
                }
            }
            #endregion
        }

        public override void OnNavigatingTo(NavigationParameters parameters)
        {
            base.OnNavigatingTo(parameters);
            IsRounding = Settings.Rounding;
        }
        public override void OnNavigatedFrom(NavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);
        }

        #endregion

        #region Private utilites
        /// <summary>
        /// Возвращает отредактированное значение для дисплея
        /// </summary>
        /// <param name="currentDisplayText">Текущее значение дисплея</param>
        /// <param name="tag">Тэг нажатой кнопки</param>
        /// <returns></returns>
        private string GetNewDisplayText(string currentDisplayText, string tag)
        {
            string Result = currentDisplayText;
            switch (tag)
            {
                case "DecPoint":
                    {
                        if (currentDisplayText.Length >= maxFiguresNumber)
                            break;
                        if (currentDisplayText.Contains(DecimalSeparator))
                            break;
                        if (currentDisplayText == "")
                        {
                            currentDisplayText = "0" + DecimalSeparator;
                            Result = currentDisplayText;
                            break;
                        }
                        currentDisplayText += DecimalSeparator;
                        Result = currentDisplayText;
                        break;
                    }
                case "0":
                    {
                        if (currentDisplayText == "0")
                            break;
                        else
                            Result = currentDisplayText += tag;
                        break;
                    }
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    {
                        if (currentDisplayText.Length >= maxFiguresNumber) break;
                        if (currentDisplayText == "0") Result = "";

                        Result += tag;

                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(tag);
                    }
            }
            return Result;
        }

        /// <summary>
        /// обновляет строку вывода(Display) каждый раз когда меняются настройки 
        /// </summary>
        private void UpdateDisplayText()
        {
            //1 обновляем видимость метки Rounding
            IsRounding = Settings.Rounding;

            //2 обновляем Expression
            Expression = _binaryOperation.GetOperationExpression();

            if (!registerOperand.HasValue || Display == "0") return;

            if (_binaryOperation.IsReadyForCalc())
                Display = _formatService.FormatInput(registerOperand.Value);
            else
                Display = _formatService.FormatResult(registerOperand.Value);
        }

        private void VibrateButton()
        {
            if (Settings.Vibration)
                CrossVibrate.Current.Vibration(TimeSpan.FromMilliseconds(VIBRATE_DURATION));
        }
        #endregion
    }
}