using AwsDoc4.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Services;

public class AwsProfileManager : IAwsProfileManager
{
    public AwsProfile? CurrentProfile { get; set; }
}