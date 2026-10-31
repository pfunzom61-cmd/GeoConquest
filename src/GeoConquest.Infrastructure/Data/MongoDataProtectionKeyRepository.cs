using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GeoConquest.Infrastructure.Data;

/// <summary>
/// Keeps ASP.NET Core's data-protection keys in MongoDB. These keys sign the login cookie and the anti-forgery
/// tokens; a container's own disk is wiped on every redeploy, which would sign everyone out and invalidate open forms.
/// </summary>
/// <remarks>
/// <see cref="IXmlRepository"/> is synchronous, so the driver's synchronous calls are used. They run only when
/// the framework loads or rotates keys (at start-up and roughly every 90 days), never per request.
/// </remarks>
public sealed class MongoDataProtectionKeyRepository(MongoContext context) : IXmlRepository
{
    private const string XmlField = "xml";

    public IReadOnlyCollection<XElement> GetAllElements() =>
        context.DataProtectionKeys
            .Find(FilterDefinition<BsonDocument>.Empty)
            .ToList()
            .Select(document => XElement.Parse(document[XmlField].AsString))
            .ToList();

    public void StoreElement(XElement element, string friendlyName)
    {
        var document = new BsonDocument { ["_id"] = friendlyName, [XmlField] = element.ToString(SaveOptions.DisableFormatting) };
        context.DataProtectionKeys.ReplaceOne(
            Builders<BsonDocument>.Filter.Eq("_id", friendlyName),
            document,
            new ReplaceOptions { IsUpsert = true });
    }
}
