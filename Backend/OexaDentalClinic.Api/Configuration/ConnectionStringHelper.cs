namespace OexaDentalClinic.Api.Configuration
{
    public static class ConnectionStringHelper
    {
        public static string Resolve(IConfiguration configuration)
        {
            var fromConfig = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromConfig))
                return fromConfig;

            var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL")
                ?? Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrWhiteSpace(mysqlUrl))
                return FromMysqlUrl(mysqlUrl);

            var host = Environment.GetEnvironmentVariable("MYSQLHOST");
            if (!string.IsNullOrWhiteSpace(host))
            {
                var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
                var user = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "root";
                var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD")
                    ?? Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD")
                    ?? "";
                var database = Environment.GetEnvironmentVariable("MYSQLDATABASE")
                    ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE")
                    ?? "railway";
                return Build(host, port, database, user, password);
            }

            throw new InvalidOperationException(
                "Database connection not configured. Set ConnectionStrings:DefaultConnection or Railway MYSQL_* variables.");
        }

        private static string FromMysqlUrl(string url)
        {
            if (!url.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Invalid MySQL URL.");

            var body = url["mysql://".Length..];
            var at = body.LastIndexOf('@');
            if (at < 0) throw new FormatException("Invalid MySQL URL.");

            var auth = body[..at];
            var hostPart = body[(at + 1)..];
            var colon = auth.IndexOf(':');
            var user = colon >= 0 ? auth[..colon] : auth;
            var password = colon >= 0 ? auth[(colon + 1)..] : "";

            var slash = hostPart.IndexOf('/');
            var hostPort = slash >= 0 ? hostPart[..slash] : hostPart;
            var database = slash >= 0 ? hostPart[(slash + 1)..] : "railway";

            var portColon = hostPort.LastIndexOf(':');
            var host = portColon >= 0 ? hostPort[..portColon] : hostPort;
            var port = portColon >= 0 ? hostPort[(portColon + 1)..] : "3306";

            return Build(host, port, database, user, password);
        }

        private static string Build(string host, string port, string database, string user, string password)
        {
            var ssl = host.Contains("railway", StringComparison.OrdinalIgnoreCase) ? "SslMode=Required;" : "";
            return $"Server={host};Port={port};Database={database};User={user};Password={password};{ssl}";
        }
    }
}
