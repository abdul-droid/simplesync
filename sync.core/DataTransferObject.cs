using System;

namespace sync.core
{
    public class DataTransferObject
    {
        public int DirectionId { get; set; }
        public string TableName { get; set; }
        public string StoredProcedure { get; set; }
        public string TableData { get; set; }
        public DateTime SyncDateTime { get; set; }
        public int RowsToSyncPerTime { get; set; }

    }
}