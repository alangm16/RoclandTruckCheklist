using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandTruckCheck.Mobile.Models;
using RoclandTruckCheck.Mobile.Services;
using System.Collections.ObjectModel;

namespace RoclandTruckCheck.Mobile.ViewModels;

public partial class ChecklistViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly SesionGuardia _sesion;
    private readonly TipoAccesoViewModel _tipoAccesoVm;

    // ── Eventos hacia el code-behind ──────────────────────────────
    public event Action? OnTodoOk;
    public event Action? OnLimpiar;
    public event Action<string>? OnRegistroEnviado;

    // ── Tipo de movimiento ────────────────────────────────────────
    private TipoRegistro _tipoRegistro;

    [ObservableProperty]
    private string _tituloTipo = "ENTRADA";

    [ObservableProperty]
    private string _textoBotonEnviar = "Enviar entrada";

    public TipoRegistro TipoRegistro { get; private set; }

    // ── Catálogos ─────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<SucursalDto> _sucursales = new();

    [ObservableProperty]
    private ObservableCollection<VehiculoDto> _vehiculos = new();

    [ObservableProperty]
    private ObservableCollection<ChoferDto> _choferes = new();
    [ObservableProperty]
    private ObservableCollection<ZonaDto> _zonasDanio = new();

    [ObservableProperty]
    private SucursalDto? _sucursalSeleccionada;

    [ObservableProperty]
    private VehiculoDto? _vehiculoSeleccionado;

    [ObservableProperty]
    private ChoferDto? _choferSeleccionado;

    [ObservableProperty]
    private ObservableCollection<CrearChecklistDanioRequest> _daniosSeleccionados = new();

    // ── Observación ───────────────────────────────────────────────
    [ObservableProperty]
    private string? _observacion;

    // ── Estado de los 6 ítems ─────────────────────────────────────
    private bool? _candados, _licencia, _danios, _llantas, _luces, _fugas;

    [ObservableProperty]
    private int _itemsOk;

    [ObservableProperty]
    private double _progresoChecklist;

    [ObservableProperty]
    private bool _puedeEnviar;

    public ChecklistViewModel(ApiService api, SesionGuardia sesion, TipoAccesoViewModel tipoAccesoVm)
    {
        _api = api;
        _sesion = sesion;
        _tipoAccesoVm = tipoAccesoVm;
    }

    // ─────────────────────────────────────────────────────────────────
    // INICIALIZACIÓN: se llama desde la QueryProperty de la View
    // ─────────────────────────────────────────────────────────────────

    public void InicializarConTipo(TipoRegistro tipo)
    {
        TipoRegistro = tipo;
        _tipoRegistro = tipo;

        TituloTipo = tipo.ToString().ToUpper();
        TextoBotonEnviar = tipo == TipoRegistro.Entrada
            ? "Enviar entrada"
            : "Enviar salida";

        CargarCatalogosDesdeCache();

        PuedeEnviar = false;
        ActualizarPuedeEnviar();
    }

    private void CargarCatalogosDesdeCache()
    {
        Sucursales = new ObservableCollection<SucursalDto>(_sesion.Sucursales);
        Vehiculos = new ObservableCollection<VehiculoDto>(_sesion.Vehiculos);
        Choferes = new ObservableCollection<ChoferDto>(_sesion.Choferes);
        ZonasDanio = new ObservableCollection<ZonaDto>(_sesion.ZonasDanio.Where(z => z.Activo));
    }

    // ─────────────────────────────────────────────────────────────────
    // PARTIAL METHODS — reaccionar a cambios de picker
    // ─────────────────────────────────────────────────────────────────

    partial void OnSucursalSeleccionadaChanged(SucursalDto? value) => ActualizarPuedeEnviar();
    partial void OnVehiculoSeleccionadoChanged(VehiculoDto? value) => ActualizarPuedeEnviar();
    partial void OnChoferSeleccionadoChanged(ChoferDto? value) => ActualizarPuedeEnviar();

    // ─────────────────────────────────────────────────────────────────
    // SINCRONIZACIÓN DESDE CODE-BEHIND (estado de los 6 ítems)
    // ─────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────
    // COMANDOS
    // ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void TodoOk() => OnTodoOk?.Invoke();

    [RelayCommand]
    private void Limpiar()
    {
        Observacion = null;
        OnLimpiar?.Invoke();
        DaniosSeleccionados.Clear();
    }

    [RelayCommand]
    private async Task Enviar()
    {
        if (!PuedeEnviar) return;

        var request = new CrearChecklistRequest
        {
            FechaHora = DateTime.UtcNow,
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

    public void AgregarDanio(int idZona, string? notas = null)
    {
        // Evitar duplicados
        if (!DaniosSeleccionados.Any(d => d.IdZonaDanio == idZona))
        {
            DaniosSeleccionados.Add(new CrearChecklistDanioRequest
            {
                IdZonaDanio = idZona,
                Notas = notas
            });
        }
    }

    public void RemoverDanio(int idZona)
    {
        var danio = DaniosSeleccionados.FirstOrDefault(d => d.IdZonaDanio == idZona);
        if (danio != null)
        {
            DaniosSeleccionados.Remove(danio);
        }
    }
}