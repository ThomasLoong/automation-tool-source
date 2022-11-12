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
            DataTable dtResult = new DataTable();
            DataSet dsResult = new DataSet();
            MySqlCommand command = null;
            MySqlDataAdapter daResult = null;
            try
            {
                if (conn.State!= ConnectionState.Open)
                {
                    conn.Open();
                }
                if (conn.State== ConnectionState.Open)
                {
                    command = conn.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = sql; 
                    if (param.Count > 0)
                    {
                        foreach(KeyValuePair<string, string> kvp in param)
                        {
                            command.Parameters.Add(new MySqlParameter(kvp.Key, MySqlDbType.VarChar)).Value = kvp.Value;
                        }
                    }
                    daResult = new MySqlDataAdapter(command);
                    if (daResult != null)
                    {
                        daResult.Fill(dsResult);
                        dtResult = dsResult.Tables[0];
                    }

                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                dsResult = null;
                command = null;
                daResult = null;
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            return dtResult;
        }
    }
}
