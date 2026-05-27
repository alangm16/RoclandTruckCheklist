using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandTruckCheck.Mobile.Services;

namespace RoclandTruckCheck.Mobile.ViewModels;

public partial class TipoAccesoViewModel : ObservableObject
{
    private readonly AuthStateService _auth;
    private readonly ApiService _api;

    [ObservableProperty]
    private string _nombreGuardia = string.Empty;

    [ObservableProperty]
    private string _resumenTurno = "0 registros en este dispositivo";

    private int _registrosTurno = 0;

    public TipoAccesoViewModel(AuthStateService auth, ApiService api)
    {
        _auth = auth;
        _api = api;
        NombreGuardia = auth.NombreGuardia;
    }

    [RelayCommand]
    public void RefrescarResumen()
    {
        ResumenTurno = _registrosTurno == 0
            ? "0 registros en este dispositivo"
            : $"{_registrosTurno} registro{(_registrosTurno > 1 ? "s" : "")} en este turno";
    }

    public void IncrementarRegistros()
    {
        _registrosTurno++;
        RefrescarResumen();
    }

    public async Task CerrarSesionAsync()
    {
        _auth.CerrarSesion();
        _registrosTurno = 0;
        await Shell.Current.GoToAsync("//LoginPage");
    }
}