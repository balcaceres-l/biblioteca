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
public class CategoriasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(PagedResultDto<CategoriaReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? activa,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Categorias.AsNoTracking();

        if (activa.HasValue)
        {
            query = query.Where(c => c.Activa == activa.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(s) ||
                                     (c.Descripcion != null && c.Descripcion.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoriaReadDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activa = c.Activa,
                CantidadLibros = c.Libros.Count,
                RowVersion = c.RowVersion != null ? Convert.ToBase64String(c.RowVersion) : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<CategoriaReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(CategoriaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var cat = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaReadDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activa = c.Activa,
                CantidadLibros = c.Libros.Count,
                RowVersion = c.RowVersion != null ? Convert.ToBase64String(c.RowVersion) : null
            })
            .FirstOrDefaultAsync();

        if (cat == null) return NotFound(new { message = $"Categoría con ID {id} no encontrada." });
        return Ok(cat);
    }

    [HttpGet("{id:int}/libros")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(IEnumerable<LibroReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLibrosByCategoria(int id)
    {
        var exists = await _context.Categorias.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound(new { message = $"Categoría con ID {id} no encontrada." });

        var libros = await _context.Libros
            .AsNoTracking()
            .Where(l => l.CategoriaId == id)
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
    [Authorize(Policy = Policies.CategoriasGestionar)]
    [ProducesResponseType(typeof(CategoriaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaCreateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.Categorias.AnyAsync(c => c.Nombre.ToLower() == model.Nombre.Trim().ToLower()))
        {
            return Conflict(new { message = $"Ya existe una categoría con el nombre '{model.Nombre.Trim()}'." });
        }

        var categoria = new Categoria
        {
            Nombre = model.Nombre.Trim(),
            Descripcion = model.Descripcion?.Trim(),
            Activa = model.Activa
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        var readDto = new CategoriaReadDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Activa = categoria.Activa,
            CantidadLibros = 0,
            RowVersion = categoria.RowVersion != null ? Convert.ToBase64String(categoria.RowVersion) : null
        };

        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.CategoriasGestionar)]
    [ProducesResponseType(typeof(CategoriaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound(new { message = $"Categoría con ID {id} no encontrada." });

        // Verificar nombre único en otras categorías
        if (await _context.Categorias.AnyAsync(c => c.Id != id && c.Nombre.ToLower() == model.Nombre.Trim().ToLower()))
        {
            return Conflict(new { message = $"Ya existe otra categoría con el nombre '{model.Nombre.Trim()}'." });
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

        _context.Entry(categoria).Property(c => c.RowVersion).OriginalValue = originalRowVersion;

        categoria.Nombre = model.Nombre.Trim();
        categoria.Descripcion = model.Descripcion?.Trim();
        categoria.Activa = model.Activa;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "La categoría fue modificada por otro usuario mientras usted realizaba los cambios. Recargue la información.",
                concurrencyConflict = true
            });
        }

        var readDto = new CategoriaReadDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Activa = categoria.Activa,
            CantidadLibros = await _context.Libros.CountAsync(l => l.CategoriaId == id),
            RowVersion = categoria.RowVersion != null ? Convert.ToBase64String(categoria.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Policies.CategoriasGestionar)]
    [ProducesResponseType(typeof(CategoriaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEstado(int id, [FromBody] CategoriaEstadoPatchDto model)
    {
        var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound(new { message = $"Categoría con ID {id} no encontrada." });

        categoria.Activa = model.Activa;
        await _context.SaveChangesAsync();

        var readDto = new CategoriaReadDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Activa = categoria.Activa,
            CantidadLibros = await _context.Libros.CountAsync(l => l.CategoriaId == id),
            RowVersion = categoria.RowVersion != null ? Convert.ToBase64String(categoria.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.CategoriasEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _context.Categorias.Include(c => c.Libros).FirstOrDefaultAsync(c => c.Id == id);
        if (categoria == null) return NotFound(new { message = $"Categoría con ID {id} no encontrada." });

        if (categoria.Libros.Any())
        {
            return Conflict(new { message = $"No se puede eliminar la categoría '{categoria.Nombre}' porque contiene {categoria.Libros.Count} libro(s) asociados." });
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
