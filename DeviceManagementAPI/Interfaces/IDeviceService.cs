using System.Data;
using DeviceManagementAPI.Models;
using System.Collections.Generic;

namespace DeviceManagementAPI.Interfaces
{
    public interface IDeviceService
    {
        DataTable GetAllDevices();         // Return all devices as DataTable
        DataTable GetDeviceById(int id);  // Return single device as DataTable
        bool CreateDevice(Device device); // return true/false
        bool UpdateDevice(Device device); // return true/false
        bool DeleteDevice(int id);        // return true/false
    }
}

