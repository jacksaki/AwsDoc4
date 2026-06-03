using Amazon.EC2;
using Amazon.EC2.Model;
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZLinq;

namespace AwsDoc4.Resources;

[AwsResource("EC2")]
public class EC2Resource : AwsResourceBase, IAwsResource<EC2Resource>
{
    private static async Task<AmazonEC2Client> GetClientAsync(AwsProfile profile)
    {
        return await AwsClientFactory
            .CreateAsync<AmazonEC2Client>(profile)
            .ConfigureAwait(false);
    }
    public static bool HasCreateDate => false;

    public static bool HasLastModified => false;

    private EC2Resource(Instance instance)
        : base(
            GetInstanceName(instance) ?? instance.InstanceId,
            instance.InstanceId,
            null)
    {
        this.InstanceId = instance.InstanceId;
        this.InstanceType = instance.InstanceType?.Value;
        this.State = instance.State?.Name?.Value;
        this.PrivateIpAddress = instance.PrivateIpAddress;
        this.PublicIpAddress = instance.PublicIpAddress;
    }

    public static async IAsyncEnumerable<EC2Resource> EnumerateResourceAsync(
        AwsProfile profile,
        EnumerateResourceRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        string? nextToken = null;

        do
        {
            var response = await client.DescribeInstancesAsync(
                new DescribeInstancesRequest
                {
                    MaxResults = 100,
                    NextToken = nextToken
                },
                ct).ConfigureAwait(false);

            if (response.Reservations != null)
            {
                foreach (var reservation in response.Reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        var name =
                            GetInstanceName(instance)
                            ?? instance.InstanceId;

                        if (request.QueryString != null &&
                            !name.Contains(
                                request.QueryString,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        yield return new EC2Resource(instance);
                    }
                }
            }

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));
    }

    // --- 基本 ---

    [PropertyDescription(1, "基本", 6, "InstanceId", "インスタンスID")]
    public string? InstanceId { get; }

    [PropertyDescription(1, "基本", 7, "State", "状態")]
    public string? State { get; private set; }

    [PropertyDescription(1, "基本", 8, "InstanceType", "インスタンスタイプ")]
    public string? InstanceType { get; private set; }

    [PropertyDescription(1, "基本", 9, "ImageId", "AMI ID")]
    public string? ImageId { get; private set; }

    [PropertyDescription(1, "基本", 10, "PlatformDetails", "OS")]
    public string? PlatformDetails { get; private set; }

    [PropertyDescription(1, "基本", 11, "LaunchTime", "起動日時")]
    public DateTime? LaunchTime { get; private set; }

    // --- Network ---

    [PropertyDescription(2, "Network", 1, "PrivateIpAddress", "Private IP")]
    public string? PrivateIpAddress { get; private set; }

    [PropertyDescription(2, "Network", 2, "PublicIpAddress", "Public IP")]
    public string? PublicIpAddress { get; private set; }

    [PropertyDescription(2, "Network", 3, "SecurityGroups", "Security Groups")]
    public JsonDocument? SecurityGroupsJson { get; private set; }

    // --- IAM ---

    [PropertyDescription(3, "IAM", 1, "IamInstanceProfile", "IAM Instance Profile")]
    public string? IamInstanceProfileArn { get; private set; }

    // --- Storage ---

    [PropertyDescription(4, "Storage", 1, "BlockDevices", "EBS Volume")]
    public JsonDocument? BlockDevicesJson { get; private set; }

    // --- 運用 ---

    [PropertyDescription(5, "運用", 1, "Tags", "タグ")]
    public JsonDocument? TagsJson { get; private set; }

    public override async Task RefreshResourceAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var client = await GetClientAsync(profile).ConfigureAwait(false);

        await RefreshInstanceAsync(client, ct).ConfigureAwait(false);

        this.IsLoaded = true;
    }

    private async Task RefreshInstanceAsync(
        AmazonEC2Client client,
        CancellationToken ct)
    {
        await SafeExecuteAsync(async () =>
        {
            var res = await client.DescribeInstancesAsync(
                new DescribeInstancesRequest
                {
                    InstanceIds = new List<string>
                    {
                        this.InstanceId ?? throw new InvalidOperationException()
                    }
                },
                ct).ConfigureAwait(false);

            var instance = res.Reservations
                .SelectMany(x => x.Instances)
                .FirstOrDefault();

            if (instance == null)
            {
                return;
            }

            this.State =
                instance.State?.Name?.Value;

            this.InstanceType =
                instance.InstanceType?.Value;

            this.ImageId =
                instance.ImageId;

            this.PlatformDetails =
                instance.PlatformDetails;

            this.LaunchTime =
                instance.LaunchTime;

            this.PrivateIpAddress =
                instance.PrivateIpAddress;

            this.PublicIpAddress =
                instance.PublicIpAddress;

            this.SecurityGroupsJson =
                instance.SecurityGroups
                    .Select(x => new
                    {
                        x.GroupId,
                        x.GroupName
                    })
                    .ToJsonDocument();

            this.IamInstanceProfileArn =
                instance.IamInstanceProfile?.Arn;

            this.BlockDevicesJson =
                instance.BlockDeviceMappings
                    .Select(x => new
                    {
                        x.DeviceName,
                        x.Ebs?.VolumeId,
                        x.Ebs?.Status,
                        x.Ebs?.AttachTime,
                        x.Ebs?.DeleteOnTermination
                    })
                    .ToJsonDocument();

            this.TagsJson =
                instance.Tags
                    .Select(x => new
                    {
                        x.Key,
                        x.Value
                    })
                    .ToJsonDocument();
        });
    }

    private static string? GetInstanceName(Instance instance)
    {
        return instance.Tags?
            .FirstOrDefault(x => x.Key == "Name")
            ?.Value;
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
