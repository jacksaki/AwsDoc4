using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("S3")]
public class S3Resource : AwsResourceBase, IAwsResource<S3Resource>
{
    private static async Task<AmazonS3Client> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory.CreateAsync<AmazonS3Client>(profile).ConfigureAwait(false);
    }
    private S3Resource(S3Bucket bucket) : base(bucket.BucketName, bucket.BucketArn, null)
    {
        this.Created = bucket.CreationDate;
    }

    public static async IAsyncEnumerable<S3Resource> EnumerateResourceAsync(AwsProfile profile, string? queryString, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var client = await GetClientAsync(profile).ConfigureAwait(false);
        string? token = null;
        do
        {
            var response = await client.ListBucketsAsync(
                new ListBucketsRequest()
                {
                    MaxBuckets = 100,
                    ContinuationToken = token,
                    BucketRegion = profile.Profile.Region.SystemName,
                }).ConfigureAwait(false);
            if (response.Buckets != null)
            {
                foreach (var bucket in response.Buckets)
                {
                    if (queryString != null && !bucket.BucketName.Contains(queryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    yield return new S3Resource(bucket);
                }
            }

            token = response.ContinuationToken;

        } while (!string.IsNullOrEmpty(token));
    }

    public override async Task RefreshResourceAsync(AwsProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var client = await GetClientAsync(profile).ConfigureAwait(false);
        await Task.WhenAll(
            RefreshBucketLocationAsync(client, ct),
            RefreshBucketVersioningAsync(client, ct),
            RefreshBucketAclAsync(client, ct),
            RefreshBucketPolicyAsync(client, ct),
            RefreshBucketPolicyStatusAsync(client, ct),
            RefreshPublicAccessBlockAsync(client, ct),
            RefreshBucketEncryptionAsync(client, ct),
            RefreshBucketTaggingAsync(client, ct),
            RefreshBucketLoggingAsync(client, ct),
            RefreshBucketWebsiteAsync(client, ct),
            RefreshBucketLifecycleAsync(client, ct),
            RefreshBucketReplicationAsync(client, ct),
            RefreshBucketCorsAsync(client, ct),
            RefreshBucketNotificationAsync(client, ct)
            ).ContinueWith(_ => this.IsLoaded = true).ConfigureAwait(false);
    }

    // --- 基本 ---
    [PropertyDescription(1, "基本", 4, "リージョン", "バケットが配置されているリージョン")]
    public string? Region { get; private set; }

    [PropertyDescription(1, "基本", 5, "S3 Uri", "S3 URI")]
    public string? S3Uri => $"s3://{this.Name}/";

    [PropertyDescription(1, "基本", 6, "作成日時", "作成日時")]
    public DateTime? Created { get; private set; }

    [PropertyDescription(1, "基本", 7, "バージョニング状態", "オブジェクトのバージョニング設定")]
    public string? VersioningStatus { get; private set; }

    [PropertyDescription(1, "基本", 8, "MFA削除", "削除時にMFAが必要かどうか")]
    public bool? MfaDeleteEnabled { get; private set; }

    // --- セキュリティ ---
    [PropertyDescription(2, "セキュリティ", 1, "ACL設定", "アクセスコントロールリスト（Grants含む完全情報）")]
    public JsonDocument? GrantsJson { get; private set; }

    [PropertyDescription(2, "セキュリティ", 2, "バケットポリシー", "バケットに設定されているIAMポリシー(JSON)")]
    public JsonDocument? BucketPolicyJson { get; private set; }

    [PropertyDescription(2, "セキュリティ", 3, "パブリック判定", "バケットが公開状態かどうか")]
    public bool IsPublic { get; private set; }

    [PropertyDescription(2, "セキュリティ", 4, "パブリックACLブロック", "パブリックACLの作成を禁止")]
    public bool BlockPublicAcls { get; private set; }

    [PropertyDescription(2, "セキュリティ", 5, "パブリックポリシーブロック", "パブリックポリシーの適用を拒否")]
    public bool BlockPublicPolicy { get; private set; }

    [PropertyDescription(2, "セキュリティ", 6, "パブリックACL無視", "既存のパブリックACLを無視")]
    public bool IgnorePublicAcls { get; private set; }

    [PropertyDescription(2, "セキュリティ", 6, "パブリック制限", "パブリックバケットへのアクセス制限")]
    public bool RestrictPublicBuckets { get; private set; }

    [PropertyDescription(2, "セキュリティ", 7, "暗号化設定", "サーバーサイド暗号化設定（KMS含む）")]
    public JsonDocument? EncryptionJson { get; private set; }


    // --- データ管理 ---
    [PropertyDescription(3, "データ管理", 1, "ライフサイクル設定", "オブジェクトの保持・削除・移行ルール")]
    public JsonDocument? LifecycleRulesJson { get; private set; }

    [PropertyDescription(3, "データ管理", 2, "レプリケーション設定", "クロスリージョンレプリケーション設定")]
    public JsonDocument? ReplicationJson { get; private set; }


    // --- 連携 ---
    [PropertyDescription(4, "連携", 1, "通知設定", "Lambda/SQS/SNSへのイベント通知設定")]
    public JsonDocument? NotificationJson { get; private set; }

    [PropertyDescription(4, "連携", 2, "CORS設定", "クロスオリジンアクセス制御設定")]
    public JsonDocument? CorsRulesJson { get; private set; }


    // --- 運用 ---
    [PropertyDescription(5, "運用", 1, "タグ", "バケットに付与されたタグ一覧")]
    public JsonDocument? TagsJson { get; private set; }

    [PropertyDescription(5, "運用", 2, "ログ出力先バケット", "アクセスログの保存先バケット")]
    public string? LoggingTargetBucket { get; private set; }

    [PropertyDescription(5, "運用", 3, "ログプレフィックス", "ログファイルのプレフィックス")]
    public string? LoggingTargetPrefix { get; private set; }

    [PropertyDescription(5, "運用", 4, "Webサイト設定", "静的ウェブサイトホスティング設定")]
    public JsonDocument? WebsiteJson { get; private set; }
    //// --- その他 ---
    //[PropertyDescription("支払い設定", "リクエスタ支払い設定（Requester Pays）")]
    //public string? RequestPaymentConfiguration { get; set; }

    //[PropertyDescription("転送高速化", "Transfer Accelerationの有効状態")]
    //public string? AccelerateStatus { get; set; }

    //[PropertyDescription("オブジェクト所有権", "オブジェクト所有者の制御設定")]
    //public string? ObjectOwnership { get; set; }

    //// --- 分析系 ---
    //[PropertyDescription("メトリクス設定", "CloudWatchメトリクス構成")]
    //public string? MetricsConfigurationsJson { get; set; }

    //[PropertyDescription("インテリジェント階層化", "自動階層化ルール設定")]
    //public string? IntelligentTieringConfigurationsJson { get; set; }

    //[PropertyDescription("分析設定", "ストレージ分析設定")]
    //public string? AnalyticsConfigurationsJson { get; set; }

    //[PropertyDescription("インベントリ設定", "オブジェクトインベントリ出力設定")]
    //public string? InventoryConfigurationsJson { get; set; }
    private async Task RefreshBucketLocationAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.Region = res.Location?.Value;
        }
        catch { }
    }
    private async Task RefreshBucketVersioningAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.VersioningStatus = res.VersioningConfig.Status?.Value;
            this.MfaDeleteEnabled = res.VersioningConfig.EnableMfaDelete;
        }
        catch { }
    }
    private async Task RefreshBucketAclAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketAclAsync(new GetBucketAclRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.GrantsJson = res.Grants.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketPolicyAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketPolicyAsync(new GetBucketPolicyRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.BucketPolicyJson = res.Policy.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketPolicyStatusAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketPolicyStatusAsync(new GetBucketPolicyStatusRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.IsPublic = res.PolicyStatus?.IsPublic ?? false;
        }
        catch { }
    }
    private async Task RefreshPublicAccessBlockAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetPublicAccessBlockAsync(new GetPublicAccessBlockRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            var c = res.PublicAccessBlockConfiguration;

            this.BlockPublicAcls = c?.BlockPublicAcls ?? false;
            this.BlockPublicPolicy = c?.BlockPublicPolicy ?? false;
            this.IgnorePublicAcls = c?.IgnorePublicAcls ?? false;
            this.RestrictPublicBuckets = c?.RestrictPublicBuckets ?? false;
        }
        catch { }
    }
    private async Task RefreshBucketEncryptionAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketEncryptionAsync(new GetBucketEncryptionRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.EncryptionJson = res.ServerSideEncryptionConfiguration.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketTaggingAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketTaggingAsync(new GetBucketTaggingRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.TagsJson = res.TagSet.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketLoggingAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketLoggingAsync(new GetBucketLoggingRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.LoggingTargetBucket = res.BucketLoggingConfig?.TargetBucketName;
            this.LoggingTargetPrefix = res.BucketLoggingConfig?.TargetPrefix;
        }
        catch { }
    }
    private async Task RefreshBucketWebsiteAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketWebsiteAsync(new GetBucketWebsiteRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.WebsiteJson = res.WebsiteConfiguration.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketLifecycleAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetLifecycleConfigurationAsync(new GetLifecycleConfigurationRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.LifecycleRulesJson = res.Configuration.Rules.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketReplicationAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketReplicationAsync(new GetBucketReplicationRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.ReplicationJson = res.Configuration.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketCorsAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetCORSConfigurationAsync(new GetCORSConfigurationRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);

            this.CorsRulesJson = res.Configuration.Rules.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshBucketNotificationAsync(AmazonS3Client client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetBucketNotificationAsync(new GetBucketNotificationRequest
            {
                BucketName = this.Name
            }, ct).ConfigureAwait(false);


            this.NotificationJson = (new
            {
                Lambda = res.LambdaFunctionConfigurations,
                SQS = res.QueueConfigurations,
                SNS = res.TopicConfigurations
            }).ToJsonDocument();
        }
        catch { }
    }

    //private async Task RefreshBucketRequestPaymentAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.GetBucketRequestPaymentAsync(new GetBucketRequestPaymentRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.RequestPaymentConfiguration = res.Payer;
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketAccelerateAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.GetBucketAccelerateConfigurationAsync(new GetBucketAccelerateConfigurationRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.AccelerateStatus = res.Status?.Value;
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketOwnershipAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.GetBucketOwnershipControlsAsync(new GetBucketOwnershipControlsRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.ObjectOwnership = res.OwnershipControls?.Rules?.FirstOrDefault()?.ObjectOwnership?.Value;
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketMetricsAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.ListBucketMetricsConfigurationsAsync(new ListBucketMetricsConfigurationsRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.MetricsConfigurationsJson = res.MetricsConfigurationList.FormatJson();
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketIntelligentTieringAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.ListBucketIntelligentTieringConfigurationsAsync(new ListBucketIntelligentTieringConfigurationsRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.IntelligentTieringConfigurationsJson = res.IntelligentTieringConfigurationList.FormatJson();
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketAnalyticsAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.ListBucketAnalyticsConfigurationsAsync(new ListBucketAnalyticsConfigurationsRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.AnalyticsConfigurationsJson = res.AnalyticsConfigurationList.FormatJson();
    //    }
    //    catch { }
    //}
    //private async Task RefreshBucketInventoryAsync(CancellationToken ct = default)
    //{
    //    try
    //    {
    //        var res = await _client.ListBucketInventoryConfigurationsAsync(new ListBucketInventoryConfigurationsRequest
    //        {
    //            BucketName = this.Name
    //        }, ct).ConfigureAwait(false);

    //        this.InventoryConfigurationsJson = res.InventoryConfigurationList.FormatJson();
    //    }
    //    catch { }
    //}
}
