using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sync.server
{
    public class DataAccess
    {
        private static string MSSQL_CONN_STR = ConfigurationManager.ConnectionStrings["SqlServerConnectionString"].ConnectionString;

        public DataTable SyncDataFromClientToServer(string sproc, DataTable dt)
        {
            DataTable r = new DataTable();
            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(sproc, sqlconn)
                        {
                            CommandType = CommandType.StoredProcedure
                        })
                    {
                        if (sqlconn.State == ConnectionState.Closed)
                        {
                            sqlconn.Open();
                        }

                        sqlCmd.Parameters.Clear();
                        sqlCmd.Parameters.Add("@Table", SqlDbType.Structured).Value = dt;

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable _dt = new DataTable())
                            {
                                da.Fill(_dt);
                                r = _dt;
                            }
                        }

                        return r;
                    }
                }
                catch (SqlException x)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dt.Rows[i]["IsSynchronized"] = false;
                    }
                    return dt;
                }
                finally
                {
                    if (sqlconn.State == ConnectionState.Open)
                    {
                        sqlconn.Close();
                    }
                }
            }
        }

        public DataTable GetTableStructure(string tableName)
        {
            StringBuilder s = new StringBuilder();
            s.Append("select top 1 * from ");
            s.Append(tableName);

            DataTable r = new DataTable();
            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(s.ToString(), sqlconn)
                        {
                            CommandType = CommandType.Text
                        })
                    {
                        if (sqlconn.State == ConnectionState.Closed)
                        {
                            sqlconn.Open();
                        }

                        sqlCmd.Parameters.Clear();

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable _dt = new DataTable())
                            {
                                da.Fill(_dt);
                                r = _dt;
                            }
                        }
                        r.Rows.Clear();
                        return r;
                    }
                }
                catch (SqlException x)
                {
                    return null;
                }
                finally
                {
                    if (sqlconn.State == ConnectionState.Open)
                    {
                        sqlconn.Close();
                    }
                }
            }
        }

        public DataTable GetServerTableData(string tableName, DateTime lastUpdatedDateTime, int TopRows)
        {
            StringBuilder s = new StringBuilder();
            s.Append("select top ");
            s.Append(TopRows);
            s.Append(" * from ");
            s.Append(tableName);
            s.Append(" where  SyncDateTime >= '");
            s.Append(lastUpdatedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            s.Append("' ");

            DataTable r = new DataTable();
            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(s.ToString(), sqlconn)
                        {
                            CommandType = CommandType.Text
                        })
                    {
                        if (sqlconn.State == ConnectionState.Closed)
                        {
                            sqlconn.Open();
                        }

                        sqlCmd.Parameters.Clear();

                        using (SqlDataAdapter da = new SqlDataAdapter(sqlCmd))
                        {
                            using (DataTable _dt = new DataTable())
                            {
                                da.Fill(_dt);
                                r = _dt;
                            }
                        }
                        return r;
                    }
                }
                catch (SqlException x)
                {
                    return null;
                }
                finally
                {
                    if (sqlconn.State == ConnectionState.Open)
                    {
                        sqlconn.Close();
                    }
                }
            }
        }

    }
}