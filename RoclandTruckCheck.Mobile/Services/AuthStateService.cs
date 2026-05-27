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

    public AuthStateService(ApiService apiService)
    {
        _apiService = apiService;
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
                CerrarSesion(); // limpiar tokens vencidos
                return false;
            }

            Token = token;
            NombreGuardia = nombre ?? "";
            GuardiaId = int.Parse(idStr);
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
        // 1. Configuramos el token temporalmente en el ApiService para poder hacer la petición
        _apiService.SetAuthToken(loginResponse.Token);

        // 2. Consumimos el endpoint 'mi-perfil'
        // Aquí es donde tu backend valida que el token sea Directo, que el rol sea 'Guardia' y la plataforma 'Mobile'.
        var perfil = await _apiService.ObtenerMiPerfilAsync();

        // 3. Validamos la respuesta
        if (perfil == null || perfil.NombreRol != "Guardia")
        {
            // El backend rechazó el acceso (ej. un Gerente intentó loguearse en la app móvil)
            CerrarSesion();
            return false;
        }

        // 4. Si todo está en orden, guardamos la sesión en el SecureStorage.
        // Nota: Tomamos el NombreCompleto y el UsuarioId directamente del loginResponse 
        // ya que SuperAdmin nos los envía en la primera respuesta.
        GuardarSesion(
            token: loginResponse.Token,
            nombre: loginResponse.NombreCompleto,
            id: loginResponse.UsuarioId
        );

        return true;
    }
}