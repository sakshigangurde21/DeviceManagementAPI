using DeviceManagementAPI.Entities;
using System.Collections.Generic;

namespace DeviceManagementAPI.Interfaces
{
    public interface IDeviceService
    {
        List<Device> GetAllDevices();
        Device? GetDeviceById(int id);
        bool CreateDevice(Device device);
        bool UpdateDevice(Device device);
        bool DeleteDevice(int id);
        (List<Device> Devices, int TotalCount) GetDevicesPagination(int pageNumber, int pageSize);
    }
}
