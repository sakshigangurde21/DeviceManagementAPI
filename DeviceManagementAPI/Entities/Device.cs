using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace DeviceManagementAPI.Entities
{
    public class Device
    {
        [BindNever]  // prevents Swagger/Model binding from showing/accepting it
        public int Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;  // soft delete flag

    }
}
