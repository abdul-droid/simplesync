using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sync.client
{
    internal static class DataAccess
    {
        internal static string MSSQL_CONN_STR;

        internal static DataTable GetTableData(string TableName, int TopRows)
        {
            StringBuilder s = new StringBuilder();
            s.Append("select top ");
            s.Append(TopRows);
            s.Append(" * from ");
            s.Append(TableName);
            s.Append(" where IsSynchronized = 0; ");

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

        internal static DataTable GetSyncableTables()
        {
            StringBuilder s = new StringBuilder();
            s.Append("select * from Sync.Tables where IsSyncable = 1");

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

        internal static bool FlagSynchronizedRows(string TableName, List<string> SyncGuids)
        {
            bool p = false;
            StringBuilder s = new StringBuilder();

            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    if (sqlconn.State == ConnectionState.Closed)
                    {
                        sqlconn.Open();
                    }
                    for (int i = 0; i < SyncGuids.Count; i++)
                    {
                        s.Clear();
                        s.Append("update ");
                        s.Append(TableName);
                        s.Append(" set IsSynchronized = 1");
                        s.Append(" where SyncGuid = ");
                        s.Append("'");
                        s.Append(SyncGuids[i]);
                        s.Append("'");

                        using (
                            SqlCommand sqlCmd = new SqlCommand(s.ToString(), sqlconn)
                            {
                                CommandType = CommandType.Text
                            })
                        {

                            sqlCmd.Parameters.Clear();

                            p = sqlCmd.ExecuteNonQuery() > 0;
                        }
                    }

                    return p;
                }
                catch (SqlException x)
                {
                    return p;
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

        internal static bool FlagSynchronizedRowsUsingRowVersion(string TableName, List<TableRow> SyncGuidAndRowVersions)
        {
            bool p = false;
            StringBuilder s = new StringBuilder();

            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    if (sqlconn.State == ConnectionState.Closed)
                    {
                        sqlconn.Open();
                    }
                    for (int i = 0; i < SyncGuidAndRowVersions.Count; i++)
                    {
                        s.Clear();
                        s.Append("update ");
                        s.Append(TableName);
                        s.Append(" set IsSynchronized = 1");
                        s.Append(" where SyncGuid = ");
                        s.Append("'");
                        s.Append(SyncGuidAndRowVersions[i].SynGuid);
                        s.Append("'");

                        if (SyncGuidAndRowVersions[i].RowVersion == string.Empty)
                        {
                            s.Append(" and [RowVersion] is null ");
                        }
                        else
                        {
                            s.Append(" and [RowVersion] = ");
                            s.Append("'");
                            s.Append(SyncGuidAndRowVersions[i].RowVersion);
                            s.Append("'");
                        }

                        try
                        {
                            using (
                            SqlCommand sqlCmd = new SqlCommand(s.ToString(), sqlconn)
                            {
                                CommandType = CommandType.Text
                            })
                            {

                                sqlCmd.Parameters.Clear();

                                p = sqlCmd.ExecuteNonQuery() > 0;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    return p;
                }
                catch (SqlException x)
                {
                    return p;
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

        internal static bool FlagSynchronizedRows(string TableName, List<TableRow> SyncGuidAndRowVersions)
        {
            bool p = false;
            StringBuilder s = new StringBuilder();

            using (SqlConnection sqlconn = new SqlConnection(MSSQL_CONN_STR))
            {
                try
                {
                    if (sqlconn.State == ConnectionState.Closed)
                    {
                        sqlconn.Open();
                    }
                    for (int i = 0; i < SyncGuidAndRowVersions.Count; i++)
                    {
                        s.Clear();
                        s.Append("update ");
                        s.Append(TableName);
                        s.Append(" set IsSynchronized = 1");
                        s.Append(" where SyncGuid = ");
                        s.Append("'");
                        s.Append(SyncGuidAndRowVersions[i].SynGuid);
                        s.Append("'");

                        try
                        {
                            using (
                            SqlCommand sqlCmd = new SqlCommand(s.ToString(), sqlconn)
                            {
                                CommandType = CommandType.Text
                            })
                            {

                                sqlCmd.Parameters.Clear();

                                p = sqlCmd.ExecuteNonQuery() > 0;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    return p;
                }
                catch (SqlException x)
                {
                    return p;
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

        internal static DateTime? GetClientDataLastUpdatedDateTime(string TableName)
        {
            DateTime? LastSyncDateTime = null;
            StringBuilder s = new StringBuilder();
            s.Append("select MAX(SyncDateTime) as SyncDateTime from ");
            s.Append(TableName);

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

                        if (r.Rows.Count > 0)
                        {
                            if (r.Rows[0]["SyncDateTime"] == DBNull.Value)
                            {
                                LastSyncDateTime = null;
                            }
                            else
                            {
                                LastSyncDateTime = Convert.ToDateTime(r.Rows[0]["SyncDateTime"]);
                            }
                        }

                        return LastSyncDateTime;
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

        internal static bool UpdateClientDataWithDataReceivedFromServer(string sproc, DataTable dt)
        {
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

                        return sqlCmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SqlException x)
                {
                    return false;
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

        internal static DataTable GetTableStructure(string TableName)
        {
            StringBuilder s = new StringBuilder();
            s.Append("select top 1 * from ");
            s.Append(TableName);

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


    }
}
