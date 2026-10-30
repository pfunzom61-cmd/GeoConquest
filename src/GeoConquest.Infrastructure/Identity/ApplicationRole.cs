using AspNetCore.Identity.MongoDbCore.Models;
using GeoConquest.Infrastructure.Data;
using MongoDbGenericRepository.Attributes;

namespace GeoConquest.Infrastructure.Identity;

/// <summary>An Identity role, stored in its own collection.</summary>
[CollectionName(MongoContext.RolesCollectionName)]
public sealed class ApplicationRole : MongoIdentityRole<Guid>;
