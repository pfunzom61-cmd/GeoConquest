using Microsoft.AspNetCore.Authorization;

namespace GeoConquest.Web.Authorization;

/// <summary>Requires the user to be the owner of the game being accessed.</summary>
public sealed class GameOwnerRequirement : IAuthorizationRequirement;
