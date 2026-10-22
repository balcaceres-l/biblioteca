using System.ComponentModel.DataAnnotations;

namespace biblioteca.Models;

public class Lector : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio")]
    [MaxLength(50)]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;

    // Vinculación opcional con la cuenta de usuario de Identity
    public string? UsuarioId { get; set; }
    public virtual ApplicationUser? Usuario { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
