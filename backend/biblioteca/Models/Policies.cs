namespace biblioteca.Models;

public static class Policies
{
    public const string CatalogoConsultar = "CatalogoConsultarPolicy";

    public const string LibrosGestionar = "LibrosGestionarPolicy";
    public const string LibrosEliminar = "LibrosEliminarPolicy";

    public const string AutoresGestionar = "AutoresGestionarPolicy";
    public const string AutoresEliminar = "AutoresEliminarPolicy";

    public const string CategoriasGestionar = "CategoriasGestionarPolicy";
    public const string CategoriasEliminar = "CategoriasEliminarPolicy";

    public const string LectoresGestionar = "LectoresGestionarPolicy";
    public const string LectoresEliminar = "LectoresEliminarPolicy";

    public const string PrestamosCrear = "PrestamosCrearPolicy";
    public const string PrestamosDevolver = "PrestamosDevolverPolicy";
    public const string PrestamosCancelar = "PrestamosCancelarPolicy";

    public const string UsuariosAdministrar = "UsuariosAdministrarPolicy";
}
