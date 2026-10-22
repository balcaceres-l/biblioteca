using System.ComponentModel.DataAnnotations;

namespace biblioteca.Models;

public class Categoria : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<Libro> Libros { get; set; } = new List<Libro>();
}
