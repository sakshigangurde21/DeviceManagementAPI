using DeviceManagementAPI.Models;
using System.Collections.Generic;
using System.Linq;
using DeviceManagementAPI.Interfaces;

public class DeviceService : IDeviceService
{
    private static List<Device> Devices = new List<Device>();
    private static int nextId = 1;

    public List<Device> GetAllDevices()
    {
        return Devices;
    }

    public Device? GetDeviceById(int id)  
    {
        return Devices.FirstOrDefault(d => d.Id == id);
    }

    public bool CreateDevice(Device device)
    {
        if (string.IsNullOrWhiteSpace(device.DeviceName))
            return false;

        if (string.IsNullOrWhiteSpace(device.Description) || device.Description.Trim().ToLower() == "string")
            device.Description = "No description";

        device.Id = nextId++;
        Devices.Add(device);
        return true;
    }

    public bool UpdateDevice(Device device)
    {
        var existing = Devices.FirstOrDefault(d => d.Id == device.Id);
        if (existing == null)
            return false;

        existing.DeviceName = device.DeviceName;

        if (string.IsNullOrWhiteSpace(device.Description) || device.Description.Trim().ToLower() == "string")
            existing.Description = "No description";
        else
            existing.Description = device.Description;

        return true;
    }

    public bool DeleteDevice(int id)
    {
        var device = Devices.FirstOrDefault(d => d.Id == id);
        if (device == null)
            return false;

        Devices.Remove(device);
        return true;
    }
}
