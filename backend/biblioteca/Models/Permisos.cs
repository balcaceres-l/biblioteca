namespace biblioteca.Models;

public static class Permisos
{
    public const string CatalogoConsultar = "Catalogo.Consultar";

    public const string LibrosGestionar = "Libros.Gestionar";
    public const string LibrosEliminar = "Libros.Eliminar";

    public const string AutoresGestionar = "Autores.Gestionar";
    public const string AutoresEliminar = "Autores.Eliminar";

    public const string CategoriasGestionar = "Categorias.Gestionar";
    public const string CategoriasEliminar = "Categorias.Eliminar";

    public const string LectoresGestionar = "Lectores.Gestionar";
    public const string LectoresEliminar = "Lectores.Eliminar";

    public const string PrestamosCrear = "Prestamos.Crear";
    public const string PrestamosDevolver = "Prestamos.Devolver";
    public const string PrestamosCancelar = "Prestamos.Cancelar";

    public const string UsuariosAdministrar = "Usuarios.Administrar";

    public static string[] ObtenerPermisosPorRol(string rol)
    {
        return rol switch
        {
            Roles.Administrador =>
            [
                CatalogoConsultar,
                LibrosGestionar,
                LibrosEliminar,
                AutoresGestionar,
                AutoresEliminar,
                CategoriasGestionar,
                CategoriasEliminar,
                LectoresGestionar,
                LectoresEliminar,
                PrestamosCrear,
                PrestamosDevolver,
                PrestamosCancelar,
                UsuariosAdministrar
            ],
            Roles.Bibliotecario =>
            [
                CatalogoConsultar,
                LibrosGestionar,
                AutoresGestionar,
                CategoriasGestionar,
                LectoresGestionar,
                PrestamosCrear,
                PrestamosDevolver,
                PrestamosCancelar
            ],
            Roles.Lector =>
            [
                CatalogoConsultar,
                PrestamosCrear,
                PrestamosCancelar
            ],
            _ => []
        };
    }
}
