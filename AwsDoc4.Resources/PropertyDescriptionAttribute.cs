using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Resources;

public class PropertyDescriptionGroup(int order, string name)
{
    public string Name => name;
    public int Order => order;
}
public class PropertyDescriptionAttribute(int groupOrder, string groupName,int order, string name, string description):Attribute
{
    public PropertyDescriptionGroup Group => new PropertyDescriptionGroup(groupOrder, groupName);
    public int Order => order;
    public string Name => name;
    public string Description => description;
}
