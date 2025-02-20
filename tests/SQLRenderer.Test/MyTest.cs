using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver.V1;
using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using openCypherTranspiler.Common.Exceptions;
using openCypherTranspiler.Common.Logging;
using openCypherTranspiler.CommonTest;
using openCypherTranspiler.openCypherParser;
using openCypherTranspiler.LogicalPlanner;
using openCypherTranspiler.SQLRenderer.Test;

namespace openCypherTranspiler.SQLRenderer.MyTest
{
    [TestClass]
    public class MyTester
    {    
        private readonly ILoggable _logger = new TestLogger(LoggingLevel.Normal);

        private ISQLDBSchemaProvider _graphDef;


        [TestInitialize]
        public void TestInitialize()
        {
            _graphDef = new JSONGraphSQLSchema(@"./TestData/MovieGraph.json");
        }

        private string TranspileToSQL(string cypherQueryText)
        {
            var parser = new OpenCypherParser(_logger);
            var queryNode = parser.Parse(cypherQueryText);
            var plan = LogicalPlan.ProcessQueryTree(
                parser.Parse(cypherQueryText),
                _graphDef,
                _logger);
            var sqlRender = new SQLRenderer(_graphDef, _logger);
            return sqlRender.RenderPlan(plan);
        }

        [TestMethod]
        public void PrintTranspiledQueryTest()
        {
         /*  var queryText = @"
MATCH (p:Person)-[a:ACTED_IN]->(m:Movie)
WITH DISTINCT m.Title as Title, p.Name as Name
ORDER BY Title ASC, Name DESC
LIMIT 20
WHERE Title <> 'A'
RETURN  Title, Name
    ";*/
         var queryText = @"
MATCH (p:Person)-[a:ACTED_IN]->(m:Movie)
RETURN  m.Title, p.Name
";
            var result = TranspileToSQL(queryText);
            Console.WriteLine($"transpiled result: {result}");
        }
        
    }
}
