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
public class AutoresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AutoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(PagedResultDto<AutorReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Autores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Nombre.ToLower().Contains(s) ||
                                     a.Apellido.ToLower().Contains(s) ||
                                     (a.Nacionalidad != null && a.Nacionalidad.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AutorReadDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Apellido = a.Apellido,
                Nacionalidad = a.Nacionalidad,
                Biografia = a.Biografia,
                FechaNacimiento = a.FechaNacimiento,
                CantidadLibros = a.Libros.Count,
                RowVersion = a.RowVersion != null ? Convert.ToBase64String(a.RowVersion) : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<AutorReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(AutorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var autor = await _context.Autores
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AutorReadDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Apellido = a.Apellido,
                Nacionalidad = a.Nacionalidad,
                Biografia = a.Biografia,
                FechaNacimiento = a.FechaNacimiento,
                CantidadLibros = a.Libros.Count,
                RowVersion = a.RowVersion != null ? Convert.ToBase64String(a.RowVersion) : null
            })
            .FirstOrDefaultAsync();

        if (autor == null) return NotFound(new { message = $"Autor con ID {id} no encontrado." });
        return Ok(autor);
    }

    [HttpGet("{id:int}/libros")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(IEnumerable<LibroReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLibrosByAutor(int id)
    {
        var exists = await _context.Autores.AnyAsync(a => a.Id == id);
        if (!exists) return NotFound(new { message = $"Autor con ID {id} no encontrado." });

        var libros = await _context.Libros
            .AsNoTracking()
            .Where(l => l.AutorId == id)
            .Select(l => new LibroReadDto
            {
                Id = l.Id,
                Titulo = l.Titulo,
                ISBN = l.ISBN,
                Resumen = l.Resumen,
                AnioPublicacion = l.AnioPublicacion,
                Editorial = l.Editorial,
                Disponible = l.Disponible,
                PortadaUrl = l.PortadaUrl,
                AutorId = l.AutorId,
                AutorNombre = $"{l.Autor!.Nombre} {l.Autor.Apellido}",
                CategoriaId = l.CategoriaId,
                CategoriaNombre = l.Categoria!.Nombre,
                RowVersion = l.RowVersion != null ? Convert.ToBase64String(l.RowVersion) : null
            })
            .ToListAsync();

        return Ok(libros);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AutoresGestionar)]
    [ProducesResponseType(typeof(AutorReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AutorCreateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var autor = new Autor
        {
            Nombre = model.Nombre.Trim(),
            Apellido = model.Apellido.Trim(),
            Nacionalidad = model.Nacionalidad?.Trim(),
            Biografia = model.Biografia?.Trim(),
            FechaNacimiento = model.FechaNacimiento
        };

        _context.Autores.Add(autor);
        await _context.SaveChangesAsync();

        var readDto = new AutorReadDto
        {
            Id = autor.Id,
            Nombre = autor.Nombre,
            Apellido = autor.Apellido,
            Nacionalidad = autor.Nacionalidad,
            Biografia = autor.Biografia,
            FechaNacimiento = autor.FechaNacimiento,
            CantidadLibros = 0,
            RowVersion = autor.RowVersion != null ? Convert.ToBase64String(autor.RowVersion) : null
        };

        return CreatedAtAction(nameof(GetById), new { id = autor.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AutoresGestionar)]
    [ProducesResponseType(typeof(AutorReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] AutorUpdateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var autor = await _context.Autores.FirstOrDefaultAsync(a => a.Id == id);
        if (autor == null) return NotFound(new { message = $"Autor con ID {id} no encontrado." });

        byte[] originalRowVersion;
        try
        {
            originalRowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch
        {
            return BadRequest(new { message = "El token de concurrencia RowVersion no tiene formato Base64 válido." });
        }

        // Configurar valor original para detección de concurrencia
        _context.Entry(autor).Property(a => a.RowVersion).OriginalValue = originalRowVersion;

        autor.Nombre = model.Nombre.Trim();
        autor.Apellido = model.Apellido.Trim();
        autor.Nacionalidad = model.Nacionalidad?.Trim();
        autor.Biografia = model.Biografia?.Trim();
        autor.FechaNacimiento = model.FechaNacimiento;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "El autor fue modificado por otro usuario mientras usted realizaba los cambios. Recargue la información para ver los datos más recientes.",
                concurrencyConflict = true
            });
        }

        var readDto = new AutorReadDto
        {
            Id = autor.Id,
            Nombre = autor.Nombre,
            Apellido = autor.Apellido,
            Nacionalidad = autor.Nacionalidad,
            Biografia = autor.Biografia,
            FechaNacimiento = autor.FechaNacimiento,
            CantidadLibros = await _context.Libros.CountAsync(l => l.AutorId == id),
            RowVersion = autor.RowVersion != null ? Convert.ToBase64String(autor.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AutoresEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var autor = await _context.Autores.Include(a => a.Libros).FirstOrDefaultAsync(a => a.Id == id);
        if (autor == null) return NotFound(new { message = $"Autor con ID {id} no encontrado." });

        if (autor.Libros.Any())
        {
            return Conflict(new { message = $"No se puede eliminar el autor '{autor.Nombre} {autor.Apellido}' porque tiene {autor.Libros.Count} libro(s) asociados en el catálogo." });
        }

        _context.Autores.Remove(autor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
