using Microsoft.Extensions.DependencyInjection;
using RoclandTruckCheck.Mobile.Services;

namespace RoclandTruckCheck.Mobile
{
    public partial class App : Application
    {
        private readonly AuthStateService _auth;
        private bool _sesionLista = false;

        public App(AuthStateService auth)
        {
            InitializeComponent();
            _auth = auth;
            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            base.OnStart();
            bool sesionRestaurada = false;

            try
            {
                sesionRestaurada = await _auth.RestaurarSesionAsync();
                await Shell.Current.GoToAsync(sesionRestaurada ? "//TipoAcceso" : "//LoginPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
                return;
            }
        }
    }
}