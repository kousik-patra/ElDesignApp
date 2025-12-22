using System.Data;

namespace ElDesignApp.Services.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        // Ensure you use the correct connection string name
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection string not found.");
    }

    public IDbConnection CreateConnection()
    {
        return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
    }
}