CREATE DATABASE ForgeOrmDb;
GO
USE ForgeOrmDb;
GO

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Price DECIMAL(18,2) NOT NULL
);
GO

INSERT INTO dbo.Products(Code, Name, Price)
VALUES ('P001','Keyboard',50),('P002','Mouse',25),('P003','Monitor',220);
GO

CREATE OR ALTER PROCEDURE dbo.GetProducts
AS
BEGIN
    SELECT Id, Code, Name, Price FROM dbo.Products;
END
GO

CREATE OR ALTER FUNCTION dbo.GetProductCount()
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;
    SELECT @Count = COUNT(1) FROM dbo.Products;
    RETURN @Count;
END
GO
