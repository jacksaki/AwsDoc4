using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("SQS")]
public class SqsResource : AwsResourceBase, IAwsResource<SqsResource>
{
    private static async Task<AmazonSQSClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonSQSClient>(profile)
            .ConfigureAwait(false);
    }

    private SqsResource(string queueUrl)
        : base(
            GetQueueName(queueUrl),
            queueUrl,
            null)
    {
        this.QueueUrl = queueUrl;
    }

    public static async IAsyncEnumerable<SqsResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        var response = await client.ListQueuesAsync(
            new ListQueuesRequest(),
            ct).ConfigureAwait(false);

        if (response.QueueUrls == null)
        {
            yield break;
        }

        foreach (var queueUrl in response.QueueUrls)
        {
            var queueName = GetQueueName(queueUrl);

            if (queryString != null &&
                !queueName.Contains(
                    queryString,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new SqsResource(queueUrl);
        }
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "QueueUrl", "Queue URL")]
    public string? QueueUrl { get; }

    [PropertyDescription(1, "基本", 5, "FifoQueue", "FIFO Queue")]
    public bool? FifoQueue { get; private set; }

    [PropertyDescription(1, "基本", 6, "CreatedTimestamp", "作成日時")]
    public DateTime? CreatedTimestamp { get; private set; }

    // --- メッセージ ---

    [PropertyDescription(2, "メッセージ", 1, "VisibilityTimeout", "Visibility Timeout")]
    public int? VisibilityTimeout { get; private set; }

    [PropertyDescription(2, "メッセージ", 2, "MessageRetentionPeriod", "保持期間")]
    public int? MessageRetentionPeriod { get; private set; }

    [PropertyDescription(2, "メッセージ", 3, "MaximumMessageSize", "最大メッセージサイズ")]
    public int? MaximumMessageSize { get; private set; }

    [PropertyDescription(2, "メッセージ", 4, "ReceiveMessageWaitTimeSeconds", "Long Polling")]
    public int? ReceiveMessageWaitTimeSeconds { get; private set; }

    // --- DLQ ---

    [PropertyDescription(3, "DLQ", 1, "RedrivePolicy", "Redrive Policy")]
    public JsonDocument? RedrivePolicyJson { get; private set; }

    [PropertyDescription(3, "DLQ", 2, "RedriveAllowPolicy", "Redrive Allow Policy")]
    public JsonDocument? RedriveAllowPolicyJson { get; private set; }

    // --- Security ---

    [PropertyDescription(4, "Security", 1, "KmsMasterKeyId", "KMS Key")]
    public string? KmsMasterKeyId { get; private set; }

    [PropertyDescription(4, "Security", 2, "SqsManagedSseEnabled", "SSE有効")]
    public bool? SqsManagedSseEnabled { get; private set; }

    [PropertyDescription(4, "Security", 3, "Policy", "Queue Policy")]
    public JsonDocument? PolicyJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(5, "運用", 1, "ApproximateNumberOfMessages", "メッセージ数")]
    public int? ApproximateNumberOfMessages { get; private set; }

    [PropertyDescription(5, "運用", 2, "ApproximateNumberOfMessagesNotVisible", "処理中メッセージ")]
    public int? ApproximateNumberOfMessagesNotVisible { get; private set; }

    [PropertyDescription(5, "運用", 3, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }
    [PropertyDescription(6, "Lambda", 1, "LambdaTriggers", "Lambda Trigger")]
    public JsonDocument? LambdaTriggersJson { get; private set; }
    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);
        var lambdaClient = await GetLambdaClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshAttributesAsync(client, ct),
            RefreshTagsAsync(client, ct),
            RefreshLambdaTriggersAsync(lambdaClient, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }
    private async Task RefreshAttributesAsync(
        AmazonSQSClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = this.QueueUrl,
                    AttributeNames = new List<string>
                    {
                        "All"
                    }
                },
                ct).ConfigureAwait(false);

            var attrs = res.Attributes;

            this.FifoQueue =
                GetBool(attrs, "FifoQueue");

            this.VisibilityTimeout =
                GetInt(attrs, "VisibilityTimeout");

            this.MessageRetentionPeriod =
                GetInt(attrs, "MessageRetentionPeriod");

            this.MaximumMessageSize =
                GetInt(attrs, "MaximumMessageSize");

            this.ReceiveMessageWaitTimeSeconds =
                GetInt(attrs, "ReceiveMessageWaitTimeSeconds");

            this.KmsMasterKeyId =
                GetString(attrs, "KmsMasterKeyId");

            this.SqsManagedSseEnabled =
                GetBool(attrs, "SqsManagedSseEnabled");

            this.ApproximateNumberOfMessages =
                GetInt(attrs, "ApproximateNumberOfMessages");

            this.ApproximateNumberOfMessagesNotVisible =
                GetInt(attrs, "ApproximateNumberOfMessagesNotVisible");

            this.PolicyJson =
                GetJson(attrs, "Policy");

            this.RedrivePolicyJson =
                GetJson(attrs, "RedrivePolicy");

            this.RedriveAllowPolicyJson =
                GetJson(attrs, "RedriveAllowPolicy");

            this.CreatedTimestamp =
                GetUnixTime(attrs, "CreatedTimestamp");
        });
    }

    private async Task RefreshTagsAsync(
        AmazonSQSClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListQueueTagsAsync(
                new ListQueueTagsRequest
                {
                    QueueUrl = this.QueueUrl
                },
                ct).ConfigureAwait(false);

            this.TagsJson =
                res.Tags
                    .Select(x => new
                    {
                        x.Key,
                        x.Value
                    })
                    .ToJsonDocument();
        });
    }

    private static string GetQueueName(string queueUrl)
    {
        return queueUrl.Split('/').Last();
    }

    private static string? GetString(
        Dictionary<string, string> attrs,
        string key)
    {
        return attrs.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static int? GetInt(
        Dictionary<string, string> attrs,
        string key)
    {
        return attrs.TryGetValue(key, out var value) &&
               int.TryParse(value, out var result)
            ? result
            : null;
    }

    private static bool? GetBool(
        Dictionary<string, string> attrs,
        string key)
    {
        return attrs.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var result)
            ? result
            : null;
    }

    private static DateTime? GetUnixTime(
        Dictionary<string, string> attrs,
        string key)
    {
        return attrs.TryGetValue(key, out var value) &&
               long.TryParse(value, out var unix)
            ? DateTimeOffset
                .FromUnixTimeSeconds(unix)
                .DateTime
            : null;
    }

    private static JsonDocument? GetJson(
        Dictionary<string, string> attrs,
        string key)
    {
        if (!attrs.TryGetValue(key, out var value))
        {
            return null;
        }

        try
        {
            return value.ToJsonDocument();
        }
        catch
        {
            return null;
        }
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
    private static async Task<AmazonLambdaClient> GetLambdaClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonLambdaClient>(profile)
            .ConfigureAwait(false);
    }
    private async Task RefreshLambdaTriggersAsync(
        AmazonLambdaClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListEventSourceMappingsAsync(
                new ListEventSourceMappingsRequest(),
                ct).ConfigureAwait(false);

            var matched = res.EventSourceMappings
                .Where(x => x.EventSourceArn == this.Arn)
                .Select(x => new
                {
                    x.FunctionArn,
                    x.State,
                    x.BatchSize,
                    x.MaximumBatchingWindowInSeconds,
                    x.UUID
                });

            this.LambdaTriggersJson =
                matched.ToJsonDocument();
        });
    }
}
