using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System.Diagnostics;
using ZLinq;
using static AwsDoc4.Resources.AwsProfile;

namespace AwsDoc4.Resources;

public class AwsProfile
{
    protected AwsProfile(CredentialProfile p)
    {
        this.Profile = p;
    }

    public AWSCredentials? Credentials { get; private set; }
    public DateTimeOffset? Expiration { get; private set; }

    public bool IsExpired => this.Expiration != null && this.Expiration <= DateTimeOffset.UtcNow.AddMinutes(5);

    public override string ToString()
    {
        return $"{this.Name}: MFA: {this.NeedMfa}";
    }
    public CredentialProfile Profile { get; }
    public string Name => this.Profile.Name;
    public string AccessKey => this.Profile.Options.AccessKey;
    public string SecretKey => this.Profile.Options.SecretKey;
    public string MfaSerial => this.Profile.Options.MfaSerial;
    public string AssumeRoleArn => this.Profile.Options.RoleArn;
    public string AccountId => this.Profile.Options.AwsAccountId;
    public bool NeedMfa => !string.IsNullOrEmpty(this.MfaSerial);
    public bool NeedAssumeRole => !string.IsNullOrEmpty(this.AssumeRoleArn);

    private MFACodeProvider? _mfaProvider;
    public delegate Task<string> MFACodeProvider(AwsProfile profile, CancellationToken ct);

    private static SessionAWSCredentials CreateSessionCredentials(Credentials c)
    {
        return new SessionAWSCredentials(
            c.AccessKeyId,
            c.SecretAccessKey,
            c.SessionToken);
    }

    public Task RefreshAsync(CancellationToken ct = default)
    {
        return this.ConnectAsync(this._mfaProvider, ct);
    }
    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (this.Credentials == null || this.IsExpired)
        {
            await this.RefreshAsync(ct);
        }
    }

    public async Task ConnectAsync(
        MFACodeProvider? mfaProvider = null,
        CancellationToken ct = default)
    {
        var file = new SharedCredentialsFile();

        if (!file.TryGetProfile(this.Name, out var profile))
        {
            throw new InvalidOperationException(
                $"Profile [{this.Name}] not found.");
        }

        //
        // MFA不要
        //
        if (!this.NeedMfa)
        {
            if (string.IsNullOrEmpty(profile.Options.AccessKey) ||
                string.IsNullOrEmpty(profile.Options.SecretKey))
            {
                throw new InvalidOperationException(
                    $"Profile [{this.Name}] does not contain credentials.");
            }

            this.Credentials = new BasicAWSCredentials(
                profile.Options.AccessKey,
                profile.Options.SecretKey);

            this.Expiration = null;
            return;
        }

        if (mfaProvider == null)
        {
            throw new InvalidOperationException(
                $"Profile [{this.Name}] requires MFA.");
        }

        var code = await mfaProvider(this, ct);

        //
        // AssumeRole時は source_profile を使う
        //
        CredentialProfile sourceProfile;

        if (this.NeedAssumeRole)
        {
            if (string.IsNullOrEmpty(profile.Options.SourceProfile))
            {
                throw new InvalidOperationException(
                    $"Profile [{this.Name}] requires source_profile.");
            }

            if (!file.TryGetProfile(
                    profile.Options.SourceProfile,
                    out sourceProfile))
            {
                throw new InvalidOperationException(
                    $"Source profile [{profile.Options.SourceProfile}] not found.");
            }
        }
        else
        {
            sourceProfile = profile;
        }

        if (string.IsNullOrEmpty(sourceProfile.Options.AccessKey) ||
            string.IsNullOrEmpty(sourceProfile.Options.SecretKey))
        {
            throw new InvalidOperationException(
                $"Profile [{sourceProfile.Name}] does not contain credentials.");
        }

        var credentials = new BasicAWSCredentials(
            sourceProfile.Options.AccessKey,
            sourceProfile.Options.SecretKey);

        using var sts = new AmazonSecurityTokenServiceClient(credentials);

        Credentials responseCredentials;

        //
        // MFA + AssumeRole
        //
        if (this.NeedAssumeRole)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            var response = await sts.AssumeRoleAsync(
                new AssumeRoleRequest
                {
                    RoleArn = this.AssumeRoleArn,
                    RoleSessionName = $"session-{this.Name}",
                    SerialNumber = this.MfaSerial,
                    TokenCode = code,
                    DurationSeconds = 3600
                },
                ct);

            responseCredentials = response.Credentials;
        }
        //
        // MFAのみ
        //
        else
        {
            var response = await sts.GetSessionTokenAsync(
                new GetSessionTokenRequest
                {
                    SerialNumber = this.MfaSerial,
                    TokenCode = code,
                    DurationSeconds = 3600
                },
                ct);

            responseCredentials = response.Credentials;
        }

        this.Credentials = new SessionAWSCredentials(
            responseCredentials.AccessKeyId,
            responseCredentials.SecretAccessKey,
            responseCredentials.SessionToken);

        this.Expiration = responseCredentials.Expiration;
        this._mfaProvider = mfaProvider;
    }

    public static AwsProfile? GetProfileFromName(string profileName)
    {
        var file = new SharedCredentialsFile();
        return file.TryGetProfile(profileName, out var profile) ? new AwsProfile(profile) : null;
    }
    public static IEnumerable<AwsProfile> ListProfiles()
    {
        var file = new SharedCredentialsFile();
        foreach(var p in file.ListProfileNames().Select(x => file.TryGetProfile(x, out var p) ? p : null).Where(x => x != null))
        {
            yield return new AwsProfile(p!);
        }
    }

    public void SetMFAProvider(MFACodeProvider provider)
    {
        this._mfaProvider = provider;
    }
}
