using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("Lambda")]
public class LambdaResource : AwsResourceBase, IAwsResource<LambdaResource>
{
    private static async Task<AmazonLambdaClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory.CreateAsync<AmazonLambdaClient>(profile).ConfigureAwait(false);
    }
    private LambdaResource(FunctionConfiguration lambda) : base(lambda.FunctionName, lambda.FunctionArn, lambda.Description)
    {
        this.LastModified = lambda.LastModified.ToDateTimeN();
    }

    public static async IAsyncEnumerable<LambdaResource> EnumerateResourceAsync(AwsProfile profile, string? queryString, [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var client = await GetClientAsync(profile).ConfigureAwait(false);
        string? marker = null;
        do
        {
            var response = await client.ListFunctionsAsync(
                new ListFunctionsRequest()
                {
                    MaxItems = 50,
                    Marker = marker,
                }).ConfigureAwait(false);
            if (response.Functions != null)
            {
                foreach (var function in response.Functions)
                {
                    if (queryString != null && !function.FunctionName.Contains(queryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    yield return new LambdaResource(function);
                }
            }

            marker = response.NextMarker;

        } while (!string.IsNullOrEmpty(marker));
    }

    [PropertyDescription(1, "基本", 4, "最終更新日時", "最終更新日時")]
    public DateTime? LastModified { get; }


    [PropertyDescription(1, "基本", 5, "ランタイム", "使用ランタイム")]
    public string? Runtime { get; private set; }

    [PropertyDescription(1, "基本", 6, "ハンドラ", "エントリポイント")]
    public string? Handler { get; private set; }

    // --- セキュリティ ---
    [PropertyDescription(2, "セキュリティ", 1, "実行ロール", "IAMロールARN")]
    public string? RoleArn { get; private set; }

    [PropertyDescription(2, "セキュリティ", 2, "リソースポリシー", "Lambdaのリソースベースポリシー")]
    public JsonDocument? ResourcePolicyJson { get; private set; }

    // --- 実行設定 ---
    [PropertyDescription(3, "実行設定", 1, "タイムアウト", "最大実行時間(秒)")]
    public int Timeout { get; private set; }

    [PropertyDescription(3, "実行設定", 2, "メモリサイズ", "割り当てメモリ(MB)")]
    public int MemorySize { get; private set; }

    [PropertyDescription(3, "実行設定", 3, "環境変数", "環境変数一覧")]
    public JsonDocument? EnvironmentVariablesJson { get; private set; }

    [PropertyDescription(3, "実行設定", 4, "VPC設定", "VPC/Subnet/SecurityGroup設定")]
    public JsonDocument? VpcConfigJson { get; private set; }

    // --- 連携 ---
    [PropertyDescription(4, "連携", 1, "イベントソースマッピング", "SQS/Kinesisなどとの接続")]
    public JsonDocument? EventSourceMappingsJson { get; private set; }

    [PropertyDescription(4, "連携", 2, "EventBridgeルール", "スケジュール/イベントトリガー")]
    public JsonDocument? EventBridgeRulesJson { get; private set; }

    // --- 運用 ---
    [PropertyDescription(5, "運用", 1, "ロググループ", "CloudWatch Logs")]
    public string? LogGroupName { get; private set; }

    [PropertyDescription(5, "運用", 2, "バージョン", "公開バージョン情報")]
    public JsonDocument? VersionsJson { get; private set; }

    private static async Task<AmazonEventBridgeClient> GetEventBridgeClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory.CreateAsync<AmazonEventBridgeClient>(profile).ConfigureAwait(false);
    }

    public override async Task RefreshResourceAsync(AwsProfile profile, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var client = await GetClientAsync(profile).ConfigureAwait(false);
        var eventBridgeClient = await GetEventBridgeClientAsync(profile).ConfigureAwait(false);
        await Task.WhenAll(
            RefreshFunctionAsync(client, ct),
            RefreshPolicyAsync(client, ct),
            RefreshEventSourceMappingsAsync(client, ct),
            RefreshEventBridgeAsync(eventBridgeClient, ct),
            RefreshVersionsAsync(client, ct)
            ).ContinueWith(_ => this.IsLoaded = true).ConfigureAwait(false);
    }

    private async Task RefreshFunctionAsync(AmazonLambdaClient client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetFunctionAsync(new GetFunctionRequest
            {
                FunctionName = this.Name
            }, ct).ConfigureAwait(false);

            var c = res.Configuration;

            this.Runtime = c.Runtime?.Value;
            this.Handler = c.Handler;
            this.Timeout = c.Timeout ?? 0;
            this.MemorySize = c.MemorySize ?? 0;

            this.RoleArn = c.Role;

            this.EnvironmentVariablesJson = c.Environment?.Variables.ToJsonDocument();
            this.VpcConfigJson = c.VpcConfig.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshPolicyAsync(AmazonLambdaClient client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.GetPolicyAsync(new GetPolicyRequest
            {
                FunctionName = this.Name
            }, ct).ConfigureAwait(false);

            this.ResourcePolicyJson = res.Policy.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshEventSourceMappingsAsync(AmazonLambdaClient client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest
            {
                FunctionName = this.Name
            }, ct).ConfigureAwait(false);

            this.EventSourceMappingsJson = res.EventSourceMappings.ToJsonDocument();
        }
        catch { }
    }

    private async Task RefreshEventBridgeAsync(AmazonEventBridgeClient client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var rules = await client.ListRulesAsync(new ListRulesRequest(), ct).ConfigureAwait(false);

            var matched = new List<object>();

            foreach (var rule in rules.Rules)
            {
                var targets = await client.ListTargetsByRuleAsync(new ListTargetsByRuleRequest
                {
                    Rule = rule.Name
                }, ct).ConfigureAwait(false);

                if (targets.Targets.Any(t => t.Arn == this.Arn))
                {
                    matched.Add(new
                    {
                        rule.Name,
                        rule.ScheduleExpression,
                        rule.EventPattern,
                        Targets = targets.Targets
                    });
                }
            }

            this.EventBridgeRulesJson = matched.ToJsonDocument();
        }
        catch { }
    }
    private async Task RefreshVersionsAsync(AmazonLambdaClient client, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var res = await client.ListVersionsByFunctionAsync(new ListVersionsByFunctionRequest
            {
                FunctionName = this.Name
            }, ct).ConfigureAwait(false);

            this.VersionsJson = res.Versions.ToJsonDocument();
        }
        catch { }
    }
}