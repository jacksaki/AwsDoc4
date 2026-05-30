
using AwsDoc4.Resources;
using System.Runtime.CompilerServices;
namespace AwsDoc4.Services;

public static class AwsResourceSearcher
{
    public static async IAsyncEnumerable<AwsResourceBase> EnumerateResourceAsync(
        Type type,
        string? query,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var method = type.GetMethod(
            "EnumerateResourceAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException($"{type.Name} does not have EnumerateResourceAsync");
        }
        var profile = App.GetService<IAwsProfileManager>()!.CurrentProfile;
        var result = method.Invoke(null, new object?[] { profile, query, ct });

        if (result is IAsyncEnumerable<object> asyncEnumerable)
        {
            await foreach (var item in asyncEnumerable.WithCancellation(ct))
            {
                yield return (AwsResourceBase)item;
            }
        }
    }
}