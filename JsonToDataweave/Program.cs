using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Humanizer;

namespace JsonToDataweave
{
    class Program
    {

        private static List<String> targetXElementNames;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="matchPercent"></param>
        //static void Main(FileInfo source, FileInfo target, double matchPercent = 80)

        static void Main(FileInfo source, FileInfo target, String outputFileName = "output.dwl")
        {

            try
            {
                if (String.IsNullOrEmpty(source.ToString()) || String.IsNullOrEmpty(target.ToString()))
                    Console.WriteLine("Specify the source(JSON) and target(XML) file. E.g, --source source.json --target target.xml");

                if ((!String.Equals(source.Extension.ToString(), "json", StringComparison.InvariantCultureIgnoreCase)) || (!String.Equals(target.Extension.ToString(), "xml", StringComparison.InvariantCultureIgnoreCase))) ;
                Console.WriteLine("Source must be of type json and Target should be of type xml.");

                if (outputFileName.Split(".").LastOrDefault().Equals("dwl", StringComparison.InvariantCultureIgnoreCase))
                    outputFileName += ".dwl";
            }
            catch
            {
                Console.WriteLine("Specify the source(JSON) and target(XML) file. E.g, --source source.json --target target.xml");
            }



            var sourceFileName = "/Users/sriganeshk/Projects/JsonToDataweave/JsonToDataweave/source.json";

            var targetFileName = "/Users/sriganeshk/Projects/JsonToDataweave/JsonToDataweave/target.xml";

            var targetFileValue = XDocument.Load(targetFileName);
            targetXElementNames = targetFileValue.Descendants().ToList().Select(x => x.Name.ToString()).ToList();


            foreach (var node in targetFileValue.Nodes())
            {
                Console.WriteLine(node);

                Console.WriteLine(String.Compare(node.ToString(), "Action_Type"));
            }

            var sourceFile = File.ReadAllText(sourceFileName);
            var sourceFileValue = JObject.Parse(sourceFile);
            var dataweave = "{";
            foreach (var attribute in sourceFileValue)
            {
                dataweave += ParseChildren(attribute.Value, "payload") + ",\n";
            }

            dataweave += "}";
            Console.WriteLine(dataweave);

            File.WriteAllText(outputFileName, dataweave);
        }

        private static string ConstructDataWeave(IJEnumerable<JToken> jTokens, string mapString)
        {
            var dataweave = string.Empty;

            foreach (var child in jTokens.Children())
            {
                dataweave += ParseChildren(child, mapString) + ",\n";
            }
            return dataweave;
        }


        private static string ParseChildren(JToken jToken, string mapString)
        {
            var dataweave = string.Empty;
            if (jToken.Type == JTokenType.Object)
            {

                var splitString = jToken.Path.Split(".");
                //dataweave += $"\"{splitString.Last()} \" : {splitString.Last()} mapObject ({splitString.Last()}item, {splitString.Last()}value) {{ {ConstructDataWeave(jToken.Children(), splitString.Last() + "item")} }} ";

                dataweave += $"\"{FindString(splitString.Last())}\" : {{ {ConstructDataWeave(jToken.Children(), mapString + "." + splitString.Last())} }} ";
            }
            else if (jToken.Type == JTokenType.Array)
            {
                var splitString = jToken.Path.Split(".");
                dataweave = $"\"{FindString(splitString.Last())}\" : {mapString + "." + splitString.Last()} map (({splitString.Last()}Item, {splitString.Last()}Index) -> {{ {ConstructDataWeave(jToken.Children(), splitString.Last() + "Item")} }} ) ";
            }

            else
            {
                var splitString = jToken.Path.Split(".");
                dataweave += $"\"{FindString(splitString.Last())}\" : {mapString}.{splitString.Last()}";
            }


            return dataweave;
        }

        private static string FindString(string toSearch)
        {

            var attributeName = targetXElementNames.Where(z => String.Equals(z.Humanize(LetterCasing.LowerCase), toSearch.Humanize(LetterCasing.LowerCase), StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (String.IsNullOrEmpty(attributeName))
                attributeName = FindHighestProbablityNodeName(targetXElementNames, toSearch);
            return attributeName;
        }



        private static string FindHighestProbablityNodeName(List<string> source, string target)
        {
            if (source.Count == 0)
            {
                throw new InvalidOperationException("Empty list");
            }
            double maxSimilarity = double.MinValue;

            string maxSimilarNodeName = string.Empty;
            foreach (var XNodeName in source)
            {
                var similarity = CalculateSimilarity(XNodeName.Humanize(LetterCasing.LowerCase).Dehumanize(), target.Humanize(LetterCasing.LowerCase).Dehumanize());
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    maxSimilarNodeName = XNodeName;
                }
            }
            return maxSimilarNodeName + "_VERIFY";
        }


        //SOURCE: https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx

        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        private static double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        /// <summary>
        /// Returns the number of steps required to transform the source string
        /// into the target string.
        /// </summary>
        private static int ComputeLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    // Step 3
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 4
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }

    }
}
