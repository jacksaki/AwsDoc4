using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("SecretsManager")]
public class SecretsManagerResource : AwsResourceBase, IAwsResource<SecretsManagerResource>
{
    private static async Task<AmazonSecretsManagerClient> GetClientAsync(
        AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonSecretsManagerClient>(profile)
            .ConfigureAwait(false);
    }

    private SecretsManagerResource(SecretListEntry secret)
        : base(
            secret.Name,
            secret.ARN,
            secret.Description)
    {
        this.SecretId = secret.Name;
        this.PrimaryRegion = secret.PrimaryRegion;
        this.LastChangedDate = secret.LastChangedDate;
    }

    public static async IAsyncEnumerable<SecretsManagerResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.ListSecretsAsync(
                new ListSecretsRequest
                {
                    MaxResults = 100,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.SecretList != null)
            {
                foreach (var secret in response.SecretList)
                {
                    if (queryString != null &&
                        !secret.Name.Contains(
                            queryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new SecretsManagerResource(secret);
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "SecretId", "Secret Name")]
    public string? SecretId { get; }

    [PropertyDescription(1, "基本", 5, "PrimaryRegion", "Primary Region")]
    public string? PrimaryRegion { get; private set; }

    [PropertyDescription(1, "基本", 6, "LastChangedDate", "最終更新")]
    public DateTime? LastChangedDate { get; private set; }

    [PropertyDescription(1, "基本", 7, "LastAccessedDate", "最終アクセス")]
    public DateTime? LastAccessedDate { get; private set; }

    // --- Rotation ---

    [PropertyDescription(2, "Rotation", 1, "RotationEnabled", "Rotation有効")]
    public bool? RotationEnabled { get; private set; }

    [PropertyDescription(2, "Rotation", 2, "RotationLambdaArn", "Rotation Lambda")]
    public string? RotationLambdaArn { get; private set; }

    [PropertyDescription(2, "Rotation", 3, "RotationRules", "Rotation Rules")]
    public JsonDocument? RotationRulesJson { get; private set; }

    // --- Security ---

    [PropertyDescription(3, "Security", 1, "KmsKeyId", "KMS Key")]
    public string? KmsKeyId { get; private set; }

    [PropertyDescription(3, "Security", 2, "OwningService", "Owning Service")]
    public string? OwningService { get; private set; }

    // --- Version ---

    [PropertyDescription(4, "Version", 1, "VersionIdsToStages", "Version Stages")]
    public JsonDocument? VersionIdsToStagesJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(5, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await RefreshSecretAsync(client, ct).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshSecretAsync(
        AmazonSecretsManagerClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeSecretAsync(
                new DescribeSecretRequest
                {
                    SecretId = this.Name
                },
                ct).ConfigureAwait(false);

            this.PrimaryRegion =
                res.PrimaryRegion;

            this.LastChangedDate =
                res.LastChangedDate;

            this.LastAccessedDate =
                res.LastAccessedDate;

            this.RotationEnabled =
                res.RotationEnabled;

            this.RotationLambdaArn =
                res.RotationLambdaARN;

            this.RotationRulesJson =
                res.RotationRules.ToJsonDocument();

            this.KmsKeyId =
                res.KmsKeyId;

            this.OwningService =
                res.OwningService;

            this.VersionIdsToStagesJson =
                res.VersionIdsToStages
                    .Select(x => new
                    {
                        VersionId = x.Key,
                        Stages = x.Value
                    })
                    .ToJsonDocument();

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
