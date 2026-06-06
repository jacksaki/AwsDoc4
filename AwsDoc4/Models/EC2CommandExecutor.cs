using Amazon.EC2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AwsDoc4.Resources;
using AwsDoc4.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.Models;

public class EC2CommandExecutor
{
    public EC2CommandExecutor()
    {

    }

    public async Task<CommandResult> ExecuteAsync(EC2Resource ec2, string command, CancellationToken ct = default)
    {
        var profile = App.GetService<IAwsProfileManager>()!.CurrentProfile!;
        using var ssm = new AmazonSimpleSystemsManagementClient(profile.Credentials);

        var response = await ssm.SendCommandAsync(
            new SendCommandRequest
            {
                InstanceIds = [ec2.InstanceId!],
                DocumentName = "AWS-RunShellScript",
                Parameters = new()
                {
                    ["commands"] = [command]
                }
            },
            ct);

        var commandId = response.Command.CommandId;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            GetCommandInvocationResponse result;
            try
            {
                result = await ssm.GetCommandInvocationAsync(
                    new GetCommandInvocationRequest
                    {
                        CommandId = commandId,
                        InstanceId = ec2.InstanceId
                    },
                    ct);
            }
            catch (InvocationDoesNotExistException)
            {
                await Task.Delay(500, ct);
                continue;
            }

            if (result.Status.Equals(CommandInvocationStatus.Success))
            {
                return new CommandResult(result.Status, result.StandardOutputContent, result.StandardErrorContent);
            }
            else if (result.Status.Equals(CommandInvocationStatus.Pending) ||
                result.Status.Equals(CommandInvocationStatus.InProgress) ||
                result.Status.Equals(CommandInvocationStatus.Delayed))
            {
                await Task.Delay(1000, ct);
            }
            else
            {
                return new CommandResult(result.Status, result.StandardOutputContent, result.StandardErrorContent);
            }
        }
    }
}
