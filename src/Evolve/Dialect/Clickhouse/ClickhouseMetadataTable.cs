using System;
using System.Collections.Generic;
using Evolve.Dialect.PostgreSQL;
using Evolve.Metadata;
using Evolve.Migration;

namespace Evolve.Dialect.Clickhouse
{
    internal class ClickhouseMetadataTable : MetadataTable
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="schema"> Existing database schema name. </param>
        /// <param name="tableName"> Metadata table name. </param>
        /// <param name="database"> A database helper used to change and restore schema of the metadata table. </param>
        public ClickhouseMetadataTable(
            string schema,
            string tableName,
            DatabaseHelper database
        )
            : base(schema, tableName, database)
        {
            
        }

        /// <summary>
        ///     Returns always true, because the lock is granted at application level.
        ///     <see cref="PostgreSQLDatabase.TryAcquireApplicationLock"/>
        /// </summary>
        protected override bool InternalTryLock() => true;

        /// <summary>
        ///     Returns always true, because the lock is released at application level.
        ///     <see cref="PostgreSQLDatabase.ReleaseApplicationLock"/>
        /// </summary>
        protected override bool InternalReleaseLock() => true;

        protected override bool InternalIsExists()
        {
            return _database.WrappedConnection.QueryForLong($"select count() from system.tables where database='{Schema}' and name='{TableName}'") == 1;
        }

        protected override void InternalCreate()
        {

            string sql = $@"create table {Schema}.{TableName}
            (
                id          Int32,
                type        Int16,
                version     String,
                description String,
                name        String,
                checksum String,
                installed_by String,
                installed_on DateTime,
                success Int8
            )
            engine = MergeTree PARTITION BY toYYYYMM(installed_on)
                PRIMARY KEY (id)
                ORDER BY (id, installed_on)
                SETTINGS index_granularity = 8192;";

            _database.WrappedConnection.ExecuteNonQuery(sql);
        }

        protected override void InternalSave(MigrationMetadata metadata)
        {
            string sql = $"select max(id) from \"{Schema}\".\"{TableName}\";";
            var recordId = (int) _database.WrappedConnection.QueryForLong(sql);
            recordId++;
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            sql = $@"INSERT INTO {Schema}.{TableName} (
                                          id,
                                          type,
                                          version,
                                          description,
                                          name,
                                          checksum,
                                          installed_by,
                                          installed_on,
                                          success) values (
                        {recordId},
                        {(int)metadata.Type},
                        {(metadata.Version is null ? "null" : $"'{metadata.Version}'")}, 
                        '{metadata.Description.TruncateWithEllipsis(200)}', 
                        '{metadata.Name.TruncateWithEllipsis(1000)}', 
                        '{metadata.Checksum}', 
                        '{_database.CurrentUser}',
                        '{now}',
                        {(metadata.Success ? "1" : "0")}
                );";

            _database.WrappedConnection.ExecuteNonQuery(sql);
        }

        protected override void InternalUpdateChecksum(int migrationId, string checksum)
        {
            string sql = $"UPDATE \"{Schema}\".\"{TableName}\" " +
                         $"SET checksum = '{checksum}' " +
                         $"WHERE id = {migrationId}";

            _database.WrappedConnection.ExecuteNonQuery(sql);
        }

        protected override IEnumerable<MigrationMetadata> InternalGetAllMetadata()
        {
            string sql = $"SELECT id, type, version, description, name, checksum, installed_by, installed_on, success FROM \"{Schema}\".\"{TableName}\"";
            return _database.WrappedConnection.QueryForList(sql, r =>
            {
                return new MigrationMetadata(r[2] as string, r.GetString(3), r.GetString(4), (MetadataType)r.GetInt16(1))
                {
                    Id = r.GetInt32(0),
                    Checksum = r.GetString(5),
                    InstalledBy = r.GetString(6),
                    InstalledOn = r.GetDateTime(7),
                    Success = r.GetBoolean(8)
                };
            });
        }
    }
}
