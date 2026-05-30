using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("SSM")]
public class SsmResource : AwsResourceBase, IAwsResource<SsmResource>
{
    private static async Task<AmazonSimpleSystemsManagementClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonSimpleSystemsManagementClient>(profile)
            .ConfigureAwait(false);
    }

    private SsmResource(ParameterMetadata parameter)
        : base(
            parameter.Name,
            parameter.ARN ?? parameter.Name,
            parameter.Description)
    {
        this.ParameterType = parameter.Type?.Value;
        this.Version = parameter.Version;
        this.LastModifiedDate = parameter.LastModifiedDate;
    }

    public static async IAsyncEnumerable<SsmResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.DescribeParametersAsync(
                new DescribeParametersRequest
                {
                    MaxResults = 50,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.Parameters != null)
            {
                foreach (var parameter in response.Parameters)
                {
                    if (queryString != null &&
                        !parameter.Name.Contains(
                            queryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new SsmResource(parameter);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "ParameterType", "Parameter Type")]
    public string? ParameterType { get; private set; }

    [PropertyDescription(1, "基本", 5, "Tier", "Tier")]
    public string? Tier { get; private set; }

    [PropertyDescription(1, "基本", 6, "Version", "Version")]
    public long? Version { get; private set; }

    [PropertyDescription(1, "基本", 7, "LastModifiedDate", "最終更新")]
    public DateTime? LastModifiedDate { get; private set; }

    // --- Security ---

    [PropertyDescription(2, "Security", 1, "KeyId", "KMS Key")]
    public string? KeyId { get; private set; }

    [PropertyDescription(2, "Security", 2, "Policies", "Policies")]
    public JsonDocument? PoliciesJson { get; private set; }

    // --- Parameter ---

    [PropertyDescription(3, "Parameter", 1, "DataType", "Data Type")]
    public string? DataType { get; private set; }

    [PropertyDescription(3, "Parameter", 2, "AllowedPattern", "Allowed Pattern")]
    public string? AllowedPattern { get; private set; }

    // --- 運用 ---

    [PropertyDescription(4, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await RefreshParameterAsync(client, ct).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshParameterAsync(
        AmazonSimpleSystemsManagementClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetParameterHistoryAsync(
                new GetParameterHistoryRequest
                {
                    Name = this.Name,
                    MaxResults = 1,
                    WithDecryption = false
                },
                ct).ConfigureAwait(false);

            var parameter = res.Parameters.FirstOrDefault();

            if (parameter == null)
            {
                return;
            }

            this.ParameterType =
                parameter.Type?.Value;

            this.Tier =
                parameter.Tier?.Value;

            this.Version =
                parameter.Version;

            this.LastModifiedDate =
                parameter.LastModifiedDate;

            this.KeyId =
                parameter.KeyId;

            this.DataType =
                parameter.DataType;

            this.AllowedPattern =
                parameter.AllowedPattern;

            this.PoliciesJson =
                parameter.Policies?.ToJsonDocument();

            if (!string.IsNullOrEmpty(this.Arn))
            {
                var tags = await client.ListTagsForResourceAsync(
                    new ListTagsForResourceRequest
                    {
                        ResourceType = ResourceTypeForTagging.Parameter,
                        ResourceId = this.Name
                    },
                    ct).ConfigureAwait(false);

                this.TagsJson =
                    tags.TagList
                        .Select(x => new
                        {
                            x.Key,
                            x.Value
                        })
                        .ToJsonDocument();
            }
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
