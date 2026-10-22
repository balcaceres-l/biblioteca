namespace biblioteca.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Bibliotecario = "Bibliotecario";
    public const string Lector = "Lector";

    public static readonly string[] Todos = [Administrador, Bibliotecario, Lector];
}
