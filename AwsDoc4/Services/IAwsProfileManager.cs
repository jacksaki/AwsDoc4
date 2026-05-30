using AwsDoc4.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Services;

public interface IAwsProfileManager
{
    AwsProfile? CurrentProfile { get; set; }
}