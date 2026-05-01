using OneDriveLocalOpener;
using Xunit;

namespace OneDriveLocalOpener.Tests;

public class OneDriveMapperTests
{
    private static OneDriveMapper MapperWith(params (string Ns, string Mp)[] entries) =>
        new(new StubRegistry(entries));

    [Fact]
    public void Returns_null_when_no_providers()
    {
        var mapper = MapperWith();
        Assert.Null(mapper.TryResolveToLocalPath("https://contoso.sharepoint.com/sites/HR/Shared Documents/file.msg"));
    }

    [Fact]
    public void Returns_null_for_non_matching_url()
    {
        var mapper = MapperWith(("https://contoso.sharepoint.com/sites/HR/Shared Documents", @"C:\Users\alice\OneDrive"));
        Assert.Null(mapper.TryResolveToLocalPath("https://other.sharepoint.com/sites/HR/Shared Documents/file.msg"));
    }

    [Fact]
    public void Resolves_simple_url_to_local_path()
    {
        var mapper = MapperWith(("https://contoso-my.sharepoint.com/personal/alice_contoso_com/Documents", @"C:\Users\alice\OneDrive - Contoso"));
        var result = mapper.TryResolveToLocalPath("https://contoso-my.sharepoint.com/personal/alice_contoso_com/Documents/Emails/quarterly.msg");
        Assert.Equal(@"C:\Users\alice\OneDrive - Contoso\Emails\quarterly.msg", result);
    }

    [Fact]
    public void Decodes_percent_encoded_characters()
    {
        var mapper = MapperWith(("https://contoso.sharepoint.com/sites/HR/Shared%20Documents", @"C:\Users\alice\HR"));
        var result = mapper.TryResolveToLocalPath("https://contoso.sharepoint.com/sites/HR/Shared%20Documents/My%20File.msg");
        Assert.Equal(@"C:\Users\alice\HR\My File.msg", result);
    }

    [Fact]
    public void Picks_longest_matching_namespace()
    {
        var mapper = MapperWith(
            ("https://contoso.sharepoint.com/sites/HR", @"C:\Sync\HR"),
            ("https://contoso.sharepoint.com/sites/HR/Shared Documents", @"C:\Sync\HRDocs")
        );
        var result = mapper.TryResolveToLocalPath("https://contoso.sharepoint.com/sites/HR/Shared Documents/file.msg");
        Assert.Equal(@"C:\Sync\HRDocs\file.msg", result);
    }

    [Fact]
    public void Strips_query_string_before_matching()
    {
        var mapper = MapperWith(("https://contoso-my.sharepoint.com/personal/alice/Documents", @"C:\OneDrive"));
        var result = mapper.TryResolveToLocalPath("https://contoso-my.sharepoint.com/personal/alice/Documents/file.msg?web=1&e=xyz");
        Assert.Equal(@"C:\OneDrive\file.msg", result);
    }

    [Fact]
    public void Returns_null_for_invalid_uri()
    {
        var mapper = MapperWith(("https://contoso.sharepoint.com/sites/HR", @"C:\Sync\HR"));
        Assert.Null(mapper.TryResolveToLocalPath("not a url"));
    }

    [Fact]
    public void Matching_is_case_insensitive()
    {
        var mapper = MapperWith(("https://CONTOSO.sharepoint.com/sites/HR", @"C:\Sync\HR"));
        var result = mapper.TryResolveToLocalPath("https://contoso.sharepoint.com/sites/HR/file.msg");
        Assert.NotNull(result);
    }
}

file sealed class StubRegistry : IRegistryProvider
{
    private readonly (string Ns, string Mp)[] _entries;
    public StubRegistry(params (string Ns, string Mp)[] entries) => _entries = entries;
    public IEnumerable<(string, string)> GetProviderMappings() => _entries.Select(e => (e.Ns, e.Mp));
}
