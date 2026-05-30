using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("StepFunctions")]
public class StepFunctionsResource : AwsResourceBase, IAwsResource<StepFunctionsResource>
{
    private static async Task<AmazonStepFunctionsClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonStepFunctionsClient>(profile)
            .ConfigureAwait(false);
    }

    private StepFunctionsResource(StateMachineListItem stateMachine)
        : base(
            stateMachine.Name,
            stateMachine.StateMachineArn,
            null)
    {
        this.StateMachineType = stateMachine.Type?.Value;
        this.CreationDate = stateMachine.CreationDate;
    }

    public static async IAsyncEnumerable<StepFunctionsResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.ListStateMachinesAsync(
                new ListStateMachinesRequest
                {
                    MaxResults = 100,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.StateMachines != null)
            {
                foreach (var stateMachine in response.StateMachines)
                {
                    if (queryString != null &&
                        !stateMachine.Name.Contains(
                            queryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new StepFunctionsResource(stateMachine);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "StateMachineType", "Type")]
    public string? StateMachineType { get; private set; }

    [PropertyDescription(1, "基本", 5, "Status", "状態")]
    public string? Status { get; private set; }

    [PropertyDescription(1, "基本", 6, "CreationDate", "作成日時")]
    public DateTime? CreationDate { get; private set; }

    // --- Workflow ---

    [PropertyDescription(2, "Workflow", 1, "Definition", "StateMachine定義")]
    public JsonDocument? DefinitionJson { get; private set; }

    [PropertyDescription(2, "Workflow", 2, "RoleArn", "IAM Role")]
    public string? RoleArn { get; private set; }

    // --- Logging ---

    [PropertyDescription(3, "Logging", 1, "LoggingConfiguration", "Logging")]
    public JsonDocument? LoggingConfigurationJson { get; private set; }

    [PropertyDescription(3, "Logging", 2, "TracingConfiguration", "X-Ray Tracing")]
    public JsonDocument? TracingConfigurationJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(4, "運用", 1, "Executions", "最近の実行")]
    public JsonDocument? ExecutionsJson { get; private set; }

    [PropertyDescription(4, "運用", 2, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshStateMachineAsync(client, ct),
            RefreshExecutionsAsync(client, ct),
            RefreshTagsAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshStateMachineAsync(
        AmazonStepFunctionsClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeStateMachineAsync(
                new DescribeStateMachineRequest
                {
                    StateMachineArn = this.Arn
                },
                ct).ConfigureAwait(false);

            this.StateMachineType =
                res.Type?.Value;

            this.Status =
                res.Status?.Value;

            this.RoleArn =
                res.RoleArn;

            this.LoggingConfigurationJson =
                res.LoggingConfiguration.ToJsonDocument();

            this.TracingConfigurationJson =
                res.TracingConfiguration.ToJsonDocument();

            try
            {
                this.DefinitionJson =
                    res.Definition.ToJsonDocument();
            }
            catch
            {
                this.DefinitionJson = null;
            }
        });
    }

    private async Task RefreshExecutionsAsync(
        AmazonStepFunctionsClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListExecutionsAsync(
                new ListExecutionsRequest
                {
                    StateMachineArn = this.Arn,
                    MaxResults = 20
                },
                ct).ConfigureAwait(false);

            this.ExecutionsJson =
                res.Executions
                    .Select(x => new
                    {
                        x.Name,
                        Status = x.Status?.Value,
                        x.StartDate,
                        x.StopDate,
                        x.ExecutionArn
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshTagsAsync(
        AmazonStepFunctionsClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListTagsForResourceAsync(
                new ListTagsForResourceRequest
                {
                    ResourceArn = this.Arn
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
