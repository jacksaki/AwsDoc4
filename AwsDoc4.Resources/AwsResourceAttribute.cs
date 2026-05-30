using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Resources;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AwsResourceAttribute(string type) : Attribute
{
    public string Type => type;
}