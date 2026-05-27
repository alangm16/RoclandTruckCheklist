using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using RoclandTruckCheck.Mobile.Services;
using RoclandTruckCheck.Mobile.ViewModels;
using RoclandTruckCheck.Mobile.Views;

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

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<TipoAccesoModel>();
            builder.Services.AddTransient<ChecklistModel>();

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
