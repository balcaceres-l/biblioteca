CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;

CREATE TABLE IF NOT EXISTS `AspNetRoles` (
    `Id` varchar(255) NOT NULL,
    `Name` varchar(256) NULL,
    `NormalizedName` varchar(256) NULL,
    `ConcurrencyStamp` longtext NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS `AspNetUsers` (
    `Id` varchar(255) NOT NULL,
    `NombreCompleto` varchar(150) NOT NULL,
    `FechaCreacion` datetime(6) NOT NULL,
    `LectorId` int NULL,
    `UserName` varchar(256) NULL,
    `NormalizedUserName` varchar(256) NULL,
    `Email` varchar(256) NULL,
    `NormalizedEmail` varchar(256) NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext NULL,
    `SecurityStamp` longtext NULL,
    `ConcurrencyStamp` longtext NULL,
    `PhoneNumber` longtext NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS `Autores` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) NOT NULL,
    `Apellido` varchar(100) NOT NULL,
    `Nacionalidad` varchar(100) NULL,
    `Biografia` varchar(2000) NULL,
    `FechaNacimiento` datetime(6) NULL,
    `RowVersion` longblob NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS `Categorias` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) NOT NULL,
    `Descripcion` varchar(500) NULL,
    `Activa` tinyint(1) NOT NULL,
    `RowVersion` longblob NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) NOT NULL,
    `ClaimType` longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) NOT NULL,
    `ClaimType` longtext NULL,
    `ClaimValue` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `AspNetUserLogins` (
    `LoginProvider` varchar(255) NOT NULL,
    `ProviderKey` varchar(255) NOT NULL,
    `ProviderDisplayName` longtext NULL,
    `UserId` varchar(255) NOT NULL,
    PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `AspNetUserRoles` (
    `UserId` varchar(255) NOT NULL,
    `RoleId` varchar(255) NOT NULL,
    PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `AspNetUserTokens` (
    `UserId` varchar(255) NOT NULL,
    `LoginProvider` varchar(255) NOT NULL,
    `Name` varchar(255) NOT NULL,
    `Value` longtext NULL,
    PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `Lectores` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `NombreCompleto` varchar(150) NOT NULL,
    `DocumentoIdentidad` varchar(50) NOT NULL,
    `Email` varchar(150) NOT NULL,
    `Telefono` varchar(30) NULL,
    `Direccion` varchar(250) NULL,
    `Activo` tinyint(1) NOT NULL,
    `UsuarioId` varchar(255) NULL,
    `FechaRegistro` datetime(6) NOT NULL,
    `RowVersion` longblob NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Lectores_AspNetUsers_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS `Libros` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Titulo` varchar(200) NOT NULL,
    `ISBN` varchar(20) NOT NULL,
    `Resumen` varchar(2000) NULL,
    `AnioPublicacion` int NOT NULL,
    `Editorial` varchar(100) NULL,
    `Disponible` tinyint(1) NOT NULL,
    `PortadaUrl` varchar(500) NULL,
    `AutorId` int NOT NULL,
    `CategoriaId` int NOT NULL,
    `RowVersion` longblob NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Libros_Autores_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Autores` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Libros_Categorias_CategoriaId` FOREIGN KEY (`CategoriaId`) REFERENCES `Categorias` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS `Prestamos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LibroId` int NOT NULL,
    `LectorId` int NOT NULL,
    `FechaPrestamo` datetime(6) NOT NULL,
    `FechaVencimiento` datetime(6) NOT NULL,
    `FechaDevolucionReal` datetime(6) NULL,
    `Estado` varchar(20) NOT NULL,
    `Observaciones` varchar(500) NULL,
    `UsuarioRegistroId` varchar(100) NULL,
    `RowVersion` longblob NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Prestamos_Lectores_LectorId` FOREIGN KEY (`LectorId`) REFERENCES `Lectores` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Prestamos_Libros_LibroId` FOREIGN KEY (`LibroId`) REFERENCES `Libros` (`Id`) ON DELETE RESTRICT
);

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021191444_InitialCreate', '10.0.12');

COMMIT;
