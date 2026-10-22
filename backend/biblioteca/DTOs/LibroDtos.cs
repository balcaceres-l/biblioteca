using System.ComponentModel.DataAnnotations;

namespace biblioteca.DTOs;

public class LibroReadDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Resumen { get; set; }
    public int AnioPublicacion { get; set; }
    public string? Editorial { get; set; }
    public bool Disponible { get; set; }
    public string? PortadaUrl { get; set; }

    public int AutorId { get; set; }
    public string AutorNombre { get; set; } = string.Empty;

    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;

    public string? RowVersion { get; set; }
}

public class LibroCreateDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ISBN es obligatorio")]
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Resumen { get; set; }

    [Range(1000, 2100, ErrorMessage = "Año de publicación inválido")]
    public int AnioPublicacion { get; set; }

    [MaxLength(100)]
    public string? Editorial { get; set; }

    public bool Disponible { get; set; } = true;

    [Required(ErrorMessage = "El autor es obligatorio")]
    public int AutorId { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    public int CategoriaId { get; set; }
}

public class LibroUpdateDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ISBN es obligatorio")]
    [MaxLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Resumen { get; set; }

    [Range(1000, 2100, ErrorMessage = "Año de publicación inválido")]
    public int AnioPublicacion { get; set; }

    [MaxLength(100)]
    public string? Editorial { get; set; }

    public bool Disponible { get; set; }

    [Required(ErrorMessage = "El autor es obligatorio")]
    public int AutorId { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "El token de concurrencia (RowVersion) es obligatorio")]
    public string RowVersion { get; set; } = string.Empty;
}

public class LibroDisponibilidadPatchDto
{
    public bool Disponible { get; set; }
}

public class LibroDisponibilidadResponseDto
{
    public int LibroId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public bool Disponible { get; set; }
    public int? PrestamoActivoId { get; set; }
    public string? LectorPrestamoNombre { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
