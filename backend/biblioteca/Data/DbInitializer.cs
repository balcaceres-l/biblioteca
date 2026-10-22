using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using biblioteca.Models;

namespace biblioteca.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'AspNetUsers';";
            var result = await checkCmd.ExecuteScalarAsync();
            var exists = Convert.ToInt32(result) > 0;

            if (!exists)
            {
                var schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql");
                if (!File.Exists(schemaPath))
                {
                    schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "schema.sql");
                }

                if (File.Exists(schemaPath))
                {
                    var sql = await File.ReadAllTextAsync(schemaPath);
                    var statements = sql.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var stmt in statements)
                    {
                        var cleanStmt = stmt.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanStmt) && !cleanStmt.StartsWith("--") && !cleanStmt.StartsWith("/*"))
                        {
                            using var execCmd = connection.CreateCommand();
                            execCmd.CommandText = cleanStmt;
                            await execCmd.ExecuteNonQueryAsync();
                        }
                    }
                    logger.LogInformation("Esquema de tablas creado exitosamente en MariaDB.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Error al inicializar la base de datos MariaDB: {Message}", ex.Message);
            return;
        }

        // 1. Sembrar los 3 roles
        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // 2. Asignar Claims de Permiso ("Permiso") a cada rol
        foreach (var rol in Roles.Todos)
        {
            var identityRole = await roleManager.FindByNameAsync(rol);
            if (identityRole != null)
            {
                var claimsExistentes = await roleManager.GetClaimsAsync(identityRole);
                var permisosEsperados = Permisos.ObtenerPermisosPorRol(rol);

                foreach (var permiso in permisosEsperados)
                {
                    if (!claimsExistentes.Any(c => c.Type == "Permiso" && c.Value == permiso))
                    {
                        await roleManager.AddClaimAsync(identityRole, new Claim("Permiso", permiso));
                    }
                }
            }
        }

        // 3. Sembrar Usuario Administrador
        var adminEmail = "admin@biblioteca.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador Principal",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Administrador);
                foreach (var permiso in Permisos.ObtenerPermisosPorRol(Roles.Administrador))
                {
                    await userManager.AddClaimAsync(adminUser, new Claim("Permiso", permiso));
                }
            }
        }

        // 4. Sembrar Usuario Bibliotecario
        var biblioEmail = "biblio@biblioteca.com";
        var biblioUser = await userManager.FindByEmailAsync(biblioEmail);
        if (biblioUser == null)
        {
            biblioUser = new ApplicationUser
            {
                UserName = biblioEmail,
                Email = biblioEmail,
                NombreCompleto = "Bibliotecario en Turno",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(biblioUser, "Biblio123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(biblioUser, Roles.Bibliotecario);
                foreach (var permiso in Permisos.ObtenerPermisosPorRol(Roles.Bibliotecario))
                {
                    await userManager.AddClaimAsync(biblioUser, new Claim("Permiso", permiso));
                }
            }
        }

        // 5. Sembrar Usuario Lector
        var lectorEmail = "lector@biblioteca.com";
        var lectorUser = await userManager.FindByEmailAsync(lectorEmail);
        if (lectorUser == null)
        {
            lectorUser = new ApplicationUser
            {
                UserName = lectorEmail,
                Email = lectorEmail,
                NombreCompleto = "Lector Aficionado",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(lectorUser, "Lector123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(lectorUser, Roles.Lector);
                foreach (var permiso in Permisos.ObtenerPermisosPorRol(Roles.Lector))
                {
                    await userManager.AddClaimAsync(lectorUser, new Claim("Permiso", permiso));
                }
            }
        }

        // 6. Sembrar Autores iniciales
        if (!await context.Autores.AnyAsync())
        {
            context.Autores.AddRange(
                new Autor
                {
                    Nombre = "Gabriel",
                    Apellido = "García Márquez",
                    Nacionalidad = "Colombiana",
                    Biografia = "Premio Nobel de Literatura en 1982, figura cumbre del realismo mágico.",
                    FechaNacimiento = new DateTime(1927, 3, 6)
                },
                new Autor
                {
                    Nombre = "Miguel",
                    Apellido = "de Cervantes",
                    Nacionalidad = "Española",
                    Biografia = "Máxima figura de la literatura española, autor de Don Quijote de la Mancha.",
                    FechaNacimiento = new DateTime(1547, 9, 29)
                },
                new Autor
                {
                    Nombre = "George",
                    Apellido = "Orwell",
                    Nacionalidad = "Británica",
                    Biografia = "Novelista, ensayista y crítico inglés, conocido por 1984 y Rebelión en la granja.",
                    FechaNacimiento = new DateTime(1903, 6, 25)
                },
                new Autor
                {
                    Nombre = "Isabel",
                    Apellido = "Allende",
                    Nacionalidad = "Chilena",
                    Biografia = "Escritora chilena, autora superventas de La casa de los espíritus.",
                    FechaNacimiento = new DateTime(1942, 8, 2)
                }
            );
            await context.SaveChangesAsync();
        }

        // 7. Sembrar Categorías iniciales
        if (!await context.Categorias.AnyAsync())
        {
            context.Categorias.AddRange(
                new Categoria
                {
                    Nombre = "Ficción Literaria",
                    Descripcion = "Novelas y obras de gran valor estilístico y profundidad psicológica.",
                    Activa = true
                },
                new Categoria
                {
                    Nombre = "Ciencia Ficción y Distopía",
                    Descripcion = "Obras que exploran futuros posibles, tecnología y sociedades alternativas.",
                    Activa = true
                },
                new Categoria
                {
                    Nombre = "Clásicos Universales",
                    Descripcion = "Obras maestras de la literatura reconocidas mundialmente a través del tiempo.",
                    Activa = true
                },
                new Categoria
                {
                    Nombre = "Historia y Ensayo",
                    Descripcion = "Textos analíticos, memorias y reconstrucciones de hechos históricos.",
                    Activa = true
                }
            );
            await context.SaveChangesAsync();
        }

        // 8. Sembrar Libros iniciales
        if (!await context.Libros.AnyAsync())
        {
            var garciaMarquez = await context.Autores.FirstAsync(a => a.Apellido == "García Márquez");
            var cervantes = await context.Autores.FirstAsync(a => a.Apellido == "de Cervantes");
            var orwell = await context.Autores.FirstAsync(a => a.Apellido == "Orwell");
            var allende = await context.Autores.FirstAsync(a => a.Apellido == "Allende");

            var catFiccion = await context.Categorias.FirstAsync(c => c.Nombre == "Ficción Literaria");
            var catCienciaFiccion = await context.Categorias.FirstAsync(c => c.Nombre == "Ciencia Ficción y Distopía");
            var catClasicos = await context.Categorias.FirstAsync(c => c.Nombre == "Clásicos Universales");

            context.Libros.AddRange(
                new Libro
                {
                    Titulo = "Cien años de soledad",
                    ISBN = "978-0307474728",
                    Resumen = "La historia de la familia Buendía a lo largo de siete generaciones en el pueblo ficticio de Macondo.",
                    AnioPublicacion = 1967,
                    Editorial = "Editorial Sudamericana",
                    Disponible = true,
                    AutorId = garciaMarquez.Id,
                    CategoriaId = catFiccion.Id,
                    PortadaUrl = null
                },
                new Libro
                {
                    Titulo = "El amor en los tiempos del cólera",
                    ISBN = "978-0307389732",
                    Resumen = "Novela dedicada al verdadero amor que perdura y triunfa frente a las adversidades del tiempo.",
                    AnioPublicacion = 1985,
                    Editorial = "Oveja Negra",
                    Disponible = true,
                    AutorId = garciaMarquez.Id,
                    CategoriaId = catFiccion.Id,
                    PortadaUrl = null
                },
                new Libro
                {
                    Titulo = "Don Quijote de la Mancha",
                    ISBN = "978-8420412146",
                    Resumen = "Las aventuras de Alonso Quijano, un hidalgo que enloquece leyendo libros de caballerías.",
                    AnioPublicacion = 1605,
                    Editorial = "Francisco de Robles",
                    Disponible = true,
                    AutorId = cervantes.Id,
                    CategoriaId = catClasicos.Id,
                    PortadaUrl = null
                },
                new Libro
                {
                    Titulo = "1984",
                    ISBN = "978-0451524935",
                    Resumen = "La inquietante distopía del Gran Hermano y la omnipresente vigilancia estatal.",
                    AnioPublicacion = 1949,
                    Editorial = "Secker & Warburg",
                    Disponible = true,
                    AutorId = orwell.Id,
                    CategoriaId = catCienciaFiccion.Id,
                    PortadaUrl = null
                },
                new Libro
                {
                    Titulo = "La casa de los espíritus",
                    ISBN = "978-0525433491",
                    Resumen = "La saga de cuatro generaciones de la familia Trueba en un país latinoamericano sin nombre.",
                    AnioPublicacion = 1982,
                    Editorial = "Plaza & Janés",
                    Disponible = true,
                    AutorId = allende.Id,
                    CategoriaId = catFiccion.Id,
                    PortadaUrl = null
                }
            );
            await context.SaveChangesAsync();
        }

        // 9. Sembrar Lector vinculado al usuario Lector
        if (!await context.Lectores.AnyAsync())
        {
            var lectorRegistrado = new Lector
            {
                NombreCompleto = lectorUser.NombreCompleto,
                DocumentoIdentidad = "05123456-7",
                Email = lectorEmail,
                Telefono = "7890-1234",
                Direccion = "Avenida Central #123, San Salvador",
                Activo = true,
                UsuarioId = lectorUser.Id,
                FechaRegistro = DateTime.UtcNow
            };

            context.Lectores.Add(lectorRegistrado);
            await context.SaveChangesAsync();

            // Actualizar la referencia en ApplicationUser
            lectorUser.LectorId = lectorRegistrado.Id;
            await userManager.UpdateAsync(lectorUser);
        }
    }
}
