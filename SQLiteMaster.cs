using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Data.SQLite;
using System.Data.SQLite.Generic;
using System.Reflection;
using CxAPI_Store.dto;
using static CxAPI_Store.CxConstant;
using System.Linq;

namespace CxAPI_Store
{

    public class SQLiteMaster : IDisposable
    {
        #region Instance Variables
        private SQLiteConnection _connection;
        private resultClass token;


        public SQLiteConnection Connection
        {
            get { return _connection; }
            set { _connection = value; }
        }

        private SQLiteTransaction _transaction;
        public SQLiteTransaction Transaction
        {
            get { return _transaction; }
            set { _transaction = value; }
        }

        #endregion

        #region Constructor
        public SQLiteMaster(resultClass token)
        {
            string baseConnection;
            this.token = token;
            if ((token.test) && (token.api_action == api_action.generateReports))
            {
                Console.WriteLine("WARNING: Using test database.");
                baseConnection = String.Format("Data Source={0}{1}sqlite-tools{1}{2};Version=3;", token.exe_directory, token.os_path, TestDB);
            }
            else
            {
                baseConnection = token.sqlite_connection;
                baseConnection = (token.db_allow_write) ? baseConnection : baseConnection + "Read Only=True;";
                baseConnection = (token.initialize) ? baseConnection + "New=True;" : baseConnection;
            }
            _connection = new SQLiteConnection(baseConnection);
            _connection.Open();
            var cmd = new SQLiteCommand("SELECT SQLITE_VERSION()", _connection);
            Console.WriteLine("SQLite version: {0}", cmd.ExecuteScalar());
        }
        #endregion

        #region Instance Methods

        public void InsertParametersDataSetToSQLite(DataTable table, int blocksize = 500)
        {
            bool finished = false;
            int count = 0;
            while (!finished)
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connection))
                {
                    SQLiteTransaction localTrans = connection.BeginTransaction();
                    try
                    {
                        for (; count < table.Rows.Count; count++)
                        {
                            finished = true;
                            SQLiteCommand command = CreateParameterInsertCommand(table.Rows[count]);
                            command.Connection = connection;
                            command.Transaction = localTrans;
                            command.CommandType = System.Data.CommandType.Text;
                            command.ExecuteScalar();
                            if (count > 0 && (count % blocksize) == 0)
                            {
                                count++;
                                finished = false;
                                break;
                            }
                        }
                        localTrans.Commit();
                    }
                    catch (Exception ex)
                    {
                        localTrans.Rollback();
                        Console.WriteLine("Error on insert {0}", ex.Message);
                        Console.WriteLine("Failed commit to table {0}", table.TableName);
                    }

                }
            }
            Console.WriteLine();
            Console.WriteLine("{0} records are written to table {1}", count, table.TableName);
        }
        public DataTable InitializeDataSet(object mapObject, string tableName, List<string> primaryKeys)
        {
            return InitDataTable(mapObject, tableName, primaryKeys);
        }

        public void InsertUpdateFromDataTable(DataTable table)
        {
            object status = null;
            SQLiteTransaction localTrans = null;
            var SQL = BuiIdSimpleSelectSQL(table);
            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                try
                {
                    localTrans = connection.BeginTransaction();
                    SQLiteDataAdapter oAdapter = new SQLiteDataAdapter(SQL, connection);
                    SQLiteCommandBuilder oBuilder = new SQLiteCommandBuilder(oAdapter);
                    oAdapter.UpdateCommand = oBuilder.GetUpdateCommand();
                    oAdapter.InsertCommand = oBuilder.GetInsertCommand();
                    oAdapter.DeleteCommand = oBuilder.GetDeleteCommand();
                    //myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    status = oAdapter.Update(table);
                    localTrans.Commit();
                }
                catch (Exception ex)
                {
                    localTrans.Rollback();
                    Console.WriteLine("Error on update {0}", ex.Message);
                    Console.WriteLine("Failed commit to table {0}", table.TableName);
                }
            }
            if (token.debug && token.verbosity > 0) Console.WriteLine("\n{0} records exist in table {1}", GetRowCount(table.TableName), table.TableName);
        }

        public SQLiteDataAdapter SelectSQLWithParameters(DataTable table, string where = null)
        {
            var SQL = BuiIdSimpleSelectSQL(table, where);
            if (TestIfTableExists(table.TableName))
            {
                SQLiteDataAdapter oAdapter = new SQLiteDataAdapter(SQL, _connection);
                SQLiteCommandBuilder oBuilder = new SQLiteCommandBuilder(oAdapter);
                return oAdapter;
            }
            return null;
        }

        public DataTable SelectIntoDataTable(DataTable table, string where = null)
        {
            var SQL = BuiIdSimpleSelectSQL(table, where);
            if (TestIfTableExists(table.TableName))
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connection))
                {
                    SQLiteDataAdapter myAdapter = new SQLiteDataAdapter(SQL, connection);
                    //myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    myAdapter.Fill(table);
                }
            }
            if (token.debug && token.verbosity > 0) Console.WriteLine("{0} records returned {1}", table.Rows.Count, table.TableName);
            return table;
        }
        public DataTable SelectIntoDataTable(DataTable table, string tableName, string where = null)
        {
            var SQL = BuiIdSimpleSelectSQL(table, where);
            DataTable local = new DataTable(tableName);

            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                SQLiteDataAdapter myAdapter = new SQLiteDataAdapter(SQL, connection);
                //myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                myAdapter.Fill(local);
            }

            if (token.debug && token.verbosity > 0) Console.WriteLine("{0} records returned {1}", local.Rows.Count, local.TableName);
            return local;
        }

        public DataTable SelectIntoDataTable(string tableName, string sql = null)
        {
            DataTable local = new DataTable(tableName);

            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                SQLiteDataAdapter myAdapter = new SQLiteDataAdapter(sql, connection);
                //myAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                myAdapter.Fill(local);
            }

            if (token.debug && token.verbosity > 0) Console.WriteLine("{0} records returned {1}", local.Rows.Count, local.TableName);
            return local;
        }

        public bool TestIfTableExists(string tableName)
        {
            var sql = String.Format("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = '{0}'", tableName);
            SQLiteCommand cmd;
            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                cmd = new SQLiteCommand(sql, connection);
                long status = (long)cmd.ExecuteScalar();
                return (status > 0);
            }
        }

        public long GetRowCount(string tableName)
        {
            var sql = String.Format("SELECT count(*) FROM {0}", tableName);
            SQLiteCommand cmd;
            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                cmd = new SQLiteCommand(sql, connection);
                return (long)cmd.ExecuteScalar();
            }
        }

        public bool TestforPrimaryKeys(string tableName, List<KeyValuePair<string, object>> kvp)
        {
            StringBuilder sql = new StringBuilder(String.Format("SELECT count(*) FROM {0} where 1=1 ", tableName));
            foreach (KeyValuePair<string, object> kv in kvp)
            {
                if (kv.Value.GetType() == typeof(string) || kv.Value.GetType() == typeof(DateTime))
                    sql.Append(String.Format(" and {0} = '{1}'", kv.Key, kv.Value));
                else
                    sql.Append(String.Format(" and {0} = {1}", kv.Key, kv.Value));
            }
            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                var cmd = new SQLiteCommand(sql.ToString(), connection);
                return ((long)cmd.ExecuteScalar() == 0);
            }
        }
        public void InsertDataSetToSQLite(DataTable table, int blocksize = 500)
        {

            StringBuilder sql = new StringBuilder(BuildInsertSQL(table));
            List<string> values = new List<string>();
            sql.Append(" VALUES ");
            int block = 0;
            foreach (DataRow dr in table.Rows)
            {
                values.Add(CreateInsertCommand(dr));
                block++;
                if (block > blocksize)
                {
                    AddTerminationAndCommit(sql.ToString(), values);
                    block = 0;
                    values.Clear();
                }
            }
            AddTerminationAndCommit(sql.ToString(), values);
        }

        public void InsertObjectListToSQLite(string tableName, List<object> mapObjects, int blocksize = 500)
        {
            StringBuilder sql = new StringBuilder(BuildInsertObjectSQL(tableName, mapObjects[0]));
            List<string> values = new List<string>();
            sql.Append(" VALUES ");
            int block = 0;
            foreach (object mapObject in mapObjects)
            {
                values.Add(CreateInsertObjectCommand(mapObject));
                block++;
                if (block >= blocksize)
                {
                    block = 0;
                    AddTerminationAndCommit(sql.ToString(), values);
                    values.Clear();
                }
            }
            AddTerminationAndCommit(sql.ToString(), values);
        }

        private void AddTerminationAndCommit(string sql, List<string> values)
        {
            if (values.Count == 0) return;
            var sqlValue = String.Format("{0} {1}", sql, String.Join(',', values.ToArray()));
            CommitToSQLite(sqlValue);
            return;
        }
        public bool CommitToSQLite(string sql, bool abort = true)
        {

            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                SQLiteTransaction localTrans = connection.BeginTransaction();
                var cmd = new SQLiteCommand(sql, connection, localTrans);

                try
                {
                    int count = cmd.ExecuteNonQuery();
                    localTrans.Commit();
                    Console.WriteLine("\n{0} records are written to database", count);
                    return true;
                }
                catch (Exception ex)
                {
                    localTrans.Rollback();
                    Console.WriteLine(ex.ToString());
                    Console.Write(sql);
                    Console.WriteLine("\nDatabase updated failed.");
                    if (abort)
                        throw new ConstraintException();
                    return false;
                }
            }
        }
        public int DropTable(string tableName)
        {
            string sql = "DROP TABLE IF EXISTS " + tableName;
            SQLiteCommand cmd;

            if (_transaction != null && _transaction.Connection != null)
                cmd = new SQLiteCommand(sql, _connection, _transaction);
            else
                cmd = new SQLiteCommand(sql, _connection);

            int status = cmd.ExecuteNonQuery();
            if (token.debug && token.verbosity > 1) Console.WriteLine("{0}:{1} table dropped", tableName, status);
            return status;
        }
        public object CreateFromDataTable(DataTable table)
        {
            DropTable(table.TableName);
            string sql = GetCreateFromDataTableSQL(table.TableName, table);

            SQLiteCommand cmd;
            if (_transaction != null && _transaction.Connection != null)
                cmd = new SQLiteCommand(sql, _connection, _transaction);
            else
                cmd = new SQLiteCommand(sql, _connection);
            int status = cmd.ExecuteNonQuery();
            if (token.debug && token.verbosity > 1) Console.WriteLine("{0}:{1} table created", table.TableName, status);
            return status;

        }

        public string GetCreateFromDataTableSQL(string tableName, DataTable table)
        {
            string sql = "CREATE TABLE " + tableName + " (" + Environment.NewLine;
            // columns
            foreach (DataColumn column in table.Columns)
            {
                sql += column.ColumnName + " " + SQLGetType(column) + ",\n";
            }
            //sql = sql.TrimEnd(new char[] { ',', '\n' }) + "\n";
            // primary keys
            if (table.PrimaryKey.Length > 0)
            {
                sql += "PRIMARY KEY (";
                foreach (DataColumn column in table.PrimaryKey)
                {
                    sql += column.ColumnName + ",";
                }
                sql = sql.TrimEnd(new char[] { ',', '\n' }) + "))" + Environment.NewLine;
            }

            return sql;
        }

        // Return T-SQL data type definition, based on schema definition for a column
        public string SQLGetType(object type, int columnSize, int numericPrecision, int numericScale)
        {
            switch (type.ToString())
            {
                case "System.String":
                    return "TEXT";

                case "System.Decimal":
                    if (numericScale > 0)
                        return "REAL";
                    else
                        return "INTEGER";

                case "System.Double":
                case "System.Single":
                    return "REAL";

                case "System.Int64":
                    return "INTEGER";

                case "System.Int16":
                case "System.Int32":
                    return "INTEGER";

                case "System.DateTime":
                    return "TEXT";

                case "System.Boolean":
                    return "INTEGER";

                case "System.Byte":
                    return "INTEGER";

                case "System.Guid":
                    return "BLOB";

                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }
        public string SQLSelectType(object type)
        {
            switch (type.ToString())
            {
                case "System.String":
                    return "";

                case "System.Decimal":
                    return "(decimal)";

                case "System.Double":
                    return "(double)";
                case "System.Single":
                    return "(int)";

                case "System.Int64":
                    return "(long)";

                case "System.Int16":
                case "System.Int32":
                    return "(int)";

                case "System.DateTime":
                    return "(datetime)";

                case "System.Boolean":
                    return "(bool)";

                case "System.Byte":
                    return "(short)";

                case "System.Guid":
                    return "(guid)";

                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }

        public string SQLInsertType(object type)
        {
            switch (type.ToString())
            {
                case "System.String":
                    return "TEXT";

                case "System.Decimal":
                case "System.Double":
                case "System.Single":
                    return "REAL";

                case "System.Byte":
                case "System.Boolean":
                case "System.Int64":
                case "System.Int16":
                case "System.Int32":
                    return "INTEGER";

                case "System.DateTime":
                    return "DATETIME";

                case "System.Guid":
                    return "BLOB";

                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }

        // Overload based on row from schema table
        public string SQLGetType(DataRow schemaRow)
        {
            return SQLGetType(schemaRow["DataType"],
                                int.Parse(schemaRow["ColumnSize"].ToString()),
                                int.Parse(schemaRow["NumericPrecision"].ToString()),
                                int.Parse(schemaRow["NumericScale"].ToString()));
        }
        // Overload based on DataColumn from DataTable type
        public string SQLGetType(DataColumn column)
        {
            return SQLGetType(column.DataType, column.MaxLength, 10, 2);
        }
        public string SQLGetType(object type)
        {
            return SQLGetType(type, 1024, 10, 2);
        }
        #endregion

        public string BuildInsertSQL(DataTable table)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO " + table.TableName + " (");
            bool bFirst = true;

            foreach (DataColumn column in table.Columns)
            {
                if (bFirst)
                    bFirst = false;
                else
                {
                    sql.Append(", ");
                }

                sql.Append(column.ColumnName);
            }
            sql.Append(")" + Environment.NewLine);
            return sql.ToString(); ;
        }
        public string BuildInsertObjectSQL(string tableName, object mapObject)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO " + tableName + " (");
            bool bFirst = true;
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                string name = property.Name;
                if (bFirst)
                    bFirst = false;
                else
                {
                    sql.Append(", ");
                }

                sql.Append(name);
            }
            sql.Append(")" + Environment.NewLine);
            return sql.ToString(); ;
        }
        public string BuiIdSelectSQL(DataTable table, string where)
        {
            StringBuilder sql = new StringBuilder("Select ");
            bool bFirst = true;

            foreach (DataColumn column in table.Columns)
            {
                if (bFirst)
                    bFirst = false;
                else
                {
                    sql.Append(", ");
                }
                var type = SQLSelectType(column.DataType);
                sql.Append(String.Format("{0}{1} as '{2}' ", type, column.ColumnName, column.ColumnName));
            }
            sql.Append(String.Format(" from {0}", table.TableName));
            if (!String.IsNullOrEmpty(where)) sql.Append(String.Format(" where 1=1 {0}", where));
            return sql.ToString();
        }

        public string BuiIdSimpleSelectSQL(DataTable table, string where = null)
        {
            StringBuilder sql = new StringBuilder("Select ");
            bool bFirst = true;

            foreach (DataColumn column in table.Columns)
            {
                if (bFirst)
                    bFirst = false;
                else
                {
                    sql.Append(", ");
                }
                sql.Append(column.ColumnName);
            }
            sql.Append(String.Format(" from {0}", table.TableName));
            if (!String.IsNullOrEmpty(where)) sql.Append(String.Format(" where 1=1 {0}", where));
            return sql.ToString();
        }

        public string CreateInsertObjectCommand(object mapObject)
        {
            StringBuilder sql = new StringBuilder(Environment.NewLine + "(");

            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                var value = property.GetValue(mapObject, null);
                string type = SQLInsertType(property.PropertyType);
                if (type.Contains("TEXT") || type.Contains("BLOB"))
                {
                    sql.Append("'" + EscapeText(value) + "',");
                }
                else if (type.Contains("DATETIME"))
                {
                    sql.Append(String.Format("'{0:yyyy-MM-dd HH:mm:ss}',", (DateTime)value));
                }
                else
                {
                    sql.Append(value + ",");
                }
            }
            string clip = sql.ToString();
            clip = clip.TrimEnd(new char[] { ',' }) + ")";
            return clip;
        }


        // Creates a SQLiteCommand for inserting a DataRow
        public string CreateInsertCommand(DataRow row)
        {
            DataTable table = row.Table;
            StringBuilder sql = new StringBuilder(Environment.NewLine + "(");

            foreach (DataColumn column in table.Columns)
            {
                string value = row[column.ColumnName].ToString();
                string type = SQLInsertType(column.DataType);
                if (type.Contains("TEXT") || type.Contains("BLOB"))
                {
                    sql.Append("'" + EscapeText(value) + "',");
                }
                else if (type.Contains("DATETIME"))
                {
                    sql.Append("'" + DateTime.Parse(value).ToString("yyyy-MM-dd HH:mm:ss") + "',");
                }
                else
                {
                    sql.Append(value + ",");
                }
            }
            string clip = sql.ToString();
            clip = clip.TrimEnd(new char[] { ',' }) + "),";
            return clip;
        }

        public class GenerateSQL
        {
            // Returns a string containing all the fields in the table

            public static string BuildAllFieldsSQL(DataTable table)
            {
                string sql = "";
                foreach (DataColumn column in table.Columns)
                {
                    if (sql.Length > 0)
                        sql += ", ";
                    sql += column.ColumnName;
                }
                return sql;
            }
        }
        // Returns a SQL INSERT command. Assumes autoincrement is identity (optional)

        public string BuildParameterInsertSQL(DataTable table)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO " + table.TableName + " (");
            StringBuilder values = new StringBuilder("VALUES (");
            bool bFirst = true;

            foreach (DataColumn column in table.Columns)
            {
                if (bFirst)
                    bFirst = false;
                else
                {
                    sql.Append(", ");
                    values.Append(", ");
                }
                sql.Append(column.ColumnName);
                values.Append("@");
                values.Append(column.ColumnName);
            }

            sql.Append(") ");
            sql.Append(values.ToString());
            sql.Append(")");

            return sql.ToString();
        }

        // Creates a SQLiteParameter and adds it to the command

        private void InsertParameter(SQLiteCommand command,
                                             string parameterName,
                                             string sourceColumn,
                                             object value)
        {
            SQLiteParameter parameter = new SQLiteParameter(parameterName, value);

            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = parameterName;
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = DataRowVersion.Current;

            command.Parameters.Add(parameter);
        }

        // Creates a SQLiteCommand for inserting a DataRow
        private SQLiteCommand CreateParameterInsertCommand(DataRow row)
        {
            DataTable table = row.Table;
            string sql = BuildParameterInsertSQL(table);
            SQLiteCommand command = new SQLiteCommand(sql);
            command.CommandType = System.Data.CommandType.Text;

            foreach (DataColumn column in table.Columns)
            {
                if (!column.AutoIncrement)
                {
                    string parameterName = "@" + column.ColumnName;
                    InsertParameter(command, parameterName,
                                      column.ColumnName,
                                      row[column.ColumnName]);
                }
            }
            return command;
        }

        // Inserts the DataRow for the connection, returning the identity
        private object InsertDataRow(DataRow row, SQLiteTransaction trans)
        {
            SQLiteCommand command = CreateParameterInsertCommand(row);

            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                command.Connection = connection;
                command.Transaction = trans;
                command.CommandType = System.Data.CommandType.Text;
                return command.ExecuteScalar();
            }
        }


        private DataTable InitDataTable(object mapObject, string tableName, List<string> primaryKeys)
        {
            DataTable table = new DataTable(tableName);
            List<DataColumn> cols = new List<DataColumn>();
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                var prop = property.PropertyType;
                table.Columns.Add(name, prop);
                if (primaryKeys.Contains(name))
                {
                    cols.Add(table.Columns[name]);
                }
            }
            table.PrimaryKey = cols.ToArray();
            return table;
        }
        public DataSet InitAllDataTables()
        {
            DataSet ds = new DataSet();
            ds.Tables.Add(InitDataTable(new CxProject(), ProjectTable, new List<string>() { "ProjectId" }));
            ds.Tables.Add(InitDataTable(new CxScan(), ScanTable, new List<string>() { "ProjectId", "ScanId" }));
            ds.Tables.Add(InitDataTable(new CxResult(), ResultTable, new List<string>() { "ProjectId", "VulnerabilityId", "SimilarityId", "FileNameHash" }));
            ds.Tables.Add(InitDataTable(new CxMetaData(), MetaTable, new List<string>() { "FileName" }));

            return ds;
        }
        public DataSet InitJustPrimaryKeys()
        {
            DataSet ds = new DataSet();
            ds.Tables.Add(InitSimpleDataTable(new CxProject(), ProjectTable, new List<string>() { "ProjectId" }));
            ds.Tables.Add(InitSimpleDataTable(new CxScan(), ScanTable, new List<string>() { "ProjectId", "ScanId" }));
            ds.Tables.Add(InitSimpleDataTable(new CxResult(), ResultTable, new List<string>() { "ProjectId", "VulnerabilityId", "SimilarityId", "FileNameHash" }));
            ds.Tables.Add(InitSimpleDataTable(new CxMetaData(), MetaTable, new List<string>() { "FileName" }));
            return ds;
        }


        private DataTable InitSimpleDataTable(object mapObject, string tableName, List<string> primaryKeys)
        {
            Dictionary<string, DataColumn> dataColumns = new Dictionary<string, DataColumn>();
            List<DataColumn> cols = new List<DataColumn>();
            DataTable table = new DataTable(tableName);
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                if (primaryKeys.Contains(name))
                {
                    var prop = property.PropertyType;
                    DataColumn col = new DataColumn(name, prop);
                    dataColumns.Add(name, col);
                }
            }
            foreach (string name in primaryKeys)
            {
                cols.Add(dataColumns[name]);
                table.Columns.Add(dataColumns[name]);
            }
            table.PrimaryKey = cols.ToArray();
            return table;
        }
        private DataTable InitSQLTable(object mapObject, string tableName, List<string> primaryKeys)
        {
            DataTable table = new DataTable(tableName);
            List<DataColumn> cols = new List<DataColumn>();
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var name = property.Name;
                var prop = property.PropertyType;
                table.Columns.Add(name, prop);
                if (primaryKeys.Contains(name))
                {
                    cols.Add(table.Columns[name]);
                }
            }
            table.PrimaryKey = cols.ToArray();
            return table;
        }
        public void InitAllSQLTables()
        {
            CreateTableFromObject(new CxProject(), ProjectTable, new List<string>() { "ProjectId" });
            CreateTableFromObject(new CxScan(), ScanTable, new List<string>() { "ProjectId", "ScanFinished" });
            CreateTableFromObject(new CxResult(), ResultTable, new List<string>() { "ProjectId", "VulnerabilityId", "SimilarityId", "FileNameHash" });
            CreateTableFromObject(new CxMetaData(), MetaTable, new List<string>() { "FileName" });
            AddDefaultIndexes();
        }

        public object CreateTableFromObject(object mapObject, string tableName, List<string> primaryKeys)
        {
            DropTable(tableName);
            string sql = CreateSQLFromObject(mapObject, tableName, primaryKeys);

            SQLiteCommand cmd;
            if (_transaction != null && _transaction.Connection != null)
                cmd = new SQLiteCommand(sql, _connection, _transaction);
            else
                cmd = new SQLiteCommand(sql, _connection);
            int status = cmd.ExecuteNonQuery();
            if (token.debug && token.verbosity > 1) Console.WriteLine("{0}:{1} table created", tableName, status);
            return status;

        }
        public string CreateSQLFromObject(object mapObject, string tableName, List<string> primaryKeys)
        {
            string sql = "CREATE TABLE " + tableName + " (" + Environment.NewLine;
            // columns
            PropertyInfo[] properties = mapObject.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                sql += property.Name + " " + SQLGetType(property.PropertyType) + ",\n";
            }
            //sql = sql.TrimEnd(new char[] { ',', '\n' }) + "\n";
            // primary keys
            if (primaryKeys.Count > 0)
            {
                sql += "PRIMARY KEY (";
                foreach (string key in primaryKeys)
                {
                    sql += key + ",";
                }
                sql = sql.TrimEnd(new char[] { ',', '\n' }) + "))" + Environment.NewLine;
            }
            return sql;
        }
        public long DeleteUsingPrimaryKeys(string tableName, Dictionary<string, object> primaryKeys)
        {
            StringBuilder sql = new StringBuilder(String.Format("Delete FROM {0} where 1=1 ", tableName));
            foreach (string Key in primaryKeys.Keys)
            {
                if (primaryKeys[Key].GetType() == typeof(string) || primaryKeys[Key].GetType() == typeof(DateTime))
                    sql.Append(String.Format(" and {0} = '{1}'", Key, primaryKeys[Key]));
                else
                    sql.Append(String.Format(" and {0} = {1}", Key, primaryKeys[Key]));
            }
            using (SQLiteConnection connection = new SQLiteConnection(_connection))
            {
                var cmd = new SQLiteCommand(sql.ToString(), connection);
                long count = (long)cmd.ExecuteNonQuery();
                string keys = String.Join(',', primaryKeys.Select(x => x.Key).ToArray());
                string values = String.Join(',', primaryKeys.Select(x => x.Value.ToString()).ToArray());
                if (token.debug && token.verbosity > 1) Console.WriteLine("Delete key(s){0} value(s){1} from {2} count: {3} ", keys, values, tableName, count);
                return count;
            }
        }
        public string EscapeText(object inObject)
        {
            if (inObject is null) return String.Empty;
            if (inObject.GetType() == typeof(string))
                return inObject.ToString().Replace("'", "''");
            return String.Empty;
        }
        public bool AddDefaultIndex(string tableName, string column)
        {
            string SQL = String.Format("Create index idx_{0}_{1} on {0}({1})", tableName, column);
            if (token.debug && token.verbosity > 0) Console.WriteLine("Indexing: {0}", SQL);
            return CommitToSQLite(SQL, false);
        }
        public bool AddDefaultIndexes()
        {
            AddDefaultIndex(ScanTable, "ScanId");
            AddDefaultIndex(ScanTable, "ScanFinished");
            AddDefaultIndex(ResultTable, "ProjectId");
            AddDefaultIndex(ResultTable, "ScanId");
            AddDefaultIndex(ResultTable, "ScanFinished");
            AddDefaultIndex(ResultTable, "SimilarityId");
            AddDefaultIndex(ResultTable, "FileNameHash");
            AddDefaultIndex(ResultTable, "ResultSeverity");
            return true;
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}






