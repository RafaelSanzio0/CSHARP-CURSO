USE [UdemyDB]
SELECT * FROM [User]

INSERT INTO [User] VALUES (NEWID(), GETDATE(), NULL, 'ADMIN', 'admin@gmail.com')

SELECT * FROM [Uf]

SELECT * FROM [Municipio]

SELECT * FROM [Cep]

-- retorna tabelas de um banco
SELECT * FROM information_schema.tables;