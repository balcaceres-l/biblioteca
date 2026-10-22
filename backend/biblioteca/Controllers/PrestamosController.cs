using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using biblioteca.Data;
using biblioteca.DTOs;
using biblioteca.DTOs.Common;
using biblioteca.Models;

namespace biblioteca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrestamosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PrestamosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<PrestamoReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] int? lectorId,
        [FromQuery] int? libroId,
        [FromQuery] bool? soloVencidos,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .AsQueryable();

        // Si es rol Lector, restringir obligatoriamente a sus propios préstamos
        if (User.IsInRole(Roles.Lector) && !User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.Bibliotecario))
        {
            var myLectorId = ObtenerLectorIdDelUsuarioActual();
            if (myLectorId == null)
            {
                return Ok(new PagedResultDto<PrestamoReadDto> { Page = page, PageSize = pageSize });
            }
            query = query.Where(p => p.LectorId == myLectorId.Value);
        }
        else if (lectorId.HasValue)
        {
            query = query.Where(p => p.LectorId == lectorId.Value);
        }

        if (libroId.HasValue)
        {
            query = query.Where(p => p.LibroId == libroId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(p => p.Estado == estado.Trim());
        }

        if (soloVencidos == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(p => p.Estado == EstadosPrestamo.Activo && p.FechaVencimiento < now);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.FechaPrestamo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PrestamoReadDto
            {
                Id = p.Id,
                LibroId = p.LibroId,
                LibroTitulo = p.Libro!.Titulo,
                LibroISBN = p.Libro.ISBN,
                LibroPortadaUrl = p.Libro.PortadaUrl,
                LectorId = p.LectorId,
                LectorNombreCompleto = p.Lector!.NombreCompleto,
                LectorDocumento = p.Lector.DocumentoIdentidad,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                FechaDevolucionReal = p.FechaDevolucionReal,
                Estado = p.Estado,
                Observaciones = p.Observaciones,
                RowVersion = p.RowVersion != null ? Convert.ToBase64String(p.RowVersion) : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<PrestamoReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("activos")]
    [Authorize(Policy = Policies.PrestamosDevolver)]
    [ProducesResponseType(typeof(PagedResultDto<PrestamoReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivos([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await GetAll(EstadosPrestamo.Activo, null, null, false, page, pageSize);
    }

    [HttpGet("vencidos")]
    [Authorize(Policy = Policies.PrestamosDevolver)]
    [ProducesResponseType(typeof(PagedResultDto<PrestamoReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVencidos([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await GetAll(EstadosPrestamo.Activo, null, null, true, page, pageSize);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PrestamoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var prestamo = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.Id == id)
            .Select(p => new PrestamoReadDto
            {
                Id = p.Id,
                LibroId = p.LibroId,
                LibroTitulo = p.Libro!.Titulo,
                LibroISBN = p.Libro.ISBN,
                LibroPortadaUrl = p.Libro.PortadaUrl,
                LectorId = p.LectorId,
                LectorNombreCompleto = p.Lector!.NombreCompleto,
                LectorDocumento = p.Lector.DocumentoIdentidad,
                FechaPrestamo = p.FechaPrestamo,
                FechaVencimiento = p.FechaVencimiento,
                FechaDevolucionReal = p.FechaDevolucionReal,
                Estado = p.Estado,
                Observaciones = p.Observaciones,
                RowVersion = p.RowVersion != null ? Convert.ToBase64String(p.RowVersion) : null
            })
            .FirstOrDefaultAsync();

        if (prestamo == null) return NotFound(new { message = $"Préstamo con ID {id} no encontrado." });

        if (User.IsInRole(Roles.Lector) && !User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.Bibliotecario))
        {
            var myLectorId = ObtenerLectorIdDelUsuarioActual();
            if (myLectorId != prestamo.LectorId)
            {
                return Forbid();
            }
        }

        return Ok(prestamo);
    }

    [HttpPost]
    [Authorize(Policy = Policies.PrestamosCrear)]
    [ProducesResponseType(typeof(PrestamoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarPrestamo([FromBody] PrestamoCreateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Determinar el lector solicitante
        int lectorIdFinal;
        if (User.IsInRole(Roles.Lector) && !User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.Bibliotecario))
        {
            var myLectorId = ObtenerLectorIdDelUsuarioActual();
            if (!myLectorId.HasValue)
            {
                return BadRequest(new { message = "El usuario no cuenta con un perfil de lector activo para solicitar préstamos." });
            }
            lectorIdFinal = myLectorId.Value;
        }
        else
        {
            if (!model.LectorId.HasValue)
            {
                return BadRequest(new { message = "Debe indicar el LectorId para registrar el préstamo." });
            }
            lectorIdFinal = model.LectorId.Value;
        }

        // Iniciar transacción explícita para asegurar atomicidad y concurrencia estricta
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validar existencia y estado del lector
            var lector = await _context.Lectores.FirstOrDefaultAsync(l => l.Id == lectorIdFinal);
            if (lector == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = $"El lector con ID {lectorIdFinal} no existe." });
            }

            if (!lector.Activo)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = $"El lector '{lector.NombreCompleto}' se encuentra inactivo y no puede recibir préstamos." });
            }

            // Validar que el libro exista y esté disponible
            var libro = await _context.Libros.FirstOrDefaultAsync(l => l.Id == model.LibroId);
            if (libro == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = $"El libro con ID {model.LibroId} no existe." });
            }

            if (!libro.Disponible)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = $"El libro '{libro.Titulo}' ya no está disponible para préstamo." });
            }

            // Si se envió el RowVersion del libro visto en la interfaz, configurar valor original para detectar colisiones
            if (!string.IsNullOrWhiteSpace(model.LibroRowVersion))
            {
                try
                {
                    var originalRowVersion = Convert.FromBase64String(model.LibroRowVersion);
                    _context.Entry(libro).Property(l => l.RowVersion).OriginalValue = originalRowVersion;
                }
                catch
                {
                    // Si no es un base64 válido, continúa con la verificación estándar
                }
            }

            // Cambiar disponibilidad del libro a false
            libro.Disponible = false;

            // Crear registro del préstamo
            var prestamo = new Prestamo
            {
                LibroId = libro.Id,
                LectorId = lector.Id,
                FechaPrestamo = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(model.DiasPrestamo > 0 ? model.DiasPrestamo : 14),
                Estado = EstadosPrestamo.Activo,
                Observaciones = model.Observaciones?.Trim(),
                UsuarioRegistroId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.Prestamos.Add(prestamo);

            // Guardar cambios dentro de la transacción
            await _context.SaveChangesAsync();

            // Confirmar transacción
            await transaction.CommitAsync();

            var readDto = new PrestamoReadDto
            {
                Id = prestamo.Id,
                LibroId = libro.Id,
                LibroTitulo = libro.Titulo,
                LibroISBN = libro.ISBN,
                LibroPortadaUrl = libro.PortadaUrl,
                LectorId = lector.Id,
                LectorNombreCompleto = lector.NombreCompleto,
                LectorDocumento = lector.DocumentoIdentidad,
                FechaPrestamo = prestamo.FechaPrestamo,
                FechaVencimiento = prestamo.FechaVencimiento,
                FechaDevolucionReal = null,
                Estado = prestamo.Estado,
                Observaciones = prestamo.Observaciones,
                RowVersion = prestamo.RowVersion != null ? Convert.ToBase64String(prestamo.RowVersion) : null
            };

            return CreatedAtAction(nameof(GetById), new { id = prestamo.Id }, readDto);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                message = "Conflicto de concurrencia: el libro acaba de ser prestado o actualizado por otro usuario en este instante.",
                concurrencyConflict = true
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error al procesar el préstamo: {ex.Message}" });
        }
    }

    [HttpPatch("{id:int}/devolucion")]
    [Authorize(Policy = Policies.PrestamosDevolver)]
    [ProducesResponseType(typeof(PrestamoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarDevolucion(int id, [FromBody] PrestamoDevolucionPatchDto model)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Lector)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = $"Préstamo con ID {id} no encontrado." });
            }

            if (prestamo.Estado != EstadosPrestamo.Activo)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = $"El préstamo ya se encuentra en estado '{prestamo.Estado}' y no puede ser devuelto nuevamente." });
            }

            prestamo.Estado = EstadosPrestamo.Devuelto;
            prestamo.FechaDevolucionReal = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.Observaciones))
            {
                prestamo.Observaciones = string.IsNullOrWhiteSpace(prestamo.Observaciones)
                    ? model.Observaciones.Trim()
                    : $"{prestamo.Observaciones} | Devolución: {model.Observaciones.Trim()}";
            }

            // Liberar el libro para futuros préstamos
            if (prestamo.Libro != null)
            {
                prestamo.Libro.Disponible = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var readDto = new PrestamoReadDto
            {
                Id = prestamo.Id,
                LibroId = prestamo.LibroId,
                LibroTitulo = prestamo.Libro?.Titulo ?? "",
                LibroISBN = prestamo.Libro?.ISBN ?? "",
                LibroPortadaUrl = prestamo.Libro?.PortadaUrl,
                LectorId = prestamo.LectorId,
                LectorNombreCompleto = prestamo.Lector?.NombreCompleto ?? "",
                LectorDocumento = prestamo.Lector?.DocumentoIdentidad ?? "",
                FechaPrestamo = prestamo.FechaPrestamo,
                FechaVencimiento = prestamo.FechaVencimiento,
                FechaDevolucionReal = prestamo.FechaDevolucionReal,
                Estado = prestamo.Estado,
                Observaciones = prestamo.Observaciones,
                RowVersion = prestamo.RowVersion != null ? Convert.ToBase64String(prestamo.RowVersion) : null
            };

            return Ok(readDto);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                message = "Conflicto de concurrencia al registrar la devolución. Recargue la información.",
                concurrencyConflict = true
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error al procesar la devolución: {ex.Message}" });
        }
    }

    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Policy = Policies.PrestamosCancelar)]
    [ProducesResponseType(typeof(PrestamoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelarPrestamo(int id, [FromBody] PrestamoCancelacionPatchDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Lector)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = $"Préstamo con ID {id} no encontrado." });
            }

            if (User.IsInRole(Roles.Lector) && !User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.Bibliotecario))
            {
                var myLectorId = ObtenerLectorIdDelUsuarioActual();
                if (myLectorId != prestamo.LectorId)
                {
                    await transaction.RollbackAsync();
                    return Forbid();
                }
            }

            if (prestamo.Estado != EstadosPrestamo.Activo)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = $"El préstamo ya se encuentra en estado '{prestamo.Estado}' y no puede ser cancelado." });
            }

            prestamo.Estado = EstadosPrestamo.Cancelado;
            prestamo.FechaDevolucionReal = DateTime.UtcNow;
            prestamo.Observaciones = string.IsNullOrWhiteSpace(prestamo.Observaciones)
                ? $"Cancelado: {model.Motivo.Trim()}"
                : $"{prestamo.Observaciones} | Cancelado: {model.Motivo.Trim()}";

            if (prestamo.Libro != null)
            {
                prestamo.Libro.Disponible = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var readDto = new PrestamoReadDto
            {
                Id = prestamo.Id,
                LibroId = prestamo.LibroId,
                LibroTitulo = prestamo.Libro?.Titulo ?? "",
                LibroISBN = prestamo.Libro?.ISBN ?? "",
                LibroPortadaUrl = prestamo.Libro?.PortadaUrl,
                LectorId = prestamo.LectorId,
                LectorNombreCompleto = prestamo.Lector?.NombreCompleto ?? "",
                LectorDocumento = prestamo.Lector?.DocumentoIdentidad ?? "",
                FechaPrestamo = prestamo.FechaPrestamo,
                FechaVencimiento = prestamo.FechaVencimiento,
                FechaDevolucionReal = prestamo.FechaDevolucionReal,
                Estado = prestamo.Estado,
                Observaciones = prestamo.Observaciones,
                RowVersion = prestamo.RowVersion != null ? Convert.ToBase64String(prestamo.RowVersion) : null
            };

            return Ok(readDto);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                message = "Conflicto de concurrencia al cancelar el préstamo. Recargue la información.",
                concurrencyConflict = true
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Error al cancelar el préstamo: {ex.Message}" });
        }
    }

    private int? ObtenerLectorIdDelUsuarioActual()
    {
        var claimLectorId = User.FindFirstValue("LectorId");
        if (!string.IsNullOrEmpty(claimLectorId) && int.TryParse(claimLectorId, out var id))
        {
            return id;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            var lector = _context.Lectores.AsNoTracking().FirstOrDefault(l => l.UsuarioId == userId);
            return lector?.Id;
        }

        return null;
    }
}
