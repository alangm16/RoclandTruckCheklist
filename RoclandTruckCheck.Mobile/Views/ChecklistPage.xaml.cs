using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.ViewModels;

namespace RoclandTruckCheck.Mobile.Views;

[QueryProperty(nameof(TipoRegistro), "TipoRegistro")]
public partial class ChecklistPage : ContentPage
{
    private readonly ChecklistViewModel _vm;

    // Estado local de los 6 ítems: null=pendiente, true=OK, false=falla
    private bool? _candados, _licencia, _danios, _llantas, _luces, _fugas;

    public TipoRegistro TipoRegistro
    {
        set
        {
            _vm.InicializarConTipo(value);
            AplicarTema(value);
        }
    }

    public ChecklistPage(ChecklistViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Suscribirse al evento de envío exitoso para mostrar toast y navegar
        _vm.OnRegistroEnviado += OnRegistroEnviadoHandler;
    }

    // ─────────────────────────────────────────────────────────────────
    // TEMA DINÁMICO: verde (entrada) vs naranja (salida)
    // ─────────────────────────────────────────────────────────────────

    private void AplicarTema(TipoRegistro tipo)
    {
        bool esEntrada = tipo == TipoRegistro.Entrada;

        // Header
        GradStop1.Color = esEntrada ? Color.FromArgb("#1B8A3A") : Color.FromArgb("#B84A00");
        GradStop2.Color = esEntrada ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F06000");

        // Botón enviar
        BtnStop1.Color = esEntrada ? Color.FromArgb("#1B8A3A") : Color.FromArgb("#B84A00");
        BtnStop2.Color = esEntrada ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F06000");
    }

    // ─────────────────────────────────────────────────────────────────
    // ÍTEMS DEL CHECKLIST
    // ─────────────────────────────────────────────────────────────────

    // Patrón unificado: cada ítem tiene su par (OK / Falla)
    // El estado se refleja en ícono + label + fondo del Frame

    private void OnCandadosOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _candados, true, IcoCandadosOk, IcoCandadosFalla, LblCandados, ItemCandados);
    private void OnCandadosFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _candados, false, IcoCandadosOk, IcoCandadosFalla, LblCandados, ItemCandados);

    private void OnLicenciaOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _licencia, true, IcoLicenciaOk, IcoLicenciaFalla, LblLicencia, ItemLicencia);
    private void OnLicenciaFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _licencia, false, IcoLicenciaOk, IcoLicenciaFalla, LblLicencia, ItemLicencia);

    private void OnDaniosOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _danios, true, IcoDaniosOk, IcoDaniosFalla, LblDanios, ItemDanios);
    private void OnDaniosFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _danios, false, IcoDaniosOk, IcoDaniosFalla, LblDanios, ItemDanios);

    private void OnLlantasOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _llantas, true, IcoLlantasOk, IcoLlantasFalla, LblLlantas, ItemLlantas);
    private void OnLlantasFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _llantas, false, IcoLlantasOk, IcoLlantasFalla, LblLlantas, ItemLlantas);

    private void OnLucesOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _luces, true, IcoLucesOk, IcoLucesFalla, LblLuces, ItemLuces);
    private void OnLucesFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _luces, false, IcoLucesOk, IcoLucesFalla, LblLuces, ItemLuces);

    private void OnFugasOkTapped(object? s, TappedEventArgs e) => MarcarItem(ref _fugas, true, IcoFugasOk, IcoFugasFalla, LblFugas, ItemFugas);
    private void OnFugasFallaTapped(object? s, TappedEventArgs e) => MarcarItem(ref _fugas, false, IcoFugasOk, IcoFugasFalla, LblFugas, ItemFugas);

    private void MarcarItem(ref bool? estado, bool valor,
        Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        // Si se toca el mismo botón que ya estaba activo → deseleccionar
        if (estado == valor)
        {
            estado = null;
            ResetearBotones(icoOk, icoFalla, lbl, frame);
        }
        else
        {
            estado = valor;
            if (valor)
                AplicarEstadoOk(icoOk, icoFalla, lbl, frame);
            else
                AplicarEstadoFalla(icoOk, icoFalla, lbl, frame);
        }

        SincronizarConViewModel();
    }

    private static void AplicarEstadoOk(Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        icoOk.Source = "check_green.png";
        icoFalla.Source = "close_gray.png";
        lbl.Text = "✓  OK";
        lbl.TextColor = Color.FromArgb("#2E7D32");
        frame.BorderColor = Color.FromArgb("#A5D6A7");
        frame.BackgroundColor = Color.FromArgb("#F1FBF1");
    }

    private static void AplicarEstadoFalla(Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        icoFalla.Source = "close_red.png";
        icoOk.Source = "check_gray.png";
        lbl.Text = "✗  Falla";
        lbl.TextColor = Color.FromArgb("#C62828");
        frame.BorderColor = Color.FromArgb("#FFCDD2");
        frame.BackgroundColor = Color.FromArgb("#FFF8F8");
    }

    private static void ResetearBotones(Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        icoOk.Source = "check_gray.png";
        icoFalla.Source = "close_gray.png";
        lbl.Text = "Pendiente";
        lbl.TextColor = Color.FromArgb("#9E9E9E");
        frame.BorderColor = Color.FromArgb("#E8F0E8");
        frame.BackgroundColor = Colors.White;
    }

    private void SincronizarConViewModel()
    {
        _vm.ActualizarItems(
            _candados, _licencia, _danios,
            _llantas, _luces, _fugas);
    }

    // ─────────────────────────────────────────────────────────────────
    // TODO OK y LIMPIAR (ViewModel notifica → code-behind actualiza UI)
    // ─────────────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _vm.OnTodoOk += HandleTodoOk;
        _vm.OnLimpiar += HandleLimpiar;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnTodoOk -= HandleTodoOk;
        _vm.OnLimpiar -= HandleLimpiar;
    }

    private void HandleTodoOk()
    {
        _candados = _licencia = _danios = _llantas = _luces = _fugas = true;

        AplicarEstadoOk(IcoCandadosOk, IcoCandadosFalla, LblCandados, ItemCandados);
        AplicarEstadoOk(IcoLicenciaOk, IcoLicenciaFalla, LblLicencia, ItemLicencia);
        AplicarEstadoOk(IcoDaniosOk, IcoDaniosFalla, LblDanios, ItemDanios);
        AplicarEstadoOk(IcoLlantasOk, IcoLlantasFalla, LblLlantas, ItemLlantas);
        AplicarEstadoOk(IcoLucesOk, IcoLucesFalla, LblLuces, ItemLuces);
        AplicarEstadoOk(IcoFugasOk, IcoFugasFalla, LblFugas, ItemFugas);

        SincronizarConViewModel();
    }

    private void HandleLimpiar()
    {
        _candados = _licencia = _danios = _llantas = _luces = _fugas = null;

        ResetearBotones(IcoCandadosOk, IcoCandadosFalla, LblCandados, ItemCandados);
        ResetearBotones(IcoLicenciaOk, IcoLicenciaFalla, LblLicencia, ItemLicencia);
        ResetearBotones(IcoDaniosOk, IcoDaniosFalla, LblDanios, ItemDanios);
        ResetearBotones(IcoLlantasOk, IcoLlantasFalla, LblLlantas, ItemLlantas);
        ResetearBotones(IcoLucesOk, IcoLucesFalla, LblLuces, ItemLuces);
        ResetearBotones(IcoFugasOk, IcoFugasFalla, LblFugas, ItemFugas);

        SincronizarConViewModel();
    }

    // ─────────────────────────────────────────────────────────────────
    // ENVÍO EXITOSO — toast + navegación de vuelta
    // ─────────────────────────────────────────────────────────────────

    private async void OnRegistroEnviadoHandler(string mensaje)
    {
        await MostrarToastAsync(mensaje, exito: true);
        await Task.Delay(600); // deja ver el toast antes de salir
        await Shell.Current.GoToAsync("..");
    }

    private async Task MostrarToastAsync(string mensaje, bool exito)
    {
        ToastIcon.Source = exito ? "icon_check.png" : "icon_exit.png";
        ToastLabel.Text = mensaje;
        ToastFrame.BackgroundColor = exito
            ? Color.FromArgb("#1B3A1B")
            : Color.FromArgb("#B71C1C");

        ToastFrame.IsVisible = true;
        ToastFrame.Opacity = 0;

        await ToastFrame.FadeToAsync(1, 220);
        await Task.Delay(exito ? 2800 : 3200);
        await ToastFrame.FadeToAsync(0, 280);

        ToastFrame.IsVisible = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // VOLVER
    // ─────────────────────────────────────────────────────────────────

    private async void OnVolverClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }
}