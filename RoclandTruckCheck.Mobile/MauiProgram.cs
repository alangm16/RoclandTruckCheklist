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

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AuthStateService>();
            builder.Services.AddSingleton<SesionGuardia>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<TipoAccesoViewModel>();
            builder.Services.AddTransient<ChecklistViewModel>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<TipoAcceso>();
            builder.Services.AddTransient<ChecklistPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
        
    }
}
