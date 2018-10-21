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

        public SyncService()
        {
            InitializeComponent();

            AppConfig.Configure();

            _timer = new Timer();
            _timer.Interval = AppConfig.TimerInterval;
            _timer.Elapsed += Timer_Elapsed;
        }

        protected override void OnStart(string[] args)
        {
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Stop();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _timer.Stop();
                await Task.Run(new Action(StartSyncServerToClient));
                await Task.Run(new Action(StartSyncClientToServer));
            }
            catch (Exception Exception)
            {
                Exception.LogErrorToTextFile();
            }
            finally
            {
                _timer.Start();
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
                var WebApi = AppConfig.WebApiUrl;
                client.BaseAddress = new Uri(WebApi);

                DataTable request_dataTable = new DataTable();
                request_dataTable = DataAccess.GetTableColumns(request_dto.TableName);

                DateTime? LastSyncDateTime = DataAccess.GetClientDataLastUpdatedDateTime(request_dto.TableName) ?? DateTime.Now.AddYears(-50);

                request_dto.SyncDateTime = Convert.ToDateTime(LastSyncDateTime);

                if (request_dataTable == null)
                {
                    return;
                }

                request_dto.TableData = request_dataTable.ConvertToJson();

                var postTask = client.PostAsJsonAsync(WebApi, request_dto);
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
                    response_dataTable = DataAccess.GetTableColumns(response_dto.TableName);

                    Tuple<List<string>, DataTable> tuple = response_dataTable.AddTempBinaryColumnAsString();

                    response_dataTable = response_dto.TableData
                        .ConvertToDataTable(tuple.Item2)
                        .ConvertStringToByteArray(tuple.Item1)
                        .RemoveExtraSlashes();

                    if (response_dataTable != null && response_dataTable.Rows.Count > 0)
                    {
                        DataAccess.UpdateData(response_dto.StoredProcedure, response_dataTable);
                    }
                }
            }
        }

        private void SyncClientToServer(DataTransferObject request_dto)
        {
            using (var client = new HttpClient())
            {
                var WebApi = AppConfig.WebApiUrl;
                client.BaseAddress = new Uri(WebApi);

                DataTable request_dataTable = new DataTable();
                request_dataTable = DataAccess.GetDataToSync(request_dto.TableName, request_dto.RowsToSyncPerTime);

                if (request_dataTable == null)
                {
                    return;
                }

                var versionedRows = request_dataTable.GetVersionedRows();
                request_dataTable = versionedRows.Item2;
                List<TableRow> VersionedRows = versionedRows.Item1;

                request_dto.TableData = request_dataTable
                    .HandleByteArrayColumns()
                    .RemoveExtraSlashes()
                    .ConvertToJson();

                var postTask = client.PostAsJsonAsync(WebApi, request_dto);
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
                        response_dataTable = DataAccess.GetTableColumns(response_dto.TableName).ExcludeRowVersionColumn();
                        response_dataTable = response_dto.TableData.ConvertToDataTable(response_dataTable);

                        if (response_dataTable != null && response_dataTable.Rows.Count > 0)
                        {
                            DataAccess.FlagSynchronizedRows(response_dto.TableName, response_dataTable.GetVersionedRowsFromServerResponse(VersionedRows));
                        }
                    }
                }
                else if (postTask.Status == TaskStatus.Faulted)
                {
                    foreach (var exception in postTask.Exception.Flatten().InnerExceptions)
                    {
                        exception.LogErrorToTextFile();
                    }
                }
            }
        }
    }
}

