using Amazon;
using Amazon.Runtime;

namespace AwsDoc4.Resources;

public static class AwsClientFactory
{
    public static async Task<T> CreateAsync<T>(AwsProfile profile) where T : AmazonServiceClient
    {
        await profile.EnsureConnectedAsync();
        var ctor = typeof(T).GetConstructor(new Type[] { typeof(AWSCredentials) });
        return (T)ctor!.Invoke(new object[] { profile.Credentials! });
    }
}