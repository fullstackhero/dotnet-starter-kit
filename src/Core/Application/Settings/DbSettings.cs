namespace DN.WebApi.Application.Settings
{
    public class DbSettings
    {
        public bool UseMsSql { get; set; }

        public bool UsePostgres { get; set; }

        public DbConnectionStrings ConnectionStrings { get; set; }

        public class DbConnectionStrings
        {
            public string MSSQL { get; set; }

            public string Postgres { get; set; }
        }
    }
}