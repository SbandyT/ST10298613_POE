using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using ST10298613_POE.Models;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ST10298613_POE.Services
{
    public class TableService
    {
        private readonly TableClient _tableClient;
        private readonly IConfiguration _configuration;

        public TableService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("CustomerProfiles");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddEntityAsync(CustomerProfile profile)
        {
            await _tableClient.AddEntityAsync(profile);
        }
        public async Task InsertCustomerAsync(CustomerProfile profile)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var query = @"INSERT INTO CustomerTable (FirstName, SecondName, Email, PhoneNumber)
                          VALUES (@FirstName, @SecondName, @Email, @PhoneNumber)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FirstName", profile.FirstName);
                command.Parameters.AddWithValue("@SecondName", profile.LastName);
                command.Parameters.AddWithValue("@Email", profile.Email);
                command.Parameters.AddWithValue("@PhoneNumber", profile.PhoneNumber);

                connection.Open();
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}