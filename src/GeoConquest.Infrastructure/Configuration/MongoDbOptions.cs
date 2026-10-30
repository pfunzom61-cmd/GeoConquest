using System.ComponentModel.DataAnnotations;

namespace GeoConquest.Infrastructure.Configuration;

/// <summary>
/// Settings for the MongoDB connection. The connection string is a secret: it comes from user-secrets locally
/// and from the <c>MongoDb__ConnectionString</c> environment variable in production, never from appsettings.
/// </summary>
public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    [Required(ErrorMessage = "MongoDb:ConnectionString is not set. Set it with dotnet user-secrets or the MongoDb__ConnectionString environment variable.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;
}
