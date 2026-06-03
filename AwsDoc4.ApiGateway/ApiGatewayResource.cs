using Amazon.ApiGatewayV2;
using Amazon.ApiGatewayV2.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("API Gateway")]
public class ApiGatewayResource : AwsResourceBase, IAwsResource<ApiGatewayResource>
{
    public static bool HasCreateDate => true;

    public static bool HasLastModified => false;

    private static async Task<AmazonApiGatewayV2Client> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonApiGatewayV2Client>(profile)
            .ConfigureAwait(false);
    }

    private ApiGatewayResource(Api api)
        // API Gateway v2 list API does not return ARN.
        // ApiId is stored in Arn property.
        : base(api.Name, api.ApiId, api.Description)
    {
        this.ProtocolType = api.ProtocolType;
        this.ApiEndpoint = api.ApiEndpoint;
        this.CreateDate = api.CreatedDate;
    }

    public static async IAsyncEnumerable<ApiGatewayResource> EnumerateResourceAsync(
        AwsProfile profile,
EnumerateResourceRequest request, [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.GetApisAsync(
                new GetApisRequest
                {
                    MaxResults = "50",
                    NextToken = nextToken,
                },
                ct).ConfigureAwait(false);

            if (response.Items != null)
            {
                foreach (var api in response.Items)
                {
                    if (request.QueryString != null &&
                        !api.Name.Contains(request.QueryString, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    // api.CreatedDate
                    yield return new ApiGatewayResource(api);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "ProtocolType", "HTTP / WEBSOCKET")]
    public string? ProtocolType { get; }

    [PropertyDescription(1, "基本", 7, "Endpoint", "API Endpoint")]
    public string? ApiEndpoint { get; }

    // --- ルーティング ---

    [PropertyDescription(2, "ルーティング", 1, "Routes", "ルート一覧")]
    public JsonDocument? RoutesJson { get; private set; }

    [PropertyDescription(2, "ルーティング", 2, "Integrations", "統合設定")]
    public JsonDocument? IntegrationsJson { get; private set; }

    // --- デプロイ ---

    [PropertyDescription(3, "デプロイ", 1, "Stages", "ステージ一覧")]
    public JsonDocument? StagesJson { get; private set; }

    // --- セキュリティ ---

    [PropertyDescription(4, "セキュリティ", 1, "Authorizers", "認証設定")]
    public JsonDocument? AuthorizersJson { get; private set; }

    // --- 設定 ---

    [PropertyDescription(5, "設定", 1, "CORS", "CORS設定")]
    public JsonDocument? CorsConfigurationJson { get; private set; }

    [PropertyDescription(5, "設定", 2, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await Task.WhenAll(
            RefreshApiAsync(client, ct),
            RefreshRoutesAsync(client, ct),
            RefreshIntegrationsAsync(client, ct),
            RefreshStagesAsync(client, ct),
            RefreshAuthorizersAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshApiAsync(
        AmazonApiGatewayV2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetApiAsync(
                new GetApiRequest
                {
                    ApiId = this.Arn
                },
                ct).ConfigureAwait(false);

            this.CorsConfigurationJson =
                res.CorsConfiguration.ToJsonDocument();

            this.TagsJson =
                res.Tags.ToJsonDocument();
        });
    }

    private async Task RefreshRoutesAsync(
        AmazonApiGatewayV2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetRoutesAsync(
                new GetRoutesRequest
                {
                    ApiId = this.Arn
                },
                ct).ConfigureAwait(false);

            this.RoutesJson = res.Items
                .Select(x => new
                {
                    x.RouteKey,
                    x.Target,
                    x.OperationName,
                    x.AuthorizationType,
                    x.AuthorizerId
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshIntegrationsAsync(
        AmazonApiGatewayV2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetIntegrationsAsync(
                new GetIntegrationsRequest
                {
                    ApiId = this.Arn
                },
                ct).ConfigureAwait(false);

            this.IntegrationsJson = res.Items
                .Select(x => new
                {
                    x.IntegrationType,
                    x.IntegrationMethod,
                    x.IntegrationUri,
                    x.PayloadFormatVersion,
                    x.TimeoutInMillis
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshStagesAsync(
        AmazonApiGatewayV2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetStagesAsync(
                new GetStagesRequest
                {
                    ApiId = this.Arn
                },
                ct).ConfigureAwait(false);

            this.StagesJson = res.Items
                .Select(x => new
                {
                    x.StageName,
                    x.AutoDeploy,
                    x.LastDeploymentStatusMessage,
                    x.CreatedDate,
                    x.LastUpdatedDate
                })
                .ToJsonDocument();
        });
    }

    private async Task RefreshAuthorizersAsync(
        AmazonApiGatewayV2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.GetAuthorizersAsync(
                new GetAuthorizersRequest
                {
                    ApiId = this.Arn
                },
                ct).ConfigureAwait(false);

            this.AuthorizersJson = res.Items
                .Select(x => new
                {
                    x.Name,
                    x.AuthorizerType,
                    x.AuthorizerUri,
                    x.IdentitySource
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