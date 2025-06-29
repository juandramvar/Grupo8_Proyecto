CREATE DATABASE Ahorcado;

USE Ahorcado;

CREATE TABLE Usuario (
    usu_id INT PRIMARY KEY,
    usu_nombre NVARCHAR(100) NOT NULL,
    usu_marcador INT DEFAULT 0,
    usu_ganadas INT DEFAULT 0,
    usu_perdidas INT DEFAULT 0
);

CREATE TABLE Palabra (
    pal_id INT PRIMARY KEY IDENTITY(1,1),
    pal_texto NVARCHAR(50) NOT NULL,
    pal_longitud INT NOT NULL,
    pal_tilde BIT DEFAULT 0,
    pal_inicial CHAR(1) NOT NULL
);

CREATE TABLE Partida (
    par_id INT PRIMARY KEY IDENTITY(1,1),
    par_nivel NVARCHAR(10) NOT NULL,
    par_resultado NVARCHAR(10) NOT NULL,
    par_fecha DATETIME DEFAULT GETDATE(),
    usu_id INT NOT NULL,
    pal_id INT NOT NULL,
    CONSTRAINT FK_Partida_Usuario FOREIGN KEY (usu_id) REFERENCES Usuario(usu_id),
    CONSTRAINT FK_Partida_Palabra FOREIGN KEY (pal_id) REFERENCES Palabra(pal_id)
);

CREATE TABLE Letra (
    let_id INT PRIMARY KEY IDENTITY(1,1),
    let_letra CHAR(1) NOT NULL,
    let_escorrecta BIT DEFAULT 0,
    par_id INT NOT NULL,
    CONSTRAINT FK_Letra_Partida FOREIGN KEY (par_id) REFERENCES Partida(par_id)
);
