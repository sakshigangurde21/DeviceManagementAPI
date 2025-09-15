using System.ComponentModel.DataAnnotations;

namespace DeviceManagementAPI.DTOs
{
    public class CreateDeviceDto
    {
        [Required(ErrorMessage = "DeviceName is required")]
        [MaxLength(100, ErrorMessage = "DeviceName cannot exceed 100 characters.")] 
        public string DeviceName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
