
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