using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.ViewModels;

namespace RoclandTruckCheck.Mobile.Views;

public partial class TipoAcceso : ContentPage
{
    private readonly TipoAccesoViewModel _vm;

    // Evita doble-tap mientras se navega
    private bool _navegando = false;

    public TipoAcceso(TipoAccesoViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    // ─────────────────────────────────────────────────────────────────
    // CARDS — animación EN PARALELO con la navegación (no bloqueante)
    // ─────────────────────────────────────────────────────────────────

    private async void OnEntradaTapped(object? sender, TappedEventArgs e)
    {
        if (_navegando) return;
        _navegando = true;

        // Lanzamos animación y navegación al mismo tiempo.
        // El usuario ve respuesta visual instantánea; la navegación
        // ya está en marcha mientras la animación se completa (≈220 ms).
        var animTask = AnimarCard(CardEntrada);
        var navTask = Shell.Current.GoToAsync(nameof(ChecklistPage),
            new Dictionary<string, object>
            {
                ["TipoRegistro"] = TipoRegistro.Entrada
            });

        await Task.WhenAll(animTask, navTask);
        _navegando = false;
    }

    private async void OnSalidaTapped(object? sender, TappedEventArgs e)
    {
        if (_navegando) return;
        _navegando = true;

        var animTask = AnimarCard(CardSalida);
        var navTask = Shell.Current.GoToAsync(nameof(ChecklistPage),
            new Dictionary<string, object>
            {
                ["TipoRegistro"] = TipoRegistro.Salida
            });

        await Task.WhenAll(animTask, navTask);
        _navegando = false;
    }

    // Rebote rápido: duración total ≈ 220 ms (antes 240 ms con 3 awaits seriales)
    private static async Task AnimarCard(View card)
    {
        await card.ScaleTo(0.96, 70, Easing.CubicIn);
        await card.ScaleTo(1.00, 80, Easing.SpringOut);
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
    // TOAST
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
    // CICLO DE VIDA
    // ─────────────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefrescarResumenCommand.Execute(null);
    }
}