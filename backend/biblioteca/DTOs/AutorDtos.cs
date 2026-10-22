using System.ComponentModel.DataAnnotations;

namespace biblioteca.DTOs;

public class AutorReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string? Nacionalidad { get; set; }
    public string? Biografia { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public int CantidadLibros { get; set; }
    public string? RowVersion { get; set; }
}

public class AutorCreateDto
{
    [Required(ErrorMessage = "El nombre del autor es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del autor es obligatorio")]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nacionalidad { get; set; }

    [MaxLength(2000)]
    public string? Biografia { get; set; }

    public DateTime? FechaNacimiento { get; set; }
}

public class AutorUpdateDto
{
    [Required(ErrorMessage = "El nombre del autor es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del autor es obligatorio")]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nacionalidad { get; set; }

    [MaxLength(2000)]
    public string? Biografia { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El token de concurrencia (RowVersion) es obligatorio")]
    public string RowVersion { get; set; } = string.Empty;
}
