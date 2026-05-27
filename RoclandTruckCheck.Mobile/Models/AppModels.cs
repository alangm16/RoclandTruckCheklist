
using System.Text.Json.Serialization;

namespace RoclandTruckCheck.Mobile.Models;

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class QrLoginRequest
{
    [JsonPropertyName("qrCode")]
    public string QRCode { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string Token { get; set; } = string.Empty;
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("expiracion")]
    public DateTime Expiracion { get; set; }
    [JsonPropertyName("usuario")]
    public UsuarioTokenDto? Usuario { get; set; }

    // Propiedades de conveniencia (mapeadas desde Usuario)
    public string NombreCompleto => Usuario?.NombreCompleto ?? string.Empty;
    public string Username => Usuario?.Username ?? string.Empty;
    public int UsuarioId => Usuario?.Id ?? 0;
}

public class UsuarioTokenDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class PerfilContextoDto
{
    [JsonPropertyName("superAdminUsuarioId")]
    public int SuperAdminUsuarioId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } = string.Empty;

    [JsonPropertyName("nivelRol")]
    public int NivelRol { get; set; }

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;
}

// ──────────────────────────────────────────────────────────────
//  CATÁLOGOS (espejo de los DTOs del backend, para uso local)
// ──────────────────────────────────────────────────────────────

public class VehiculoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("placas")]
    public string Placas { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public class SucursalDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public class ChoferDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public class CatalogosMobileResponse
{
    [JsonPropertyName("vehiculos")]
    public List<VehiculoDto> Vehiculos { get; set; } = new();

    [JsonPropertyName("sucursales")]
    public List<SucursalDto> Sucursales { get; set; } = new();

    [JsonPropertyName("choferes")]
    public List<ChoferDto> Choferes { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────
//  CHECKLIST — REQUEST Y RESPONSE
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Lo que el guardia llena y se envía al backend.
/// TipoRegistro: "Entrada" | "Salida"
/// </summary>
public class CrearChecklistRequest
{
    [JsonPropertyName("fechaHora")]
    public DateTime? FechaHora { get; set; }

    [JsonPropertyName("tipoRegistro")]
    public string TipoRegistro { get; set; } = string.Empty;

    [JsonPropertyName("idSucursal")]
    public int IdSucursal { get; set; }

    [JsonPropertyName("idVehiculo")]
    public int IdVehiculo { get; set; }

    [JsonPropertyName("nombreChofer")]
    public string NombreChofer { get; set; } = string.Empty;

    [JsonPropertyName("candados")]
    public bool Candados { get; set; }

    [JsonPropertyName("licencia")]
    public bool Licencia { get; set; }

    [JsonPropertyName("sinDaniosNuevos")]
    public bool SinDaniosNuevos { get; set; }

    [JsonPropertyName("llantasBienEstado")]
    public bool LlantasBienEstado { get; set; }

    [JsonPropertyName("lucesFuncionando")]
    public bool LucesFuncionando { get; set; }

    [JsonPropertyName("sinFugasVisibles")]
    public bool SinFugasVisibles { get; set; }

    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }
}

/// <summary>
/// Respuesta simple tras crear el checklist: solo el ID generado.
/// </summary>
public class CrearChecklistResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;
}

// ──────────────────────────────────────────────────────────────
//  ESTADO LOCAL DE LA SESIÓN DEL GUARDIA
// ──────────────────────────────────────────────────────────────

/// <summary>
/// Se guarda en memoria (o SecureStorage) tras el login.
/// Concentra todo lo que la app necesita durante la sesión activa.
/// </summary>
public class SesionGuardia
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiracion { get; set; }

    // Catálogos cargados al iniciar sesión — evita llamadas repetidas
    public List<VehiculoDto> Vehiculos { get; set; } = new();
    public List<SucursalDto> Sucursales { get; set; } = new();
    public List<ChoferDto> Choferes { get; set; } = new();

    // Conveniencia: CEDIS resuelto de la lista de sucursales
    public SucursalDto? SucursalCedis =>
        Sucursales.FirstOrDefault(s =>
            s.Nombre.Equals("CEDIS", StringComparison.OrdinalIgnoreCase));

    public bool TokenVigente =>
        DateTime.UtcNow < TokenExpiracion.AddMinutes(-2); // margen de 2 min
}

// ──────────────────────────────────────────────────────────────
//  SELECCIÓN DE TIPO DE REGISTRO (para la pantalla de cards)
// ──────────────────────────────────────────────────────────────

public enum TipoRegistro
{
    Entrada,
    Salida
}