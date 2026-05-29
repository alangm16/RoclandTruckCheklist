using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.Services;
using System.Collections.ObjectModel;

namespace RoclandTruckCheck.Mobile.ViewModels;

// ── Wrapper para manejar la selección en la UI ─────────────────
public partial class ZonaItemViewModel : ObservableObject
{
    public ZonaDto Zona { get; set; } = null!;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class ChecklistViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SesionGuardia _sesion;
    private readonly TipoAccesoViewModel _tipoAccesoVm;

    public event Action? OnTodoOk;
    public event Action? OnLimpiar;
    public event Action<string>? OnRegistroEnviado;

    private TipoRegistro _tipoRegistro;

    [ObservableProperty]
    private string _tituloTipo = "ENTRADA";

    [ObservableProperty]
    private string _textoBotonEnviar = "Enviar entrada";

    [ObservableProperty]
    private bool _isBusy;

    public TipoRegistro TipoRegistro { get; private set; }

    [ObservableProperty]
    private ObservableCollection<SucursalDto> _sucursales = new();

    [ObservableProperty]
    private ObservableCollection<VehiculoDto> _vehiculos = new();

    [ObservableProperty]
    private ObservableCollection<ChoferDto> _choferes = new();

    // ── Catálogo interactivo de zonas ─────────────────────────────
    [ObservableProperty]
    private ObservableCollection<ZonaItemViewModel> _zonasSeleccionables = new();

    [ObservableProperty]
    private SucursalDto? _sucursalSeleccionada;

    [ObservableProperty]
    private VehiculoDto? _vehiculoSeleccionado;

    [ObservableProperty]
    private ChoferDto? _choferSeleccionado;

    [ObservableProperty]
    private ObservableCollection<CrearChecklistDanioRequest> _daniosSeleccionados = new();

    [ObservableProperty]
    private string? _observacion;

    private bool? _candados, _licencia, _danios, _llantas, _luces, _fugas;

    [ObservableProperty]
    private int _itemsOk;

    [ObservableProperty]
    private double _progresoChecklist;

    [ObservableProperty]
    private bool _puedeEnviar;

    // ── Controla si se muestra el panel de daños ──────────────────
    [ObservableProperty]
    private bool _panelDaniosVisible;

    public ChecklistViewModel(ApiService api, SesionGuardia sesion, TipoAccesoViewModel tipoAccesoVm)
    {
        _api = api;
        _sesion = sesion;
        _tipoAccesoVm = tipoAccesoVm;
    }

    public void InicializarConTipo(TipoRegistro tipo)
    {
        ResetearEstadoCompleto();               
        TipoRegistro = tipo;
        _tipoRegistro = tipo;
        TituloTipo = tipo.ToString().ToUpper();
        TextoBotonEnviar = tipo == TipoRegistro.Entrada ? "Enviar entrada" : "Enviar salida";

        CargarCatalogosDesdeCache();            // Colecciones frescas (rápido, desde cache)

        if (tipo == TipoRegistro.Entrada)
        {
            SucursalSeleccionada = Sucursales.FirstOrDefault(s =>
                s.Nombre.Contains("CEDIS BRAVO", StringComparison.OrdinalIgnoreCase));
        }

        ActualizarPuedeEnviar();
    }

    private void CargarCatalogosDesdeCache()
    {
        Sucursales = new ObservableCollection<SucursalDto>(_sesion.Sucursales);
        Vehiculos = new ObservableCollection<VehiculoDto>(_sesion.Vehiculos);
        Choferes = new ObservableCollection<ChoferDto>(_sesion.Choferes);

        ZonasSeleccionables = new ObservableCollection<ZonaItemViewModel>(
            _sesion.ZonasDanio.Where(z => z.Activo).Select(z => new ZonaItemViewModel
            {
                Zona = z,
                IsSelected = false
            })
        );
    }

    partial void OnSucursalSeleccionadaChanged(SucursalDto? value) => ActualizarPuedeEnviar();
    partial void OnVehiculoSeleccionadoChanged(VehiculoDto? value) => ActualizarPuedeEnviar();
    partial void OnChoferSeleccionadoChanged(ChoferDto? value) => ActualizarPuedeEnviar();

    public void ActualizarItems(bool? candados, bool? licencia, bool? danios,
                                 bool? llantas, bool? luces, bool? fugas)
    {
        _candados = candados;
        _licencia = licencia;
        _danios = danios;
        _llantas = llantas;
        _luces = luces;
        _fugas = fugas;

        var estados = new[] { candados, licencia, danios, llantas, luces, fugas };
        ItemsOk = estados.Count(e => e == true);
        ProgresoChecklist = ItemsOk / 6.0;

        ActualizarPuedeEnviar();
    }

    /// <summary>
    /// Recibe la lista de daños actualizada directamente desde el WebView.
    /// Reemplaza DaniosSeleccionados completo para mantener la colección en sync.
    /// El campo "Notas" del request transporta la severidad (leve/grave/critico)
    /// como texto hasta que el backend soporte el campo Severidad.
    /// </summary>
    public void ActualizarDaniosDesdeWebView(List<CrearChecklistDanioRequest> danios)
    {
        DaniosSeleccionados.Clear();
        foreach (var d in danios)
            DaniosSeleccionados.Add(d);

        ActualizarPuedeEnviar();
    }

    // ── Lógica para mostrar/ocultar el panel de zonas ─────────────
    public void EvaluarEstadoDanios(bool? estadoDanios)
    {
        // Si hay daños (Falla = false), mostramos el panel interactivo
        if (estadoDanios == false)
        {
            PanelDaniosVisible = true;
        }
        else
        {
            // Si está OK o Pendiente, ocultamos y limpiamos las selecciones
            PanelDaniosVisible = false;
            DaniosSeleccionados.Clear();
            foreach (var zona in ZonasSeleccionables)
            {
                zona.IsSelected = false;
            }
        }
    }

    // ── Comando que ejecuta el guardia al tocar un Chip de Zona ───
    [RelayCommand]
    private void ToggleZonaDanio(ZonaItemViewModel item)
    {
        item.IsSelected = !item.IsSelected;

        if (item.IsSelected)
            AgregarDanio(item.Zona.Id);
        else
            RemoverDanio(item.Zona.Id);
    }

    private void ActualizarPuedeEnviar()
    {
        bool checklistCompleto =
            _candados.HasValue &&
            _licencia.HasValue &&
            _danios.HasValue &&
            _llantas.HasValue &&
            _luces.HasValue &&
            _fugas.HasValue;

        PuedeEnviar =
            SucursalSeleccionada != null &&
            VehiculoSeleccionado != null &&
            ChoferSeleccionado != null &&
            checklistCompleto;
    }

    [RelayCommand]
    private void TodoOk() => OnTodoOk?.Invoke();

    [RelayCommand]
    private void Limpiar()
    {
        Observacion = null;
        DaniosSeleccionados.Clear();
        foreach (var zona in ZonasSeleccionables) zona.IsSelected = false;
        PanelDaniosVisible = false;
        OnLimpiar?.Invoke();
    }

    [RelayCommand]
    private async Task Enviar()
    {
        if (!PuedeEnviar || IsBusy) return;

        IsBusy = true; // Activa el estado de carga
        PuedeEnviar = false; // Opcional: apaga el botón
        try 
        {
            var request = new CrearChecklistRequest
            {
                FechaHora = DateTime.Now,
                TipoRegistro = _tipoRegistro.ToString(),
                IdVehiculo = VehiculoSeleccionado!.Id,
                IdSucursal = SucursalSeleccionada!.Id,
                NombreChofer = ChoferSeleccionado!.Nombre,
                Candados = _candados ?? false,
                Licencia = _licencia ?? false,
                SinDaniosNuevos = _danios ?? false,
                LlantasBienEstado = _llantas ?? false,
                LucesFuncionando = _luces ?? false,
                SinFugasVisibles = _fugas ?? false,
                Observacion = Observacion,
                DaniosReportados = DaniosSeleccionados.ToList()
            };

            var resultado = await _api.RegistrarChecklistAsync(request);

            if (resultado.HasValue)
            {
                _tipoAccesoVm.IncrementarRegistros();
                OnRegistroEnviado?.Invoke(resultado.Value.Mensaje);
            }
        }
        finally
        {
            IsBusy = false; // Apaga el estado de carga sin importar qué pase
            ActualizarPuedeEnviar(); // Restaura el estado del botón si hubo error
        }
    }

    private void AgregarDanio(int idZona, string? notas = null)
    {
        if (!DaniosSeleccionados.Any(d => d.IdZonaDanio == idZona))
        {
            DaniosSeleccionados.Add(new CrearChecklistDanioRequest
            {
                IdZonaDanio = idZona,
                Notas = notas
            });
        }
    }

    private void RemoverDanio(int idZona)
    {
        var danio = DaniosSeleccionados.FirstOrDefault(d => d.IdZonaDanio == idZona);
        if (danio != null) DaniosSeleccionados.Remove(danio);
    }

    private void ResetearEstadoCompleto()
    {
        SucursalSeleccionada = null;
        VehiculoSeleccionado = null;
        ChoferSeleccionado = null;
        Observacion = null;
        DaniosSeleccionados.Clear();
        PanelDaniosVisible = false;
        _candados = null;
        _licencia = null;
        _danios = null;
        _llantas = null;
        _luces = null;
        _fugas = null;
        ItemsOk = 0;
        ProgresoChecklist = 0;
        PuedeEnviar = false;

        // Notificar a la UI
        OnPropertyChanged(nameof(SucursalSeleccionada));
        OnPropertyChanged(nameof(VehiculoSeleccionado));
        OnPropertyChanged(nameof(ChoferSeleccionado));
        OnPropertyChanged(nameof(Observacion));
        OnPropertyChanged(nameof(PanelDaniosVisible));
        OnPropertyChanged(nameof(ItemsOk));
        OnPropertyChanged(nameof(ProgresoChecklist));
        OnPropertyChanged(nameof(PuedeEnviar));
    }
}