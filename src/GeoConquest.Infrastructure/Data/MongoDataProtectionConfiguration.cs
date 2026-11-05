using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace GeoConquest.Infrastructure.Data;

/// <summary>Points the framework's key ring at whichever <see cref="IXmlRepository"/> is registered (MongoDB in production).</summary>
internal sealed class MongoDataProtectionConfiguration(IXmlRepository keyRepository) : IConfigureOptions<KeyManagementOptions>
{
    public void Configure(KeyManagementOptions options) => options.XmlRepository = keyRepository;
}
