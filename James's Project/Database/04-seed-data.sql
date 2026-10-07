USE JamesProject
GO

DECLARE @TenantA UNIQUEIDENTIFIER = NEWID();
DECLARE @TenantB UNIQUEIDENTIFIER = NEWID();

INSERT INTO Tenants (TenantId, TenantName)
VALUES
	(@TenantA, 'Tenant A'),
	(@TenantB, 'Tenant B');


INSERT INTO Users
    (MemberId, FirstName, LastName, UserEmail, UserPhone, UserPasswordHash)
    /*at the moment a tenancy can belong to multiple people which would be okay but they 
    would have to be apart of the same company for cobble so I will add in company name and ID later for this security*/
VALUES
    (@TenantA, 1001, 'James', 'Smith', 'james@example.com', '0400000001', 'TestHash1'),
    (@TenantA, 1002, 'Sarah', 'Jones', 'sarah@example.com', '0400000002', 'TestHash2'),
    (@TenantB, 2001, 'Michael', 'Brown', 'michael@example.com', '0400000003', 'TestHash3'),
    (@TenantB, 2002, 'Emily', 'Wilson', 'emily@example.com', '0400000004', 'TestHash4');

GO