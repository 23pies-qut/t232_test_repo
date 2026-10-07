USE JamesProject;
GO


CREATE TABLE Tenants (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    TenantName NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_Tenants
        PRIMARY KEY (TenantId),

    CONSTRAINT Unique_TenantName
        UNIQUE (TenantName)
);
/* At the moment there is a many to one relationship between users and tenancies. 
This is because TenantId is directly stored inside the users table so a user can only have 1 tenantID. 
In the future if the design requires a many to many relationship between users and tenancies I will make 
a seperate "Tenancies" tables to store all tenantIDs that will link to userIDs.*/
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    UserEmail NVARCHAR(100) NOT NULL,
    UserPhone NVARCHAR(20) NOT NULL,
    UserPasswordHash NVARCHAR(255) NOT NULL,

    CONSTRAINT Unique_UserPhone
        UNIQUE (UserPhone),

    CONSTRAINT Unique_UserEmail
        UNIQUE (UserEmail)
);
GO

/* New table that links users with their tenacies, allowing a single user to have multiple tenancies
and one tenancy to have multiple users (many-to-many relationship)*/
CREATE TABLE UserTenants (
    UserId INT NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT PK_UserTenants
        PRIMARY KEY (UserId, TenantId),

    CONSTRAINT FK_UserTenants_Users
        FOREIGN KEY (UserId)
        REFERENCES Users(Id),

    CONSTRAINT FK_UserTenants_Tenants
        FOREIGN KEY (TenantId)
        REFERENCES Tenants(TenantId)
);
GO