using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Resources;

public class EnumerateResourceRequest
{
    public DateTime? CreateDateStart { get; set; }
    public DateTime? CreateDateEnd { get; set; }
    public DateTime? LastModifiedStart { get; set; }
    public DateTime? LastModifiedEnd { get; set; }
    public string? QueryString { get; set; }
}

public interface IAwsResource<T>
     where T : AwsResourceBase
{
    public static abstract bool HasCreateDate { get; }
    public static abstract bool HasLastModified { get; }
    public static abstract IAsyncEnumerable<T> EnumerateResourceAsync(AwsProfile profile, EnumerateResourceRequest request, CancellationToken ct);
}
