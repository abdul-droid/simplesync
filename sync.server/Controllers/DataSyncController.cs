using sync.core;
using System.Collections.Generic;
using System.Web.Http;

namespace sync.server.Controller
{
    public class DataSyncController : ApiController
    {
        [Route("simple/data/sync")]
        public IEnumerable<string> Get()
        {
            return new string[] { "If you can see this means your web api is configured properly." };
        }

        [HttpPost]
        [Route("simple/data/sync")]
        public IHttpActionResult Process(DataTransferObject RequestDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            if (RequestDTO.DirectionId == (int)SyncDirection.ClientToServer)
            {
                var tuple = new DataAccess()
                    .GetTableColumns(RequestDTO.TableName)
                    .AddTempBinaryColumnAsString();

                RequestDTO.TableData = new DataAccess()
                    .UpdateServerData(RequestDTO.StoredProcedure,
                        RequestDTO.TableData
                            .ConvertToDataTable(tuple.Item2)
                            .ConvertStringToByteArray(tuple.Item1)
                            .RemoveExtraSlashes())
                    .ConvertToJson();
            }
            else if (RequestDTO.DirectionId == (int)SyncDirection.ServerToClient)
            {
                RequestDTO.TableData = new DataAccess()
                    .GetServerData(RequestDTO.TableName, RequestDTO.SyncDateTime, RequestDTO.RowsToSyncPerTime)
                    .HandleByteArrayColumns()
                    .RemoveExtraSlashes()
                    .ConvertToJson();
            }

            return Ok(RequestDTO);
        }
    }
}