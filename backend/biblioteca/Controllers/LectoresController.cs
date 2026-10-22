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
public class LectoresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LectoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Policies.LectoresGestionar)]
    [ProducesResponseType(typeof(PagedResultDto<LectorReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? activo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Lectores.AsNoTracking();

        if (activo.HasValue)
        {
            query = query.Where(l => l.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.NombreCompleto.ToLower().Contains(s) ||
                                     l.DocumentoIdentidad.ToLower().Contains(s) ||
                                     l.Email.ToLower().Contains(s) ||
                                     (l.Telefono != null && l.Telefono.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.NombreCompleto)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LectorReadDto
            {
                Id = l.Id,
                NombreCompleto = l.NombreCompleto,
                DocumentoIdentidad = l.DocumentoIdentidad,
                Email = l.Email,
                Telefono = l.Telefono,
                Direccion = l.Direccion,
                Activo = l.Activo,
                UsuarioId = l.UsuarioId,
                FechaRegistro = l.FechaRegistro,
                PrestamosActivosCount = l.Prestamos.Count(p => p.Estado == EstadosPrestamo.Activo),
                RowVersion = l.RowVersion != null ? Convert.ToBase64String(l.RowVersion) : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<LectorReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("mi-perfil")]
    [ProducesResponseType(typeof(LectorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMiPerfil()
    {
        var lectorIdClaim = User.FindFirstValue("LectorId");
        if (string.IsNullOrEmpty(lectorIdClaim) || !int.TryParse(lectorIdClaim, out var lectorId))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lectorByUser = await _context.Lectores
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UsuarioId == userId);

            if (lectorByUser == null)
            {
                return NotFound(new { message = "El usuario autenticado no tiene un perfil de lector vinculado." });
            }

            lectorId = lectorByUser.Id;
        }

        return await GetById(lectorId);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TienePermisoSobreLector(id))
        {
            return Forbid();
        }

        var lector = await _context.Lectores
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LectorReadDto
            {
                Id = l.Id,
                NombreCompleto = l.NombreCompleto,
                DocumentoIdentidad = l.DocumentoIdentidad,
                Email = l.Email,
                Telefono = l.Telefono,
                Direccion = l.Direccion,
                Activo = l.Activo,
                UsuarioId = l.UsuarioId,
                FechaRegistro = l.FechaRegistro,
                PrestamosActivosCount = l.Prestamos.Count(p => p.Estado == EstadosPrestamo.Activo),
                RowVersion = l.RowVersion != null ? Convert.ToBase64String(l.RowVersion) : null
            })
            .FirstOrDefaultAsync();

        if (lector == null) return NotFound(new { message = $"Lector con ID {id} no encontrado." });
        return Ok(lector);
    }

    [HttpGet("{id:int}/prestamos")]
    [ProducesResponseType(typeof(IEnumerable<PrestamoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistorialPrestamos(int id)
    {
        if (!TienePermisoSobreLector(id))
        {
            return Forbid();
        }

        var exists = await _context.Lectores.AnyAsync(l => l.Id == id);
        if (!exists) return NotFound(new { message = $"Lector con ID {id} no encontrado." });

        var prestamos = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.LectorId == id)
            .OrderByDescending(p => p.FechaPrestamo)
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

        return Ok(prestamos);
    }

    [HttpGet("{id:int}/prestamos-activos")]
    [ProducesResponseType(typeof(IEnumerable<PrestamoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrestamosActivos(int id)
    {
        if (!TienePermisoSobreLector(id))
        {
            return Forbid();
        }

        var exists = await _context.Lectores.AnyAsync(l => l.Id == id);
        if (!exists) return NotFound(new { message = $"Lector con ID {id} no encontrado." });

        var prestamos = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Include(p => p.Lector)
            .Where(p => p.LectorId == id && p.Estado == EstadosPrestamo.Activo)
            .OrderBy(p => p.FechaVencimiento)
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

        return Ok(prestamos);
    }

    [HttpPost]
    [Authorize(Policy = Policies.LectoresGestionar)]
    [ProducesResponseType(typeof(LectorReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] LectorCreateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.Lectores.AnyAsync(l => l.DocumentoIdentidad == model.DocumentoIdentidad.Trim()))
        {
            return Conflict(new { message = $"Ya existe un lector registrado con el documento '{model.DocumentoIdentidad.Trim()}'." });
        }

        if (await _context.Lectores.AnyAsync(l => l.Email.ToLower() == model.Email.Trim().ToLower()))
        {
            return Conflict(new { message = $"Ya existe un lector registrado con el correo '{model.Email.Trim()}'." });
        }

        var lector = new Lector
        {
            NombreCompleto = model.NombreCompleto.Trim(),
            DocumentoIdentidad = model.DocumentoIdentidad.Trim(),
            Email = model.Email.Trim(),
            Telefono = model.Telefono?.Trim(),
            Direccion = model.Direccion?.Trim(),
            Activo = model.Activo,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Lectores.Add(lector);
        await _context.SaveChangesAsync();

        var readDto = new LectorReadDto
        {
            Id = lector.Id,
            NombreCompleto = lector.NombreCompleto,
            DocumentoIdentidad = lector.DocumentoIdentidad,
            Email = lector.Email,
            Telefono = lector.Telefono,
            Direccion = lector.Direccion,
            Activo = lector.Activo,
            UsuarioId = lector.UsuarioId,
            FechaRegistro = lector.FechaRegistro,
            PrestamosActivosCount = 0,
            RowVersion = lector.RowVersion != null ? Convert.ToBase64String(lector.RowVersion) : null
        };

        return CreatedAtAction(nameof(GetById), new { id = lector.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.LectoresGestionar)]
    [ProducesResponseType(typeof(LectorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] LectorUpdateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var lector = await _context.Lectores.FirstOrDefaultAsync(l => l.Id == id);
        if (lector == null) return NotFound(new { message = $"Lector con ID {id} no encontrado." });

        if (await _context.Lectores.AnyAsync(l => l.Id != id && l.DocumentoIdentidad == model.DocumentoIdentidad.Trim()))
        {
            return Conflict(new { message = $"Ya existe otro lector con el documento '{model.DocumentoIdentidad.Trim()}'." });
        }

        if (await _context.Lectores.AnyAsync(l => l.Id != id && l.Email.ToLower() == model.Email.Trim().ToLower()))
        {
            return Conflict(new { message = $"Ya existe otro lector con el correo '{model.Email.Trim()}'." });
        }

        byte[] originalRowVersion;
        try
        {
            originalRowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch
        {
            return BadRequest(new { message = "El token de concurrencia RowVersion no tiene formato Base64 válido." });
        }

        _context.Entry(lector).Property(l => l.RowVersion).OriginalValue = originalRowVersion;

        lector.NombreCompleto = model.NombreCompleto.Trim();
        lector.DocumentoIdentidad = model.DocumentoIdentidad.Trim();
        lector.Email = model.Email.Trim();
        lector.Telefono = model.Telefono?.Trim();
        lector.Direccion = model.Direccion?.Trim();
        lector.Activo = model.Activo;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "El lector fue modificado por otro usuario mientras realizaba los cambios. Recargue la información.",
                concurrencyConflict = true
            });
        }

        var readDto = new LectorReadDto
        {
            Id = lector.Id,
            NombreCompleto = lector.NombreCompleto,
            DocumentoIdentidad = lector.DocumentoIdentidad,
            Email = lector.Email,
            Telefono = lector.Telefono,
            Direccion = lector.Direccion,
            Activo = lector.Activo,
            UsuarioId = lector.UsuarioId,
            FechaRegistro = lector.FechaRegistro,
            PrestamosActivosCount = await _context.Prestamos.CountAsync(p => p.LectorId == id && p.Estado == EstadosPrestamo.Activo),
            RowVersion = lector.RowVersion != null ? Convert.ToBase64String(lector.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Policies.LectoresGestionar)]
    [ProducesResponseType(typeof(LectorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchEstado(int id, [FromBody] LectorEstadoPatchDto model)
    {
        var lector = await _context.Lectores.FirstOrDefaultAsync(l => l.Id == id);
        if (lector == null) return NotFound(new { message = $"Lector con ID {id} no encontrado." });

        if (!model.Activo)
        {
            var tienePrestamosActivos = await _context.Prestamos
                .AnyAsync(p => p.LectorId == id && p.Estado == EstadosPrestamo.Activo);

            if (tienePrestamosActivos)
            {
                return Conflict(new { message = "No se puede inhabilitar al lector porque posee préstamos activos pendientes de devolución." });
            }
        }

        lector.Activo = model.Activo;
        await _context.SaveChangesAsync();

        var readDto = new LectorReadDto
        {
            Id = lector.Id,
            NombreCompleto = lector.NombreCompleto,
            DocumentoIdentidad = lector.DocumentoIdentidad,
            Email = lector.Email,
            Telefono = lector.Telefono,
            Direccion = lector.Direccion,
            Activo = lector.Activo,
            UsuarioId = lector.UsuarioId,
            FechaRegistro = lector.FechaRegistro,
            PrestamosActivosCount = await _context.Prestamos.CountAsync(p => p.LectorId == id && p.Estado == EstadosPrestamo.Activo),
            RowVersion = lector.RowVersion != null ? Convert.ToBase64String(lector.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.LectoresEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var lector = await _context.Lectores.Include(l => l.Prestamos).FirstOrDefaultAsync(l => l.Id == id);
        if (lector == null) return NotFound(new { message = $"Lector con ID {id} no encontrado." });

        if (lector.Prestamos.Any())
        {
            return Conflict(new { message = $"No se puede eliminar el lector '{lector.NombreCompleto}' porque cuenta con un historial de {lector.Prestamos.Count} préstamo(s)." });
        }

        _context.Lectores.Remove(lector);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TienePermisoSobreLector(int lectorId)
    {
        if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Bibliotecario))
            return true;

        var claimLectorId = User.FindFirstValue("LectorId");
        return claimLectorId != null && int.TryParse(claimLectorId, out var id) && id == lectorId;
    }
}
