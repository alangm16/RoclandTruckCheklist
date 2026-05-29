using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using RoclandTruckCheck.Mobile.Services;
using RoclandTruckCheck.Mobile.ViewModels;
using RoclandTruckCheck.Mobile.Views;
using RoclandTruckCheck.Mobile.Models;

namespace RoclandTruckCheck.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Servicios de estado → Singleton (sin cambio)
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AuthStateService>();
            builder.Services.AddSingleton<SesionGuardia>();

            // ViewModels:
            // - LoginViewModel: Transient está bien (se usa poco)
            // - TipoAccesoViewModel: Singleton → se reutiliza en cada vuelta sin reconstruir
            // - ChecklistViewModel: Singleton → el más pesado; InicializarConTipo() lo resetea
            //   correctamente antes de cada uso, así que es seguro reutilizarlo
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddSingleton<TipoAccesoViewModel>();
            builder.Services.AddSingleton<ChecklistViewModel>();

            // Views:
            // - LoginPage: Transient está bien
            // - TipoAcceso: Singleton → evita reconstruir el árbol visual cada vez
            // - ChecklistPage: Transient necesario porque QueryProperty llega en cada navegación
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<TipoAcceso>();
            builder.Services.AddTransient<ChecklistPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}