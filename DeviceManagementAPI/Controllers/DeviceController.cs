using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using DeviceManagementAPI.Interfaces;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[Route("api/[controller]")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private const int MAX_DEVICE_NAME_LENGTH = 100;

    public DeviceController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }



    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var devices = _deviceService.GetAllDevices();
            return Ok(devices);
        }
        catch (Exception)
        {
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

            return Ok(device);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Unexpected error while fetching the device." });
        }
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateDeviceDto dto)
    {
        try
        {
            var trimmedName = dto.DeviceName?.Trim() ?? "";

            // Validations
            if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.ToLower() == "string")
                return BadRequest(new { message = "Device name is required." });

            if (trimmedName.Length > MAX_DEVICE_NAME_LENGTH)
                return BadRequest(new { message = $"Device name cannot exceed {MAX_DEVICE_NAME_LENGTH} characters." });

            if (Regex.IsMatch(trimmedName, @"^\d+$"))
                return BadRequest(new { message = "Device name cannot be only numbers." });

            if (!Regex.IsMatch(trimmedName, @"^[a-zA-Z0-9 _-]+$"))
                return BadRequest(new { message = "Device name can only contain letters, numbers, spaces, hyphens, or underscores." });

            if (!Regex.IsMatch(trimmedName, @"[a-zA-Z0-9]"))
                return BadRequest(new { message = "Device name must contain at least one letter or number." });

            if (_deviceService.GetAllDevices().Any(d => d.DeviceName.Trim().ToLower() == trimmedName.ToLower()))
                return BadRequest(new { message = "Device with this name already exists." });

            var device = new Device
            {
                DeviceName = trimmedName,
                Description = string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().ToLower() == "string"
                    ? "No description"
                    : dto.Description.Trim()
            };

            var success = _deviceService.CreateDevice(device);
            if (!success)
                return StatusCode(500, new { message = "Failed to create device." });

            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Unexpected error while creating the device." });
        }
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] UpdateDeviceDto dto)
    {
        try
        {
            var trimmedName = dto.DeviceName?.Trim() ?? "";

            // Validations
            if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.ToLower() == "string")
                return BadRequest(new { message = "Device name is required." });

            if (trimmedName.Length > MAX_DEVICE_NAME_LENGTH)
                return BadRequest(new { message = $"Device name cannot exceed {MAX_DEVICE_NAME_LENGTH} characters." });

            if (Regex.IsMatch(trimmedName, @"^\d+$"))
                return BadRequest(new { message = "Device name cannot be only numbers." });

            if (!Regex.IsMatch(trimmedName, @"^[a-zA-Z0-9 _-]+$"))
                return BadRequest(new { message = "Device name can only contain letters, numbers, spaces, hyphens, or underscores." });

            if (!Regex.IsMatch(trimmedName, @"[a-zA-Z0-9]"))
                return BadRequest(new { message = "Device name must contain at least one letter or number." });

            if (_deviceService.GetAllDevices().Any(d => d.DeviceName.Trim().ToLower() == trimmedName.ToLower() && d.Id != id))
                return BadRequest(new { message = "Device with this name already exists." });

            var device = new Device
            {
                Id = id,
                DeviceName = trimmedName,
                Description = string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().ToLower() == "string"
                    ? "No description"
                    : dto.Description.Trim()
            };

            var success = _deviceService.UpdateDevice(device);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            return Ok(new { message = $"Device with ID {id} updated successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Unexpected error while updating the device." });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        try
        {
            var success = _deviceService.DeleteDevice(id);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            return Ok(new { message = $"Device with ID {id} deleted successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Unexpected error while deleting the device." });
        }
    }
}
