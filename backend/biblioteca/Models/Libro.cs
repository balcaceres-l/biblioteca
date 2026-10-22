using System.ComponentModel.DataAnnotations;

namespace biblioteca.Models;

public class Libro : IConcurrencyEntity
{
    public int Id { get; set; }

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

    [MaxLength(500)]
    public string? PortadaUrl { get; set; }

    [Required]
    public int AutorId { get; set; }
    public virtual Autor? Autor { get; set; }

    [Required]
    public int CategoriaId { get; set; }
    public virtual Categoria? Categoria { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
