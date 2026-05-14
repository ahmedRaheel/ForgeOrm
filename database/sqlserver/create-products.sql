CREATE DATABASE ForgeOrmDb;
GO
USE ForgeOrmDb;
GO

IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders','U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.CustomerProfiles','U') IS NOT NULL DROP TABLE dbo.CustomerProfiles;
IF OBJECT_ID('dbo.ProductCategories','U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID('dbo.Products','U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Brands','U') IS NOT NULL DROP TABLE dbo.Brands;
IF OBJECT_ID('dbo.Categories','U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Customers','U') IS NOT NULL DROP TABLE dbo.Customers;
IF TYPE_ID('dbo.OrderItemTvp') IS NOT NULL DROP TYPE dbo.OrderItemTvp;
GO

CREATE TABLE dbo.Categories
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL
);

CREATE TABLE dbo.Brands
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL
);

CREATE TABLE dbo.Products
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CategoryId INT NULL,
    BrandId INT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT FK_Products_Brands FOREIGN KEY(BrandId) REFERENCES dbo.Brands(Id)
);

CREATE TABLE dbo.ProductCategories
(
    ProductId INT NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT PK_ProductCategories PRIMARY KEY(ProductId, CategoryId),
    CONSTRAINT FK_ProductCategories_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
    CONSTRAINT FK_ProductCategories_Categories FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(Id)
);

CREATE TABLE dbo.Customers
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(250) NOT NULL
);

CREATE TABLE dbo.CustomerProfiles
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_CustomerProfiles_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.Orders
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    OrderNo NVARCHAR(50) NOT NULL,
    Status NVARCHAR(40) NOT NULL,
    GrandTotal DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    OrderDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY(CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.OrderItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(Id),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id)
);
GO

CREATE TYPE dbo.OrderItemTvp AS TABLE
(
    Id INT NULL,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.InsertOrderItemsTvp
    @Items dbo.OrderItemTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OrderItems(OrderId, ProductId, Quantity, UnitPrice, LineTotal)
    SELECT OrderId, ProductId, Quantity, UnitPrice, LineTotal
    FROM @Items;
END
GO

INSERT INTO dbo.Categories(Name) VALUES ('Keyboards'),('Mice'),('Monitors'),('Laptops');
INSERT INTO dbo.Brands(Name) VALUES ('ForgeTech'),('NovaWare'),('ApexDigital');

INSERT INTO dbo.Products(Code, Name, Price, CategoryId, BrandId)
VALUES
('P001','Mechanical Keyboard',50,1,1),
('P002','Wireless Mouse',25,2,2),
('P003','4K Monitor',220,3,3),
('P004','Developer Laptop',1450,4,1),
('P005','Ergonomic Keyboard',85,1,2);

INSERT INTO dbo.ProductCategories(ProductId, CategoryId)
SELECT Id, CategoryId FROM dbo.Products WHERE CategoryId IS NOT NULL;

INSERT INTO dbo.Customers(Name, Email)
VALUES ('Acme Corp','it@acme.test'),('Fervidex Systems','hello@fervidex.test'),('Contoso Retail','ops@contoso.test');

INSERT INTO dbo.CustomerProfiles(CustomerId, Phone, City)
VALUES (1,'+92-300-1111111','Karachi'),(2,'+92-300-2222222','Lahore'),(3,'+92-300-3333333','Islamabad');

INSERT INTO dbo.Orders(CustomerId, OrderNo, Status, GrandTotal, CreatedAt, OrderDate, TotalAmount)
VALUES
(1,'ORD-1001','Paid',270,SYSDATETIMEOFFSET(),SYSUTCDATETIME(),270),
(1,'ORD-1002','Processing',1535,SYSDATETIMEOFFSET(),SYSUTCDATETIME(),1535),
(2,'ORD-2001','Draft',75,SYSDATETIMEOFFSET(),SYSUTCDATETIME(),75);

INSERT INTO dbo.OrderItems(OrderId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES
(1,1,1,50,50),(1,3,1,220,220),(2,4,1,1450,1450),(2,5,1,85,85),(3,2,3,25,75);
GO

CREATE OR ALTER PROCEDURE dbo.GetProducts
AS
BEGIN
    SELECT Id, Code, Name, Price, CategoryId, BrandId FROM dbo.Products;
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

    SELECT Id, Code, Name, Price, CategoryId, BrandId
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

    SELECT p.Id, p.Code, p.Name, p.Price, c.Name AS CategoryName, b.Name AS BrandName
    FROM dbo.Products p
    LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
    LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
    WHERE p.Price >= @MinPrice
    ORDER BY p.Id DESC;
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
