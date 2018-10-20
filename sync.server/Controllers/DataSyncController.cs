using Newtonsoft.Json;
using sync.server.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace sync.server.Controller
{
    public class DataSyncController : ApiController
    {
        // GET api/<controller>
        [Route("unity/data/sync")]
        public IEnumerable<string> Get()
        {
            return new string[] { "Test", "Unity", "Data", "Sync", "If you can see this means your web api is configured properly." };
        }

        [HttpPost]
        [Route("unity/data/sync")]
        public IHttpActionResult Process(DataTransferObject request_dto)
        {
            List<string> ByteArrayColumns = new List<string>();

            JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;

            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            if (request_dto.DirectionId == 1) //client to server
            {
                DataAccess d = new DataAccess();
                DataTable request_dataTable = new DataTable(request_dto.TableName);
                DataTable response_dataTable = new DataTable();

                request_dataTable = d.GetTableStructure(request_dto.TableName);

                ByteArrayColumns.Clear();

                for (int i = 0; i < request_dataTable.Columns.Count; i++)
                {
                    if (request_dataTable.Columns[i].DataType.ToString().ToLower() == "system.byte[]")
                    {
                        ByteArrayColumns.Add(request_dataTable.Columns[i].ColumnName);
                    }
                }

                if (ByteArrayColumns.Count > 0)
                {
                    for (int i = 0; i < ByteArrayColumns.Count; i++)
                    {
                        request_dataTable.Columns.Add($"Temp{ByteArrayColumns[i]}", typeof(string));
                    }
                }

                dynamic dynObj = JsonConvert.DeserializeObject(request_dto.TableData, _jsonSerializerSettings);

                request_dataTable = JSONToDataTable(dynObj, request_dataTable);

                if (ByteArrayColumns.Count > 0)
                {
                    byte[] br = null;
                    for (int c = 0; c < ByteArrayColumns.Count; c++)
                    {
                        for (int i = 0; i < request_dataTable.Rows.Count; i++)
                        {
                            br = null;

                            br = Convert.FromBase64String(request_dataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"].ToString());

                            request_dataTable.Rows[i][ByteArrayColumns[c]] = (br.Length == 0) ? null : br;
                        }
                    }

                    foreach (var c in ByteArrayColumns)
                    {
                        for (var i = request_dataTable.Columns.Count - 1; i >= 0; i--)
                        {
                            if ($"Temp{c}" == request_dataTable.Columns[i].ToString())
                            {
                                request_dataTable.Columns.Remove(request_dataTable.Columns[i]);
                                break;
                            }
                        }
                    }
                }

                for (int i = 0; i < request_dataTable.Columns.Count; i++)
                {
                    if (request_dataTable.Columns[i].DataType == System.Type.GetType("System.String")
                        || request_dataTable.Columns[i].DataType == System.Type.GetType("System.Char"))
                    {
                        for (int j = 0; j < request_dataTable.Rows.Count; j++)
                        {
                            if (request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName] != DBNull.Value)
                            {
                                request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName] = ClearSlashes(request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName].ToString().Trim());
                            }
                        }
                    }
                }

                response_dataTable = d.SyncDataFromClientToServer(request_dto.StoredProcedure, request_dataTable);

                for (int i = 0; i < response_dataTable.Columns.Count; i++)
                {
                    if (response_dataTable.Columns[i].DataType == System.Type.GetType("System.String")
                        || response_dataTable.Columns[i].DataType == System.Type.GetType("System.Char"))
                    {
                        for (int j = 0; j < response_dataTable.Rows.Count; j++)
                        {
                            if (response_dataTable.Rows[j][response_dataTable.Columns[i].ColumnName] != DBNull.Value)
                            {
                                response_dataTable.Rows[j][response_dataTable.Columns[i].ColumnName] = response_dataTable.Rows[j][response_dataTable.Columns[i].ColumnName].ToString().Trim();
                            }
                        }
                    }
                }

                request_dto.TableData = JsonConvert.SerializeObject(response_dataTable, _jsonSerializerSettings);
            }
            else if (request_dto.DirectionId == 2) //server to client
            {
                DataAccess d = new DataAccess();
                DataTable request_dataTable = new DataTable(request_dto.TableName);
                DataTable response_dataTable = new DataTable();

                response_dataTable = d.GetServerTableData(request_dto.TableName, request_dto.SyncDateTime, request_dto.RowsToSyncPerTime);

                if (response_dataTable == null)
                {
                    for (int i = 0; i < request_dataTable.Columns.Count; i++)
                    {
                        if (request_dataTable.Columns[i].DataType == System.Type.GetType("System.String")
                            || request_dataTable.Columns[i].DataType == System.Type.GetType("System.Char"))
                        {
                            for (int j = 0; j < request_dataTable.Rows.Count; j++)
                            {
                                if (request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName] != DBNull.Value)
                                {
                                    request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName] = ClearSlashes(request_dataTable.Rows[j][request_dataTable.Columns[i].ColumnName].ToString().Trim());
                                }
                            }
                        }
                    }
                }

                ByteArrayColumns.Clear();

                for (int i = 0; i < response_dataTable.Columns.Count; i++)
                {
                    if (response_dataTable.Columns[i].DataType.ToString().ToLower() == "system.byte[]")
                    {
                        ByteArrayColumns.Add(response_dataTable.Columns[i].ColumnName);
                    }
                }

                if (ByteArrayColumns.Count > 0)
                {
                    for (int i = 0; i < ByteArrayColumns.Count; i++)
                    {
                        response_dataTable.Columns.Add($"Temp{ByteArrayColumns[i]}", typeof(string));
                    }

                    DataTable dt = response_dataTable.Copy();
                    for (int c = 0; c < ByteArrayColumns.Count; c++)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            response_dataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"] = Convert.ToBase64String((dt.Rows[i][ByteArrayColumns[c]] != DBNull.Value)
                                ? (byte[])dt.Rows[i][ByteArrayColumns[c]]
                                : new byte[0]);
                        }
                    }

                    for (int c = 0; c < ByteArrayColumns.Count; c++)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            response_dataTable.Rows[i][ByteArrayColumns[c]] = null;
                        }
                    }
                }

                request_dto.TableData = JsonConvert.SerializeObject(response_dataTable, _jsonSerializerSettings);
            }

            return Ok(request_dto);
        }

        private DataTable JSONToDataTable(dynamic dynObj, DataTable dt_source)
        {

            foreach (var record in dynObj)
            {
                string cou1 = Convert.ToString(record);
                string[] RowData = Regex.Split(cou1.Replace
                ("{", "").Replace("}", ""), ",");
                DataRow nr = dt_source.NewRow();
                string RowDataString = String.Empty;
                int idx = -1;
                string RowColumns = String.Empty;
                int colIndex = -1;

                foreach (string rowData in RowData)
                {
                    try
                    {
                        RowDataString = String.Empty;
                        RowColumns = String.Empty;
                        idx = -1;
                        colIndex = -1;

                        idx = rowData.IndexOf(":");
                        RowColumns = rowData.Substring
                        (0, idx - 1).Replace("\"", "").Trim();
                        RowDataString = rowData.Substring
                        (idx + 1).Replace("\"", "");
                        if (RowDataString.Contains(@"\"))
                        {
                            nr[RowColumns] = RowDataString.Replace(@"\\", @"\").Trim();
                        }
                        else
                        {
                            nr[RowColumns] = RowDataString.Trim();
                        }

                        if (nr[RowColumns].ToString().ToLower() == @"null")
                        {
                            nr[RowColumns] = String.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!String.IsNullOrEmpty(RowDataString) && RowDataString.ToLower().Contains("null"))
                        {
                            for (int j = 0; j < dt_source.Columns.Count; j++)
                            {
                                if (dt_source.Columns[j].ColumnName == RowColumns)
                                {
                                    colIndex = j;
                                    break;
                                }
                            }
                            if (colIndex > 0 && !String.IsNullOrEmpty(RowColumns))
                            {
                                if (dt_source.Columns[colIndex].DataType.FullName == "System.Decimal"
                                    || dt_source.Columns[colIndex].DataType.FullName == "System.Double")
                                {
                                    nr[RowColumns] = "0.00";
                                }
                                else if (dt_source.Columns[colIndex].DataType.FullName == "System.Int32"
                                    || dt_source.Columns[colIndex].DataType.FullName == "System.Int64"
                                    || dt_source.Columns[colIndex].DataType.FullName == "System.Int16")
                                {
                                    nr[RowColumns] = "0";
                                }
                                else if (dt_source.Columns[colIndex].DataType.FullName == "System.String"
                                    || dt_source.Columns[colIndex].DataType.FullName == "System.Char")
                                {
                                    nr[RowColumns] = "";
                                }
                                else if (dt_source.Columns[colIndex].DataType.FullName == "System.Boolean")
                                {
                                    nr[RowColumns] = "false";
                                }

                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                dt_source.Rows.Add(nr);
            }

            return dt_source;
        }

        private string ClearSlashes(string stringValue)
        {
            if (IsHtml(stringValue))
            {
                if (stringValue.Contains(@"\n"))
                {
                    stringValue = stringValue.Replace(@"\n", string.Empty);
                }
                if (stringValue.Contains(@"\t"))
                {
                    stringValue = stringValue.Replace(@"\t", string.Empty);
                }
                if (stringValue.Contains(@"\r"))
                {
                    stringValue = stringValue.Replace(@"\r", string.Empty);
                }

                //should be in last
                if (stringValue.Contains(@"=\"))
                {
                    stringValue = stringValue.Replace(@"\", "\"");
                }
            }
            else if (stringValue.Contains(@"\\"))
            {
                stringValue = stringValue.Replace(@"\\", @"\");
            }

            return stringValue;
        }

        private bool IsHtml(string rowDataString)
        {
            if (rowDataString.Length > 0)
            {
                char firstChar = rowDataString.Trim()[0];
                char lastChar = rowDataString.Trim()[rowDataString.Trim().Length - 1];

                return (firstChar == '<' && lastChar == '>');
            }

            return false;
        }
    }
}