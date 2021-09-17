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
using System.Text.RegularExpressions;

namespace JsonToDataweave
{
    class Program
    {

        private static List<string> targetFileValue;

        // A very simple regular expression.
        private static string regexPattern = @"\[*\d\]";

        private static Regex regex = new Regex(regexPattern);

        // Assign the replace method to the MatchEvaluator delegate.
        private static MatchEvaluator _matchEval = new MatchEvaluator(ReplaceRegex);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="outputFileName"></param>
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


            try
            {

                var sourceFileName = "/Users/sriganeshk/Projects/JsonToDataweave/JsonToDataweave/rand/fake_swfo_json.json";

                var targetFileName = "/Users/sriganeshk/Projects/JsonToDataweave/JsonToDataweave/rand/swfo_rand_xml.xml";



                var targetFile = XDocument.Load(targetFileName);
                targetFileValue = targetFile.Descendants().ToList().Select(x => x.Name.ToString()).ToList();


                var sourceFile = File.ReadAllText(sourceFileName);
                var sourceFileValue = JObject.Parse(sourceFile);


                var dataweave = "{";
                foreach (var attribute in sourceFileValue)
                {
                    dataweave += ConstructDataWeave(attribute.Value, "payload") + ",\n";
                }

                dataweave += "}";
                Console.WriteLine(dataweave);

                File.WriteAllText(outputFileName, dataweave);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Occured: \n\n ======================================================= \n {e}");
            }
        }

        public static string ReplaceRegex(Match m)
        // Replace each Regex with empty string.
        {
            return "";
        }

        private static string CompositeString(string[] strings)
        {
            var selectedString = new[] { strings[strings.Length - 2], strings.Last() };
            var compositeString = "";
            foreach (var x in selectedString)
            {
                var regexString = regex.Replace(x, _matchEval);
                if (!x.Equals(regexString, StringComparison.InvariantCultureIgnoreCase))
                    compositeString += regexString + "Item.";
                else if (string.IsNullOrEmpty(compositeString))
                    compositeString += regexString + ".";
                else
                    compositeString += regexString;
            }

            //compositeString = regex.Replace(splitString[splitString.Length - 2], _matchEval) + "Item." + splitString.Last();

            return compositeString;
        }

        private static string ConstructDataWeave(JToken jToken, string mapString, bool isArray = false)
        {
            var dataweave = string.Empty;
            if (jToken.Type == JTokenType.Object)
            {

                var splitString = jToken.Path.Split(".");
                //dataweave += $"\"{splitString.Last()} \" : {splitString.Last()} mapObject ({splitString.Last()}Value, {splitString.Last()}Key) {{ {ConstructDataWeave(jToken.Children(), splitString.Last() + "item")} }} ";
                if (!isArray)
                {
                    //var compositeString = mapString + "." + splitString.Last();

                    //if (mapString.Equals(splitString.Last(), StringComparison.InvariantCultureIgnoreCase))
                    //{

                    //    compositeString = regex.Replace(splitString[splitString.Length - 2], _matchEval) + "Item." + splitString.Last();
                    //}


                    dataweave += $"\"{FindString(splitString.Last())}\" : {{ ";

                    //dataweave += $"\"{FindString(splitString.Last())}\" : {compositeString} mapObject (({splitString.Last()}Value, {splitString.Last()}Key) -> {{";

                    foreach (var x in jToken.Children())
                    {
                        //splitString = x.Path.Split(".");
                        dataweave += ConstructDataWeave(x, mapString + "." + regex.Replace(splitString.Last(), _matchEval)) + ",\n";
                    }
                    dataweave += "}";
                }
                else
                {
                    foreach (var x in jToken.Children())
                    {
                        dataweave += ConstructDataWeave(x, mapString) + ",\n";
                    }
                }
            }

            else if (jToken.Type == JTokenType.Array)
            {
                var splitString = jToken.Path.Split(".");
                //dataweave = $"\"{FindString(splitString.Last())}\" : {mapString + "." + splitString.Last()} map (({splitString.Last()}Item, {splitString.Last()}Index) -> {{ {ConstructDataWeave(jToken.Children(), splitString.Last() + "Item")} }} ) ";

                var compositeString = mapString + "." + splitString.Last();

                if (mapString.Equals(splitString.Last(), StringComparison.InvariantCultureIgnoreCase))
                //{
                //    var temp = string.Empty;
                //    //foreach (var split in splitString.Take(splitString.Length - 1))
                //        temp += r.Replace(splitString[splitString.Length-1], myEvaluator) + "Item.";
                //    compositeString = temp + splitString.Last();
                {
                    compositeString = CompositeString(splitString);
                    //compositeString = regex.Replace(splitString[splitString.Length - 2], _matchEval) + "Item." + splitString.Last();
                }
                dataweave += $"\"{FindString(splitString.Last())}\" : {compositeString} map (({splitString.Last()}Item, {splitString.Last()}Index) -> {{";
                foreach (var x in jToken.Children())
                {
                    // splitString = x.Path.Split(".");
                    dataweave += ConstructDataWeave(x, splitString.Last() + "Item", isArray: true);// + ",\n";
                }
                dataweave += "})";
            }

            //Internal Array or Object
            else if (jToken.Count() > 0 && ((jToken.Children().FirstOrDefault().Type == JTokenType.Array)))
            {
                var splitString = jToken.Path.Split(".");

                foreach (var x in jToken.Children())
                {

                    dataweave += ConstructDataWeave(x, splitString.Last());
                }
            }

            //Internal Array or Object
            else if (jToken.Count() > 0 && (jToken.Children().FirstOrDefault().Type == JTokenType.Object))
            {
                //var splitString = jToken.Path.Split(".");

                foreach (var x in jToken.Children())
                {

                    dataweave += ConstructDataWeave(x, mapString);
                }
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
            toSearch = toSearch.Dehumanize().Humanize(LetterCasing.LowerCase).Replace(" ", "");
            var attributeName = targetFileValue.Where(z => String.Equals(z.Dehumanize().Humanize(LetterCasing.LowerCase).Replace(" ", ""), toSearch, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (String.IsNullOrEmpty(attributeName))
                attributeName = FindHighestProbablityNodeName(targetFileValue, toSearch) + "_VERIFY";
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
                var similarity = CalculateSimilarity(XNodeName.Dehumanize().Humanize(LetterCasing.LowerCase).Replace(" ", ""), target);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    maxSimilarNodeName = XNodeName;
                }
            }
            return maxSimilarNodeName;
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
