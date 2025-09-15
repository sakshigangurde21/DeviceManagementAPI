using DeviceManagementAPI.Models;
using System.Collections.Generic;

namespace DeviceManagementAPI.Interfaces
{
    public interface IDeviceService
    {
        List<Device> GetAllDevices();
        Device? GetDeviceById(int id);   // nullable return type
        bool CreateDevice(Device device); // return true/false
        bool UpdateDevice(Device device); // return true/false
        bool DeleteDevice(int id);        // return true/false
    }
}
