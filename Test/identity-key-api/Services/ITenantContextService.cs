namespace IdentityKeyApi.Services;

public interface ITenantContextService
{
    string TenantId { get; }

    string MemberId { get; }

    void SetContext(
        string tenantId,
        string memberId
    );
}