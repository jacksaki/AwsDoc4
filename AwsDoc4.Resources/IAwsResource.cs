using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Resources;

internal interface IAwsResource<T>
     where T : AwsResourceBase
{
    public static abstract IAsyncEnumerable<T> EnumerateResourceAsync(AwsProfile profile, string? queryString, CancellationToken ct);
}
