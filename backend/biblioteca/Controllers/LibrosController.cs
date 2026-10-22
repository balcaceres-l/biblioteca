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
public class LibrosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public LibrosController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(PagedResultDto<LibroReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? autorId,
        [FromQuery] int? categoriaId,
        [FromQuery] bool? disponible,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Libros
            .AsNoTracking()
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .AsQueryable();

        if (disponible.HasValue)
        {
            query = query.Where(l => l.Disponible == disponible.Value);
        }

        if (autorId.HasValue)
        {
            query = query.Where(l => l.AutorId == autorId.Value);
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(l => l.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.Titulo.ToLower().Contains(s) ||
                                     l.ISBN.ToLower().Contains(s) ||
                                     (l.Editorial != null && l.Editorial.ToLower().Contains(s)) ||
                                     l.Autor!.Nombre.ToLower().Contains(s) ||
                                     l.Autor.Apellido.ToLower().Contains(s) ||
                                     l.Categoria!.Nombre.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.Titulo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return Ok(new PagedResultDto<LibroReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpGet("disponibles")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(IEnumerable<LibroReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDisponibles()
    {
        var items = await _context.Libros
            .AsNoTracking()
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .Where(l => l.Disponible)
            .OrderBy(l => l.Titulo)
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

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(LibroReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var libro = await _context.Libros
            .AsNoTracking()
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .Where(l => l.Id == id)
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
            .FirstOrDefaultAsync();

        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });
        return Ok(libro);
    }

    [HttpGet("{id:int}/disponibilidad")]
    [Authorize(Policy = Policies.CatalogoConsultar)]
    [ProducesResponseType(typeof(LibroDisponibilidadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisponibilidad(int id)
    {
        var libro = await _context.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        var prestamoActivo = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Lector)
            .Where(p => p.LibroId == id && p.Estado == EstadosPrestamo.Activo)
            .FirstOrDefaultAsync();

        var response = new LibroDisponibilidadResponseDto
        {
            LibroId = libro.Id,
            Titulo = libro.Titulo,
            Disponible = libro.Disponible,
            PrestamoActivoId = prestamoActivo?.Id,
            LectorPrestamoNombre = prestamoActivo?.Lector?.NombreCompleto,
            FechaVencimiento = prestamoActivo?.FechaVencimiento
        };

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = Policies.LibrosCrear)]
    [ProducesResponseType(typeof(LibroReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] LibroCreateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.Libros.AnyAsync(l => l.ISBN == model.ISBN.Trim()))
        {
            return Conflict(new { message = $"Ya existe un libro registrado con el ISBN '{model.ISBN.Trim()}'." });
        }

        if (!await _context.Autores.AnyAsync(a => a.Id == model.AutorId))
        {
            return BadRequest(new { message = $"El autor con ID {model.AutorId} no existe." });
        }

        if (!await _context.Categorias.AnyAsync(c => c.Id == model.CategoriaId))
        {
            return BadRequest(new { message = $"La categoría con ID {model.CategoriaId} no existe." });
        }

        var libro = new Libro
        {
            Titulo = model.Titulo.Trim(),
            ISBN = model.ISBN.Trim(),
            Resumen = model.Resumen?.Trim(),
            AnioPublicacion = model.AnioPublicacion,
            Editorial = model.Editorial?.Trim(),
            Disponible = model.Disponible,
            AutorId = model.AutorId,
            CategoriaId = model.CategoriaId,
            PortadaUrl = null
        };

        _context.Libros.Add(libro);
        await _context.SaveChangesAsync();

        // Cargar nombres para el DTO
        var autor = await _context.Autores.FindAsync(libro.AutorId);
        var categoria = await _context.Categorias.FindAsync(libro.CategoriaId);

        var readDto = new LibroReadDto
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            ISBN = libro.ISBN,
            Resumen = libro.Resumen,
            AnioPublicacion = libro.AnioPublicacion,
            Editorial = libro.Editorial,
            Disponible = libro.Disponible,
            PortadaUrl = libro.PortadaUrl,
            AutorId = libro.AutorId,
            AutorNombre = $"{autor?.Nombre} {autor?.Apellido}",
            CategoriaId = libro.CategoriaId,
            CategoriaNombre = categoria?.Nombre ?? "",
            RowVersion = libro.RowVersion != null ? Convert.ToBase64String(libro.RowVersion) : null
        };

        return CreatedAtAction(nameof(GetById), new { id = libro.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.LibrosGestionar)]
    [ProducesResponseType(typeof(LibroReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] LibroUpdateDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var libro = await _context.Libros.FirstOrDefaultAsync(l => l.Id == id);
        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        if (await _context.Libros.AnyAsync(l => l.Id != id && l.ISBN == model.ISBN.Trim()))
        {
            return Conflict(new { message = $"Ya existe otro libro con el ISBN '{model.ISBN.Trim()}'." });
        }

        if (!await _context.Autores.AnyAsync(a => a.Id == model.AutorId))
        {
            return BadRequest(new { message = $"El autor con ID {model.AutorId} no existe." });
        }

        if (!await _context.Categorias.AnyAsync(c => c.Id == model.CategoriaId))
        {
            return BadRequest(new { message = $"La categoría con ID {model.CategoriaId} no existe." });
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

        _context.Entry(libro).Property(l => l.RowVersion).OriginalValue = originalRowVersion;

        libro.Titulo = model.Titulo.Trim();
        libro.ISBN = model.ISBN.Trim();
        libro.Resumen = model.Resumen?.Trim();
        libro.AnioPublicacion = model.AnioPublicacion;
        libro.Editorial = model.Editorial?.Trim();
        libro.Disponible = model.Disponible;
        libro.AutorId = model.AutorId;
        libro.CategoriaId = model.CategoriaId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "El libro fue modificado simultáneamente por otro usuario. Por favor recargue la información para ver los datos más recientes.",
                concurrencyConflict = true
            });
        }

        var autor = await _context.Autores.FindAsync(libro.AutorId);
        var categoria = await _context.Categorias.FindAsync(libro.CategoriaId);

        var readDto = new LibroReadDto
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            ISBN = libro.ISBN,
            Resumen = libro.Resumen,
            AnioPublicacion = libro.AnioPublicacion,
            Editorial = libro.Editorial,
            Disponible = libro.Disponible,
            PortadaUrl = libro.PortadaUrl,
            AutorId = libro.AutorId,
            AutorNombre = $"{autor?.Nombre} {autor?.Apellido}",
            CategoriaId = libro.CategoriaId,
            CategoriaNombre = categoria?.Nombre ?? "",
            RowVersion = libro.RowVersion != null ? Convert.ToBase64String(libro.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpPatch("{id:int}/disponibilidad")]
    [Authorize(Policy = Policies.LibrosGestionar)]
    [ProducesResponseType(typeof(LibroReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchDisponibilidad(int id, [FromBody] LibroDisponibilidadPatchDto model)
    {
        var libro = await _context.Libros
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        if (model.Disponible)
        {
            var tienePrestamoActivo = await _context.Prestamos
                .AnyAsync(p => p.LibroId == id && p.Estado == EstadosPrestamo.Activo);

            if (tienePrestamoActivo)
            {
                return Conflict(new { message = "No se puede marcar el libro como disponible porque actualmente se encuentra en préstamo activo. Primero debe registrarse la devolución o cancelación del préstamo." });
            }
        }

        libro.Disponible = model.Disponible;
        await _context.SaveChangesAsync();

        var readDto = new LibroReadDto
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            ISBN = libro.ISBN,
            Resumen = libro.Resumen,
            AnioPublicacion = libro.AnioPublicacion,
            Editorial = libro.Editorial,
            Disponible = libro.Disponible,
            PortadaUrl = libro.PortadaUrl,
            AutorId = libro.AutorId,
            AutorNombre = $"{libro.Autor!.Nombre} {libro.Autor.Apellido}",
            CategoriaId = libro.CategoriaId,
            CategoriaNombre = libro.Categoria!.Nombre,
            RowVersion = libro.RowVersion != null ? Convert.ToBase64String(libro.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpPost("{id:int}/portada")]
    [Authorize(Policy = Policies.LibrosGestionar)]
    [ProducesResponseType(typeof(LibroReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPortada(int id, IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new { message = "Debe adjuntar un archivo de imagen válido." });
        }

        var libro = await _context.Libros
            .Include(l => l.Autor)
            .Include(l => l.Categoria)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        if (!ValidarImagen(archivo, out var errorMensaje))
        {
            return BadRequest(new { message = errorMensaje });
        }

        // Si ya tenía portada física, eliminarla antes de guardar la nueva
        EliminarArchivoFisico(libro.PortadaUrl);

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var carpetaDestino = Path.Combine(webRoot, "uploads", "libros");
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombreUnico = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = Path.Combine(carpetaDestino, nombreUnico);

        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        libro.PortadaUrl = $"/uploads/libros/{nombreUnico}";
        await _context.SaveChangesAsync();

        var readDto = new LibroReadDto
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            ISBN = libro.ISBN,
            Resumen = libro.Resumen,
            AnioPublicacion = libro.AnioPublicacion,
            Editorial = libro.Editorial,
            Disponible = libro.Disponible,
            PortadaUrl = libro.PortadaUrl,
            AutorId = libro.AutorId,
            AutorNombre = $"{libro.Autor!.Nombre} {libro.Autor.Apellido}",
            CategoriaId = libro.CategoriaId,
            CategoriaNombre = libro.Categoria!.Nombre,
            RowVersion = libro.RowVersion != null ? Convert.ToBase64String(libro.RowVersion) : null
        };

        return Ok(readDto);
    }

    [HttpDelete("{id:int}/portada")]
    [Authorize(Policy = Policies.LibrosGestionar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePortada(int id)
    {
        var libro = await _context.Libros.FirstOrDefaultAsync(l => l.Id == id);
        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        if (string.IsNullOrWhiteSpace(libro.PortadaUrl))
        {
            return NotFound(new { message = "El libro no tiene una portada asignada." });
        }

        EliminarArchivoFisico(libro.PortadaUrl);
        libro.PortadaUrl = null;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.LibrosEliminar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var libro = await _context.Libros
            .Include(l => l.Prestamos)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (libro == null) return NotFound(new { message = $"Libro con ID {id} no encontrado." });

        if (libro.Prestamos.Any())
        {
            return Conflict(new { message = $"No se puede eliminar el libro '{libro.Titulo}' porque cuenta con un historial de {libro.Prestamos.Count} préstamo(s) asociado(s)." });
        }

        EliminarArchivoFisico(libro.PortadaUrl);
        _context.Libros.Remove(libro);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool ValidarImagen(IFormFile archivo, out string error)
    {
        error = string.Empty;
        var extensionesValidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (!extensionesValidas.Contains(extension))
        {
            error = "Solo se permiten imágenes con extensión .jpg, .jpeg, .png o .webp.";
            return false;
        }

        const long maxTamano = 5 * 1024 * 1024; // 5 MB
        if (archivo.Length > maxTamano)
        {
            error = "El tamaño de la imagen no puede superar los 5 MB.";
            return false;
        }

        return true;
    }

    private void EliminarArchivoFisico(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var rutaNormalizada = rutaRelativa.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var rutaFisica = Path.Combine(webRoot, rutaNormalizada);

        if (System.IO.File.Exists(rutaFisica))
        {
            try
            {
                System.IO.File.Delete(rutaFisica);
            }
            catch
            {
                // Ignorar excepción si el archivo está temporalmente en uso
            }
        }
    }
}
