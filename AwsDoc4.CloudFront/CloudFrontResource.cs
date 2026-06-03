using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("CloudFront")]
public class CloudFrontResource : AwsResourceBase, IAwsResource<CloudFrontResource>
{
    private static async Task<AmazonCloudFrontClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonCloudFrontClient>(profile)
            .ConfigureAwait(false);
    }
    public static bool HasCreateDate => false;

    public static bool HasLastModified => true;

    private CloudFrontResource(DistributionSummary distribution)
        : base(
            distribution.Comment ?? distribution.Id,
            distribution.ARN,
            distribution.DomainName)
    {
        this.LastModified = distribution.LastModifiedTime;
        this.DistributionId = distribution.Id;
        this.Status = distribution.Status;
        this.DomainName = distribution.DomainName;
        this.Enabled = distribution.Enabled == true;
    }

    public static async IAsyncEnumerable<CloudFrontResource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? marker = null;

        do
        {
            var response = await client.ListDistributionsAsync(
                new ListDistributionsRequest
                {
                    Marker = marker
                },
                ct).ConfigureAwait(false);

            var items = response.DistributionList?.Items;

            if (items != null)
            {
                foreach (var distribution in items)
                {
                    var name =
                        distribution.Comment ??
                        distribution.Id ??
                        string.Empty;

                    if (request.QueryString != null &&
                        !name.Contains(request.QueryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new CloudFrontResource(distribution);
                }
            }

            marker = response.DistributionList?.NextMarker;

        } while (!string.IsNullOrEmpty(marker));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "DistributionId", "Distribution ID")]
    public string? DistributionId { get; }

    [PropertyDescription(1, "基本", 7, "Status", "デプロイ状態")]
    public string? Status { get; private set; }

    [PropertyDescription(1, "基本", 8, "DomainName", "CloudFrontドメイン")]
    public string? DomainName { get; }

    [PropertyDescription(1, "基本", 9, "Enabled", "有効状態")]
    public bool Enabled { get; }

    // --- ルーティング ---

    [PropertyDescription(2, "ルーティング", 1, "Origins", "Origin一覧")]
    public JsonDocument? OriginsJson { get; private set; }

    [PropertyDescription(2, "ルーティング", 2, "CacheBehaviors", "Behavior一覧")]
    public JsonDocument? CacheBehaviorsJson { get; private set; }

    // --- ドメイン ---

    [PropertyDescription(3, "ドメイン", 1, "Aliases", "独自ドメイン")]
    public JsonDocument? AliasesJson { get; private set; }

    [PropertyDescription(3, "ドメイン", 2, "ViewerCertificate", "証明書")]
    public JsonDocument? ViewerCertificateJson { get; private set; }

    // --- セキュリティ ---

    [PropertyDescription(4, "セキュリティ", 1, "WebACLId", "WAF WebACL")]
    public string? WebAclId { get; private set; }

    // --- Edge ---

    [PropertyDescription(5, "Edge", 1, "LambdaFunctionAssociations", "Lambda@Edge")]
    public JsonDocument? LambdaFunctionAssociationsJson { get; private set; }

    [PropertyDescription(5, "Edge", 2, "FunctionAssociations", "CloudFront Functions")]
    public JsonDocument? FunctionAssociationsJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(6, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshDistributionAsync(client, ct),
            RefreshTagsAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshDistributionAsync(
        AmazonCloudFrontClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetDistributionAsync(
                new GetDistributionRequest
                {
                    Id = this.DistributionId
                },
                ct).ConfigureAwait(false);

            var config = res.Distribution.DistributionConfig;

            this.Status = res.Distribution.Status;

            this.OriginsJson = config.Origins?.Items
                .Select(x => new
                {
                    x.Id,
                    x.DomainName,
                    x.OriginPath,
                    x.ConnectionAttempts,
                    x.ConnectionTimeout
                })
                .ToJsonDocument();

            this.CacheBehaviorsJson = config.CacheBehaviors?.Items
                .Select(x => new
                {
                    x.PathPattern,
                    x.TargetOriginId,
                    x.ViewerProtocolPolicy,
                    x.CachePolicyId,
                    x.OriginRequestPolicyId
                })
                .ToJsonDocument();

            this.AliasesJson =
                config.Aliases?.Items.ToJsonDocument();

            this.ViewerCertificateJson = new
            {
                config.ViewerCertificate?.ACMCertificateArn,
                config.ViewerCertificate?.CloudFrontDefaultCertificate,
                config.ViewerCertificate?.MinimumProtocolVersion,
                config.ViewerCertificate?.SSLSupportMethod
            }.ToJsonDocument();

            this.WebAclId = config.WebACLId;

            this.LambdaFunctionAssociationsJson =
                config.DefaultCacheBehavior?
                    .LambdaFunctionAssociations?
                    .Items
                    .Select(x => new
                    {
                        x.EventType,
                        x.IncludeBody,
                        x.LambdaFunctionARN
                    })
                    .ToJsonDocument();

            this.FunctionAssociationsJson =
                config.DefaultCacheBehavior?
                    .FunctionAssociations?
                    .Items
                    .Select(x => new
                    {
                        x.EventType,
                        x.FunctionARN
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshTagsAsync(
        AmazonCloudFrontClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListTagsForResourceAsync(
                new ListTagsForResourceRequest
                {
                    Resource = this.Arn
                },
                ct).ConfigureAwait(false);

            this.TagsJson =
                res.Tags?.Items.ToJsonDocument();
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
