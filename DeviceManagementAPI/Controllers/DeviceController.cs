using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Helpers;
using DeviceManagementAPI.Hubs; 
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; 
using System;
using System.Data;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")] // <-- important

public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IHubContext<DeviceHub> _hubContext; // inject SignalR Hub
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(IDeviceService deviceService, IHubContext<DeviceHub> hubContext, ILogger<DeviceController> logger)
    {
        _deviceService = deviceService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private IActionResult FormatResponse(object data)
    {
        var acceptHeader = Request.Headers["Accept"].ToString();

        if (!string.IsNullOrEmpty(acceptHeader) && acceptHeader.Contains("application/xml"))
        {
            var json = JsonConvert.SerializeObject(data);  // object → JSON
            var xml = JsonConvert.DeserializeXNode(json, "Root");   // JSON → XML

            return Content(xml.ToString(), "application/xml");    // return XML
        }

        return Ok(data);           // default = JSON
    }

    // GET: api/device
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var devices = _deviceService.GetAllDevices();
            return FormatResponse(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching devices.");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        try
        {
            var device = _deviceService.GetDeviceById(id);
            if (device == null)
                return NotFound(new { message = $"Device with ID {id} not found." });

            return FormatResponse(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching devices.");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }

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
            _logger.LogError(ex, "Error while creating devices.");
            return StatusCode(500, new { message = "Unexpected error while creating devices." });
        }
    }

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
            _logger.LogError(ex, "Error while updating devices.");
            return StatusCode(500, new { message = "Unexpected error while updating devices." });
        }
    }

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
            _logger.LogError(ex, "Error while deleting devices.");
            return StatusCode(500, new { message = "Unexpected error while deleting devices." });
        }
    }

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

            return FormatResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching devices.");
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }
}
