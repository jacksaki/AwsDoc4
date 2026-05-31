using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("DynamoDB")]
public class DynamoDbResource : AwsResourceBase, IAwsResource<DynamoDbResource>
{
    private static async Task<AmazonDynamoDBClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonDynamoDBClient>(profile)
            .ConfigureAwait(false);
    }
    public static bool HasCreateDate => true;
    public static bool HasLastModified => false;
    private DynamoDbResource(string tableName)
        : base(tableName, tableName, null)
    {

    }

    public static async IAsyncEnumerable<DynamoDbResource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? lastEvaluatedTableName = null;

        do
        {
            var response = await client.ListTablesAsync(
                new ListTablesRequest
                {
                    Limit = 100,
                    ExclusiveStartTableName = lastEvaluatedTableName
                },
                ct).ConfigureAwait(false);

            if (response.TableNames != null)
            {
                foreach (var tableName in response.TableNames)
                {
                    if (request.QueryString != null &&
                        !tableName.Contains(
                            request.QueryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new DynamoDbResource(tableName);
                }
            }

            lastEvaluatedTableName =
                response.LastEvaluatedTableName;

        } while (!string.IsNullOrEmpty(lastEvaluatedTableName));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "TableStatus", "テーブル状態")]
    public string? TableStatus { get; private set; }

    [PropertyDescription(1, "基本", 7, "BillingMode", "課金モード")]
    public string? BillingMode { get; private set; }

    [PropertyDescription(1, "基本", 8, "TableSizeBytes", "サイズ(Bytes)")]
    public long? TableSizeBytes { get; private set; }

    [PropertyDescription(1, "基本", 9, "ItemCount", "アイテム数")]
    public long? ItemCount { get; private set; }

    // --- キー ---

    [PropertyDescription(2, "キー", 1, "KeySchema", "キー定義")]
    public JsonDocument? KeySchemaJson { get; private set; }

    [PropertyDescription(2, "キー", 2, "AttributeDefinitions", "属性定義")]
    public JsonDocument? AttributeDefinitionsJson { get; private set; }

    // --- Index ---

    [PropertyDescription(3, "Index", 1, "GlobalSecondaryIndexes", "GSI")]
    public JsonDocument? GlobalSecondaryIndexesJson { get; private set; }

    [PropertyDescription(3, "Index", 2, "LocalSecondaryIndexes", "LSI")]
    public JsonDocument? LocalSecondaryIndexesJson { get; private set; }

    // --- Streams / Backup ---

    [PropertyDescription(4, "Streams", 1, "StreamSpecification", "Streams設定")]
    public JsonDocument? StreamSpecificationJson { get; private set; }

    [PropertyDescription(4, "Streams", 2, "LatestStreamArn", "Stream ARN")]
    public string? LatestStreamArn { get; private set; }

    [PropertyDescription(4, "Streams", 3, "PointInTimeRecovery", "PITR")]
    public string? PointInTimeRecoveryStatus { get; private set; }

    // --- セキュリティ ---

    [PropertyDescription(5, "セキュリティ", 1, "SSE", "暗号化")]
    public JsonDocument? SseSpecificationJson { get; private set; }

    // --- TTL ---

    [PropertyDescription(6, "TTL", 1, "TimeToLive", "TTL設定")]
    public JsonDocument? TimeToLiveJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(7, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshTableAsync(client, ct),
            RefreshTtlAsync(client, ct),
            RefreshTagsAsync(client, ct),
            RefreshPointInTimeRecoveryAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshTableAsync(
        AmazonDynamoDBClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeTableAsync(
                new DescribeTableRequest
                {
                    TableName = this.Name
                },
                ct).ConfigureAwait(false);

            var table = res.Table;

            this.TableStatus =
                table.TableStatus?.Value;

            this.TableSizeBytes =
                table.TableSizeBytes;

            this.ItemCount =
                table.ItemCount;

            this.CreateDate =
                table.CreationDateTime;

            this.BillingMode =
                table.BillingModeSummary?.BillingMode?.Value;

            this.KeySchemaJson =
                table.KeySchema
                    .Select(x => new
                    {
                        x.AttributeName,
                        x.KeyType
                    })
                    .ToJsonDocument();

            this.AttributeDefinitionsJson =
                table.AttributeDefinitions
                    .Select(x => new
                    {
                        x.AttributeName,
                        x.AttributeType
                    })
                    .ToJsonDocument();

            this.GlobalSecondaryIndexesJson =
                table.GlobalSecondaryIndexes?
                    .Select(x => new
                    {
                        x.IndexName,
                        x.IndexStatus,
                        x.KeySchema,
                        x.Projection
                    })
                    .ToJsonDocument();

            this.LocalSecondaryIndexesJson =
                table.LocalSecondaryIndexes?
                    .Select(x => new
                    {
                        x.IndexName,
                        x.KeySchema,
                        x.Projection
                    })
                    .ToJsonDocument();

            this.StreamSpecificationJson =
                table.StreamSpecification.ToJsonDocument();

            this.LatestStreamArn =
                table.LatestStreamArn;

            this.SseSpecificationJson =
                table.SSEDescription.ToJsonDocument();
        });
    }

    private async Task RefreshTtlAsync(
        AmazonDynamoDBClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeTimeToLiveAsync(
                new DescribeTimeToLiveRequest
                {
                    TableName = this.Name
                },
                ct).ConfigureAwait(false);

            this.TimeToLiveJson =
                res.TimeToLiveDescription.ToJsonDocument();
        });
    }

    private async Task RefreshTagsAsync(
        AmazonDynamoDBClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var table = await client.DescribeTableAsync(
                new DescribeTableRequest
                {
                    TableName = this.Name
                },
                ct).ConfigureAwait(false);

            var res = await client.ListTagsOfResourceAsync(
                new ListTagsOfResourceRequest
                {
                    ResourceArn = table.Table.TableArn
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

    private async Task RefreshPointInTimeRecoveryAsync(
        AmazonDynamoDBClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeContinuousBackupsAsync(
                new DescribeContinuousBackupsRequest
                {
                    TableName = this.Name
                },
                ct).ConfigureAwait(false);

            this.PointInTimeRecoveryStatus =
                res.ContinuousBackupsDescription?
                    .PointInTimeRecoveryDescription?
                    .PointInTimeRecoveryStatus?
                    .Value;
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
