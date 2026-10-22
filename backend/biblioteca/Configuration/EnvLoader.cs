namespace biblioteca.Configuration;

public static class EnvLoader
{
    private static bool _isLoaded;

    public static void Load(string? filePath = null)
    {
        if (_isLoaded) return;

        filePath ??= FindEnvFile();
        if (filePath != null && File.Exists(filePath))
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex > 0)
                {
                    var key = trimmed[..separatorIndex].Trim();
                    var value = trimmed[(separatorIndex + 1)..].Trim();

                    // Remover comillas si existen
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value[1..^1];
                    }

                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
            }
        }

        _isLoaded = true;
    }

    public static string GetConnectionString()
    {
        Load();

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var database = Environment.GetEnvironmentVariable("DB_DATABASE")
            ?? Environment.GetEnvironmentVariable("DB_NAME")
            ?? "biblioteca_db";
        var user = Environment.GetEnvironmentVariable("DB_USERNAME")
            ?? Environment.GetEnvironmentVariable("DB_USER")
            ?? "root";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";

        return $"Server={host};Port={port};Database={database};User={user};Password={password};CharSet=utf8mb4;";
    }

    private static string? FindEnvFile()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null)
        {
            var potentialPath = Path.Combine(currentDir.FullName, ".env");
            if (File.Exists(potentialPath))
            {
                return potentialPath;
            }

            // Buscar también dentro de backend/biblioteca/.env si estamos en la raíz del proyecto
            var nestedPath = Path.Combine(currentDir.FullName, "backend", "biblioteca", ".env");
            if (File.Exists(nestedPath))
            {
                return nestedPath;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
