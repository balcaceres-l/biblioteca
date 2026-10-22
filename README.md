# Sistema Web de Gestión de Biblioteca

Sistema web completo para la gestión de una biblioteca desarrollado con **ASP.NET Core Web API (.NET 10)**, persistencia en **MariaDB** mediante **Entity Framework Core 10**, autenticación y autorización con **ASP.NET Core Identity (Policies y Claims de permisos)**, control de concurrencia optimista (`RowVersion`), carga de imágenes de portadas y una **SPA en Angular 21 con Bootstrap 5 y paginación**.

---

## 🚀 Tecnologías Utilizadas

- **Backend**:
  - ASP.NET Core Web API en **.NET 10**
  - Entity Framework Core 10 (`MySql.EntityFrameworkCore`)
  - MariaDB (alojado en contenedor Docker / servidor remoto y gestionado con SQLyog)
  - ASP.NET Core Identity con Roles (`Administrador`, `Bibliotecario`, `Lector`) y **Policies** basadas en Claims (`"Permiso"`)
  - Autenticación con JSON Web Tokens (**JWT Bearer**)
  - Control de concurrencia optimista mediante simulación de `RowVersion` y transacciones explícitas (`BeginTransactionAsync`) para evitar doble préstamo simultáneo de libros
  - Carga, consulta y eliminación de imágenes físicas de portadas en `wwwroot/uploads/libros`
  - Documentación de servicios con OpenAPI y **Swagger UI**
- **Frontend**:
  - **Angular 21** (Standalone Components con detección de cambios zoneless)
  - **TypeScript** & **RxJS**
  - **Bootstrap 5.3** & **Bootstrap Icons**
  - Enrutamiento protegido por **Guards** de autenticación y de rol
  - **HTTP Interceptor** para inyección automática de Bearer Tokens
  - Componente reutilizable de paginación (`app-pagination`)

---

## 📁 Estructura del Proyecto

```text
├── backend/
│   └── biblioteca/
│       ├── Configuration/     # Carga de variables de entorno (.env)
│       ├── Controllers/       # Auth, Libros, Autores, Categorias, Lectores, Prestamos
│       ├── Data/              # ApplicationDbContext, DbInitializer, schema.sql
│       ├── DTOs/              # DTOs de lectura, creación, actualización y paginación
│       ├── Migrations/        # Migraciones de Entity Framework Core
│       ├── Models/            # Entidades, Roles, Permisos y Policies
│       ├── .env.example       # Plantilla de variables de entorno
│       └── Program.cs         # Configuración del pipeline, Identity, JWT, Policies, CORS y Swagger
│
├── frontend/
│   └── frontend/
│       └── src/
│           └── app/
│               ├── core/      # Modelos, AuthService, Servicios API, Interceptors y Guards
│               ├── pages/     # Login, Registro, Libros, Autores, Categorías, Lectores, Préstamos
│               └── shared/    # Navbar con roles y Componente de Paginación Bootstrap
```

---

## ⚙️ Configuración y Puesta en Marcha

### 1. Variables de Entorno (.env)
En la carpeta `backend/biblioteca`, crear o editar el archivo `.env` tomando como base `.env.example`:

```env
DB_HOST=localhost
DB_PORT=3306
DB_DATABASE=biblioteca_db
DB_USERNAME=root
DB_PASSWORD=tu_password

JWT_KEY=ClaveSecretaSuperSeguraParaBibliotecaJwt2026Net10!
JWT_ISSUER=BibliotecaApi
JWT_AUDIENCE=BibliotecaSpa
JWT_EXPIRE_HOURS=8
```

### 2. Ejecución del Backend (.NET 10)
```powershell
cd backend/biblioteca
dotnet restore
dotnet run
```
- La API REST se levantará en: `http://localhost:5000`
- La documentación interactiva de Swagger estará accesible en: `http://localhost:5000/`

### 3. Ejecución del Frontend (Angular SPA)
```powershell
cd frontend/frontend
npm install
npm start
```
- La aplicación web estará accesible en: `http://localhost:4200`

---

## 🔑 Credenciales de Acceso

La aplicación incluye usuarios de demostración precargados con acceso de un clic en la pantalla de Login:

| Rol | Correo | Contraseña | Permisos Principales |
| :--- | :--- | :--- | :--- |
| **Administrador** | `admin@biblioteca.com` | `Admin123!` | Control total, CRUD de catálogo, eliminación física y gestión de lectores. |
| **Bibliotecario** | `biblio@biblioteca.com` | `Biblio123!` | Altas y ediciones de libros/portadas, registro de préstamos y devoluciones (sin borrado físico). |
| **Lector** | `lector@biblioteca.com` | `Lector123!` | Consulta de catálogo paginado, solicitud de libros disponibles y seguimiento de sus préstamos. |

---

## 🛡️ Control de Concurrencia y Transacciones

- Las entidades implementan `IConcurrencyEntity` con un token `RowVersion` que se actualiza automáticamente con `Guid.NewGuid().ToByteArray()` en cada inserción o modificación.
- Al solicitar un préstamo, `PrestamosController` ejecuta una transacción atómica (`BeginTransactionAsync`), valida la disponibilidad del libro y su token de concurrencia. Si dos usuarios intentan prestar el mismo libro simultáneamente, la API responde con **HTTP 409 Conflict** y Angular muestra una notificación clara advirtiendo que el libro ya fue tomado por otro lector.
