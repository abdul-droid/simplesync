using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sync.server
{
    internal class DataAccess
    {
        private static string MSSQL_CONN_STR = ConfigurationManager.ConnectionStrings["SqlServerConnectionString"].ConnectionString;

        public DataTable UpdateServerData(string StoredProcedure, DataTable DataTable)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(StoredProcedure, sqlConn)
                        {
                            CommandType = CommandType.StoredProcedure
                        })
                    {
                        if (sqlConn.State == ConnectionState.Closed)
                        {
                            sqlConn.Open();
                        }

                        sqlCmd.Parameters.Clear();
                        sqlCmd.Parameters.Add("@Table", SqlDbType.Structured).Value = DataTable;

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable dt = new DataTable())
                            {
                                da.Fill(dt);
                                dataTable = dt;
                            }
                        }

                        return dataTable;
                    }
                }
                catch (SqlException x)
                {
                    for (int i = 0; i < DataTable.Rows.Count; i++)
                    {
                        DataTable.Rows[i]["IsSynchronized"] = false;
                    }
                    return DataTable;
                }
                finally
                {
                    if (sqlConn.State == ConnectionState.Open)
                    {
                        sqlConn.Close();
                    }
                }
            }
        }

        public DataTable GetTableColumns(string TableName)
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select top 1 * from ");
            sqlQuery.Append(TableName);

            DataTable dataTable = new DataTable(TableName);
            using (SqlConnection sqlConn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(sqlQuery.ToString(), sqlConn)
                        {
                            CommandType = CommandType.Text
                        })
                    {
                        if (sqlConn.State == ConnectionState.Closed)
                        {
                            sqlConn.Open();
                        }

                        sqlCmd.Parameters.Clear();

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable dt = new DataTable())
                            {
                                da.Fill(dt);
                                dataTable = dt;
                            }
                        }
                        dataTable.Rows.Clear();
                        return dataTable;
                    }
                }
                catch (SqlException x)
                {
                    return null;
                }
                finally
                {
                    if (sqlConn.State == ConnectionState.Open)
                    {
                        sqlConn.Close();
                    }
                }
            }
        }

        public DataTable GetServerData(string TableName, DateTime LastUpdatedDateTime, int TopRows)
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select top ");
            sqlQuery.Append(TopRows);
            sqlQuery.Append(" * from ");
            sqlQuery.Append(TableName);
            sqlQuery.Append(" where  SyncDateTime >= '");
            sqlQuery.Append(LastUpdatedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sqlQuery.Append("' ");

            DataTable dataTable = new DataTable(TableName);
            using (SqlConnection sqlConn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(sqlQuery.ToString(), sqlConn)
                        {
                            CommandType = CommandType.Text
                        })
                    {
                        if (sqlConn.State == ConnectionState.Closed)
                        {
                            sqlConn.Open();
                        }

                        sqlCmd.Parameters.Clear();

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable dt = new DataTable())
                            {
                                da.Fill(dt);
                                dataTable = dt;
                            }
                        }
                        return dataTable;
                    }
                }
                catch (SqlException x)
                {
                    return null;
                }
                finally
                {
                    if (sqlConn.State == ConnectionState.Open)
                    {
                        sqlConn.Close();
                    }
                }
            }
        }
    }
}