-- ===================================
-- P_TESTINGTABLEARTICLES - SQL SETUP
-- Execute this in SQL Server Management Studio (SSMS) or Azure Data Studio
-- ===================================

-- ===================================
-- 1. DROP TABLE (if exists)
-- ===================================
IF OBJECT_ID('dbo.P_TestingTableArticles', 'U') IS NOT NULL
    DROP TABLE dbo.P_TestingTableArticles;
GO

-- ===================================
-- 2. CREATE TABLE
-- ===================================
CREATE TABLE dbo.P_TestingTableArticles (
    -- Primary Key
    ArticleId INT IDENTITY(1,1) PRIMARY KEY,

    -- String columns
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX),
    Category VARCHAR(50),
    Author NVARCHAR(100),

    -- Numeric columns
    ViewCount INT DEFAULT 0,
    LikeCount BIGINT DEFAULT 0,
    Rating DECIMAL(3,2),  -- Example: 4.75

    -- Date/Time columns
    CreatedDate DATETIME DEFAULT GETDATE(),
    PublishedDate DATETIME,
    LastModifiedDate DATETIME,

    -- Boolean columns (BIT in SQL Server)
    IsPublished BIT DEFAULT 0,
    IsFeatured BIT DEFAULT 0,
    AllowComments BIT DEFAULT 1,

    -- Additional types
    MetaDataJson NVARCHAR(MAX),  -- For storing JSON data
    Tags VARCHAR(500),  -- Comma-separated tags
    Version VARCHAR(20)
);
GO

-- ===================================
-- 3. INSERT SAMPLE DATA (Optional)
-- ===================================
INSERT INTO dbo.P_TestingTableArticles
    (Title, Content, Category, Author, Rating, ViewCount, LikeCount, IsPublished, IsFeatured, Tags, Version)
VALUES
    ('Introduction to SQL Server',
     'Complete guide to SQL Server fundamentals covering database design, querying, and optimization.',
     'Technology',
     'John Doe',
     4.75,
     150,
     45,
     1,
     1,
     'SQL,Database,Tutorial,Beginner',
     '1.0.0'),

    ('Advanced Query Optimization',
     'Performance tuning techniques for complex SQL queries including indexing strategies and execution plans.',
     'Technology',
     'Jane Smith',
     4.50,
     200,
     60,
     1,
     0,
     'SQL,Performance,Optimization,Advanced',
     '1.0.0'),

    ('Database Security Best Practices',
     'How to secure your database with encryption, access control, and audit logging.',
     'Security',
     'Bob Johnson',
     4.90,
     300,
     90,
     1,
     1,
     'Security,Database,BestPractices,Enterprise',
     '1.0.0'),

    ('Understanding Transactions and Locks',
     'Deep dive into SQL Server transaction management and locking mechanisms.',
     'Technology',
     'Alice Brown',
     4.60,
     175,
     50,
     1,
     0,
     'SQL,Transactions,Concurrency,Advanced',
     '1.0.0'),

    ('Draft Article - Work in Progress',
     'This is a draft article that has not been published yet.',
     'General',
     'Mike Wilson',
     NULL,
     0,
     0,
     0,
     0,
     'Draft',
     '0.1.0');
GO

-- ===================================
-- 4. VERIFY DATA
-- ===================================
SELECT * FROM dbo.P_TestingTableArticles;
GO

-- ===================================
-- 5. QUERY EXAMPLES
-- ===================================

-- Get all published articles
SELECT ArticleId, Title, Category, Author, Rating, ViewCount
FROM dbo.P_TestingTableArticles
WHERE IsPublished = 1
ORDER BY CreatedDate DESC;
GO

-- Get featured articles
SELECT ArticleId, Title, Author, Rating, ViewCount
FROM dbo.P_TestingTableArticles
WHERE IsFeatured = 1 AND IsPublished = 1
ORDER BY ViewCount DESC;
GO

-- Get articles by category
SELECT ArticleId, Title, Author, Rating
FROM dbo.P_TestingTableArticles
WHERE Category = 'Technology' AND IsPublished = 1;
GO

-- Search by title
SELECT ArticleId, Title, Category, Author
FROM dbo.P_TestingTableArticles
WHERE Title LIKE '%SQL%';
GO

-- Get highly rated articles (rating >= 4.5)
SELECT ArticleId, Title, Author, Rating, ViewCount
FROM dbo.P_TestingTableArticles
WHERE Rating >= 4.5 AND IsPublished = 1
ORDER BY Rating DESC;
GO

-- Get articles with high view count
SELECT ArticleId, Title, Author, ViewCount, LikeCount
FROM dbo.P_TestingTableArticles
WHERE ViewCount >= 100
ORDER BY ViewCount DESC;
GO

-- Statistics by category
SELECT
    Category,
    COUNT(*) as ArticleCount,
    AVG(Rating) as AverageRating,
    SUM(ViewCount) as TotalViews,
    SUM(LikeCount) as TotalLikes,
    MAX(ViewCount) as MaxViews,
    MIN(ViewCount) as MinViews
FROM dbo.P_TestingTableArticles
WHERE IsPublished = 1
GROUP BY Category
ORDER BY ArticleCount DESC;
GO

-- ===================================
-- 6. UPDATE EXAMPLES
-- ===================================

-- Increment view count for an article
UPDATE dbo.P_TestingTableArticles
SET ViewCount = ViewCount + 1,
    LastModifiedDate = GETDATE()
WHERE ArticleId = 1;
GO

-- Increment like count
UPDATE dbo.P_TestingTableArticles
SET LikeCount = LikeCount + 1
WHERE ArticleId = 1;
GO

-- Publish a draft article
UPDATE dbo.P_TestingTableArticles
SET IsPublished = 1,
    PublishedDate = GETDATE()
WHERE ArticleId = 5;
GO

-- Update article content
UPDATE dbo.P_TestingTableArticles
SET Content = 'Updated content here...',
    LastModifiedDate = GETDATE(),
    Version = '1.1.0'
WHERE ArticleId = 1;
GO

-- ===================================
-- 7. DELETE EXAMPLES
-- ===================================

-- Delete unpublished drafts older than 30 days
DELETE FROM dbo.P_TestingTableArticles
WHERE IsPublished = 0
  AND CreatedDate < DATEADD(DAY, -30, GETDATE());
GO

-- ===================================
-- 8. PERFORMANCE - FOR JSON PATH EXAMPLE
-- ===================================
-- This is what the library uses for 40-60% better performance

SELECT
    ArticleId,
    Title,
    Category,
    Author,
    Rating,
    ViewCount
FROM dbo.P_TestingTableArticles
WHERE Category = 'Technology' AND IsPublished = 1
FOR JSON PATH, INCLUDE_NULL_VALUES;
GO

-- ===================================
-- 9. CLEANUP (Use with caution!)
-- ===================================

-- Uncomment to delete all data (keeps table structure)
-- DELETE FROM dbo.P_TestingTableArticles;
-- GO

-- Uncomment to drop the table completely
-- DROP TABLE dbo.P_TestingTableArticles;
-- GO
