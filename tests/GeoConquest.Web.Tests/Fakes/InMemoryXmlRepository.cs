using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace GeoConquest.Web.Tests.Fakes;

/// <summary>Stands in for the Mongo-backed data-protection key store so tests never reach a database.</summary>
public sealed class InMemoryXmlRepository : IXmlRepository
{
    private readonly List<XElement> _elements = [];

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (_elements)
        {
            return [.. _elements];
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        lock (_elements)
        {
            _elements.Add(element);
        }
    }
}
