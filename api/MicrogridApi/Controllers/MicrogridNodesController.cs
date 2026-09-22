using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/microgridnodes")]
public class MicrogridNodesController : ControllerBase
{
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
}