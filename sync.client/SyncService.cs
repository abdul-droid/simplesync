
using Newtonsoft.Json;
using sync.core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;

namespace sync.client
{
    public partial class SyncService : ServiceBase
    {
        private Timer _timer = null;
        JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings();
        List<TableRow> ClientTableRows;
        List<TableRow> TableRowsFromServerResponse;
        List<string> ByteArrayColumns;

        public SyncService()
        {
            InitializeComponent();

            ByteArrayColumns = new List<string>();
            ClientTableRows = new List<TableRow>();
            TableRowsFromServerResponse = new List<TableRow>();
            _jsonSerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;

            AppConfig.Configure();
            _timer = new Timer();
            _timer.Interval = AppConfig.TimerInterval;
            _timer.Elapsed += Timer_Elapsed;
        }

        protected override void OnStart(string[] args)
        {
            _timer.Enabled = true; _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Enabled = false; _timer.Stop();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false; _timer.Stop();
                await Task.Run(new Action(StartSyncServerToClient));
                await Task.Run(new Action(StartSyncClientToServer));
            }
            catch (Exception x)
            {
                AppConfig.LogErrorToTextFile(x);
            }
            finally
            {
                _timer.Enabled = true; _timer.Start();
            }
        }

        private List<DataTransferObject> GetSyncableTables()
        {
            List<DataTransferObject> l = new List<DataTransferObject>();

            DataTable dt = new DataTable();

            dt = DataAccess.GetSyncableTables();

            if (dt != null && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataTransferObject dto = new DataTransferObject();

                    dto.DirectionId = (dt.Rows[i]["DirectionId"] != System.DBNull.Value ? Convert.ToInt32(dt.Rows[i]["DirectionId"]) : 0);
                    dto.TableName = (dt.Rows[i]["TableName"] != System.DBNull.Value ? dt.Rows[i]["TableName"].ToString() : "");
                    dto.RowsToSyncPerTime = (dt.Rows[i]["RowsToSyncPerTime"] != System.DBNull.Value ? Convert.ToInt32(dt.Rows[i]["RowsToSyncPerTime"]) : 0);
                    dto.StoredProcedure = (dt.Rows[i]["StoredProcedure"] != System.DBNull.Value ? dt.Rows[i]["StoredProcedure"].ToString() : "");

                    if (dto.DirectionId == 0 || dto.TableName == "" || dto.RowsToSyncPerTime == 0 || dto.StoredProcedure == "")
                    {
                        continue;
                    }

                    l.Add(dto);
                }

            }
            return l;
        }


        private void StartSyncServerToClient()
        {
            List<DataTransferObject> dtos = GetSyncableTables();
            List<DataTransferObject> dtos_ServerToClient = new List<DataTransferObject>();

            if (dtos.Count > 0)
            {
                dtos_ServerToClient = dtos.FindAll(x => x.DirectionId == 2);
                for (int i = 0; i < dtos_ServerToClient.Count; i++)
                {
                    SyncServerToClient(dtos_ServerToClient[i]);
                }
            }

        }

        private void StartSyncClientToServer()
        {
            List<DataTransferObject> dtos = GetSyncableTables();
            List<DataTransferObject> dtos_ClientToServer = new List<DataTransferObject>();

            if (dtos.Count > 0)
            {
                dtos_ClientToServer = dtos.FindAll(x => x.DirectionId == (int)SyncDirection.ClientToServer);
                for (int i = 0; i < dtos_ClientToServer.Count; i++)
                {
                    SyncClientToServer(dtos_ClientToServer[i]);
                }
            }
        }

        private void SyncServerToClient(DataTransferObject request_dto)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(AppConfig.ClientBaseAddressUri);

                DataTable request_dataTable = new DataTable();
                request_dataTable = DataAccess.GetTableStructure(request_dto.TableName);

                DateTime? LastSyncDateTime = DataAccess.GetClientDataLastUpdatedDateTime(request_dto.TableName);

                if (LastSyncDateTime == null)
                {
                    LastSyncDateTime = DateTime.Now.AddYears(-50);
                }

                request_dto.SyncDateTime = Convert.ToDateTime(LastSyncDateTime);

                if (request_dataTable == null)
                {
                    return;
                }

                request_dto.TableData = JsonConvert.SerializeObject(request_dataTable, _jsonSerializerSettings);

                var postTask = client.PostAsJsonAsync(AppConfig.RequestUri, request_dto);
                postTask.Wait();

                var result = postTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<DataTransferObject>();
                    readTask.Wait();

                    var response_dto = readTask.Result;

                    if (response_dto == null)
                    {
                        return;
                    }

                    DataTable response_dataTable = new DataTable();
                    response_dataTable = DataAccess.GetTableStructure(response_dto.TableName);

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
                    }

                    response_dataTable = response_dto.TableData.ConvertToDataTable(response_dataTable);

                    if (ByteArrayColumns.Count > 0)
                    {
                        byte[] br = null;

                        for (int c = 0; c < ByteArrayColumns.Count; c++)
                        {
                            for (int i = 0; i < response_dataTable.Rows.Count; i++)
                            {
                                br = null;

                                br = Convert.FromBase64String(response_dataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"].ToString());

                                response_dataTable.Rows[i][ByteArrayColumns[c]] = (br.Length == 0) ? null : br;
                            }
                        }

                        foreach (var c in ByteArrayColumns)
                        {
                            for (var i = response_dataTable.Columns.Count - 1; i >= 0; i--)
                            {
                                if ($"Temp{c}" == response_dataTable.Columns[i].ToString())
                                {
                                    response_dataTable.Columns.Remove(response_dataTable.Columns[i]);
                                    break;
                                }
                            }
                        }
                    }

                    if (response_dataTable != null && response_dataTable.Rows.Count > 0)
                    {
                        DataAccess.UpdateClientDataWithDataReceivedFromServer(response_dto.StoredProcedure, response_dataTable.RemoveExtraSlashes());
                    }
                    else
                    {
                    }

                }
                else
                {
                }
            }
        }

        private void SyncClientToServer(DataTransferObject request_dto)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(AppConfig.ClientBaseAddressUri);

                DataTable request_dataTable = new DataTable();
                request_dataTable = DataAccess.GetTableData(request_dto.TableName, request_dto.RowsToSyncPerTime);

                if (request_dataTable == null)
                {
                    return;
                }

                ClientTableRows.Clear();
                bool HasRowVersionColumn = false;

                for (int i = 0; i < request_dataTable.Columns.Count; i++)
                {
                    if (request_dataTable.Columns[i].ColumnName == "RowVersion")
                    {
                        HasRowVersionColumn = true;
                        break;
                    }
                }

                for (int i = 0; i < request_dataTable.Rows.Count; i++)
                {
                    TableRow t = new TableRow();
                    t.SyncGuid = request_dataTable.Rows[i]["SyncGuid"] == DBNull.Value ? string.Empty : request_dataTable.Rows[i]["SyncGuid"].ToString();
                    if (HasRowVersionColumn)
                    {
                        t.RowVersion = request_dataTable.Rows[i]["RowVersion"] == DBNull.Value ? string.Empty : request_dataTable.Rows[i]["RowVersion"].ToString();
                    }
                    else
                    {
                        t.RowVersion = string.Empty;
                    }
                    ClientTableRows.Add(t);
                }

                for (int i = 0; i < request_dataTable.Columns.Count; i++)
                {
                    if (request_dataTable.Columns[i].ColumnName.ToLower() == "rowversion")
                    {
                        request_dataTable.Columns.Remove(request_dataTable.Columns[i]);
                        break;
                    }
                }

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

                    DataTable dt = request_dataTable.Copy();
                    for (int c = 0; c < ByteArrayColumns.Count; c++)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            request_dataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"] = Convert.ToBase64String((dt.Rows[i][ByteArrayColumns[c]] != DBNull.Value)
                                ? (byte[])dt.Rows[i][ByteArrayColumns[c]]
                                : new byte[0]);
                        }
                    }

                    for (int c = 0; c < ByteArrayColumns.Count; c++)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            request_dataTable.Rows[i][ByteArrayColumns[c]] = null;
                        }
                    }
                }

                request_dto.TableData = request_dataTable.ConvertToJson();

                var postTask = client.PostAsJsonAsync<DataTransferObject>(AppConfig.RequestUri, request_dto);
                postTask.Wait();

                if (postTask.Status == TaskStatus.RanToCompletion)
                {
                    var result = postTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var readTask = result.Content.ReadAsAsync<DataTransferObject>();
                        readTask.Wait();

                        var response_dto = readTask.Result;

                        if (response_dto == null)
                        {
                            return;
                        }

                        DataTable response_dataTable = new DataTable();
                        response_dataTable = DataAccess.GetTableStructure(response_dto.TableName);

                        for (int i = 0; i < response_dataTable.Columns.Count; i++)
                        {
                            if (response_dataTable.Columns[i].ColumnName.ToLower() == "rowversion")
                            {
                                response_dataTable.Columns.Remove(response_dataTable.Columns[i]);
                                break;
                            }
                        }

                        dynamic dynObj = JsonConvert.DeserializeObject(response_dto.TableData, _jsonSerializerSettings);

                        if (dynObj == null)
                        {
                            return;
                        }

                        response_dataTable = response_dto.TableData.ConvertToDataTable(response_dataTable);

                        if (response_dataTable != null && response_dataTable.Rows.Count > 0)
                        {
                            TableRowsFromServerResponse.Clear();

                            for (int i = 0; i < response_dataTable.Rows.Count; i++)
                            {
                                TableRow tr = ClientTableRows.Find(t => t.SyncGuid.ToLower() == response_dataTable.Rows[i]["SyncGuid"].ToString().ToLower());

                                if (tr == null)
                                {
                                    continue;
                                }

                                TableRowsFromServerResponse.Add(tr);
                            }

                            if (TableRowsFromServerResponse.Count > 0)
                            {
                                if (HasRowVersionColumn)
                                    DataAccess.FlagSynchronizedRowsUsingRowVersion(response_dto.TableName, TableRowsFromServerResponse);
                                else
                                    DataAccess.FlagSynchronizedRows(response_dto.TableName, TableRowsFromServerResponse);
                            }
                        }
                    }
                }
                else if (postTask.Status == TaskStatus.Faulted)
                {
                    foreach (var exception in postTask.Exception.Flatten().InnerExceptions)
                    {
                        AppConfig.LogErrorToTextFile(exception);
                    }
                }
            }
        }
    }
}

