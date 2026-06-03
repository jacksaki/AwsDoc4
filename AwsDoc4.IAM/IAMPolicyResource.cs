using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("IAM Policy")]
public class IamPolicyResource : AwsResourceBase, IAwsResource<IamPolicyResource>
{
    private static async Task<AmazonIdentityManagementServiceClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonIdentityManagementServiceClient>(profile)
            .ConfigureAwait(false);
    }
    public static bool HasCreateDate => true;

    public static bool HasLastModified => true;

    private IamPolicyResource(ManagedPolicy policy)
        : base(
            policy.PolicyName,
            policy.Arn,
            policy.Description)
    {
        this.PolicyId = policy.PolicyId;
        this.Path = policy.Path;
        this.DefaultVersionId = policy.DefaultVersionId;
        this.AttachmentCount = policy.AttachmentCount;
        this.CreateDate = policy.CreateDate;
        this.LastModified = policy.UpdateDate;
    }

    public static async IAsyncEnumerable<IamPolicyResource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? marker = null;

        do
        {
            var response = await client.ListPoliciesAsync(
                new ListPoliciesRequest
                {
                    Scope = PolicyScopeType.Local,
                    MaxItems = 100,
                    Marker = marker
                },
                ct).ConfigureAwait(false);

            if (response.Policies != null)
            {
                foreach (var policy in response.Policies)
                {
                    if (request.QueryString != null &&
                        !policy.PolicyName.Contains(
                            request.QueryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new IamPolicyResource(policy);
                }
            }

            marker = response.Marker;

        } while (marker != null);
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "PolicyId", "Policy ID")]
    public string? PolicyId { get; }

    [PropertyDescription(1, "基本", 7, "Path", "Path")]
    public string? Path { get; }

    [PropertyDescription(1, "基本", 8, "DefaultVersionId", "デフォルトVersion")]
    public string? DefaultVersionId { get; }

    [PropertyDescription(1, "基本", 9, "AttachmentCount", "アタッチ数")]
    public int? AttachmentCount { get; }

    // --- Policy ---

    [PropertyDescription(2, "Policy", 1, "PolicyDocument", "Policy Document")]
    public JsonDocument? PolicyDocumentJson { get; private set; }

    [PropertyDescription(2, "Policy", 2, "Versions", "Policy Versions")]
    public JsonDocument? VersionsJson { get; private set; }

    // --- 関連 ---

    [PropertyDescription(3, "関連", 1, "AttachedRoles", "Attached Roles")]
    public JsonDocument? AttachedRolesJson { get; private set; }

    [PropertyDescription(3, "関連", 2, "AttachedUsers", "Attached Users")]
    public JsonDocument? AttachedUsersJson { get; private set; }

    [PropertyDescription(3, "関連", 3, "AttachedGroups", "Attached Groups")]
    public JsonDocument? AttachedGroupsJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(4, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshPolicyAsync(client, ct),
            RefreshVersionsAsync(client, ct),
            RefreshEntitiesAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshPolicyAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetPolicyAsync(
                new GetPolicyRequest
                {
                    PolicyArn = this.Arn
                },
                ct).ConfigureAwait(false);

            var policy = res.Policy;

            this.TagsJson =
                policy.Tags
                    .Select(x => new
                    {
                        x.Key,
                        x.Value
                    })
                    .ToJsonDocument();

            if (!string.IsNullOrEmpty(policy.DefaultVersionId))
            {
                var version = await client.GetPolicyVersionAsync(
                    new GetPolicyVersionRequest
                    {
                        PolicyArn = this.Arn,
                        VersionId = policy.DefaultVersionId
                    },
                    ct).ConfigureAwait(false);

                this.PolicyDocumentJson =
                    version.PolicyVersion.Document
                        .ToJsonDocument();
            }
        });
    }

    private async Task RefreshVersionsAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListPolicyVersionsAsync(
                new ListPolicyVersionsRequest
                {
                    PolicyArn = this.Arn
                },
                ct).ConfigureAwait(false);

            this.VersionsJson =
                res.Versions
                    .Select(x => new
                    {
                        x.VersionId,
                        x.IsDefaultVersion,
                        x.CreateDate
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshEntitiesAsync(
        AmazonIdentityManagementServiceClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListEntitiesForPolicyAsync(
                new ListEntitiesForPolicyRequest
                {
                    PolicyArn = this.Arn
                },
                ct).ConfigureAwait(false);

            this.AttachedRolesJson =
                res.PolicyRoles
                    .Select(x => new
                    {
                        x.RoleName,
                        x.RoleId
                    })
                    .ToJsonDocument();

            this.AttachedUsersJson =
                res.PolicyUsers
                    .Select(x => new
                    {
                        x.UserName,
                        x.UserId
                    })
                    .ToJsonDocument();

            this.AttachedGroupsJson =
                res.PolicyGroups
                    .Select(x => new
                    {
                        x.GroupName,
                        x.GroupId
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