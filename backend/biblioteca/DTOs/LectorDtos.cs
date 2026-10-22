using System.ComponentModel.DataAnnotations;

namespace biblioteca.DTOs;

public class LectorReadDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public string? UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int PrestamosActivosCount { get; set; }
    public string? RowVersion { get; set; }
}

public class LectorCreateDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio")]
    [MaxLength(50)]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
}

public class LectorUpdateDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio")]
    [MaxLength(50)]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; }

    [Required(ErrorMessage = "El token de concurrencia (RowVersion) es obligatorio")]
    public string RowVersion { get; set; } = string.Empty;
}

public class LectorEstadoPatchDto
{
    public bool Activo { get; set; }
}
