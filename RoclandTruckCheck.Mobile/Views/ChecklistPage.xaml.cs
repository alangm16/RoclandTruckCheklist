using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.ViewModels;
using System.Text.Json;

namespace RoclandTruckCheck.Mobile.Views;

[QueryProperty(nameof(TipoRegistro), "TipoRegistro")]
public partial class ChecklistPage : ContentPage
{
    private readonly ChecklistViewModel _vm;

    private bool? _candados, _licencia, _danios, _llantas, _luces, _fugas;

    // El WebView se carga la primera vez que el panel de daños se hace visible.
    // Se usa una TaskCompletionSource para saber cuándo terminó sin bloquear.
    private bool _webViewReady = false;
    private TaskCompletionSource<bool>? _webViewLoadTcs;

    public TipoRegistro TipoRegistro
    {
        set
        {
            // InicializarConTipo es barato (solo asigna propiedades del VM),
            // pero lo dejamos en el setter para que el dato llegue antes de
            // que OnAppearing comience a suscribirse.
            _vm.InicializarConTipo(value);
            AplicarTema(value);
        }
    }

    public ChecklistPage(ChecklistViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnRegistroEnviado += OnRegistroEnviadoHandler;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ChecklistViewModel.PuedeEnviar))
                ActualizarColorBoton();

            if (e.PropertyName == nameof(ChecklistViewModel.PanelDaniosVisible))
                InicializarWebViewSiNecesario();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.OnTodoOk += HandleTodoOk;
        _vm.OnLimpiar += HandleLimpiar;
        _vm.OnRegistroEnviado += OnRegistroEnviadoHandler;    
        _vm.PropertyChanged += OnVmPropertyChanged;           
        TruckWebView.Navigated += OnTruckWebViewNavigated;
    }

    // ══════════════════════════════════════════════════════════════════
    //  TRUCK WEBVIEW
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Solo carga el HTML cuando el panel de daños realmente se muestra.
    /// Evita cargar un WebView pesado al abrir la página.
    /// </summary>
    private void InicializarWebViewSiNecesario()
    {
        if (!_vm.PanelDaniosVisible) return;
        if (_webViewReady) return;

        // Carga asíncrona real: no bloquea el hilo principal
        _ = CargarWebViewAsync();
    }

    /// <summary>
    /// Lee truck_damage.html de forma asíncrona y asigna al WebView
    /// sin bloquear el hilo de UI.
    /// </summary>
    private async Task CargarWebViewAsync()
    {
        try
        {
            _webViewLoadTcs = new TaskCompletionSource<bool>();

            // Apertura asíncrona → no usa .Result
            using var stream = await FileSystem.OpenAppPackageFileAsync("truck_damage.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            // Asignamos en el hilo principal
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TruckWebView.Source = new HtmlWebViewSource { Html = html };
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView] Error cargando HTML: {ex.Message}");
        }
    }

    private void OnTruckWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("truckdamage://")) return;

        e.Cancel = true;

        var uri = new Uri(e.Url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var data = query["data"];
        if (string.IsNullOrEmpty(data)) return;

        var danios = DeserializarDanios(data);
        _vm.ActualizarDaniosDesdeWebView(danios);
    }

    private void OnTruckWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
        {
            _webViewReady = true;
            _webViewLoadTcs?.TrySetResult(true);
        }
    }

    private async Task LimpiarWebViewAsync()
    {
        if (!_webViewReady) return;
        try
        {
            await TruckWebView.EvaluateJavaScriptAsync("limpiarTodo()");
        }
        catch { }
    }

    public async Task CargarDaniosPreviosAsync(List<CrearChecklistDanioRequest> danios)
    {
        if (!_webViewReady) return;
        var json = JsonSerializer.Serialize(danios);
        var escaped = json.Replace("'", "\\'");
        await TruckWebView.EvaluateJavaScriptAsync($"window.setDamagesJson('{escaped}')");
    }

    private static List<CrearChecklistDanioRequest> DeserializarDanios(string json)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<CrearChecklistDanioRequest>>(json, options)
                   ?? new List<CrearChecklistDanioRequest>();
        }
        catch { return new List<CrearChecklistDanioRequest>(); }
    }

    // ══════════════════════════════════════════════════════════════════
    //  TEMA Y COLOR DE BOTÓN
    // ══════════════════════════════════════════════════════════════════

    private void AplicarTema(TipoRegistro tipo)
    {
        bool esEntrada = tipo == TipoRegistro.Entrada;
        GradStop1.Color = esEntrada ? Color.FromArgb("#1B8A3A") : Color.FromArgb("#B84A00");
        GradStop2.Color = esEntrada ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F06000");
        BtnStop1.Color = esEntrada ? Color.FromArgb("#1B8A3A") : Color.FromArgb("#B84A00");
        BtnStop2.Color = esEntrada ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F06000");
        ActualizarColorBoton();
    }

    private void ActualizarColorBoton()
    {
        if (!_vm.PuedeEnviar)
        {
            BtnStop1.Color = Colors.Gray;
            BtnStop2.Color = Color.FromArgb("#BDBDBD");
        }
        else
        {
            bool esEntrada = _vm.TipoRegistro == TipoRegistro.Entrada;
            BtnStop1.Color = Color.FromArgb(esEntrada ? "#1B8A3A" : "#B84A00");
            BtnStop2.Color = Color.FromArgb(esEntrada ? "#4CAF50" : "#F06000");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ITEMS DEL CHECKLIST
    // ══════════════════════════════════════════════════════════════════

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
        if (estado == valor)
        {
            estado = null;
            ResetearBotones(icoOk, icoFalla, lbl, frame);
        }
        else
        {
            estado = valor;
            if (valor) AplicarEstadoOk(icoOk, icoFalla, lbl, frame);
            else AplicarEstadoFalla(icoOk, icoFalla, lbl, frame);
        }
        SincronizarConViewModel();
    }

    private static void AplicarEstadoOk(Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        icoOk.Source = "check_green.png";
        icoFalla.Source = "close_gray.png";
        lbl.Text = "✓  OK";
        lbl.TextColor = Color.FromArgb("#155724");
        frame.BorderColor = Color.FromArgb("#4CAF50");
        frame.BackgroundColor = Color.FromArgb("#E8F5E9");
    }

    private static void AplicarEstadoFalla(Image icoOk, Image icoFalla, Label lbl, Frame frame)
    {
        icoFalla.Source = "close_red.png";
        icoOk.Source = "check_gray.png";
        lbl.Text = "✗  Falla";
        lbl.TextColor = Color.FromArgb("#B71C1C");
        frame.BorderColor = Color.FromArgb("#EF5350");
        frame.BackgroundColor = Color.FromArgb("#FFEBEE");
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
        _vm.ActualizarItems(_candados, _licencia, _danios, _llantas, _luces, _fugas);
        _vm.EvaluarEstadoDanios(_danios);
    }

    // ══════════════════════════════════════════════════════════════════
    //  HANDLERS DE EVENTOS DEL VIEWMODEL
    // ══════════════════════════════════════════════════════════════════

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
        ActualizarColorBoton();
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

        _ = LimpiarWebViewAsync();
        _webViewReady = false;

        SincronizarConViewModel();
        ActualizarColorBoton();
    }

    private async void OnRegistroEnviadoHandler(string mensaje)
    {
        await MostrarModalExitoAsync(mensaje);
        await Task.Delay(600);
        await Shell.Current.GoToAsync("..");
    }

    private async Task MostrarModalExitoAsync(string mensaje)
    {
        SuccessLabel.Text = mensaje;
        SuccessOverlay.Opacity = 0;
        SuccessOverlay.IsVisible = true;
        await SuccessOverlay.FadeTo(1, 200, Easing.CubicOut);
        await Task.Delay(3000);
        await SuccessOverlay.FadeTo(0, 200, Easing.CubicIn);
        SuccessOverlay.IsVisible = false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  NAVEGACIÓN
    // ══════════════════════════════════════════════════════════════════

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnTodoOk -= HandleTodoOk;
        _vm.OnLimpiar -= HandleLimpiar;
        _vm.OnRegistroEnviado -= OnRegistroEnviadoHandler;    
        _vm.PropertyChanged -= OnVmPropertyChanged;           
        TruckWebView.Navigated -= OnTruckWebViewNavigated;
    }

    private async void OnVolverClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChecklistViewModel.PuedeEnviar))
            ActualizarColorBoton();

        if (e.PropertyName == nameof(ChecklistViewModel.PanelDaniosVisible))
            InicializarWebViewSiNecesario();
    }
}