using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace sync.core
{
    public static class Extensions
    {
        public static string ConvertToJson(this DataTable DataTable)
        {
            return JsonConvert.SerializeObject(DataTable.RemoveExtraSlashes(),
                    new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
        }

        public static DataTable ConvertToDataTable(this string Data, DataTable DataTable)
        {
            dynamic o = JsonConvert.DeserializeObject(Data,
                      new JsonSerializerSettings() { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });

            return JsonToDataTable(o, DataTable);
        }

        public static DataTable HandleByteArrayColumns(this DataTable DataTable)
        {
            var tuple = DataTable.AddTempBinaryColumnAsString();
            List<string> ByteArrayColumns = tuple.Item1;
            DataTable dataTable = tuple.Item2;

            if (ByteArrayColumns.Count > 0)
            {
                DataTable = dataTable.ConvertByteArrayToString(ByteArrayColumns);
            }

            return DataTable;
        }

        public static DataTable ConvertStringToByteArray(this DataTable DataTable, List<string> ByteArrayColumns)
        {
            byte[] br = null;
            for (int c = 0; c < ByteArrayColumns.Count; c++)
            {
                for (int i = 0; i < DataTable.Rows.Count; i++)
                {
                    br = null;

                    br = Convert.FromBase64String(DataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"].ToString());

                    DataTable.Rows[i][ByteArrayColumns[c]] = (br.Length == 0) ? null : br;
                }
            }

            foreach (var c in ByteArrayColumns)
            {
                for (var i = DataTable.Columns.Count - 1; i >= 0; i--)
                {
                    if ($"Temp{c}" == DataTable.Columns[i].ToString())
                    {
                        DataTable.Columns.Remove(DataTable.Columns[i]);
                        break;
                    }
                }
            }

            return DataTable;
        }

        private static DataTable ConvertByteArrayToString(this DataTable DataTable, List<string> ByteArrayColumns)
        {
            DataTable dt = DataTable.Copy();

            for (int c = 0; c < ByteArrayColumns.Count; c++)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataTable.Rows[i][$"Temp{ByteArrayColumns[c]}"] = Convert.ToBase64String((dt.Rows[i][ByteArrayColumns[c]] != DBNull.Value)
                        ? (byte[])dt.Rows[i][ByteArrayColumns[c]]
                        : new byte[0]);
                }
            }

            for (int c = 0; c < ByteArrayColumns.Count; c++)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataTable.Rows[i][ByteArrayColumns[c]] = null;
                }
            }

            return DataTable;
        }

        public static Tuple<List<string>, DataTable> AddTempBinaryColumnAsString(this DataTable DataTable)
        {
            List<string> ByteArrayColumns = new List<string>();

            ByteArrayColumns.Clear();

            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                if (DataTable.Columns[i].DataType.ToString().ToLower() == "system.byte[]")
                {
                    ByteArrayColumns.Add(DataTable.Columns[i].ColumnName);
                }
            }

            if (ByteArrayColumns.Count > 0)
            {
                for (int i = 0; i < ByteArrayColumns.Count; i++)
                {
                    DataTable.Columns.Add($"Temp{ByteArrayColumns[i]}", typeof(string));
                }
            }

            return new Tuple<List<string>, DataTable>(ByteArrayColumns, DataTable);
        }

        private static DataTable JsonToDataTable(dynamic DeserializedObject, DataTable DataTable)
        {
            foreach (var record in DeserializedObject)
            {
                string cou1 = Convert.ToString(record);
                string[] RowData = Regex.Split(cou1.Replace
                ("{", "").Replace("}", ""), ",");
                DataRow nr = DataTable.NewRow();
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
                            for (int j = 0; j < DataTable.Columns.Count; j++)
                            {
                                if (DataTable.Columns[j].ColumnName == RowColumns)
                                {
                                    colIndex = j;
                                    break;
                                }
                            }
                            if (colIndex > 0 && !String.IsNullOrEmpty(RowColumns))
                            {
                                if (DataTable.Columns[colIndex].DataType.FullName == "System.Decimal"
                                    || DataTable.Columns[colIndex].DataType.FullName == "System.Double")
                                {
                                    nr[RowColumns] = "0.00";
                                }
                                else if (DataTable.Columns[colIndex].DataType.FullName == "System.Int32"
                                    || DataTable.Columns[colIndex].DataType.FullName == "System.Int64"
                                    || DataTable.Columns[colIndex].DataType.FullName == "System.Int16")
                                {
                                    nr[RowColumns] = "0";
                                }
                                else if (DataTable.Columns[colIndex].DataType.FullName == "System.String"
                                    || DataTable.Columns[colIndex].DataType.FullName == "System.Char")
                                {
                                    nr[RowColumns] = "";
                                }
                                else if (DataTable.Columns[colIndex].DataType.FullName == "System.Boolean")
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
                DataTable.Rows.Add(nr);
            }

            return DataTable;
        }

        public static DataTable RemoveExtraSlashes(this DataTable DataTable)
        {
            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                if (DataTable.Columns[i].DataType == Type.GetType("System.String")
                    || DataTable.Columns[i].DataType == Type.GetType("System.Char"))
                {
                    for (int j = 0; j < DataTable.Rows.Count; j++)
                    {
                        if (DataTable.Rows[j][DataTable.Columns[i].ColumnName] != DBNull.Value)
                        {
                            DataTable.Rows[j][DataTable.Columns[i].ColumnName] = DataTable.Rows[j][DataTable.Columns[i].ColumnName].ToString().Trim().RemoveSlashes();
                        }
                    }
                }
            }

            return DataTable;
        }

        private static string RemoveSlashes(this string stringValue)
        {
            if (stringValue.IsHtml())
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

        private static bool IsHtml(this string stringValue)
        {
            if (stringValue.Length > 0)
            {
                char firstChar = stringValue.Trim()[0];
                char lastChar = stringValue.Trim()[stringValue.Trim().Length - 1];

                return (firstChar == '<' && lastChar == '>');
            }

            return false;
        }
    }
}
