namespace DN.WebApi.Application.Configurations
{
    public class PersistenceConfiguration
    {
         public bool UseMsSql { get; set; }

        public bool UsePostgres { get; set; }

        public PersistenceConnectionStrings ConnectionStrings { get; set; }

        public class PersistenceConnectionStrings
        {
            public string MSSQL { get; set; }

            public string Postgres { get; set; }
        }
    }
}