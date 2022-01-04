namespace DN.WebApi.Infrastructure.Multitenancy
{
    public class HideSecureConnectionString
    {
        internal static string? GetSecureConnectionString(string? dbProvider, string? connectionString)
        {
            string[] postgresql_userStrings = new string[] { "user id", "userID", "username", "uid", "user name", "user" };
            string[] postgresql_passwordStrings = new[] { "password", "pwd" };
            string[] mssql_userStrings = new string[] { "user id", "userID", "username", "uid", "user name", "user" };
            string[] mssql_passwordStrings = new[] { "password", "pwd" };
            string[] mysql_userStrings = new string[] { "user id", "userID", "username", "uid", "user name", "user" };
            string[] mysql_passwordStrings = new[] { "password", "pwd" };
            string[] oracle_userStrings = new string[] { "user id", "userID", "username", "uid", "user name", "user" };
            string[] oracle_passwordStrings = new[] { "password", "pwd" };

            string? result = connectionString;
            switch (dbProvider?.ToLower())
            {
                case "postgresql":
                    result = HideSecureConnectionStringByProvider(connectionString, postgresql_userStrings, postgresql_passwordStrings);
                    break;
                case "mssql":
                    result = HideSecureConnectionStringByProvider(connectionString, mssql_userStrings, mssql_passwordStrings);
                    break;
                case "mysql":
                    result = HideSecureConnectionStringByProvider(connectionString, mysql_userStrings, mysql_passwordStrings);
                    break;
                case "oracle":
                    result = HideSecureConnectionStringByProvider(connectionString, oracle_userStrings, oracle_passwordStrings);
                    break;
            }

            return result;
        }
        private static string? HideSecureConnectionStringByProvider(string? connectionString, string[] userStrings, string[] passwordStrings)
        {
            string? result = connectionString;

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var stringParts = connectionString.Split(new char[] { ';' });
                var query = stringParts.AsQueryable();
                foreach (string userString in userStrings)
                {
                    query = query.Where(s =>
                        s.Contains(userString, StringComparison.InvariantCultureIgnoreCase) == false);
                }
                foreach (string passwordString in passwordStrings)
                {
                    query = query.Where(s =>
                        s.Contains(passwordString, StringComparison.InvariantCultureIgnoreCase) == false);
                }
                var newConnectionString = query.ToArray();
                result = string.Join(";", newConnectionString);
            }
            return result;
        }
    }
}