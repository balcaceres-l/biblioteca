using System.ComponentModel.DataAnnotations;
using biblioteca.Models;

namespace biblioteca.DTOs;

public class PrestamoReadDto
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public string LibroISBN { get; set; } = string.Empty;
    public string? LibroPortadaUrl { get; set; }

    public int LectorId { get; set; }
    public string LectorNombreCompleto { get; set; } = string.Empty;
    public string LectorDocumento { get; set; } = string.Empty;

    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucionReal { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public bool EstaVencido => Estado == EstadosPrestamo.Activo && DateTime.UtcNow > FechaVencimiento;
    public string? RowVersion { get; set; }
}

public class PrestamoCreateDto
{
    [Required(ErrorMessage = "El libro es obligatorio")]
    public int LibroId { get; set; }

    // Opcional si el usuario que solicita es un Lector (se infiere de su sesión)
    public int? LectorId { get; set; }

    [Range(1, 60, ErrorMessage = "El plazo de préstamo debe estar entre 1 y 60 días")]
    public int DiasPrestamo { get; set; } = 14;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    // Opcional para validación de concurrencia optimista sobre el libro desde la vista
    public string? LibroRowVersion { get; set; }
}

public class PrestamoDevolucionPatchDto
{
    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

public class PrestamoCancelacionPatchDto
{
    [Required(ErrorMessage = "El motivo de cancelación es obligatorio")]
    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}
