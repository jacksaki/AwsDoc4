using Amazon;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using AwsDoc4.Resources;
using AwsDoc4Cmd;
using ConsoleAppFramework;
using System.Reflection;
using System.Text.Json;
using ZLinq;
public class Program
{
    static Dictionary<string, Type> GetAllTypes()
    {
        var result = new Dictionary<string, Type>();
        var rootDir = System.IO.Path.GetDirectoryName(typeof(AwsResourceBase).Assembly.Location)!;
        foreach (var path in System.IO.Directory.EnumerateFiles(rootDir, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFile(path);
                var types = asm.GetTypes().AsValueEnumerable().Where(x =>
                    x.IsSubclassOf(typeof(AwsResourceBase)) &&
                    x.GetCustomAttribute<AwsResourceAttribute>() != null
                    );
                foreach(var t in types)
                {
                    result.Add(t.GetCustomAttribute<AwsResourceAttribute>()!.Type, t);
                }
            }
            catch
            {

            }
        }
        return result;
    }

    static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.Add<ResourceCommand>();
        app.Run(args);
    }
}
