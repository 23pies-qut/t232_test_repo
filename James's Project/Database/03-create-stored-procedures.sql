USE JamesProject;
GO 

/*Gets all of the users that belong to the specified tenant and then converts the results into Json. 
This is used for when the API needs to retreive a list of users for a particular tenant*/

/* changed both GetUserByTenant and GetUserById because I changed how how the relationship between users and tenants work. 
Originally, the "Users: table contained TenantId which made it easy to pair a user with a TenantId but 
it meant it was only a many to one relationship where the tenancy could have many users but the user can only have one tenant.
Now that there is a seperate table in 02 "UserTenants" the relationship is a many to many relationship.
But this meant that the procedures could not look directly inside Users for TenantId anymore which is why there is "INNER JOIN" 
to connect the two tables together.*/
CREATE PROCEDURE GetUsersByTenant
	@TenantId UNIQUEIDENTIFIER
AS 
BEGIN 
	SELECT
		U.Id,
		U.MemberId,
		U.FirstName,
		U.LastName,
		U.UserEmail,
		U.UserPhone
	FROM Users U
	/*INNER JOIN joins both the User and UserTenant tables together*/
	INNER JOIN UserTenants UT
		ON U.Id = UT.UserId
	WHERE UT.TenantId = @TenantId
	FOR JSON PATH;
END;
GO

/*searching for one specific user belonging to a specific Tenant by providing both member ID and Tenant ID*/
CREATE PROCEDURE GetUserById
	@Id INT,
	@TenantId UNIQUEIDENTIFIER
AS
BEGIN
	SELECT
	/*the U. and UT. prefixed had to be added to specify which table the data is coming from now that the two have been joined together*/
		U.Id,
		U.MemberId,
		U.FirstName,
		U.LastName,
		U.UserEmail,
		U.UserPhone
	FROM Users U
	INNER JOIN UserTenants UT
	/* Finds the row where the Id from the User table and the UserId from the UserTenants table equal the same thing.
	So then SQL connects those two rows.*/
		ON U.Id = UT.UserId
	WHERE U.Id = @Id
		AND UT.TenantId = @TenantId
	FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO

CREATE PROCEDURE CreateUser
/*Procedure is recieving these parameters, which will create the user.*/
	@TenantId UNIQUEIDENTIFIER,
	@MemberId INT,
	@FirstName NVARCHAR(100),
	@LastName NVARCHAR(100),
	@UserEmail NVARCHAR(100),
	@UserPhone NVARCHAR(20),
	@UserPasswordHash NVARCHAR (225)
AS
BEGIN
	/*Inserts the user into the "Users" table*/
	INSERT INTO Users (
		MemberId,
		FirstName,
		LastName,
		UserEmail,
		UserPhone,
		UserPasswordHash
	)

	VALUES (
	@MemberId,
	@FirstName,
	@LastName,
	@UserEmail,
	@UserPhone,
	@UserPasswordHash
);
	/*gets the Id SQL server just created*/
	DECLARE @UserId INT = SCOPE_IDENTITY();

	INSERT INTO UserTenants (
		UserId,
		TenantId
	)

	VALUES (
		@UserId,
		@TenantId
	);

	SELECT
		U.Id,
		U.MemberId,
		U.FirstName,
		U.LastName,
		U.UserEmail,
		U.UserPhone
	FROM Users U
	WHERE U.Id = @UserId
	FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO


CREATE PROCEDURE UpdateUser
    @Id INT,
    @TenantId UNIQUEIDENTIFIER,
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @UserEmail NVARCHAR(100),
    @UserPhone NVARCHAR(20)
AS
BEGIN
    UPDATE Users
    SET
        FirstName = @FirstName,
        LastName = @LastName,
        UserEmail = @UserEmail,
        UserPhone = @UserPhone
    WHERE Id = @Id
      AND Id IN (
          SELECT UserId
          FROM UserTenants
          WHERE TenantId = @TenantId
      );

    SELECT
        U.Id,
        U.MemberId,
        U.FirstName,
        U.LastName,
        U.UserEmail,
        U.UserPhone
    FROM Users U
    WHERE U.Id = @Id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO


CREATE PROCEDURE DeleteUser
    @Id INT
AS
BEGIN
	/*deleted from UserTenants first because it is a foreign key that links to Users.Id.*/
    DELETE FROM UserTenants
    WHERE UserId = @Id;

    DELETE FROM Users
    WHERE Id = @Id;

    SELECT
        @Id AS DeletedUserId
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;
GO