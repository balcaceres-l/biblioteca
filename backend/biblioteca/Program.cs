using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using biblioteca.Configuration;
using biblioteca.Data;
using biblioteca.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cargar variables de entorno desde .env
EnvLoader.Load();
var connectionString = EnvLoader.GetConnectionString();

// 2. Configurar Entity Framework Core con MariaDB / MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

// 3. Configurar ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. Configurar Autenticación JWT Bearer
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "ClaveSecretaSuperSeguraParaBibliotecaJwt2026Net10!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "BibliotecaApi";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "BibliotecaSpa";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Configurar Políticas de Autorización (Role + Claim "Permiso")
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CatalogoConsultar, policy =>
        policy.RequireClaim("Permiso", Permisos.CatalogoConsultar));

    options.AddPolicy(Policies.LibrosCrear, policy =>
        policy.RequireRole(Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.LibrosGestionar));

    options.AddPolicy(Policies.LibrosGestionar, policy =>
        policy.RequireRole(Roles.Administrador, Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.LibrosGestionar));

    options.AddPolicy(Policies.LibrosEliminar, policy =>
        policy.RequireRole(Roles.Administrador)
              .RequireClaim("Permiso", Permisos.LibrosEliminar));

    options.AddPolicy(Policies.AutoresGestionar, policy =>
        policy.RequireRole(Roles.Administrador, Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.AutoresGestionar));

    options.AddPolicy(Policies.AutoresEliminar, policy =>
        policy.RequireRole(Roles.Administrador)
              .RequireClaim("Permiso", Permisos.AutoresEliminar));

    options.AddPolicy(Policies.CategoriasGestionar, policy =>
        policy.RequireRole(Roles.Administrador, Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.CategoriasGestionar));

    options.AddPolicy(Policies.CategoriasEliminar, policy =>
        policy.RequireRole(Roles.Administrador)
              .RequireClaim("Permiso", Permisos.CategoriasEliminar));

    options.AddPolicy(Policies.LectoresGestionar, policy =>
        policy.RequireRole(Roles.Administrador, Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.LectoresGestionar));

    options.AddPolicy(Policies.LectoresEliminar, policy =>
        policy.RequireRole(Roles.Administrador)
              .RequireClaim("Permiso", Permisos.LectoresEliminar));

    options.AddPolicy(Policies.PrestamosCrear, policy =>
        policy.RequireClaim("Permiso", Permisos.PrestamosCrear));

    options.AddPolicy(Policies.PrestamosDevolver, policy =>
        policy.RequireRole(Roles.Administrador, Roles.Bibliotecario)
              .RequireClaim("Permiso", Permisos.PrestamosDevolver));

    options.AddPolicy(Policies.PrestamosCancelar, policy =>
        policy.RequireClaim("Permiso", Permisos.PrestamosCancelar));

    options.AddPolicy(Policies.UsuariosAdministrar, policy =>
        policy.RequireRole(Roles.Administrador)
              .RequireClaim("Permiso", Permisos.UsuariosAdministrar));
});

// 6. CORS para la SPA en Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// 7. Swagger y OpenAPI con soporte para JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Biblioteca API (.NET 10)",
        Version = "v1",
        Description = "API RESTful para la gestión de biblioteca con Identity, MariaDB y control de concurrencia"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el encabezado Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Inicializar base de datos y semillero (migraciones, roles, permisos y usuarios)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

// Crear carpeta para portadas si no existe
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var uploadsLibros = Path.Combine(webRoot, "uploads", "libros");
if (!Directory.Exists(uploadsLibros))
{
    Directory.CreateDirectory(uploadsLibros);
}

// Configurar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz "/"
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
