namespace CleanArchitecture.Shared;

public static class Services
{
    //#if (!UseApiOnly)
    /// <summary>
    /// The name of the Web Frontend service.
    /// This service is responsible for hosting the frontend application.
    /// </summary>
    public const string WebFrontend = "webfrontend";

    //#endif

    /// <summary>
    /// The name of the Web API service.
    /// This service is responsible for hosting the Web API application.
    /// </summary>
    public const string WebApi = "webapi";

    /// <summary>
    /// The name of the Database Server service.
    /// This service is responsible for hosting the database server (e.g., PostgreSQL, SQL Server, or SQLite).
    /// </summary>
    public const string DatabaseServer = "dbserver";

    /// <summary>
    /// The name of the Database.
    /// This is the name of the database that will be created and used by the application.
    /// </summary>
    public const string Database = "CleanArchitectureDb";

    /// <summary>
    /// The name of the Migration worker.
    /// This worker applies EF Core migrations and seeds the database at startup.
    /// </summary>
    public const string Migration = "migration";

    /// <summary>
    /// The name of the Postgres resource (generic Aspire container).
    /// </summary>
    public const string PostgresServer = "postgres";

    /// <summary>
    /// The name of the Aspire parameter holding the postgres password.
    /// </summary>
    public const string PostgresPasswordParameter = "postgres-password";

    /// <summary>
    /// Names of the shared local development containers.
    /// <para>
    /// Deliberately free of the project name: every solution generated from this template targets
    /// the <b>same</b> PostgreSQL instance and the same pgAdmin, each with its own database inside
    /// it. <c>dotnet new</c> rewrites every case variant of the source name, so a name built from
    /// it would give each project its own container — and they would then all fight over the fixed
    /// host ports below.
    /// </para>
    /// </summary>
    public static class Shared
    {
        public const string PostgresContainer = "ca-shared-postgres";

        public const string PostgresDataVolume = "ca-shared-pg-data";

        public const string PgAdminContainer = "ca-shared-pgadmin";

        public const int PostgresHostPort = 5431;

        public const int PgAdminHostPort = 5050;

        /// <summary>
        /// Machine-wide environment variable holding the shared PostgreSQL password.
        /// <para>
        /// Not user secrets: <c>dotnet new</c> gives every generated project its own
        /// <c>UserSecretsId</c>, so each would hold a different password while the container keeps
        /// the one it was first initialised with — every project but the first would fail to
        /// authenticate. The EF Core scripts under <c>scripts/db</c> read the same variable.
        /// </para>
        /// </summary>
        public const string PostgresPasswordEnvVar = "CA_SHARED_PG_PWD";
    }
}
