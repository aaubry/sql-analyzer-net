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
            var args = new Args { not_found = new DbString() { IsAnsi = true, Value = "some_string" } };
            var args2 = new { args };
            sql.Execute(@"DECLARE @id VARCHAR(50);
                            select 1 FROM table WHERE b = @not_found", 
                args2.args);
        }

        private class Args
        {
            public DbString not_found { get; set; }
            public static string ShouldBeIgnored { get; } = "ignore me";
        }
    }
}
