// =============================================================================
// File: MongoStationLookup.cs
// Description: Resolves stations from SolarStationInfo by application NodeId.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Data;
using MicrogridApi.Models;
using MongoDB.Driver;

namespace MicrogridApi.Services;

public class MongoStationLookup : IStationLookup
{
    private readonly MongoDbContext _db;

    public MongoStationLookup(MongoDbContext db)
    {
        _db = db;
    }

    // Finds a station using MicrogridNode.NodeId (not Mongo ObjectId).
    public async Task<MicrogridNode?> FindByNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        return await _db.MicrogridNodes
            .Find(n => n.NodeId == nodeId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
