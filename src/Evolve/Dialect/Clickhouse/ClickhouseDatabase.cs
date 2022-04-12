using Evolve.Connection;
using Evolve.Metadata;

namespace Evolve.Dialect.Clickhouse
{
    internal class ClickhouseDatabase : DatabaseHelper
    {
        private const int LOCK_ID = 12345;

        public ClickhouseDatabase(WrappedConnection wrappedConnection) : base(wrappedConnection)
        {
        }

        public override string DatabaseName => "PostgreSQL";

        public override string CurrentUser => "current_user";

        public override SqlStatementBuilderBase SqlStatementBuilder => new ClickhouseStatementBuilder();

        public override IEvolveMetadata GetMetadataTable(string schema, string tableName) => new ClickhouseMetadataTable(schema, tableName, this);

        public override string GetCurrentSchemaName() => CleanSchemaName(WrappedConnection.QueryForString("SELECT currentDatabase()"));

        public override Schema GetSchema(string schemaName) => new ClickhouseSchema(schemaName, WrappedConnection);

        // public override bool TryAcquireApplicationLock() => WrappedConnection.QueryForBool($"SELECT pg_try_advisory_lock({LOCK_ID})");
        public override bool TryAcquireApplicationLock() => true;

        // public override bool ReleaseApplicationLock() => WrappedConnection.QueryForBool($"SELECT pg_advisory_unlock({LOCK_ID})");
        public override bool ReleaseApplicationLock() => true;

        private string CleanSchemaName(string schemaName)
        {
            if(schemaName.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            string newSchemaName = schemaName.Replace("\"", "")
                                             .Replace("$user", "")
                                             .Trim();

            if(newSchemaName.StartsWith(","))
            {
                newSchemaName = newSchemaName.Substring(1);
            }

            if (newSchemaName.Contains(","))
            {
                newSchemaName = newSchemaName.Substring(0, newSchemaName.IndexOf(","));
            }

            return newSchemaName.Trim();
        }
    }
}
