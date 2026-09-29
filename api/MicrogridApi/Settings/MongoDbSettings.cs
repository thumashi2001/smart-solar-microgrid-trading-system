// =============================================================================
// File: MongoDbSettings.cs
// Description: Strongly-typed settings bound from the "MongoDbSettings" section
//              of appsettings.json (connection string and database name).
// Author: Thumashi (Component 1)
// =============================================================================

namespace MicrogridApi.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}