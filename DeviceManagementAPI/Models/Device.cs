using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagementAPI.Models
{
    public class Device
    {
        [BindNever]  // prevents Swagger/Model binding from showing/accepting it
        public int Id { get; set; }

        [Required(ErrorMessage = "DeviceName is required")]
        public string DeviceName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
