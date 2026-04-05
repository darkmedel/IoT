using Microsoft.Data.SqlClient;
using System.Data;

namespace cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;

public sealed class CitadelDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public CitadelDbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("IoTCommon")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró ConnectionStrings:IoTCommon ni ConnectionStrings:DefaultConnection.");

        return new SqlConnection(connectionString);
    }
}
