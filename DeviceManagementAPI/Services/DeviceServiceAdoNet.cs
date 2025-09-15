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
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection", "Connection string not found.");
        }

        // Get all devices
        public DataTable GetAllDevices()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter("GetAllDevices", conn))
                {
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.Fill(dt);
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Description"] == DBNull.Value || string.IsNullOrWhiteSpace(row["Description"]?.ToString()))
                        row["Description"] = "No description";
                }

                return dt;
            }
            catch
            {
                throw;
            }
        }

        // Get device by ID
        public DataTable GetDeviceById(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter("GetDeviceById", conn))
                {
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    if (row["Description"] == DBNull.Value || string.IsNullOrWhiteSpace(row["Description"]?.ToString()))
                        row["Description"] = "No description";
                }

                return dt;
            }
            catch
            {
                throw;
            }
        }

        // Create device
        public bool CreateDevice(Device device)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    adapter.InsertCommand = new SqlCommand("InsertDevice", conn);
                    adapter.InsertCommand.CommandType = CommandType.StoredProcedure;
                    adapter.InsertCommand.Parameters.AddWithValue("@DeviceName", device.DeviceName ?? "");
                    adapter.InsertCommand.Parameters.AddWithValue("@Description",
                        string.IsNullOrWhiteSpace(device.Description) ? DBNull.Value : device.Description);

                    conn.Open();
                    int rows = adapter.InsertCommand.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch
            {
                throw;
            }
        }

        // Update device
        public bool UpdateDevice(Device device)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    adapter.UpdateCommand = new SqlCommand("UpdateDevice", conn);
                    adapter.UpdateCommand.CommandType = CommandType.StoredProcedure;
                    adapter.UpdateCommand.Parameters.AddWithValue("@Id", device.Id);
                    adapter.UpdateCommand.Parameters.AddWithValue("@DeviceName", device.DeviceName ?? "");
                    adapter.UpdateCommand.Parameters.AddWithValue("@Description",
                        string.IsNullOrWhiteSpace(device.Description) ? DBNull.Value : device.Description);

                    conn.Open();
                    int rows = adapter.UpdateCommand.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch
            {
                throw;
            }
        }

        // Delete device
        public bool DeleteDevice(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    adapter.DeleteCommand = new SqlCommand("DeleteDevice", conn);
                    adapter.DeleteCommand.CommandType = CommandType.StoredProcedure;
                    adapter.DeleteCommand.Parameters.AddWithValue("@Id", id);

                    conn.Open();
                    int rows = adapter.DeleteCommand.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
