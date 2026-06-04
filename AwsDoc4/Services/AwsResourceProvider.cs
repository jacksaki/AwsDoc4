using AwsDoc4.Resources;
using AwsDoc4.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using ZLinq;

namespace AwsDoc4.Services;

internal class AwsResourceProvider
{
    private static Dictionary<string, ResourceTypeState>? _resourceTypes = null;
    public static Dictionary<string, ResourceTypeState> GetAll()
    {
        if (_resourceTypes != null)
        {
            return _resourceTypes;
        }
        _resourceTypes = new Dictionary<string, ResourceTypeState>();
        var rootDir = System.IO.Path.GetDirectoryName(typeof(AwsResourceBase).Assembly.Location) !;
        foreach(var path in System.IO.Directory.EnumerateFiles(rootDir,"*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(path);
                var types=asm.GetTypes().AsValueEnumerable().Where(x=>
                    x.IsSubclassOf(typeof(AwsResourceBase)) &&
                    x.GetCustomAttribute<AwsResourceAttribute>() != null
                    ).ToDictionary(
                        x => x.GetCustomAttribute<AwsResourceAttribute>()!.Type,
                        y => new ResourceTypeState(y));
                foreach(var t in types)
                {
                    _resourceTypes.Add(t.Key, t.Value);
                }
            }
            catch 
            {
            }
        }
        return _resourceTypes;
    }
}
