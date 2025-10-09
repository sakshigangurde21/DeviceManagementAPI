using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Entities;
using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims; // make sure this is added
using System.Threading.Tasks;
using DeviceManagementAPI.Data;

[Authorize] // Require authentication for all endpoints by default
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly ILogger<DeviceController> _logger;
    private readonly DeviceDbContext _context;

    public DeviceController(IDeviceService deviceService, IHubContext<DeviceHub> hubContext, ILogger<DeviceController> logger, DeviceDbContext context)
    {
        _deviceService = deviceService;
        _hubContext = hubContext;
        _logger = logger;
        _context = context;
    }

    // GET: api/Device?deleted=false
    [HttpGet]
    [Authorize(Roles = "Admin,User")] // Both can view devices
    public IActionResult GetAll([FromQuery] bool deleted = false)
    {
        try
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized(new { message = "Invalid token: UserId or Role missing" });

            IEnumerable<Device> devices;

            if (deleted)
            {
                // Admin sees all deleted devices, users see only their own
                devices = role == "Admin"
                    ? _deviceService.GetDeletedDevices()
                    : _deviceService.GetDeletedDevices().Where(d => d.UserId == userId);
            }
            else
            {
                // Admin sees all active devices, users see only their own
                devices = role == "Admin"
                    ? _deviceService.GetAllDevices()
                    : _deviceService.GetAllDevices().Where(d => d.UserId == userId);
            }

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
    [Authorize(Roles = "Admin,User")]
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
    [Authorize(Roles = "Admin, User")]
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

            var userId = User.FindFirst("UserId")?.Value ?? ""; // JWT "sub" claim usually holds user ID

            var username = User.Identity?.Name;
            Console.WriteLine($"Added by: {username}"); // will now print actual username

            var device = new Device
            {
                DeviceName = trimmedName,
                Description = string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Trim().ToLower() == "string"
                    ? "No description"
                    : dto.Description.Trim(),
                UserId = userId

            };

            bool success = _deviceService.CreateDevice(device);
            if (!success)
                return StatusCode(500, new { message = "Failed to create device." });

            await _hubContext.Clients.All.SendAsync("DeviceAdded", new
            {
                DeviceId = device.Id,
                DeviceName = device.DeviceName,
                AddedBy = User.Identity.Name,  // or your username claim
                UserId = device.UserId
            });

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"{User.Identity?.Name} added device \"{device.DeviceName}\""
            });
            await _context.SaveChangesAsync();

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
    [Authorize(Roles = "Admin,User")]
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

            var userId = User.FindFirst("UserId")?.Value ?? "";

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

            await _hubContext.Clients.All.SendAsync("DeviceUpdated", new
            {
                DeviceId = device.Id,
                DeviceName = device.DeviceName,
                UpdatedBy = User.Identity.Name,
                UserId = device.UserId
            });

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = $"{User.Identity?.Name} updated device \"{device.DeviceName}\""
            });
            await _context.SaveChangesAsync();


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
    [Authorize(Roles = "Admin")] // Only Admin can delete
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            bool success = _deviceService.SoftDeleteDevice(id);
            if (!success)
                return NotFound(new { message = $"Device with ID {id} not found." });

            await _hubContext.Clients.All.SendAsync("DeviceDeleted", new
            {
                DeviceId = id,
                Message = "Device deleted successfully"
            });

            _context.Notifications.Add(new Notification
            {
                Message = $"{User.Identity?.Name} deleted device with ID {id}"
            });
            await _context.SaveChangesAsync();


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
    [Authorize(Roles = "Admin")] // Only Admin can restore
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
    [Authorize(Roles = "Admin")] // Only Admin can restore all
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
    [Authorize(Roles = "Admin")] // Only Admin can permanently delete
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
    [Authorize(Roles = "Admin,User")]
    public IActionResult GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized(new { message = "Invalid token: UserId or Role missing" });

            // Fetch devices from service
            var (devices, totalCount) = _deviceService.GetDevicesPagination(pageNumber, pageSize, includeDeleted);

            // Filter only for non-admin users
            if (role != "Admin")
            {
                devices = devices.Where(d => d.UserId == userId).ToList();
                totalCount = devices.Count();
            }

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

    // GET: api/Device/notifications
    [HttpGet("notifications")]
    [Authorize(Roles = "Admin,User")]
    public IActionResult GetNotifications()
    {
        try
        {
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid user" });

            var notifications = role == "Admin"
                ? _context.Notifications.OrderByDescending(n => n.CreatedAt).ToList()
                : _context.Notifications.Where(n => n.UserId == userId)
                                        .OrderByDescending(n => n.CreatedAt)
                                        .ToList();

            return Ok(notifications.Select(n => new {
                n.Id,
                n.Message,
                n.CreatedAt,
                n.IsRead
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return StatusCode(500, new { message = "Error fetching notifications" });
        }
    }

    [HttpPut("notifications/markread/{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok();
    }


}
