using System.ComponentModel.DataAnnotations;
using DeviceManagementAPI.Validations;

namespace DeviceManagementAPI.DTOs
{
    public class UpdateDeviceDto
    {
        [DeviceNameValidation]
        public string DeviceName { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }
    }
}
