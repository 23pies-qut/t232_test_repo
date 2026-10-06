/* =============================================================================
   seed_multitenant.sql - run after schema.sql (safe to run again)

   Inserts demo data for tenant switching:
     adds Tenant C; Andy joins Tenant B and C; Bob joins Tenant C.
   Result: Andy = A/B/C, Bob = B/C. In Tenant B both see the same files;
   in other tenants RLS keeps them apart.
   ============================================================================= */
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.tenants WHERE id = '00000000000000000000000000000003')
    INSERT dbo.tenants (id, name, created_at, updated_at)
    VALUES ('00000000000000000000000000000003', 'Tenant C', SYSUTCDATETIME(), SYSUTCDATETIME());

INSERT dbo.tenant_memberships (id, user_id, tenant_id, joined_at)
SELECT v.id, v.user_id, v.tenant_id, SYSUTCDATETIME()
FROM (VALUES
    ('000000000000000000000000000000A1', '00000000000000000000000000000001', '00000000000000000000000000000002'),  -- Andy → Tenant B
    ('000000000000000000000000000000A2', '00000000000000000000000000000001', '00000000000000000000000000000003'),  -- Andy → Tenant C
    ('000000000000000000000000000000A3', '00000000000000000000000000000002', '00000000000000000000000000000003')   -- Bob  → Tenant C
) AS v (id, user_id, tenant_id)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tenant_memberships m WHERE m.user_id = v.user_id AND m.tenant_id = v.tenant_id);

PRINT 'Model v8 demo memberships ready: Andy = Tenant A/B/C, Bob = Tenant B/C.';
