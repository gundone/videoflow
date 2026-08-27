using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace UploadService.Services;

/// <summary>
/// Replaces the removed JsonWebKeySetRetriever from Microsoft.IdentityModel.Protocols.OpenIdConnect 8.x.
/// ConfigurationManager&lt;JsonWebKeySet&gt; needs IConfigurationRetriever&lt;JsonWebKeySet&gt; as second argument.
/// </summary>
public class JwksRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public async Task<JsonWebKeySet> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancel)
    {
        var document = await retriever.GetDocumentAsync(address, cancel);
        return new JsonWebKeySet(document);
    }
}