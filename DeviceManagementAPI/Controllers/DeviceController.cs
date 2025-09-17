using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using DeviceManagementAPI.Interfaces;
using System;
using System.Linq;
using System.Data;
using DeviceManagementAPI.Helpers;
using DeviceManagementAPI.Hubs; 
using Microsoft.AspNetCore.SignalR; 

[Route("api/[controller]")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IHubContext<DeviceHub> _hubContext; // inject SignalR Hub

    public DeviceController(IDeviceService deviceService, IHubContext<DeviceHub> hubContext)
    {
        _deviceService = deviceService;
        _hubContext = hubContext;
    }

    // GET: api/device
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            DataTable dt = _deviceService.GetAllDevices();
            return Ok(dt.ToDictionaryList());
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while fetching devices." });
        }
    }

    // GET: api/device/5
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        try
        {
            DataTable dt = _deviceService.GetDeviceById(id);
            if (dt.Rows.Count == 0)
                return NotFound(new { message = $"Device with ID {id} not found." });

            return Ok(dt.ToDictionaryList()[0]);
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while fetching the device." });
        }
    }

    // POST: api/device
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string trimmedName = dto.DeviceName?.Trim() ?? "";

            var allDevices = _deviceService.GetAllDevices().ToDictionaryList();
            if (allDevices.Any(d => d["DeviceName"].ToString()?.Trim().ToLower() == trimmedName.ToLower()))
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

            // Broadcast event
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device added successfully");

            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while creating the device." });
        }
    }

    // PUT: api/device/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string trimmedName = dto.DeviceName?.Trim() ?? "";

            var allDevices = _deviceService.GetAllDevices().ToDictionaryList();
            if (allDevices.Any(d => d["DeviceName"].ToString()?.Trim().ToLower() == trimmedName.ToLower()
                                   && Convert.ToInt32(d["Id"]) != id))
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

            // Broadcast event
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device updated successfully");

            return Ok(new { message = $"Device with ID {id} updated successfully." });
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while updating the device." });
        }
    }

    // DELETE: api/device/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            bool success = _deviceService.DeleteDevice(id);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            // Broadcast event
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Device deleted successfully");

            return Ok(new { message = $"Device with ID {id} deleted successfully." });
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while deleting the device." });
        }
    }

    [HttpGet("paged")]
    public IActionResult GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            DataSet ds = _deviceService.GetDevicesPagination(pageNumber, pageSize);

            var devices = ds.Tables[0].ToDictionaryList();
            int totalCount = Convert.ToInt32(ds.Tables[1].Rows[0]["TotalCount"]);

            return Ok(new
            {
                Data = devices,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch
        {
            return StatusCode(500, new { message = "Unexpected error while fetching paged devices." });
        }
    }
}
