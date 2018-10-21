using sync.core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sync.client
{
    internal static class DataAccess
    {
        internal static DataTable GetDataToSync(string TableName, int TopRows)
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select top ");
            sqlQuery.Append(TopRows);
            sqlQuery.Append(" * from ");
            sqlQuery.Append(TableName);
            sqlQuery.Append(" where IsSynchronized = 0; ");

            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
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
                catch (SqlException SqlException)
                {
                    SqlException.LogErrorToTextFile();
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

        internal static DataTable GetSyncableTables()
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select * from Sync.Tables where IsSyncable = 1");

            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
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
                catch (SqlException SqlException)
                {
                    SqlException.LogErrorToTextFile();
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

        internal static bool FlagSynchronizedRows(string TableName, List<TableRow> SyncGuidAndRowVersions)
        {
            bool success = false;

            if (SyncGuidAndRowVersions.Count > 0)
            {
                StringBuilder sqlQuery = new StringBuilder();

                using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
                {
                    try
                    {
                        if (sqlConn.State == ConnectionState.Closed)
                        {
                            sqlConn.Open();
                        }
                        for (int i = 0; i < SyncGuidAndRowVersions.Count; i++)
                        {
                            sqlQuery.Clear();
                            sqlQuery.Append("update ");
                            sqlQuery.Append(TableName);
                            sqlQuery.Append(" set IsSynchronized = 1");
                            sqlQuery.Append(" where SyncGuid = ");
                            sqlQuery.Append("'");
                            sqlQuery.Append(SyncGuidAndRowVersions[i].SyncGuid);
                            sqlQuery.Append("'");

                            if (SyncGuidAndRowVersions[i].RowVersion == string.Empty)
                            {
                                sqlQuery.Append(" and [RowVersion] is null ");
                            }
                            else
                            {
                                sqlQuery.Append(" and [RowVersion] = ");
                                sqlQuery.Append("'");
                                sqlQuery.Append(SyncGuidAndRowVersions[i].RowVersion);
                                sqlQuery.Append("'");
                            }

                            try
                            {
                                using (
                                SqlCommand sqlCmd = new SqlCommand(sqlQuery.ToString(), sqlConn)
                                {
                                    CommandType = CommandType.Text
                                })
                                {

                                    sqlCmd.Parameters.Clear();

                                    success = sqlCmd.ExecuteNonQuery() > 0;
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }

                        return success;
                    }
                    catch (SqlException SqlException)
                    {
                        SqlException.LogErrorToTextFile();
                        return success;
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
            return success;
        }

        internal static DateTime? GetClientDataLastUpdatedDateTime(string TableName)
        {
            DateTime? LastSyncDateTime = null;
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select MAX(SyncDateTime) as SyncDateTime from ");
            sqlQuery.Append(TableName);

            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
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

                        if (dataTable.Rows.Count > 0)
                        {
                            if (dataTable.Rows[0]["SyncDateTime"] == DBNull.Value)
                            {
                                LastSyncDateTime = null;
                            }
                            else
                            {
                                LastSyncDateTime = Convert.ToDateTime(dataTable.Rows[0]["SyncDateTime"]);
                            }
                        }

                        return LastSyncDateTime;
                    }
                }
                catch (SqlException SqlException)
                {
                    SqlException.LogErrorToTextFile();
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

        internal static bool UpdateData(string Sproc, DataTable DataTable)
        {
            using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
            {
                try
                {
                    using (
                        SqlCommand sqlCmd = new SqlCommand(Sproc, sqlConn)
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

                        return sqlCmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (SqlException SqlException)
                {
                    SqlException.LogErrorToTextFile();
                    return false;
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

        internal static DataTable GetTableColumns(string TableName)
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.Append("select top 1 * from ");
            sqlQuery.Append(TableName);

            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConn = new SqlConnection(AppConfig.SqlServerConnectionString))
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
                catch (SqlException SqlException)
                {
                    SqlException.LogErrorToTextFile();
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
