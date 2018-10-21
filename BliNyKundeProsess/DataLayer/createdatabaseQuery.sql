CREATE TABLE Person
(
PersonId   INT IDENTITY PRIMARY KEY,
Navn   NVARCHAR(200) NOT NULL,
Epostadresse NVARCHAR(100),
Mobilnummer   NVARCHAR(100)
)

CREATE TABLE Sakstype
(
Id   INT IDENTITY PRIMARY KEY,
Navn NVARCHAR(200) NOT NULL,
)

CREATE TABLE Sak
(
Saksnummer   INT IDENTITY PRIMARY KEY,
Sakstype  INT REFERENCES Sakstype (Id),
Kundenummer NVARCHAR(200),
Kundenavn NVARCHAR(200),
UtcDateTimeCreated   DATETIME NOT NULL
)

Delete from dbo.sak where Sakstype = 1;
DROP TABLE Sak;



INSERT INTO Sakstype (Navn) VALUES ('Nykunde SUS');
INSERT INTO Sakstype (Navn) VALUES ('Nykunde AS');



