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
            sql.Execute(@"DECLARE @id VARCHAR(50) = CONVERT(nvarchar(MAX), DECOMPRESS(@not_found));
                            select 1 FROM table WHERE b = @id", 
                new { not_found = new byte[0] });
        }
    }
}
