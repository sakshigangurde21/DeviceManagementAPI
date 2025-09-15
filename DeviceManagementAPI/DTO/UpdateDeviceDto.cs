using System.ComponentModel.DataAnnotations;

namespace DeviceManagementAPI.DTOs
{
    public class UpdateDeviceDto
    {
        [Required(ErrorMessage = "Id is required for update")]
        public int Id { get; set; }

        [Required(ErrorMessage = "DeviceName is required")]
        public string DeviceName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
