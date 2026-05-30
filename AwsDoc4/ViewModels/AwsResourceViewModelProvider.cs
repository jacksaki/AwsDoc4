using AwsDoc4.Resources;
using ZLinq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace AwsDoc4.ViewModels;
public static class AwsResourceViewModelProvider
{
    private static readonly Dictionary<Type, ConstructorInfo> _ctors;
    static AwsResourceViewModelProvider()
    {
        _ctors = new Dictionary<Type, ConstructorInfo>();
        var vmType = typeof(AwsResourceViewModel);
        var assembly = vmType.Assembly;
        foreach (var type in assembly.GetTypes().AsValueEnumerable().Where(x=>!x.IsAbstract && vmType.IsAssignableFrom(x)))
        {
            var ctor = GetConstructor(type.GetConstructors());
            if (ctor != null)
            {
                _ctors.Add(type, ctor);
            }
        }
    }
    private static ConstructorInfo? GetConstructor(ConstructorInfo[] ctors)
    {
        return ctors.Where(x => x.GetParameters().Length == 1 && typeof(AwsResourceBase).IsAssignableFrom(x.GetParameters()[0].ParameterType)).FirstOrDefault();
    }
    public static AwsResourceViewModel? Create(AwsResourceBase resource)
    {
        if (_ctors.TryGetValue(resource.GetType(), out var ctor))
        {
            var instance = (AwsResourceViewModel)ctor.Invoke(null);
            instance.Resource.Value = resource;
            return instance;
        }
        return null;
    }
}
