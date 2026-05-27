using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.ViewModels;

namespace RoclandTruckCheck.Mobile.Views;

public partial class TipoAcceso : ContentPage
{
    private readonly TipoAccesoViewModel _vm;

    public TipoAcceso(TipoAccesoViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    // ─────────────────────────────────────────────────────────────────
    // CARDS — animación de rebote + navegación
    // ─────────────────────────────────────────────────────────────────

    private async void OnEntradaTapped(object? sender, TappedEventArgs e)
    {
        await AnimarCard(CardEntrada);
        await Shell.Current.GoToAsync(nameof(ChecklistPage),
            new Dictionary<string, object>
            {
                ["TipoRegistro"] = TipoRegistro.Entrada
            });
    }

    private async void OnSalidaTapped(object? sender, TappedEventArgs e)
    {
        await AnimarCard(CardSalida);
        await Shell.Current.GoToAsync(nameof(ChecklistPage),
            new Dictionary<string, object>
            {
                ["TipoRegistro"] = TipoRegistro.Salida
            });
    }

    private static async Task AnimarCard(View card)
    {
        // Pequeño rebote: escala baja → sube → normal
        await card.ScaleTo(0.96, 80, Easing.CubicIn);
        await card.ScaleTo(1.02, 80, Easing.CubicOut);
        await card.ScaleTo(1.00, 60, Easing.CubicIn);
    }

    // ─────────────────────────────────────────────────────────────────
    // LOGOUT
    // ─────────────────────────────────────────────────────────────────

    private void OnLogoutClicked(object? sender, EventArgs e)
        => OverlayLogout.IsVisible = true;

    private void OnCancelarLogoutClicked(object? sender, EventArgs e)
        => OverlayLogout.IsVisible = false;

    private async void OnConfirmarLogoutClicked(object? sender, EventArgs e)
    {
        OverlayLogout.IsVisible = false;
        await _vm.CerrarSesionAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // TOAST (llamado desde ChecklistPage via MessagingCenter / Shell)
    // ─────────────────────────────────────────────────────────────────

    public async Task MostrarToastExitoAsync(string mensaje)
    {
        ToastLabel.Text = mensaje;
        ToastIcon.Source = "icon_check.png";
        ToastFrame.BackgroundColor = Color.FromArgb("#1B3A1B");
        ToastFrame.IsVisible = true;
        ToastFrame.Opacity = 0;

        await ToastFrame.FadeToAsync(1, 250);
        await Task.Delay(2800);
        await ToastFrame.FadeToAsync(0, 300);
        ToastFrame.IsVisible = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // CICLO DE VIDA — refrescar contador al volver
    // ─────────────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefrescarResumenCommand.Execute(null);
    }
}