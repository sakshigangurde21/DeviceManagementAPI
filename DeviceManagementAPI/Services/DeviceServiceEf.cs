using DeviceManagementAPI.Data;
using DeviceManagementAPI.Entities;
using DeviceManagementAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeviceManagementAPI.Services
{
    public class DeviceServiceEf : IDeviceService
    {
        private readonly DeviceDbContext _context;
        public DeviceServiceEf(DeviceDbContext context)
        {
            _context = context;
        }

        public List<Device> GetAllDevices()
        {
            return _context.Devices.ToList();
        }

        public Device? GetDeviceById(int id)
        {
            return _context.Devices.FirstOrDefault(d => d.Id == id);
        }

        public bool CreateDevice(Device device)
        {
            _context.Devices.Add(device);
            return _context.SaveChanges() > 0;
        }

        public bool UpdateDevice(Device device)
        {
            var existing = _context.Devices.FirstOrDefault(d => d.Id == device.Id);
            if (existing == null) return false;

            existing.DeviceName = device.DeviceName;
            existing.Description = device.Description;
            return _context.SaveChanges() > 0;
        }
        public bool DeleteDevice(int id)
        {
            var device = _context.Devices.FirstOrDefault(d => d.Id == id);
            if (device == null) return false;

            device.IsDeleted = true;  // SAFE DELETE
            return _context.SaveChanges() > 0;
        }

        public List<Device> GetAllDevicesIncludingDeleted()
        {
            return _context.Devices.IgnoreQueryFilters().ToList();
        }

        public (List<Device> Devices, int TotalCount) GetDevicesPagination(int pageNumber, int pageSize)
        {
            var query = _context.Devices.AsQueryable();
            int totalCount = query.Count();

            var devices = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (devices, totalCount);
        }
    }
}
