// =============================================================================
// File: MicrogridNodesController.cs
// Description: Microgrid node CRUD (Nethasa) plus nearby read extension (Suwani).
// Author: Nethasa / Suwani (nearby endpoint only)
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Dtos.Transfers;
using MicrogridApi.Models;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/microgridnodes")]
public class MicrogridNodesController : ControllerBase
{
    private const double DefaultNearbyRadiusKm = 10.0;
    private const double MaxNearbyRadiusKm = 100.0;

    private readonly MongoDbContext _db;

    public MicrogridNodesController(MongoDbContext db)
    {
        _db = db;
    }

    // GET: api/microgridnodes
    // Returns all microgrid nodes.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var nodes = await _db.MicrogridNodes
            .Find(_ => true)
            .ToListAsync();

        return Ok(nodes);
    }

    // GET: api/microgridnodes/nearby?lat=&lng=&radiusKm=
    // Returns active stations within radius (default 10 km). Suwani map integration.
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double? radiusKm = null)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
        {
            return BadRequest(new { message = "Latitude/longitude are out of valid range." });
        }

        var radius = radiusKm ?? DefaultNearbyRadiusKm;
        if (radius <= 0 || radius > MaxNearbyRadiusKm)
        {
            return BadRequest(new
            {
                message = $"radiusKm must be between 0 exclusive and {MaxNearbyRadiusKm}."
            });
        }

        var nodes = await _db.MicrogridNodes
            .Find(n => n.Status == "active")
            .ToListAsync();

        var nearby = nodes
            .Where(n => IsValidCoordinate(n.Latitude, n.Longitude))
            .Select(n =>
            {
                var distance = HaversineKm(lat, lng, n.Latitude, n.Longitude);
                return new NearbyStationResponse
                {
                    Id = n.Id,
                    NodeId = n.NodeId,
                    NodeName = n.NodeName,
                    Location = n.Location,
                    Latitude = n.Latitude,
                    Longitude = n.Longitude,
                    CapacityKWh = n.CapacityKWh,
                    BatterySlots = n.BatterySlots,
                    Status = n.Status,
                    DistanceKm = Math.Round(distance, 3)
                };
            })
            .Where(n => n.DistanceKm <= radius)
            .OrderBy(n => n.DistanceKm)
            .ToList();

        return Ok(nearby);
    }

    // GET: api/microgridnodes/{id}
    // Returns one microgrid node using its MongoDB ID.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _db.MicrogridNodes
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            return NotFound(new
            {
                message = "Microgrid node not found."
            });
        }

        return Ok(node);
    }

    // POST: api/microgridnodes
    // Creates a new microgrid node.
    [HttpPost]
    public async Task<IActionResult> Create(MicrogridNode newNode)
    {
        if (string.IsNullOrWhiteSpace(newNode.NodeName))
        {
            return BadRequest(new
            {
                message = "Node name is required."
            });
        }

        if (newNode.CapacityKWh <= 0)
        {
            return BadRequest(new
            {
                message = "Capacity must be greater than zero."
            });
        }

        if (newNode.BatterySlots < 0)
        {
            return BadRequest(new
            {
                message = "Battery slots cannot be negative."
            });
        }

        // Generate the application-level node ID.
        newNode.NodeId = $"NODE-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        newNode.Status = "active";
        newNode.CreatedAt = DateTime.UtcNow;
        newNode.UpdatedAt = DateTime.UtcNow;

        await _db.MicrogridNodes.InsertOneAsync(newNode);

        return CreatedAtAction(
            nameof(GetById),
            new { id = newNode.Id },
            newNode
        );
    }

    // PUT: api/microgridnodes/{id}
    // Updates an existing microgrid node.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        MicrogridNode updatedNode)
    {
        var existingNode = await _db.MicrogridNodes
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync();

        if (existingNode == null)
        {
            return NotFound(new
            {
                message = "Microgrid node not found."
            });
        }

        if (string.IsNullOrWhiteSpace(updatedNode.NodeName))
        {
            return BadRequest(new
            {
                message = "Node name is required."
            });
        }

        if (updatedNode.CapacityKWh <= 0)
        {
            return BadRequest(new
            {
                message = "Capacity must be greater than zero."
            });
        }

        if (updatedNode.BatterySlots < 0)
        {
            return BadRequest(new
            {
                message = "Battery slots cannot be negative."
            });
        }

        // Preserve values that should not be replaced by the client.
        updatedNode.Id = existingNode.Id;
        updatedNode.NodeId = existingNode.NodeId;
        updatedNode.CreatedAt = existingNode.CreatedAt;
        updatedNode.UpdatedAt = DateTime.UtcNow;

        // Preserve current status if the client does not provide one.
        if (string.IsNullOrWhiteSpace(updatedNode.Status))
        {
            updatedNode.Status = existingNode.Status;
        }

        await _db.MicrogridNodes.ReplaceOneAsync(
            n => n.Id == id,
            updatedNode
        );

        return Ok(updatedNode);
    }

    // PATCH: api/microgridnodes/{id}/deactivate
    // Marks a microgrid node as inactive.
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var node = await _db.MicrogridNodes
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            return NotFound(new
            {
                message = "Microgrid node not found."
            });
        }

        var update = Builders<MicrogridNode>.Update
            .Set(n => n.Status, "inactive")
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        await _db.MicrogridNodes.UpdateOneAsync(
            n => n.Id == id,
            update
        );

        return Ok(new
        {
            message = "Microgrid node deactivated successfully."
        });
    }

    // PATCH: api/microgridnodes/{id}/reactivate
    // Reactivates an inactive microgrid node.
    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id)
    {
        var node = await _db.MicrogridNodes
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            return NotFound(new
            {
                message = "Microgrid node not found."
            });
        }

        var update = Builders<MicrogridNode>.Update
            .Set(n => n.Status, "active")
            .Set(n => n.UpdatedAt, DateTime.UtcNow);

        await _db.MicrogridNodes.UpdateOneAsync(
            n => n.Id == id,
            update
        );

        return Ok(new
        {
            message = "Microgrid node reactivated successfully."
        });
    }

    // Accepts any finite lat/lng in WGS84 range, including valid zeros.
    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return !double.IsNaN(latitude)
            && !double.IsNaN(longitude)
            && !double.IsInfinity(latitude)
            && !double.IsInfinity(longitude)
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180;
    }

    // Great-circle distance in kilometres between two WGS84 points.
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1))
            * Math.Cos(DegreesToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}