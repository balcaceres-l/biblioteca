using System.ComponentModel.DataAnnotations;

namespace biblioteca.Models;

public class Autor : IConcurrencyEntity
{
    public int Id { get; set; }

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

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<Libro> Libros { get; set; } = new List<Libro>();
}
