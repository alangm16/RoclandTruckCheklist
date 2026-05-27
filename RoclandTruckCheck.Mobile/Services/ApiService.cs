using RoclandTruckCheck.Mobile.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoclandTruckCheck.Mobile.Services;

public class  ApiService
{
    private readonly HttpClient _http;
    private readonly IServiceProvider _serviceProvider;

    private static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? AppConstants.BaseUrlAndroid : AppConstants.BaseUrlWindows;

    private const string ApiBasePath = "api/mob/truckcheck";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            }
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    private void SetAuthHeader()
    {
        var authService = _serviceProvider.GetService<AuthStateService>();
        if (authService != null && !string.IsNullOrEmpty(authService.Token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authService.Token);
        }
    }

    public void SetAuthToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ======================== AUTENTICACIÓN ========================

    public async Task<LoginResponse?> LoginDirectoAsync(string username, string password)
    {
        try
        {
            var payload = new
            {
                Username = username,
                Password = password,
                CodigoProyecto = AppConstants.CodigoProyectoGuardiaRelevo,
                Plataforma = AppConstants.PlataformaMobile
            };

            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-directo", payload);
            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlertAsync("Error Backend", $"Status: {response.StatusCode}\n{rawJson}", "OK");
                });
                return null;
            }

            return JsonSerializer.Deserialize<LoginResponse>(rawJson, JsonOpts);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlertAsync("Error al convertir JSON", ex.Message, "OK");
            });
            return null;
        }
    }

    public async Task<LoginResponse?> LoginQrAsync(string qrCode)
    {
        try
        {
            var payload = new
            {
                QrCode = qrCode,
                CodigoProyecto = AppConstants.CodigoProyectoGuardiaRelevo,
                Plataforma = AppConstants.PlataformaMobile
            };

            var response = await _http.PostAsJsonAsync("api/superadmin/Auth/login-qr", payload);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoginQrAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<PerfilContextoDto?> ObtenerMiPerfilAsync()
    {
        try
        {
            SetAuthHeader(); // Asegura que el token esté en la cabecera
            var response = await _http.GetAsync("api/mob/truckcheck/Auth/mi-perfil");

            // Si el backend devuelve 403 Forbidden (ej. no es Guardia o no es Mobile), fallará aquí
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<PerfilContextoDto>(JsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObtenerMiPerfilAsync error: {ex.Message}");
            return null;
        }
    }

    // ======================== GUARDIAS ========================

    public async Task<CatalogosMobileResponse?> ObtenerCatalogosAsync()
    {
        try
        {
            SetAuthHeader();
            var response = await _http.GetAsync($"{ApiBasePath}/Guardias/catalogos");

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<CatalogosMobileResponse>(JsonOpts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObtenerCatalogosAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<(int Id, string Mensaje)?> RegistrarChecklistAsync(CrearChecklistRequest request)
    {
        try
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync($"{ApiBasePath}/Guardias/checklist", request);
            var rawJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlertAsync("Error al registrar", $"Status: {response.StatusCode}\n{rawJson}", "OK");
                });
                return null;
            }

            var resultado = JsonSerializer.Deserialize<CrearChecklistResponse>(rawJson, JsonOpts);
            if (resultado == null) return null;

            return (resultado.Id, resultado.Mensaje);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlertAsync("Error de conexión", ex.Message, "OK");
            });
            return null;
        }
    }
}