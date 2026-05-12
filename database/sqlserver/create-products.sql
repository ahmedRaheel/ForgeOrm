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

CREATE OR ALTER PROCEDURE dbo.SearchProducts
    @Code NVARCHAR(100) = NULL,
    @Name NVARCHAR(250) = NULL,
    @MinPrice DECIMAL(18,2) = NULL,
    @MaxPrice DECIMAL(18,2) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SET @Page = ISNULL(NULLIF(@Page, 0), 1);
    SET @PageSize = ISNULL(NULLIF(@PageSize, 0), 20);

    SELECT Id, Code, Name, Price
    FROM dbo.Products
    WHERE (@Code IS NULL OR Code = @Code)
      AND (@Name IS NULL OR Name LIKE '%' + @Name + '%')
      AND (@MinPrice IS NULL OR Price >= @MinPrice)
      AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
    ORDER BY Id DESC
    OFFSET ((@Page - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetProductList
    @MinPrice DECIMAL(18,2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Code, Name, Price
    FROM dbo.Products
    WHERE Price >= @MinPrice
    ORDER BY Id DESC;
END
GO

CREATE OR ALTER FUNCTION dbo.fn_ProductCount()
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;
    SELECT @Count = COUNT(1) FROM dbo.Products;
    RETURN @Count;
END
GO
