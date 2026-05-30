using Amazon.RDS;
using Amazon.RDS.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AwsDoc4.Resources;

[AwsResource("RDS")]
public class RdsResource : AwsResourceBase, IAwsResource<RdsResource>
{
    private static async Task<AmazonRDSClient> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonRDSClient>(profile)
            .ConfigureAwait(false);
    }

    private RdsResource(DBInstance db)
        : base(
            db.DBInstanceIdentifier,
            db.DBInstanceArn,
            db.Engine)
    {
        this.DBInstanceIdentifier = db.DBInstanceIdentifier;
        this.Engine = db.Engine;
        this.EngineVersion = db.EngineVersion;
        this.DBInstanceClass = db.DBInstanceClass;
        this.DBClusterIdentifier = db.DBClusterIdentifier;
    }

    public static async IAsyncEnumerable<RdsResource> EnumerateResourceAsync(
        AwsProfile profile,
        string? queryString,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? marker = null;

        do
        {
            var response = await client.DescribeDBInstancesAsync(
                new DescribeDBInstancesRequest
                {
                    Marker = marker,
                    MaxRecords = 100
                },
                ct).ConfigureAwait(false);

            if (response.DBInstances != null)
            {
                foreach (var db in response.DBInstances)
                {
                    if (queryString != null &&
                        !db.DBInstanceIdentifier.Contains(
                            queryString,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new RdsResource(db);
                }
            }

            marker = response.Marker;

        } while (!string.IsNullOrEmpty(marker));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 4, "DBInstanceIdentifier", "DB Instance")]
    public string? DBInstanceIdentifier { get; }

    [PropertyDescription(1, "基本", 5, "Engine", "Engine")]
    public string? Engine { get; private set; }

    [PropertyDescription(1, "基本", 6, "EngineVersion", "Engine Version")]
    public string? EngineVersion { get; private set; }

    [PropertyDescription(1, "基本", 7, "DBInstanceClass", "Instance Class")]
    public string? DBInstanceClass { get; private set; }

    [PropertyDescription(1, "基本", 8, "DBClusterIdentifier", "Cluster")]
    public string? DBClusterIdentifier { get; private set; }

    [PropertyDescription(1, "基本", 9, "Status", "状態")]
    public string? Status { get; private set; }

    // --- 接続 ---

    [PropertyDescription(2, "接続", 1, "Endpoint", "Endpoint")]
    public string? Endpoint { get; private set; }

    [PropertyDescription(2, "接続", 2, "Port", "Port")]
    public int? Port { get; private set; }

    [PropertyDescription(2, "接続", 3, "PubliclyAccessible", "Public Access")]
    public bool? PubliclyAccessible { get; private set; }

    // --- Security ---

    [PropertyDescription(3, "Security", 1, "VpcSecurityGroups", "Security Groups")]
    public JsonDocument? VpcSecurityGroupsJson { get; private set; }

    [PropertyDescription(3, "Security", 2, "IAMDatabaseAuthenticationEnabled", "IAM認証")]
    public bool? IAMDatabaseAuthenticationEnabled { get; private set; }

    [PropertyDescription(3, "Security", 3, "StorageEncrypted", "暗号化")]
    public bool? StorageEncrypted { get; private set; }

    [PropertyDescription(3, "Security", 4, "KmsKeyId", "KMS Key")]
    public string? KmsKeyId { get; private set; }

    // --- Parameter ---

    [PropertyDescription(4, "Parameter", 1, "DBParameterGroups", "DB Parameter Group")]
    public JsonDocument? DBParameterGroupsJson { get; private set; }

    [PropertyDescription(4, "Parameter", 2, "DBClusterParameterGroups", "Cluster Parameter Group")]
    public JsonDocument? DBClusterParameterGroupsJson { get; private set; }

    // --- Backup ---

    [PropertyDescription(5, "Backup", 1, "BackupRetentionPeriod", "Backup保持日数")]
    public int? BackupRetentionPeriod { get; private set; }

    [PropertyDescription(5, "Backup", 2, "PreferredBackupWindow", "Backup Window")]
    public string? PreferredBackupWindow { get; private set; }

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
            RefreshInstanceAsync(client, ct),
            RefreshClusterAsync(client, ct),
            RefreshTagsAsync(client, ct)
        ).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshInstanceAsync(
        AmazonRDSClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeDBInstancesAsync(
                new DescribeDBInstancesRequest
                {
                    DBInstanceIdentifier = this.Name
                },
                ct).ConfigureAwait(false);

            var db = res.DBInstances.FirstOrDefault();

            if (db == null)
            {
                return;
            }

            this.Status =
                db.DBInstanceStatus;

            this.Endpoint =
                db.Endpoint?.Address;

            this.Port =
                db.Endpoint?.Port;

            this.PubliclyAccessible =
                db.PubliclyAccessible;

            this.IAMDatabaseAuthenticationEnabled =
                db.IAMDatabaseAuthenticationEnabled;

            this.StorageEncrypted =
                db.StorageEncrypted;

            this.KmsKeyId =
                db.KmsKeyId;

            this.BackupRetentionPeriod =
                db.BackupRetentionPeriod;

            this.PreferredBackupWindow =
                db.PreferredBackupWindow;

            this.VpcSecurityGroupsJson =
                db.VpcSecurityGroups
                    .Select(x => new
                    {
                        x.VpcSecurityGroupId,
                        x.Status
                    })
                    .ToJsonDocument();

            this.DBParameterGroupsJson =
                db.DBParameterGroups
                    .Select(x => new
                    {
                        x.DBParameterGroupName,
                        x.ParameterApplyStatus
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshClusterAsync(
        AmazonRDSClient client,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(this.DBClusterIdentifier))
        {
            return;
        }

        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeDBClustersAsync(
                new DescribeDBClustersRequest
                {
                    DBClusterIdentifier = this.DBClusterIdentifier
                },
                ct).ConfigureAwait(false);

            var cluster = res.DBClusters.FirstOrDefault();

            if (cluster == null)
            {
                return;
            }

            this.DBClusterParameterGroupsJson =
                cluster.DBClusterMembers
                    .Select(x => new
                    {
                        x.DBInstanceIdentifier,
                        x.IsClusterWriter
                    })
                    .ToJsonDocument();
        });
    }

    private async Task RefreshTagsAsync(
        AmazonRDSClient client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.ListTagsForResourceAsync(
                new ListTagsForResourceRequest
                {
                    ResourceName = this.Arn
                },
                ct).ConfigureAwait(false);

            this.TagsJson =
                res.TagList
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
