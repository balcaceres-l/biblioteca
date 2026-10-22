using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace biblioteca.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Relación opcional 1 a 1 con Lector si este usuario es lector
    public int? LectorId { get; set; }
    public virtual Lector? Lector { get; set; }
}
