using System;
using System.Collections.Generic;

namespace Evolve.Dialect.Clickhouse
{
    /// <summary>
    ///     A simple sql statement builder that does nothing and returns only one 
    ///     sql statement that must be enlists in a transaction.
    /// </summary>
    internal class ClickhouseStatementBuilder : SqlStatementBuilderBase
    {
        public char Delimeter => ';';
        public override string? StatementDelimiter { get; }

        protected override IEnumerable<SqlStatement> Parse(string sqlScript, bool transactionEnabled)
        {
            if (sqlScript.IsNullOrWhiteSpace())
            {
                return new List<SqlStatement>();
            }

            var statements = new List<SqlStatement>();
            foreach (var strStatement in sqlScript.Split(Delimeter)) {
                var processedStrStatement = strStatement.Trim();
                if (string.IsNullOrEmpty(processedStrStatement)) continue;
                statements.Add(new SqlStatement(processedStrStatement, false));
            }

            return statements;
        }
    }
}
