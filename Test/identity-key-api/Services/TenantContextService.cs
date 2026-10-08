namespace IdentityKeyApi.Services;

public class TenantContextService
    : ITenantContextService
{
    private string? _tenantId;
    private string? _memberId;

    public string TenantId =>
        _tenantId
        ?? throw new InvalidOperationException(
            "Tenant context has not been set."
        );

    public string MemberId =>
        _memberId
        ?? throw new InvalidOperationException(
            "Member context has not been set."
        );

    public void SetContext(
        string tenantId,
        string memberId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId)
            );
        }

        if (string.IsNullOrWhiteSpace(memberId))
        {
            throw new ArgumentException(
                "Member ID is required.",
                nameof(memberId)
            );
        }

        _tenantId = tenantId;
        _memberId = memberId;
    }
}