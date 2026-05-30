using AwsDoc4.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Services;

public interface IMfaProvider
{
    Task<string> GetCodeAsync(AwsProfile profile, CancellationToken ct);
}