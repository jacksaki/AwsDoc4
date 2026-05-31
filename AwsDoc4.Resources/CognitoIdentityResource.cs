using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("Cognito Identity")]
public class CognitoIdentityResource : AwsResourceBase, IAwsResource<CognitoIdentityResource>
{
    private static async Task<AmazonCognitoIdentityClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonCognitoIdentityClient>(profile)
            .ConfigureAwait(false);
    }
    public static bool HasCreateDate => false;

    public static bool HasLastModified => false;

    private CognitoIdentityResource(IdentityPoolShortDescription pool)
        : base(
            pool.IdentityPoolName,
            pool.IdentityPoolId,
            null)
    {
        this.IdentityPoolId = pool.IdentityPoolId;
    }

    public static async IAsyncEnumerable<CognitoIdentityResource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.ListIdentityPoolsAsync(
                new ListIdentityPoolsRequest
                {
                    MaxResults = 50,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.IdentityPools != null)
            {
                foreach (var pool in response.IdentityPools)
                {
                    if (request.QueryString != null &&
                        !pool.IdentityPoolName.Contains(
                            request.QueryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new CognitoIdentityResource(pool);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "IdentityPoolId", "Identity Pool ID")]
    public string? IdentityPoolId { get; }

    [PropertyDescription(1, "基本", 7, "AllowUnauthenticatedIdentities", "未認証許可")]
    public bool AllowUnauthenticatedIdentities { get; private set; }

    [PropertyDescription(1, "基本", 8, "AllowClassicFlow", "Classic Flow")]
    public bool AllowClassicFlow { get; private set; }

    // --- 認証プロバイダ ---

    [PropertyDescription(2, "認証", 1, "SupportedLoginProviders", "ログインプロバイダ")]
    public JsonDocument? SupportedLoginProvidersJson { get; private set; }

    [PropertyDescription(2, "認証", 2, "CognitoIdentityProviders", "Cognito Provider")]
    public JsonDocument? CognitoIdentityProvidersJson { get; private set; }

    [PropertyDescription(2, "認証", 3, "OpenIdConnectProviderARNs", "OIDC Provider")]
    public JsonDocument? OpenIdConnectProviderARNsJson { get; private set; }

    [PropertyDescription(2, "認証", 4, "SamlProviderARNs", "SAML Provider")]
    public JsonDocument? SamlProviderARNsJson { get; private set; }

    // --- IAM ---

    [PropertyDescription(3, "IAM", 1, "Roles", "IAMロール")]
    public JsonDocument? RolesJson { get; private set; }

    [PropertyDescription(3, "IAM", 2, "RoleMappings", "Role Mapping")]
    public JsonDocument? RoleMappingsJson { get; private set; }

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
            RefreshPoolAsync(client, ct),
            RefreshRolesAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshPoolAsync(
        AmazonCognitoIdentityClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeIdentityPoolAsync(
                new DescribeIdentityPoolRequest
                {
                    IdentityPoolId = this.IdentityPoolId
                },
                ct).ConfigureAwait(false);

            this.AllowUnauthenticatedIdentities =
                res.AllowUnauthenticatedIdentities == true;

            this.AllowClassicFlow =
                res.AllowClassicFlow == true;

            this.SupportedLoginProvidersJson =
                res.SupportedLoginProviders.ToJsonDocument();

            this.CognitoIdentityProvidersJson =
                res.CognitoIdentityProviders
                    .Select(x => new
                    {
                        x.ProviderName,
                        x.ClientId,
                        x.ServerSideTokenCheck
                    })
                    .ToJsonDocument();

            this.OpenIdConnectProviderARNsJson =
                res.OpenIdConnectProviderARNs.ToJsonDocument();

            this.SamlProviderARNsJson =
                res.SamlProviderARNs.ToJsonDocument();

            this.TagsJson =
                res.IdentityPoolTags.ToJsonDocument();
        });
    }

    private async Task RefreshRolesAsync(
        AmazonCognitoIdentityClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetIdentityPoolRolesAsync(
                new GetIdentityPoolRolesRequest
                {
                    IdentityPoolId = this.IdentityPoolId
                },
                ct).ConfigureAwait(false);

            this.RolesJson =
                res.Roles.ToJsonDocument();

            this.RoleMappingsJson =
                res.RoleMappings.ToJsonDocument();
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
