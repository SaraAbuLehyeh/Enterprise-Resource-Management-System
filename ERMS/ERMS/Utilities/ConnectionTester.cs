using Microsoft.Data.SqlClient;

namespace ERMS.Utilities
{
    public class ConnectionTester
    {
        private readonly IConfiguration _configuration;

        public ConnectionTester(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool TestConnection()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connection to SQL Server established successfully.");
                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("SQL Server connection failed: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return false;
            }
        }
    }
}
