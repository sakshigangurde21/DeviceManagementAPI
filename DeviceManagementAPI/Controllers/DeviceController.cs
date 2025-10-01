using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Entities;
using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

// [Authorize]  // require authentication
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly ILogger<DeviceController> _logger;
    public DeviceController(IDeviceService deviceService, IHubContext<DeviceHub> hubContext, ILogger<DeviceController> logger)
    {
        _deviceService = deviceService;
        _hubContext = hubContext;
        _logger = logger;
    }

    // GET: api/Device?deleted=false
    [HttpGet]
    public IActionResult GetAll([FromQuery] bool deleted = false)
    {
        try
        {
            var devices = deleted
                ? _deviceService.GetDeletedDevices()
                : _deviceService.GetAllDevices();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching devices");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }

    // GET: api/Device/{id}
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        try
        {
            var device = _deviceService.GetDeviceById(id);
            if (device == null)
                return NotFound(new { message = $"Device with ID {id} not found." });

            return Ok(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching device by ID");
            return StatusCode(500, new { message = "Unexpected error while fetching device." });
        }
    }

    // POST: api/Device
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string trimmedName = dto.DeviceName?.Trim() ?? "";

            var allDevices = _deviceService.GetAllDevices();
            if (allDevices.Any(d => d.DeviceName.Trim().ToLower() == trimmedName.ToLower()))
                return BadRequest(new { message = "Device with this name already exists." });

            var device = new Device
            {
                DeviceName = trimmedName,
                Description = string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().ToLower() == "string"
                    ? "No description"
                    : dto.Description.Trim()
            };

            bool success = _deviceService.CreateDevice(device);
            if (!success)
                return StatusCode(500, new { message = "Failed to create device." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device added successfully");

            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating device");
            return StatusCode(500, new { message = "Unexpected error while creating device." });
        }
    }

    // PUT: api/Device/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string trimmedName = dto.DeviceName?.Trim() ?? "";

            var allDevices = _deviceService.GetAllDevices();
            if (allDevices.Any(d => d.DeviceName.Trim().ToLower() == trimmedName.ToLower() && d.Id != id))
                return BadRequest(new { message = "Device with this name already exists." });

            var device = new Device
            {
                Id = id,
                DeviceName = trimmedName,
                Description = string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().ToLower() == "string"
                    ? "No description"
                    : dto.Description.Trim()
            };

            bool success = _deviceService.UpdateDevice(device);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device updated successfully");

            return Ok(new { message = $"Device with ID {id} updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating device");
            return StatusCode(500, new { message = "Unexpected error while updating device." });
        }
    }

    // DELETE: api/Device/{id}   --> SOFT DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            bool success = _deviceService.SoftDeleteDevice(id);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device deleted successfully");

            return Ok(new { message = $"Device with ID {id} deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting device");
            return StatusCode(500, new { message = "Unexpected error while deleting device." });
        }
    }

    // PUT: api/Device/restore/{id}  --> RESTORE
    [HttpPut("restore/{id}")]
    public async Task<IActionResult> RestoreDevice(int id)
    {
        try
        {
            bool success = _deviceService.RestoreDevice(id);
            if (!success)
                return NotFound(new { message = $"Deleted device with ID {id} not found." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device restored successfully");

            return Ok(new { message = $"Device with ID {id} restored successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while restoring device");
            return StatusCode(500, new { message = "Unexpected error while restoring device." });
        }
    }

    // PUT: api/Device/restoreAll  --> RESTORE ALL
    [HttpPut("restoreAll")]
    public async Task<IActionResult> RestoreAllDeletedDevices()
    {
        try
        {
            bool success = _deviceService.RestoreAllDeletedDevices();
            if (!success)
                return NotFound(new { message = "No deleted devices found to restore." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "All deleted devices restored successfully");

            return Ok(new { message = "All deleted devices restored successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while restoring all devices");
            return StatusCode(500, new { message = "Unexpected error while restoring devices." });
        }
    }

    // DELETE: api/Device/permanent/{id}  --> PERMANENT DELETE
    [HttpDelete("permanent/{id}")]
    public async Task<IActionResult> DeletePermanent(int id)
    {
        try
        {
            bool success = _deviceService.PermanentDeleteDevice(id);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found for permanent deletion." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device permanently deleted");

            return Ok(new { message = $"Device with ID {id} permanently deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while permanently deleting device");
            return StatusCode(500, new { message = "Unexpected error while permanently deleting device." });
        }
    }

    // GET: api/Device/paged?pageNumber=1&pageSize=10
    [HttpGet("paged")]
    public IActionResult GetPaged(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] bool includeDeleted = false)
    {
        try
        {
            // Fetch devices from service with includeDeleted flag
            var (devices, totalCount) = _deviceService.GetDevicesPagination(pageNumber, pageSize, includeDeleted);

            // Ensure pagination includes deleted devices when requested
            var response = new
            {
                Data = devices,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeDeleted = includeDeleted
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching paged devices");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }

}
