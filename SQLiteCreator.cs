using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Data.SQLite;
using System.Data.SQLite.Generic;

namespace CxAPI_Store
{

    public class SQLiteCreator
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
        public SQLiteCreator(resultClass token)
        {
            this.token = token;
            string baseConnection = token.sqlite_connection;
            baseConnection = (token.initialize) ? baseConnection + "New=True;" : baseConnection;
            _connection = new SQLiteConnection(baseConnection);
            _connection.Open();
            var cmd = new SQLiteCommand("SELECT SQLITE_VERSION()", _connection);
            Console.WriteLine("SQLite version: {0}",cmd.ExecuteScalar());
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
            Console.WriteLine("{0} records are written to table {1}", count,table.TableName);
        }

        public void InsertDataSetToSQLite(DataTable table, int blocksize = 500)
        {

            StringBuilder sql = new StringBuilder(BuildInsertSQL(table));
            StringBuilder values = new StringBuilder();
            sql.Append(" VALUES ");
            int block = 0;
            foreach (DataRow dr in table.Rows)
            {
                values.Append(CreateInsertCommand(dr));
                block++;
                if (block > blocksize)
                {
                    values = addTerminationAndCommit(sql, values);
                }
            }
            addTerminationAndCommit(sql, values);
        }

        private StringBuilder addTerminationAndCommit(StringBuilder sql, StringBuilder values)
        {
            if (values.Length == 0)
                return values;
            sql.Append(values.ToString().TrimEnd(new char[] { ',' }));
            //            sql.Append(")");
            CommitToSQLite(sql.ToString());
            return new StringBuilder();
        }
        public void CommitToSQLite(string sql)
        {
            SQLiteCommand cmd;
            cmd = new SQLiteCommand(sql, _connection);
            SQLiteTransaction localTrans = _connection.BeginTransaction();
            cmd.Transaction = localTrans;

            try
            {
                int count = cmd.ExecuteNonQuery();
                localTrans.Commit();
                Console.WriteLine("{0} records are written to database", count);
            }
            catch (Exception ex)
            {
                localTrans.Rollback();
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Database updated failed.");
            }
        }

        public int DropTable(DataTable table)
        {
            string sql = "DROP TABLE IF EXISTS " + table.TableName;
            SQLiteCommand cmd;
            if (_transaction != null && _transaction.Connection != null)
                cmd = new SQLiteCommand(sql, _connection, _transaction);
            else
                cmd = new SQLiteCommand(sql, _connection);

            int status = cmd.ExecuteNonQuery();
            if (token.debug && token.verbosity > 1) Console.WriteLine("{0}:{1} table dropped", table.TableName, status);
            return status;
        }
        public object CreateFromDataTable(DataTable table)
        {
            DropTable(table);
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
                    sql.Append("'" + value + "',");
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

        public void InsertParameter(SQLiteCommand command,
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
        public SQLiteCommand CreateParameterInsertCommand(DataRow row)
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
        public object InsertDataRow(DataRow row, SQLiteTransaction trans)
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

    }
}






