using MySqlConnector;
using System.Data;

namespace BITool.Helpers
{
    public class dbHelper
    {
        public static MySqlConnection connect(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public static DataTable executeQueryDataTable (MySqlConnection conn, string sql, Dictionary<string, string>param) {
            DataTable dtResult = null;
            DataSet dsResult = null;
            MySqlCommand command = null;
            MySqlDataAdapter daResult = null;
            try
            {
                if (conn.State== ConnectionState.Open)
                {
                    dtResult = new DataTable();
                    command = new MySqlCommand(sql, conn);
                    command.CommandType = CommandType.Text;
                    if (param.Count > 0)
                    {
                        foreach(KeyValuePair<string, string> kvp in param)
                        {
                            command.Parameters.Add(new MySqlParameter(kvp.Key, MySqlDbType.VarChar)).Value = kvp.Value;
                        }
                    }
                    daResult = new MySqlDataAdapter(command);
                    daResult.Fill(dsResult);
                    dtResult = dsResult.Tables[0];

                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                dsResult.Dispose();
                command.Dispose();
                daResult.Dispose();
            }
            return dtResult;
        }
    }
}
