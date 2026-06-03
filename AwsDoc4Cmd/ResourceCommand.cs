using Amazon;
using AwsDoc4.Resources;
using ConsoleAppFramework;
using Json.Path;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AwsDoc4Cmd;

public class ResourceCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="profile">-p, profile</param>
    /// <param name="jsonPath">-j, json path</param>
    /// <returns></returns>
    [Command("s3")]
    public async Task ListS3Async(string profile, string? jsonPath = null)
    {
        var p = await GetProfileFromNameAsync(profile);
        var enumerator = S3Resource.EnumerateResourceAsync(p, new EnumerateResourceRequest(), CancellationToken.None);
        await ListAsync<S3Resource>(enumerator, profile, jsonPath);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="profile">-p, profile</param>
    /// <param name="jsonPath">-j, json path</param>
    /// <returns></returns>
    [Command("lambda")]
    public async Task ListLambdaAsync(string profile, string? jsonPath = null)
    {
        var p = await GetProfileFromNameAsync(profile);
        var enumerator = LambdaResource.EnumerateResourceAsync(p, new EnumerateResourceRequest(), CancellationToken.None);
        await ListAsync<LambdaResource>(enumerator, profile, jsonPath);
    }

    private async Task ListAsync<T>(IAsyncEnumerable<T>enumerator, string profile,string? jsonPath)
        where T: AwsResourceBase
    {
        await foreach (var resource in enumerator)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                var json = JsonSerializer.Serialize(resource, new JsonSerializerOptions() { WriteIndented = true });
                Console.WriteLine(json);
            }
            else
            {
                var path = JsonPath.Parse(jsonPath);
                var jsonText = JsonSerializer.Serialize(resource);
                var json = JsonNode.Parse(jsonText)!;
                var results = path.Evaluate(json);
                foreach (var match in results.Matches)
                {
                    Console.WriteLine(match.Value);
                }
            }
        }
    }

    private static async Task<AwsProfile> GetProfileFromNameAsync(string name)
    {
        var p = AwsResourceBase.GetProfileFromName(name);
        if (p.NeedMfa)
        {
            Console.Write("Enter MFA code: ");
            string? tokenCode = null;
            while (string.IsNullOrEmpty(tokenCode))
            {
                tokenCode = Console.ReadLine();
            }
            await p.ConnectAsync((_, _) => Task.FromResult(tokenCode));
        }
        else
        {
            await p.ConnectAsync(null);
        }

        return p;
    }
}
