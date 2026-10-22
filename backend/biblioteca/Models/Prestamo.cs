using System.ComponentModel.DataAnnotations;

namespace biblioteca.Models;

public static class EstadosPrestamo
{
    public const string Activo = "Activo";
    public const string Devuelto = "Devuelto";
    public const string Cancelado = "Cancelado";
    public const string Vencido = "Vencido";
}

public class Prestamo : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required]
    public int LibroId { get; set; }
    public virtual Libro? Libro { get; set; }

    [Required]
    public int LectorId { get; set; }
    public virtual Lector? Lector { get; set; }

    public DateTime FechaPrestamo { get; set; } = DateTime.UtcNow;

    public DateTime FechaVencimiento { get; set; } = DateTime.UtcNow.AddDays(14);

    public DateTime? FechaDevolucionReal { get; set; }

    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = EstadosPrestamo.Activo;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(100)]
    public string? UsuarioRegistroId { get; set; }

    public byte[]? RowVersion { get; set; }
}
