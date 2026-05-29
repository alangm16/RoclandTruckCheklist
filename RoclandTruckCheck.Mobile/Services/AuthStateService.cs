using RoclandTruckCheck.Mobile.Models;
using System.Net.Http.Json;

namespace RoclandTruckCheck.Mobile.Services;

public class AuthStateService
{
    public string Token { get; private set; } = string.Empty;
    public string NombreGuardia { get; private set; } = string.Empty;
    public int GuardiaId { get; private set; }
    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    private readonly ApiService _apiService;
    private readonly SesionGuardia _sesion;

    public AuthStateService(ApiService apiService, SesionGuardia sesion)
    {
        _apiService = apiService;
        _sesion = sesion;
    }

    public void GuardarSesion(string token, string refreshToken, string nombre, int id, DateTime expiracionToken)
    {
        Token = token;
        NombreGuardia = nombre;
        GuardiaId = id;

        SecureStorage.Default.SetAsync("jwt_token", token);
        SecureStorage.Default.SetAsync("refresh_token", refreshToken);
        SecureStorage.Default.SetAsync("guardia_nombre", nombre);
        SecureStorage.Default.SetAsync("guardia_id", id.ToString());
        SecureStorage.Default.SetAsync("token_expira", expiracionToken.ToString("O"));
    }

    public async Task<bool> GarantizarTokenValidoAsync()
    {
        if (!EstaAutenticado) return false;

        // ── NUEVO: VERIFICAR LA REGLA DE LAS 6 HORAS ──
        if (!await EsSesionAbsolutaValidaAsync())
        {
            CerrarSesion();
            return false; // Fuerza la salida si pasaron las 6 horas
        }
        // ──────────────────────────────────────────────

        if (_sesion.TokenVigente) return true;

        // Si el token de 60 minutos sigue vigente (con su margen de seguridad de 2 min), no hace nada
        if (_sesion.TokenVigente) return true;

        // Si ya expiró o está por expirar, recuperamos el refresh_token de la memoria segura
        var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return false;

        // Solicitamos renovación al servidor
        var nuevoLogin = await _apiService.RefrescarTokenAsync(refreshToken);
        if (nuevoLogin == null || string.IsNullOrEmpty(nuevoLogin.Token))
        {
            CerrarSesion();
            return false;
        }

        // Actualizamos las credenciales locales y del HttpClient
        GuardarSesion(nuevoLogin.Token, nuevoLogin.RefreshToken, NombreGuardia, GuardiaId, nuevoLogin.Expiracion);

        Token = nuevoLogin.Token;
        _sesion.Token = nuevoLogin.Token;
        _sesion.RefreshToken = nuevoLogin.RefreshToken;
        _sesion.TokenExpiracion = nuevoLogin.Expiracion;

        _apiService.SetAuthToken(nuevoLogin.Token);

        return true;
    }

    public async Task<bool> RestaurarSesionAsync()
    {
        try
        {
            // ── NUEVO: VERIFICAR LA REGLA DE LAS 6 HORAS ──
            if (!await EsSesionAbsolutaValidaAsync())
            {
                CerrarSesion();
                return false;
            }

            var token = await SecureStorage.Default.GetAsync("jwt_token");
            var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
            var nombre = await SecureStorage.Default.GetAsync("guardia_nombre");
            var idStr = await SecureStorage.Default.GetAsync("guardia_id");
            var expiraStr = await SecureStorage.Default.GetAsync("token_expira");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(idStr))
                return false;

            Token = token;
            NombreGuardia = nombre ?? "";
            GuardiaId = int.Parse(idStr);

            _apiService.SetAuthToken(token);

            _sesion.Token = token;
            _sesion.RefreshToken = refreshToken ?? "";
            _sesion.NombreCompleto = NombreGuardia;
            _sesion.UsuarioId = GuardiaId;

            if (DateTime.TryParse(expiraStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expira))
            {
                _sesion.TokenExpiracion = expira;
            }

            // Validamos/Refrescamos inmediatamente al abrir la app si el token ya caducó en el tiempo inactivo
            bool tokenValido = await GarantizarTokenValidoAsync();
            if (!tokenValido) return false;

            var catalogos = await _apiService.ObtenerCatalogosAsync();
            if (catalogos != null)
            {
                _sesion.Vehiculos = catalogos.Vehiculos;
                _sesion.Sucursales = catalogos.Sucursales;
                _sesion.Choferes = catalogos.Choferes;
                _sesion.ZonasDanio = catalogos.ZonasDanio;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EsSesionAbsolutaValidaAsync()
    {
        var expiraStr = await SecureStorage.Default.GetAsync("sesion_absoluta_expira");
        if (string.IsNullOrEmpty(expiraStr)) return false;

        if (DateTime.TryParse(expiraStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiraAbsoluta))
        {
            // Si la hora actual ya superó la hora de expiración absoluta (6 horas), la sesión es inválida
            if (DateTime.Now >= expiraAbsoluta) return false;

            return true; // Sigue dentro de las 6 horas de turno
        }
        return false;
    }

    public void CerrarSesion()
    {
        Token = string.Empty;
        NombreGuardia = string.Empty;
        GuardiaId = 0;
        SecureStorage.Default.Remove("jwt_token");
        SecureStorage.Default.Remove("refresh_token");
        SecureStorage.Default.Remove("guardia_nombre");
        SecureStorage.Default.Remove("guardia_id");
        SecureStorage.Default.Remove("token_expira");
        SecureStorage.Default.Remove("sesion_absoluta_expira");
    }

    public async Task<bool> IniciarSesionAsync(string username, string password)
    {
        try
        {
            var loginResponse = await _apiService.LoginDirectoAsync(username, password);
            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token)) return false;
            return await ProcesarSesionExitosa(loginResponse);
        }
        catch { return false; }
    }

    public async Task<bool> IniciarSesionPorQrAsync(string qrCode)
    {
        try
        {
            var loginResponse = await _apiService.LoginQrAsync(qrCode);
            if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token)) return false;
            return await ProcesarSesionExitosa(loginResponse);
        }
        catch { return false; }
    }

    private async Task<bool> ProcesarSesionExitosa(LoginResponse loginResponse)
    {
        _apiService.SetAuthToken(loginResponse.Token);

        var perfil = await _apiService.ObtenerMiPerfilAsync();
        if (perfil == null || perfil.NombreRol != "Guardia")
        {
            CerrarSesion();
            return false;
        }

        // ── NUEVO: ESTABLECER EL LÍMITE MAESTRO DE 6 HORAS ──
        var expiracionAbsoluta = DateTime.Now.AddHours(8);
        SecureStorage.Default.SetAsync("sesion_absoluta_expira", expiracionAbsoluta.ToString("O"));
        // ────────────────────────────────────────────────────

        GuardarSesion(
            token: loginResponse.Token,
            refreshToken: loginResponse.RefreshToken,
            nombre: loginResponse.NombreCompleto,
            id: loginResponse.UsuarioId,
            expiracionToken: loginResponse.Expiracion
        );

        _sesion.UsuarioId = loginResponse.UsuarioId;
        _sesion.NombreCompleto = loginResponse.NombreCompleto;
        _sesion.Token = loginResponse.Token;
        _sesion.RefreshToken = loginResponse.RefreshToken;
        _sesion.TokenExpiracion = loginResponse.Expiracion;

        var catalogos = await _apiService.ObtenerCatalogosAsync();
        if (catalogos != null)
        {
            _sesion.Vehiculos = catalogos.Vehiculos;
            _sesion.Sucursales = catalogos.Sucursales;
            _sesion.Choferes = catalogos.Choferes;
            _sesion.ZonasDanio = catalogos.ZonasDanio;
        }

        return true;
    }
}