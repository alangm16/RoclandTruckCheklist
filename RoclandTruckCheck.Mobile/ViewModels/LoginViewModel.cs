
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandTruckCheck.Mobile.Services;

namespace RoclandTruckCheck.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private string _usuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _hayError;

    public LoginViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Guardia - Acceso";
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MostrarError("Ingresa usuario y contraseña.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            // Usamos el AuthStateService que centraliza y orquesta el login completo
            var exito = await _auth.IniciarSesionAsync(Usuario, Password);

            if (exito)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                // El mensaje es más amplio, porque el error real puede ser de roles o credenciales
                MostrarError("Error: Credenciales inválidas, usuario sin rol de 'Guardia' o sin perfil asignado.");
            }
        }
        catch (Exception)
        {
            MostrarError("Ocurrió un error inesperado de conexión.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    public async Task IniciarSesionQrAsync(string qr)
    {
        if (string.IsNullOrWhiteSpace(qr))
        {
            MostrarError("Código QR inválido.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            var exito = await _auth.IniciarSesionPorQrAsync(qr);

            if (exito)
            {
                await Shell.Current.GoToAsync("//TipoAcceso");
            }
            else
            {
                MostrarError("Error al iniciar sesión con QR. Verifica que el código sea correcto y que tu usuario tenga un perfil asignado.");
            }

        }
        catch (Exception ex)
        {
            MostrarError("Ocurrión un error inesperado.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void MostrarError(string msg)
    {
        MensajeError = msg;
        HayError = true;
    }
}