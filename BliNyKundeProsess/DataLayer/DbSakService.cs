using System;
using System.Data.SqlClient;

namespace DataLayer
{
    public class DbSakService : IDbSakService
    {

        public void CreateSak(DbSak sak)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = "afsdatabase.database.windows.net",
                UserID = "ServerAdmin",
                Password = "Zse4Xdr5",
                InitialCatalog = "afsDatabase"
            };

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;

                    var sqlFormattedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.CommandText = $"INSERT Sak (Sakstype, Kundenummer, Kundenavn, UtcDateTimeCreated) VALUES (1, '{sak.Kundenummer}', '{sak.Kundenavn}', '{sqlFormattedDate}')";
                    cmd.Connection = connection;

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
    }
}
