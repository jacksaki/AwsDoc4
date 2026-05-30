using Amazon.Runtime;
using System.Reflection;

namespace AwsDoc4.Resources;

public abstract class AwsResourceBase
{
    [PropertyDescription(1, "基本", 1, "ARN", "ARN")]
    public string Arn { get; }
    [PropertyDescription(1, "基本", 2, "名前", "名前")]
    public string Name { get; }
    [PropertyDescription(1, "基本", 3, "説明", "説明")]
    public string? Description { get; }
    public string Type { get; }
    public bool IsLoaded { get; set; }
    private void RefreshCore()
    {
    }
    public async Task RefreshAsync(AwsProfile profile, CancellationToken ct)
    {
        RefreshCore();
        await RefreshResourceAsync(profile, ct);
    }

    public abstract Task RefreshResourceAsync(AwsProfile profile, CancellationToken ct);
    protected AwsResourceBase(string name, string arn, string? description)
    {
        this.Name = name;
        this.Type = GetAwsResourceType();
        this.Arn = arn;
        this.Description = description;
    }
    protected string GetAwsResourceType()
    {
        return this.GetType().GetCustomAttribute<AwsResourceAttribute>()!.Type;
    }
}
