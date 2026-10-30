using AspNetCore.Identity.MongoDbCore.Models;
using GeoConquest.Infrastructure.Data;
using MongoDbGenericRepository.Attributes;

namespace GeoConquest.Infrastructure.Identity;

/// <summary>A registered player. Email, password hash and lockout state come from ASP.NET Core Identity.</summary>
[CollectionName(MongoContext.UsersCollectionName)]
public sealed class ApplicationUser : MongoIdentityUser<Guid>
{
    /// <summary>The name shown to other players, for example on the public list of live games.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
