using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("EventBridge")]
public class EventBridgeResource : AwsResourceBase, IAwsResource<EventBridgeResource>
{
    private static async Task<AmazonEventBridgeClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonEventBridgeClient>(profile)
            .ConfigureAwait(false);
    }

    public static bool HasCreateDate => false;

    public static bool HasLastModified => false;
    private EventBridgeResource(Rule rule)
        : base(
            rule.Name,
            rule.Arn,
            rule.Description)
    {
        this.EventBusName = rule.EventBusName;
        this.ScheduleExpression = rule.ScheduleExpression;
        this.State = rule.State?.Value;
    }

    public static async IAsyncEnumerable<EventBridgeResource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.ListRulesAsync(
                new ListRulesRequest
                {
                    Limit = 50,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.Rules != null)
            {
                foreach (var rule in response.Rules)
                {
                    if (request.QueryString != null &&
                        !rule.Name.Contains(
                            request.QueryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new EventBridgeResource(rule);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "EventBusName", "Event Bus")]
    public string? EventBusName { get; private set; }

    [PropertyDescription(1, "基本", 7, "State", "状態")]
    public string? State { get; private set; }

    [PropertyDescription(1, "基本", 8, "ScheduleExpression", "スケジュール")]
    public string? ScheduleExpression { get; private set; }

    // --- イベント ---

    [PropertyDescription(2, "イベント", 1, "EventPattern", "イベントパターン")]
    public JsonDocument? EventPatternJson { get; private set; }

    // --- Targets ---

    [PropertyDescription(3, "Targets", 1, "Targets", "ターゲット一覧")]
    public JsonDocument? TargetsJson { get; private set; }

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
            RefreshRuleAsync(client, ct),
            RefreshTargetsAsync(client, ct),
            RefreshTagsAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshRuleAsync(
        AmazonEventBridgeClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeRuleAsync(
                new DescribeRuleRequest
                {
                    Name = this.Name,
                    EventBusName = this.EventBusName
                },
                ct).ConfigureAwait(false);

            this.State =
                res.State?.Value;

            this.ScheduleExpression =
                res.ScheduleExpression;

            this.EventPatternJson =
                res.EventPattern.ToJsonDocument();
        });
    }

    private async Task RefreshTargetsAsync(
        AmazonEventBridgeClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListTargetsByRuleAsync(
                new ListTargetsByRuleRequest
                {
                    Rule = this.Name,
                    EventBusName = this.EventBusName
                },
                ct).ConfigureAwait(false);

            this.TargetsJson = res.Targets
                .Select(x => new
                {
                    x.Id,
                    x.Arn,
                    x.RoleArn,
                    x.Input,
                    x.InputPath,
                    x.DeadLetterConfig,
                    x.RetryPolicy
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshTagsAsync(
        AmazonEventBridgeClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListTagsForResourceAsync(
                new ListTagsForResourceRequest
                {
                    ResourceARN = this.Arn
                },
                ct).ConfigureAwait(false);

            this.TagsJson = res.Tags
                .Select(x => new
                {
                    x.Key,
                    x.Value
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
