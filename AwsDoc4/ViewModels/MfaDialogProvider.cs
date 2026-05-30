using AwsDoc4.Resources;
using AwsDoc4.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwsDoc4.ViewModels;

public static class MfaDialogProvider
{
    public static Task<string> GetCodeAsync(
        AwsProfile profile,
        CancellationToken ct)
    {
        var window = new MFAWindow();
        var vm = (MFAWindowViewModel)window.DataContext;
        vm.Profile.Value = profile;
        return window.ShowDialog() == true
            ? Task.FromResult(vm.MFACode.Value)
            : throw new OperationCanceledException();
    }
}