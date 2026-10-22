using System.ComponentModel.DataAnnotations;

namespace biblioteca.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio")]
    [MaxLength(50)]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }
}

public class CreateUserDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio (Administrador, Bibliotecario, Lector)")]
    public string Rol { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string[] Permisos { get; set; } = [];
    public int? LectorId { get; set; }
    public DateTime Expiracion { get; set; }
}

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string[] Permisos { get; set; } = [];
    public int? LectorId { get; set; }
}
