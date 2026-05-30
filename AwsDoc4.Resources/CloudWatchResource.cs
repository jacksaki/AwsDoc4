using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("CloudWatch")]
public class CloudWatchResource : AwsResourceBase, IAwsResource<CloudWatchResource>
{
    private static async Task<AmazonCloudWatchClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonCloudWatchClient>(profile)
            .ConfigureAwait(false);
    }

    private CloudWatchResource(MetricAlarm alarm)
        : base(
            alarm.AlarmName,
            alarm.AlarmArn,
            alarm.AlarmDescription)
    {
        this.StateValue = alarm.StateValue?.Value;
        this.Namespace = alarm.Namespace;
        this.MetricName = alarm.MetricName;
    }

    public static async IAsyncEnumerable<CloudWatchResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.DescribeAlarmsAsync(
                new DescribeAlarmsRequest
                {
                    MaxRecords = 50,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.MetricAlarms != null)
            {
                foreach (var alarm in response.MetricAlarms)
                {
                    if (queryString != null &&
                        !alarm.AlarmName.Contains(queryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new CloudWatchResource(alarm);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "State", "現在状態")]
    public string? StateValue { get; private set; }

    [PropertyDescription(1, "基本", 5, "Namespace", "メトリクスNamespace")]
    public string? Namespace { get; private set; }

    [PropertyDescription(1, "基本", 6, "MetricName", "メトリクス名")]
    public string? MetricName { get; private set; }

    // --- 条件 ---

    [PropertyDescription(2, "条件", 1, "ComparisonOperator", "比較条件")]
    public string? ComparisonOperator { get; private set; }

    [PropertyDescription(2, "条件", 2, "Threshold", "しきい値")]
    public double? Threshold { get; private set; }

    [PropertyDescription(2, "条件", 3, "EvaluationPeriods", "評価期間")]
    public int? EvaluationPeriods { get; private set; }

    [PropertyDescription(2, "条件", 4, "Statistic", "統計方式")]
    public string? Statistic { get; private set; }

    [PropertyDescription(2, "条件", 5, "Dimensions", "Dimension一覧")]
    public JsonDocument? DimensionsJson { get; private set; }

    // --- アクション ---

    [PropertyDescription(3, "アクション", 1, "AlarmActions", "ALARM時アクション")]
    public JsonDocument? AlarmActionsJson { get; private set; }

    [PropertyDescription(3, "アクション", 2, "OKActions", "OK時アクション")]
    public JsonDocument? OkActionsJson { get; private set; }

    [PropertyDescription(3, "アクション", 3, "InsufficientDataActions", "INSUFFICIENT_DATA時アクション")]
    public JsonDocument? InsufficientDataActionsJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(4, "運用", 1, "StateReason", "状態理由")]
    public string? StateReason { get; private set; }

    [PropertyDescription(4, "運用", 2, "StateUpdatedTimestamp", "状態更新日時")]
    public DateTime? StateUpdatedTimestamp { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await RefreshAlarmAsync(client, ct).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshAlarmAsync(
        AmazonCloudWatchClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeAlarmsAsync(
                new DescribeAlarmsRequest
                {
                    AlarmNames = new List<string>
                    {
                        this.Name
                    }
                },
                ct).ConfigureAwait(false);

            var alarm = res.MetricAlarms.FirstOrDefault();

            if (alarm == null)
            {
                return;
            }

            this.StateValue = alarm.StateValue?.Value;
            this.Namespace = alarm.Namespace;
            this.MetricName = alarm.MetricName;

            this.ComparisonOperator =
                alarm.ComparisonOperator?.Value;

            this.Threshold = alarm.Threshold;

            this.EvaluationPeriods =
                alarm.EvaluationPeriods;

            this.Statistic =
                alarm.Statistic?.Value;

            this.DimensionsJson = alarm.Dimensions
                .Select(x => new
                {
                    x.Name,
                    x.Value
                })
                .ToJsonDocument();

            this.AlarmActionsJson =
                alarm.AlarmActions.ToJsonDocument();

            this.OkActionsJson =
                alarm.OKActions.ToJsonDocument();

            this.InsufficientDataActionsJson =
                alarm.InsufficientDataActions.ToJsonDocument();

            this.StateReason =
                alarm.StateReason;

            this.StateUpdatedTimestamp =
                alarm.StateUpdatedTimestamp;
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
