using Amazon.AppSync;
using Amazon.AppSync.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("AppSync")]
public class AppSyncResource : AwsResourceBase, IAwsResource<AppSyncResource>
{
    private static async Task<AmazonAppSyncClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonAppSyncClient>(profile)
            .ConfigureAwait(false);
    }

    private AppSyncResource(GraphqlApi api)
        : base(api.Name, api.Arn, null)
    {
        this.ApiId = api.ApiId;
        this.AuthenticationType = api.AuthenticationType?.Value;
        this.UrisJson = api.Uris.ToJsonDocument();
    }

    public static async IAsyncEnumerable<AppSyncResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.ListGraphqlApisAsync(
                new ListGraphqlApisRequest
                {
                    MaxResults = 25,
                    NextToken = nextToken,
                },
                ct).ConfigureAwait(false);

            if (response.GraphqlApis != null)
            {
                foreach (var api in response.GraphqlApis)
                {
                    if (queryString != null &&
                        !api.Name.Contains(queryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new AppSyncResource(api);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "ApiId", "GraphQL API ID")]
    public string? ApiId { get; }

    [PropertyDescription(1, "基本", 5, "AuthenticationType", "認証方式")]
    public string? AuthenticationType { get; }

    [PropertyDescription(1, "基本", 6, "Uris", "GraphQL Endpoint")]
    public JsonDocument? UrisJson { get; }

    // --- セキュリティ ---

    [PropertyDescription(2, "セキュリティ", 1, "AdditionalAuthenticationProviders", "追加認証")]
    public JsonDocument? AdditionalAuthenticationProvidersJson { get; private set; }

    // --- データソース ---

    [PropertyDescription(3, "データソース", 1, "DataSources", "データソース一覧")]
    public JsonDocument? DataSourcesJson { get; private set; }

    // --- Resolver ---

    [PropertyDescription(4, "Resolver", 1, "Resolvers", "Resolver一覧")]
    public JsonDocument? ResolversJson { get; private set; }

    // --- スキーマ ---

    [PropertyDescription(5, "スキーマ", 1, "Schema", "GraphQLスキーマ")]
    public string? Schema { get; private set; }

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
            RefreshApiAsync(client, ct),
            RefreshDataSourcesAsync(client, ct),
            RefreshResolversAsync(client, ct),
            RefreshSchemaAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshApiAsync(
        AmazonAppSyncClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetGraphqlApiAsync(
                new GetGraphqlApiRequest
                {
                    ApiId = this.ApiId
                },
                ct).ConfigureAwait(false);

            this.AdditionalAuthenticationProvidersJson =
                res.GraphqlApi
                    .AdditionalAuthenticationProviders
                    .Select(x => new
                    {
                        AuthenticationType = x.AuthenticationType?.Value
                    })
                    .ToJsonDocument();

            this.TagsJson =
                res.GraphqlApi.Tags.ToJsonDocument();
        });
    }

    private async Task RefreshDataSourcesAsync(
        AmazonAppSyncClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListDataSourcesAsync(
                new ListDataSourcesRequest
                {
                    ApiId = this.ApiId
                },
                ct).ConfigureAwait(false);

            this.DataSourcesJson = res.DataSources
                .Select(x => new
                {
                    x.Name,
                    Type = x.Type?.Value,
                    x.ServiceRoleArn,
                    x.LambdaConfig,
                    x.DynamodbConfig,
                    x.OpenSearchServiceConfig,
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshResolversAsync(
        AmazonAppSyncClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var types = new[]
            {
                "Query",
                "Mutation",
                "Subscription"
            };

            var resolvers = new List<object>();

            foreach (var typeName in types)
            {
                var res = await client.ListResolversAsync(
                    new ListResolversRequest
                    {
                        ApiId = this.ApiId,
                        TypeName = typeName
                    },
                    ct).ConfigureAwait(false);

                resolvers.AddRange(
                    res.Resolvers.Select(x => new
                    {
                        TypeName = typeName,
                        x.FieldName,
                        x.DataSourceName,
                        x.Kind,
                    }));
            }

            this.ResolversJson = resolvers.ToJsonDocument();
        });
    }

    private async Task RefreshSchemaAsync(
        AmazonAppSyncClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetIntrospectionSchemaAsync(
                new GetIntrospectionSchemaRequest
                {
                    ApiId = this.ApiId,
                    Format = OutputType.JSON
                },
                ct).ConfigureAwait(false);

            using var reader = new StreamReader(res.Schema);

            this.Schema = await reader
                .ReadToEndAsync(ct)
                .ConfigureAwait(false);
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
