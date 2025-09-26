using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

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

    // GET: api/Device
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var devices = _deviceService.GetAllDevices(); // List<Device>
            return Ok(devices); // Direct JSON
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

            return Ok(device); // Return JSON directly
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

    // DELETE: api/Device/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            bool success = _deviceService.DeleteDevice(id);
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

    // GET: api/Device/paged?pageNumber=1&pageSize=10
    [HttpGet("paged")]
    public IActionResult GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (devices, totalCount) = _deviceService.GetDevicesPagination(pageNumber, pageSize);

            var response = new
            {
                Data = devices,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(response); // Return JSON
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching paged devices");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }
}
