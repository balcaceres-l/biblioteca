using System.ComponentModel.DataAnnotations;

namespace biblioteca.DTOs;

public class CategoriaReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }
    public int CantidadLibros { get; set; }
    public string? RowVersion { get; set; }
}

public class CategoriaCreateDto
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;
}

public class CategoriaUpdateDto
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; }

    [Required(ErrorMessage = "El token de concurrencia (RowVersion) es obligatorio")]
    public string RowVersion { get; set; } = string.Empty;
}

public class CategoriaEstadoPatchDto
{
    public bool Activa { get; set; }
}
