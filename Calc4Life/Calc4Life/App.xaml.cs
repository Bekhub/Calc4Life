using Prism;
using Prism.Ioc;
using Calc4Life.ViewModels;
using Calc4Life.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Prism.Unity;
using Calc4Life.Data;
using System.Diagnostics;
using Calc4Life.Services;
using Calc4Life.Services.RepositoryServices;
using Unity.Lifetime;
using Calc4Life.Services.OperationServices;
using Calc4Life.Helpers;
using Calc4Life.Services.FormatServices;
using Calc4Life.Services.PurchasingServices;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Calc4Life
{

    public partial class App : PrismApplication
    {
        static ConstItemDatabase database;

        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        public static ConstItemDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new ConstItemDatabase(DependencyService.Get<IFileHelper>().GetLocalFilePath("CalculatorQLite.db3"));
                }
                return database;
            }
        }

        protected override void OnInitialized()
        {
            InitializeComponent();

            Resources = new ResourceDictionary();
            Resources.Add("primaryBlue", Color.FromHex("0d47a1"));
            Resources.Add("colorTitle", Color.WhiteSmoke);

#if DEBUG
            Debug.WriteLine("OnInitialized");
#endif
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<CalcPage>();
            //containerRegistry.RegisterForNavigation<MainPage>();
            containerRegistry.RegisterForNavigation<ConstantsPage>();
            containerRegistry.RegisterForNavigation<OptionsPage>();
            containerRegistry.RegisterForNavigation<AboutPage>();
            containerRegistry.RegisterForNavigation<SettingsPage>();
            containerRegistry.RegisterForNavigation<EditConstPage>();
            containerRegistry.RegisterForNavigation<DedicationPage>();

            containerRegistry.RegisterSingleton(typeof(IConstantsRepositoryService), typeof(ConstantsRepositoryServiceFake));

            containerRegistry.RegisterSingleton(typeof(IBinaryOperationService), typeof(BinaryOperationService));
            containerRegistry.RegisterSingleton(typeof(FormatService));
            containerRegistry.RegisterInstance(typeof(DedicationService));
            containerRegistry.RegisterInstance(typeof(ConstantsPurchasingService));

            
#if DEBUG
            Debug.WriteLine("RegisterTypes");
#endif
        }

        protected async override void OnStart()
        {
            var pageOne = new CalcPage();

            NavigationPage.SetHasNavigationBar(pageOne, true);
            NavigationPage navPage = new NavigationPage(pageOne) { BarTextColor = Color.White };

            navPage.BarBackgroundColor = (Color)App.Current.Resources["primaryBlue"];
            //navPage.BarTextColor = (Color)App.Current.Resources["colorTitle"];

            this.MainPage = navPage;
            await NavigationService.NavigateAsync("CalcPage");

#if DEBUG
            Debug.WriteLine("OnStart");
#endif
        }

        protected override void OnSleep()
        {
            base.OnSleep();
#if DEBUG
            Debug.WriteLine("OnSleep");
#endif
        }

        protected override void OnResume()
        {
            base.OnResume();
#if DEBUG
            Debug.WriteLine("OnResume");
#endif
        }
    }
}
