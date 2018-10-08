using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class DbSakService
    {

        public static void createSak(DbSak sak)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            builder.DataSource = "afsdatabase.database.windows.net";
            builder.UserID = "ServerAdmin";
            builder.Password = "Zse4Xdr5";
            builder.InitialCatalog = "afsDatabase";

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
               

                //StringBuilder sb = new StringBuilder();
                //sb.Append("SELECT TOP 20 pc.Name as CategoryName, p.name as ProductName ");
                //sb.Append("FROM [SalesLT].[ProductCategory] pc ");
                //sb.Append("JOIN [SalesLT].[Product] p ");
                //sb.Append("ON pc.productcategoryid = p.productcategoryid;");
                //String sql = sb.ToString();

                using (var cmd = new SqlCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;

                    var sqlFormattedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.CommandText = $"INSERT Sak (Sakstype, Kundenummer, Kundenavn, DateCreated) VALUES (1, '{sak.Kundenummer}', '{sak.Kundenavn}', '{sqlFormattedDate}')";
                    cmd.Connection = connection;

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }

            //using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            //{
            //    Console.WriteLine("\nQuery data example:");
            //    Console.WriteLine("=========================================\n");

            //    connection.Open();
            //    StringBuilder sb = new StringBuilder();
            //    sb.Append("SELECT TOP 20 pc.Name as CategoryName, p.name as ProductName ");
            //    sb.Append("FROM [SalesLT].[ProductCategory] pc ");
            //    sb.Append("JOIN [SalesLT].[Product] p ");
            //    sb.Append("ON pc.productcategoryid = p.productcategoryid;");
            //    String sql = sb.ToString();

            //    using (SqlCommand command = new SqlCommand(sql, connection))
            //    {
            //        using (SqlDataReader reader = command.ExecuteReader())
            //        {
            //            while (reader.Read())
            //            {
            //                Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
            //            }
            //        }
            //    }
            //}
        }
    }
}
