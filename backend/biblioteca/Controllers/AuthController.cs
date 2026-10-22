using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using biblioteca.Data;
using biblioteca.DTOs;
using biblioteca.Models;

namespace biblioteca.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized(new { message = "Credenciales incorrectas." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Credenciales incorrectas." });

        var response = await GenerarTokenJwtAsync(user);
        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _userManager.FindByEmailAsync(model.Email) != null)
            return Conflict(new { message = "El correo electrónico ya está registrado." });

        if (await _context.Lectores.AnyAsync(l => l.DocumentoIdentidad == model.DocumentoIdentidad))
            return Conflict(new { message = "El documento de identidad ya está registrado." });

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            NombreCompleto = model.NombreCompleto,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Rol por defecto para autoregistro: Lector
        await _userManager.AddToRoleAsync(user, Roles.Lector);
        var permisos = Permisos.ObtenerPermisosPorRol(Roles.Lector);
        foreach (var permiso in permisos)
        {
            await _userManager.AddClaimAsync(user, new Claim("Permiso", permiso));
        }

        // Crear ficha de Lector asociada
        var lector = new Lector
        {
            NombreCompleto = model.NombreCompleto,
            DocumentoIdentidad = model.DocumentoIdentidad,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Activo = true,
            UsuarioId = user.Id,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Lectores.Add(lector);
        await _context.SaveChangesAsync();

        user.LectorId = lector.Id;
        await _userManager.UpdateAsync(user);

        var response = await GenerarTokenJwtAsync(user);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("usuarios")]
    [Authorize(Policy = Policies.UsuariosAdministrar)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearUsuario([FromBody] CreateUserDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!Roles.Todos.Contains(model.Rol))
            return BadRequest(new { message = $"Rol inválido. Opciones válidas: {string.Join(", ", Roles.Todos)}" });

        if (await _userManager.FindByEmailAsync(model.Email) != null)
            return Conflict(new { message = "El correo electrónico ya está registrado." });

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            NombreCompleto = model.NombreCompleto,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, model.Rol);
        var permisos = Permisos.ObtenerPermisosPorRol(model.Rol);
        foreach (var permiso in permisos)
        {
            await _userManager.AddClaimAsync(user, new Claim("Permiso", permiso));
        }

        var summary = new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            NombreCompleto = user.NombreCompleto,
            Rol = model.Rol,
            Permisos = permisos
        };

        return StatusCode(StatusCodes.Status201Created, summary);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var rol = roles.FirstOrDefault() ?? Roles.Lector;
        var claims = await _userManager.GetClaimsAsync(user);
        var permisos = claims.Where(c => c.Type == "Permiso").Select(c => c.Value).ToArray();

        var summary = new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            NombreCompleto = user.NombreCompleto,
            Rol = rol,
            Permisos = permisos,
            LectorId = user.LectorId
        };

        return Ok(summary);
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = Policies.UsuariosAdministrar)]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUsuarios()
    {
        var users = await _userManager.Users.ToListAsync();
        var list = new List<UserSummaryDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var rol = roles.FirstOrDefault() ?? "Sin Rol";
            var claims = await _userManager.GetClaimsAsync(u);

            list.Add(new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                NombreCompleto = u.NombreCompleto,
                Rol = rol,
                Permisos = claims.Where(c => c.Type == "Permiso").Select(c => c.Value).ToArray(),
                LectorId = u.LectorId
            });
        }

        return Ok(list);
    }

    private async Task<AuthResponseDto> GenerarTokenJwtAsync(ApplicationUser user)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "ClaveSecretaSuperSeguraParaBibliotecaJwt2026Net10!";
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "BibliotecaApi";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "BibliotecaSpa";
        var expireHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRE_HOURS"), out var hours) ? hours : 8;

        var roles = await _userManager.GetRolesAsync(user);
        var rolPrincipal = roles.FirstOrDefault() ?? Roles.Lector;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.NombreCompleto),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, rolPrincipal),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.LectorId.HasValue)
        {
            claims.Add(new Claim("LectorId", user.LectorId.Value.ToString()));
        }

        // Obtener permisos asignados al usuario y al rol
        var userClaims = await _userManager.GetClaimsAsync(user);
        var roleClaims = new List<Claim>();
        var identityRole = await _roleManager.FindByNameAsync(rolPrincipal);
        if (identityRole != null)
        {
            roleClaims = (await _roleManager.GetClaimsAsync(identityRole)).ToList();
        }

        var todosLosPermisos = userClaims.Concat(roleClaims)
            .Where(c => c.Type == "Permiso")
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        foreach (var p in todosLosPermisos)
        {
            claims.Add(new Claim("Permiso", p));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiracion = DateTime.UtcNow.AddHours(expireHours);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiracion,
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email ?? "",
            NombreCompleto = user.NombreCompleto,
            Rol = rolPrincipal,
            Permisos = todosLosPermisos,
            LectorId = user.LectorId,
            Expiracion = expiracion
        };
    }
}
