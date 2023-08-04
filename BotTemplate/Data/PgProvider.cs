using Template.Monitoring;
using Npgsql;
using System.Data;

namespace Template
{
    public class PgProvider
    {
        public string connectionString { get; set; }


        public PgProvider(string path) => connectionString = path;


        public EnumerableRowCollection<DataRow> ExecuteSqlQueryAsEnumerable(string sqlQuery)
        {
            return ExecuteSqlQueryAsDataTable(sqlQuery).AsEnumerable();
        }


        public DataTable ExecuteSqlQueryAsDataTable(string sqlQuery)
        {
            int num = 0;
            int commandTimeout = 600;
            int num2 = 6;
            while (true)
            {
                try
                {
                    DataTable dataTable = new DataTable();
                    using (NpgsqlConnection pgConnection = new NpgsqlConnection(connectionString))
                    {
                        pgConnection.Open();
                        using (NpgsqlCommand pgCommand = new NpgsqlCommand(sqlQuery, pgConnection)
                        {
                            CommandType = CommandType.Text,
                            CommandTimeout = commandTimeout
                        })
                        {
                            using NpgsqlDataReader reader = pgCommand.ExecuteReader();
                            dataTable.Load(reader);
                        }

                        pgConnection.Close();
                    }

                    return dataTable;
                }
                catch (Exception ex)
                {
                    num++;
                    if (num > num2)
                    {
                        _ = Logger.LogCritical("Database error: " + ex.Message);
                    }
                }

                Thread.Sleep(500);
            }
        }
    }
}

