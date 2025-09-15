using System.Data;

namespace DeviceManagementAPI.Helpers
{
    public static class DataTableExtensions
    {
        public static List<Dictionary<string, object?>> ToDictionaryList(this DataTable dt)
        {
            var list = new List<Dictionary<string, object?>>();

            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                list.Add(dict);
            }

            return list;
        }
    }
}
