# SimpleSync

This project serves the purpose of setting up a data synchronization system.


## NuGet Packages 

It uses Json.NET to (de)serialize data between the endpoints.

## This project contains the following components:
1.  Client: windows service
2.  Server: asp.net webapi

## Set up your own data synchronization system
1.  Extend each 'syncable' table with the following three columns:
    * [IsSynchronized] [bit]
    * [SyncGuid] [uniqueidentifier]
    * [SyncDateTime] [datetime]

    For each ‘syncable’ table create the following:
2.  Trigger (after update trigger)
3.  Table value type for each ‘syncable’ table
4.  Stored procedure(s)
5.  A table named Sync.Tables to manage the tables 
    This table has the following columns:
      - [TableId]           : identity column
	    - [TableName]         : the name of the table (for example [Sale].[Items])
	    - [StoredProcedure]   : the stored procedure name to sync the data, using SQL merge statement
	    - [RowsToSyncPerTime] : how many records to transfer during one trip
	    - [DirectionId]       : direction id ```1``` is client to server, ```2``` is server to client
	    - [IsSyncable]        : (de)activate sync

## How does it work?
1.  The windows service on the client side loops through the tables which are listed in the table Sync.Tables
2.  There are two directions defined namely
      1. client to server: data is synchronized from client to server
      2. server to client: data is synchronized from server to client
    Each table in the Sync.Tables has a sync direction. 
    If the direction is 1 then it pushes data from the client to the server.
    How?
      1. Fetches all the data from the table where the IsSynchronized flag is false, this is how the service (client) knows which data needs to be synchronized.
      2. Packs the table name, the stored procedure name , and the table data in an object. 
      3. Serializes the object to JSON format.
      4. Calls the webapi method (client to server) to post the data to the server.
      5. The server deserializes the JSON data and unpacks the object. 
      6. The received data is stored in a datatable (which has the matching columns of the table value type)
      7. The webapi method (at the server) executes the stored procedure by passing in the datatable to update the data on the server.
      8. The stored procedure updates the data using it merge sql statement. In case the data (all or a part) failed to update on the server then the client will try to send the data again. 
      9. The client receives the SyncGuid with the flags (IsSynchronized). The client then updates its data flags with the response received from the server. 

If the direction is 2 (server to client) then it pulls data from the server to the client.
    How?
      1. the windows service gets the value of [SyncDateTime] of the recently updated record of a particular table. 
      2. the windows service creates a transfer object with this [SyncDateTime] value and post it to the server (calling webapi method: server to client), as JSON. 
      3. the server generates a SQL select statement for the table and fetches all the data from the particular based on the [SyncDateTime] value.
      4. the server serializes the data to JSON and sends it to the client.
      5. the client updates its data using the server's response. 
         Note: the client's data is updated with the server's data inclusive the [SyncDateTime] value. This way the client fetches only the updated data from the server. 
         

