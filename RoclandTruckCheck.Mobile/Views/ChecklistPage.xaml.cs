using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.ViewModels;
using System.Text.Json;

namespace RoclandTruckCheck.Mobile.Views;

[QueryProperty(nameof(TipoRegistro), "TipoRegistro")]
public partial class ChecklistPage : ContentPage
{
    private readonly ChecklistViewModel _vm;

    private bool? _candados, _licencia, _danios, _llantas, _luces, _fugas;

    // ── Controla si el WebView ya terminó de cargar ────────────────
    private bool _webViewReady = false;

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

        _vm.OnRegistroEnviado += OnRegistroEnviadoHandler;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ChecklistViewModel.PuedeEnviar))
                ActualizarColorBoton();

            // Cuando el panel cambia a visible, cargamos el HTML
            if (e.PropertyName == nameof(ChecklistViewModel.PanelDaniosVisible))
                InicializarWebViewSiNecesario();
        };
    }

    // ══════════════════════════════════════════════════════════════════
    //  TRUCK WEBVIEW — región completa
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Carga el HTML local la primera vez que el panel se hace visible.
    /// Las veces siguientes ya está cargado; solo limpia si el usuario
    /// tocó "Limpiar" (ver HandleLimpiar).
    /// </summary>
    private void InicializarWebViewSiNecesario()
    {
        if (!_vm.PanelDaniosVisible) return;
        if (_webViewReady) return;

        // MauiAsset embebido → accesible mediante el esquema local
        TruckWebView.Source = new HtmlWebViewSource
        {
            Html = LoadEmbeddedHtml()
        };
    }

    /// <summary>
    /// Lee truck_damage.html desde Resources/Raw (MauiAsset).
    /// </summary>
    private static string LoadEmbeddedHtml()
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("truck_damage.html").Result;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// El WebView intercepta navegaciones con el esquema "truckdamage://"
    /// que el HTML emite cada vez que el usuario agrega, cambia o borra un daño.
    /// </summary>
    private void OnTruckWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("truckdamage://")) return;

        e.Cancel = true;   // evitamos navegación real

        // Extraemos el parámetro ?data=...
        var uri = new Uri(e.Url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var data = query["data"];
        if (string.IsNullOrEmpty(data)) return;

        // Deserializamos el array de daños que envía el HTML
        var danios = DeserializarDanios(data);
        _vm.ActualizarDaniosDesdeWebView(danios);
    }

    /// <summary>
    /// Cuando el ViewModel cambia porque el panel se ocultó (el guardia
    /// marcó "Sin daños" como OK), limpiamos el WebView también.
    /// </summary>
    private async Task LimpiarWebViewAsync()
    {
        if (!_webViewReady) return;
        try
        {
            await TruckWebView.EvaluateJavaScriptAsync("limpiarTodo()");
        }
        catch { /* WebView puede estar en proceso de carga */ }
    }

    /// <summary>
    /// Permite pre-cargar daños en el WebView (útil si en el futuro
    /// abres la página en modo edición con datos previos).
    /// </summary>
    public async Task CargarDaniosPreviosAsync(List<CrearChecklistDanioRequest> danios)
    {
        if (!_webViewReady) return;
        var json = JsonSerializer.Serialize(danios);
        var escaped = json.Replace("'", "\\'");
        await TruckWebView.EvaluateJavaScriptAsync($"window.setDamagesJson('{escaped}')");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static List<CrearChecklistDanioRequest> DeserializarDanios(string json)
    {
        try
        {
            // Agrega estas opciones:
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Pásale las opciones al deserializador:
            return JsonSerializer.Deserialize<List<CrearChecklistDanioRequest>>(json, options)
                   ?? new List<CrearChecklistDanioRequest>();
        }
        catch { return new List<CrearChecklistDanioRequest>(); }
    }

    // ══════════════════════════════════════════════════════════════════
    //  MÉTODOS EXISTENTES — sin cambios salvo los marcados con ★
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.OnTodoOk += HandleTodoOk;
        _vm.OnLimpiar += HandleLimpiar;

        // ★ Marcamos el WebView como listo una vez que la página aparece
        TruckWebView.Navigated += OnTruckWebViewNavigated;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.OnTodoOk -= HandleTodoOk;
        _vm.OnLimpiar -= HandleLimpiar;
        TruckWebView.Navigated -= OnTruckWebViewNavigated;
    }

    // ★ Callback cuando el WebView termina de cargar el HTML
    private void OnTruckWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
            _webViewReady = true;
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

        // ★ También limpia el camión dibujado
        _ = LimpiarWebViewAsync();
        _webViewReady = false;   // fuerza recarga limpia si vuelve a abrirse

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

    private async void OnVolverClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }
}