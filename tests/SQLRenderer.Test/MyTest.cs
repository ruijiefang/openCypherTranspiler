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
using Newtonsoft.Json;
using openCypherTranspiler.Common.GraphSchema;


namespace openCypherTranspiler.SQLRenderer.MyTest
{

    
    [TestClass]
    public class MyTester
    {    
        private readonly ILoggable _logger = new TestLogger(LoggingLevel.Normal);

        private JSONGraphSQLSchema _graphDef;


        [TestInitialize]
        public void TestInitialize()
        {
            _graphDef = new JSONGraphSQLSchema(@"./TestData/MovieGraph.json");
        }

        private string TranspileToSQL(string cypherQueryText, JSONGraphSQLSchema graphDef)  
        {
            try
            {
                var parser = new OpenCypherParser(_logger);
                var queryNode = parser.Parse(cypherQueryText);
                var plan = LogicalPlan.ProcessQueryTree(
                    parser.Parse(cypherQueryText),
                    graphDef,
                    _logger);
                var sqlRender = new SQLRenderer(_graphDef, _logger);
                return sqlRender.RenderPlan(plan);
            }
            catch (Exception e)
            {
                throw new Exception("error: " + e.ToString());
            }
        }
        
        
        public static List<(string CypherQuery, string FileName)> ReadJsonLines(string filePath)
        {
            var results = new List<(string, string)>();

            // Read the file line by line
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var r = new JsonTextReader(new StringReader(line));
                var serializer = new JsonSerializer();
                try
                {
                    string[] values = serializer.Deserialize<string[]>(r);
                    if (values != null && values.Length == 2)
                    {
                        results.Add((values[0], values[1]));
                    }
                    else
                    {
                        Console.WriteLine("Invalid line format (expected exactly 2 elements): " + line);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Error parsing line: " + line);
                    Console.WriteLine(ex.Message);
                }
            }

            return results;
        }

        [TestMethod]
        public void ReadGraphitiAndOutput()
        {
            var prefix = "./TestData/";
            var fileName = prefix+"filelist.txt";

            var l = ReadJsonLines(fileName);
            foreach (var i in l)
            {
                /*Console.WriteLine($"[{i.CypherQuery}, {i.FileName}");*/
                var schema = new JSONGraphSQLSchema(i.FileName);
                try
                {
                    var result = TranspileToSQL(i.CypherQuery, schema);
                    Console.WriteLine($"[\"{i.CypherQuery}\", \"{i.FileName}\", \"{result.ToString().Replace('\n', ' ')}\"]");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{i.CypherQuery}, {i.FileName}, \"ERROR: ${e.ToString().Substring(0, Math.Max(30,e.ToString().Length-1))}\"]");
                }
            }
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
    /*     var queryText = @"
MATCH (p:Person)-[a:ACTED_IN]->(m:Movie)
RETURN  m.Title, p.Name
";*/
    var queryText = @"
OPTIONAL MATCH (p:Person)-[a:ACTED_IN]->(m) RETURN p.Name";
            var result = TranspileToSQL(queryText, _graphDef);
            Console.WriteLine($"transpiled result: {result}");
        }
        
    }
}
