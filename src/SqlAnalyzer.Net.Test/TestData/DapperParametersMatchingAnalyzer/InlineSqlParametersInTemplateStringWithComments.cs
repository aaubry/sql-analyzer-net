using System.Data.SqlClient;
using System.Threading.Tasks;

using Dapper;

namespace Sql.Analyzer.Test.TestData
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var sql = new SqlConnection();
            var header = "/* header here */";
            var paramFilter = " @param ";
            sql.Execute(new CommandDefinition($"""
                {header} inline sql {paramFilter /* SQL: @param */}
                """, new { param = new DbString() { IsAnsi = true, Value = "some_string" } }));
                
        }
    }
}
