using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("IAM Role")]
public class IamRoleResource : AwsResourceBase, IAwsResource<IamRoleResource>
{
    private static async Task<AmazonIdentityManagementServiceClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonIdentityManagementServiceClient>(profile)
            .ConfigureAwait(false);
    }

    private IamRoleResource(Role role)
        : base(
            role.RoleName,
            role.Arn,
            role.Description)
    {
        this.RoleId = role.RoleId;
        this.Path = role.Path;
        this.CreateDate = role.CreateDate;
    }

    public static async IAsyncEnumerable<IamRoleResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? marker = null;

        do
        {
            var response = await client.ListRolesAsync(
                new ListRolesRequest
                {
                    MaxItems = 100,
                    Marker = marker
                },
                ct).ConfigureAwait(false);

            if (response.Roles != null)
            {
                foreach (var role in response.Roles)
                {
                    if (queryString != null &&
                        !role.RoleName.Contains(
                            queryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new IamRoleResource(role);
                }
            }

            marker = response.Marker;

        } while (marker != null);

    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "RoleId", "Role ID")]
    public string? RoleId { get; }

    [PropertyDescription(1, "基本", 5, "Path", "Path")]
    public string? Path { get; }

    [PropertyDescription(1, "基本", 6, "CreateDate", "作成日時")]
    public DateTime? CreateDate { get; }

    [PropertyDescription(1, "基本", 7, "MaxSessionDuration", "最大セッション時間")]
    public int? MaxSessionDuration { get; private set; }

    // --- AssumeRole ---

    [PropertyDescription(2, "AssumeRole", 1, "AssumeRolePolicy", "AssumeRole Policy")]
    public JsonDocument? AssumeRolePolicyJson { get; private set; }

    // --- Policy ---

    [PropertyDescription(3, "Policy", 1, "AttachedPolicies", "アタッチ済みPolicy")]
    public JsonDocument? AttachedPoliciesJson { get; private set; }

    [PropertyDescription(3, "Policy", 2, "InlinePolicies", "Inline Policy")]
    public JsonDocument? InlinePoliciesJson { get; private set; }

    // --- 関連 ---

    [PropertyDescription(4, "関連", 1, "InstanceProfiles", "Instance Profile")]
    public JsonDocument? InstanceProfilesJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(5, "運用", 1, "RoleLastUsed", "最終使用")]
    public JsonDocument? RoleLastUsedJson { get; private set; }

    [PropertyDescription(5, "運用", 2, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshRoleAsync(client, ct),
            RefreshAttachedPoliciesAsync(client, ct),
            RefreshInlinePoliciesAsync(client, ct),
            RefreshInstanceProfilesAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshRoleAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetRoleAsync(
                new GetRoleRequest
                {
                    RoleName = this.Name
                },
                ct).ConfigureAwait(false);

            var role = res.Role;

            this.MaxSessionDuration =
                role.MaxSessionDuration;

            this.AssumeRolePolicyJson =
                role.AssumeRolePolicyDocument
                    .ToJsonDocument();

            this.RoleLastUsedJson =
                role.RoleLastUsed.ToJsonDocument();

            this.TagsJson =
                role.Tags
                    .Select(x => new
                    {
                        x.Key,
                        x.Value
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshAttachedPoliciesAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListAttachedRolePoliciesAsync(
                new ListAttachedRolePoliciesRequest
                {
                    RoleName = this.Name
                },
                ct).ConfigureAwait(false);

            this.AttachedPoliciesJson = res.AttachedPolicies
                .Select(x => new
                {
                    x.PolicyName,
                    x.PolicyArn
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshInlinePoliciesAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var names = await client.ListRolePoliciesAsync(
                new ListRolePoliciesRequest
                {
                    RoleName = this.Name
                },
                ct).ConfigureAwait(false);

            var policies = new List<object>();

            foreach (var policyName in names.PolicyNames)
            {
                var policy = await client.GetRolePolicyAsync(
                    new GetRolePolicyRequest
                    {
                        RoleName = this.Name,
                        PolicyName = policyName
                    },
                    ct).ConfigureAwait(false);

                policies.Add(new
                {
                    policy.PolicyName,
                    policy.PolicyDocument
                });
            }

            this.InlinePoliciesJson =
                policies.ToJsonDocument();
        });
    }

    private async Task RefreshInstanceProfilesAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListInstanceProfilesForRoleAsync(
                new ListInstanceProfilesForRoleRequest
                {
                    RoleName = this.Name
                },
                ct).ConfigureAwait(false);

            this.InstanceProfilesJson =
                res.InstanceProfiles
                    .Select(x => new
                    {
                        x.InstanceProfileName,
                        x.Arn,
                        x.Path
                    })
                    .ToJsonDocument();
        });
    }

    private static async Task SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
