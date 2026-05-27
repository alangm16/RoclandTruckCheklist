using RoclandTruckCheck.Mobile.Models;

namespace RoclandTruckCheck.Mobile.Services;

public class AuthStateService
{
    public string Token { get; private set; } = string.Empty;
    public string NombreGuardia { get; private set; } = string.Empty;
    public int GuardiaId { get; private set; }
    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    private const int SesionMinutos = 2;
    private readonly ApiService _apiService;
    private readonly SesionGuardia _sesion;

    public AuthStateService(ApiService apiService, SesionGuardia sesion)
    {
        _apiService = apiService;
        _sesion = sesion;
    }

    public void GuardarSesion(string token, string nombre, int id)
    {
        Token = token;
        NombreGuardia = nombre;
        GuardiaId = id;

        var expiracion = DateTime.UtcNow.AddMinutes(SesionMinutos).ToString("O");

        SecureStorage.Default.SetAsync("jwt_token", token);
        SecureStorage.Default.SetAsync("guardia_nombre", nombre);
        SecureStorage.Default.SetAsync("guardia_id", id.ToString());
        SecureStorage.Default.SetAsync("sesion_expira", expiracion); // ← timestamp de expiración
    }

    public async Task<bool> RestaurarSesionAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            var nombre = await SecureStorage.Default.GetAsync("guardia_nombre");
            var idStr = await SecureStorage.Default.GetAsync("guardia_id");
            var expiraStr = await SecureStorage.Default.GetAsync("sesion_expira");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(idStr))
                return false;

            // Validar expiración
            if (string.IsNullOrEmpty(expiraStr) ||
                !DateTime.TryParse(expiraStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expira) ||
                DateTime.UtcNow >= expira)
            {
                CerrarSesion();
                return false;
            }

            // 1. Restaurar propiedades de la clase
            Token = token;
            NombreGuardia = nombre ?? "";
            GuardiaId = int.Parse(idStr);

            // 2. CONFIGURAR EL TOKEN PARA LAS PETICIONES API
            _apiService.SetAuthToken(token);

            // 3. POBLAR EL OBJETO SESIONGUARDIA INYECTADO
            _sesion.Token = token;
            _sesion.NombreCompleto = NombreGuardia;
            _sesion.UsuarioId = GuardiaId;
            _sesion.TokenExpiracion = expira;

            // 4. DESCARGAR CATÁLOGOS NUEVAMENTE PARA TENERLOS LISTOS
            var catalogos = await _apiService.ObtenerCatalogosAsync();
            if (catalogos != null)
            {
                _sesion.Vehiculos = catalogos.Vehiculos;
                _sesion.Sucursales = catalogos.Sucursales;
                _sesion.Choferes = catalogos.Choferes;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void CerrarSesion()
    {
        Token = string.Empty;
        NombreGuardia = string.Empty;
        GuardiaId = 0;
        SecureStorage.Default.Remove("jwt_token");
        SecureStorage.Default.Remove("guardia_nombre");
        SecureStorage.Default.Remove("guardia_id");
        SecureStorage.Default.Remove("sesion_expira"); // ← limpiar también el timestamp
    }

    // --- sin cambios abajo ---

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

        GuardarSesion(
            token: loginResponse.Token,
            nombre: loginResponse.NombreCompleto,
            id: loginResponse.UsuarioId
        );

        // ── Poblar SesionGuardia con catálogos y datos del guardia ──
        _sesion.UsuarioId = loginResponse.UsuarioId;
        _sesion.NombreCompleto = loginResponse.NombreCompleto;
        _sesion.Token = loginResponse.Token;
        _sesion.TokenExpiracion = loginResponse.Expiracion;

        var catalogos = await _apiService.ObtenerCatalogosAsync();
        if (catalogos != null)
        {
            _sesion.Vehiculos = catalogos.Vehiculos;
            _sesion.Sucursales = catalogos.Sucursales;
            _sesion.Choferes = catalogos.Choferes;
        }

        return true;
    }
}