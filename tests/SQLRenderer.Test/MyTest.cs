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
                var sqlRender = new SQLRenderer(graphDef, _logger);
                return sqlRender.RenderPlan(plan);
            }
            catch (Exception e)
            {
                throw new Exception("error: " + e.ToString());
            }
        }


        public static List<(int Index, string CypherQuery, string FileName)> ReadJsonLines(string filePath)
        {
            var results = new List<(int, string, string)>();

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
                    if (values != null && values.Length == 3)
                    {
                        results.Add((Int32.Parse(values[0]), values[1], values[2]));
                    }
                    else
                    {
                        /*Console.WriteLine("Invalid line format (expected exactly 2 elements): " + line);*/
                    }
                }
                catch (JsonException ex)
                {
                    // 
                }
            }

            return results;
        }

        public static void WriteLinesToFile(List<string> lines, string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }
        
        [TestMethod]
        public void ReadGraphitiAndOutput()
        {
            var prefix = "./TestData/";
            var fileName = prefix + "human.opencypher";
            List<string> outputs = new List<string>();
            var l = ReadJsonLines(fileName);
            foreach (var i in l)
            {
                {
                    //Console.WriteLine($"[{i.CypherQuery}, {i.FileName}");
                    var schema = new JSONGraphSQLSchema(i.FileName);
                    try
                    {
                        var result = TranspileToSQL(i.CypherQuery, schema);
                        var o = $"[{i.Index}, \"{i.CypherQuery}\", \"{i.FileName}\", \"{result.ToString().Replace('\n', ' ')}\"]";
                        outputs.Add(o);
                        Console.WriteLine(o);
                    }
                    catch (Exception e)
                    {
                        var o = $"[{i.Index}, \"{i.CypherQuery}\", \"{i.FileName}\", \"ERROR: ${e.ToString().Split('\n')[0]}\"]";
                        outputs.Add(o);
                        Console.WriteLine(o);
                    }
                }
            }
            WriteLinesToFile(outputs, "/tmp/output.txt");
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
MATCH (N:N$NODE) WHERE [N.ID, N.CODE] IN [[1234,'PQR'], [4567, 'ABC']] RETURN N.ID, N.CODE";
            //MATCH (N:N$NODE) WHERE [N.ID, N.CODE] IN [[1234,'PQR'], [4567, 'ABC']] RETURN N.ID, N.CODE
            // 
            var graphDef = new JSONGraphSQLSchema("./TestData/toy_test.json");
            //@"OPTIONAL MATCH (p:Person)-[a:ACTED_IN]->(m) RETURN p.Name";
            var result = TranspileToSQL(queryText, graphDef);
            Console.WriteLine($"transpiled result: {result}");
        }



        [TestMethod]
        public void PrintTranspiledTableTest()
        {
            var graphDef = new JSONGraphSQLSchema("./TestData/openschemas_chatgpt4o/tutorial_017.json");
            var tableEntries = graphDef._tableDescs;
            foreach (var (k, v) in tableEntries)
            {
                Console.WriteLine(
                    $"Attribute Name: '{k}', Attribute.TableOrViewName: '{v.TableOrViewName}', Attribute.EntityId: '{v.EntityId}'");
            }
        }
    }
}
