using Amazon.SimpleSystemsManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Models;

public sealed record CommandResult(CommandInvocationStatus Status, string StdOut, string StdErr);