using Microsoft.Data.SqlClient;
using System.Data;
using DeviceManagementAPI.Models;
using DeviceManagementAPI.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DeviceManagementAPI.Services
{
    public class DeviceServiceAdoNet : IDeviceService
    {
        private readonly string _connectionString;

        public DeviceServiceAdoNet(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        // Get all devices
        public List<Device> GetAllDevices()
        {
            var devices = new List<Device>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, DeviceName, Description FROM Devices", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    devices.Add(new Device
                    {
                        Id = reader.GetInt32(0),
                        DeviceName = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "No description" : reader.GetString(2)
                    });
                }
            }
            return devices;
        }

        // Get by ID
        public Device? GetDeviceById(int id)
        {
            Device? device = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Id, DeviceName, Description FROM Devices WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    device = new Device
                    {
                        Id = reader.GetInt32(0),
                        DeviceName = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "No description" : reader.GetString(2)
                    };
                }
            }
            return device; // null if not found
        }

        // Create device
        public bool CreateDevice(Device device)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Devices (DeviceName, Description) VALUES (@DeviceName, @Description)", conn);
                cmd.Parameters.AddWithValue("@DeviceName", device.DeviceName);
                cmd.Parameters.AddWithValue("@Description",
                    string.IsNullOrEmpty(device.Description) ? DBNull.Value : device.Description);

                int rows = cmd.ExecuteNonQuery();
                return rows > 0; // true if inserted
            }
        }

        // Update device
        public bool UpdateDevice(Device device)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Devices SET DeviceName = @DeviceName, Description = @Description WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", device.Id);
                cmd.Parameters.AddWithValue("@DeviceName", device.DeviceName);
                cmd.Parameters.AddWithValue("@Description",
                    string.IsNullOrEmpty(device.Description) ? DBNull.Value : device.Description);

                int rows = cmd.ExecuteNonQuery();
                return rows > 0; 
            }
        }

        // Delete device
        public bool DeleteDevice(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Devices WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                int rows = cmd.ExecuteNonQuery();
                return rows > 0; 
            }
        }
    }
}
