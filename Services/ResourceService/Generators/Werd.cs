using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ERAServer.Generators
{
    /// <summary>
    /// Random words from simple rules (see datafiles); recursive info on datafile 
    /// format can be found in the English rulset by Chris Pound, pound@rice.edu, 
    /// after Mark Rosenfelder's Pascal version.
    /// Modified and ported to C# by Derk-Jan Karrenbeld. Added capitalisation.
    /// </summary>
    internal static class Werd
    {
        private static Dictionary<String, WerdData> _cache = new Dictionary<String, WerdData>();

        /// <summary>
        /// Runs the werd generator
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <param name="number">Number of lines to return</param>
        /// <param name="overrideCache">Overrides cache if true</param>
        /// <returns>Array with [number] lines</returns>
        internal static String[] Run(String filename, Int32 number, Boolean overrideCache = false)
        {
            // Get data
            WerdData data = null;
            if (overrideCache || !_cache.TryGetValue(filename, out data))
            {
                _cache[filename] = new WerdData(filename);
                data = _cache[filename];
            }

            // Get results
            String[] result = new String[number];

            for (Int32 i = 0; i < number; i++)
            {
                result[i] = data.Fetch();
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        internal class WerdData
        {
            /// <summary>
            /// Dictionairy with rules
            /// </summary>
            internal Dictionary<Char, String> Data
            {
                get;
                private set;
            }

            /// <summary>
            /// Key to run on
            /// </summary>
            internal Char KeyOnFetch 
            {
                get;
                private set;
            }

            /// <summary>
            /// Creates a new instance from a filename
            /// </summary>
            /// <param name="fileName"></param>
            internal WerdData(String fileName)
            {
                KeyOnFetch = ' ';
                Data = new Dictionary<Char, String>();
                using (StreamReader file = File.OpenText(fileName))
                {
                    String contents;
                    // Read in the data, cleaning it up as we go, and making it one long array
                    while ((contents = file.ReadLine()) != null)
                    {
                        // Letter:Rule
                        Match match = Regex.Match(contents + "\n", @"^([A-Z]):(.*)\s+$");

                        if (match.Success)
                        {
                            KeyOnFetch = KeyOnFetch == ' ' ? match.Groups[1].Value[0] : KeyOnFetch;
                            Data.Add(match.Groups[1].Value[0], match.Groups[2].Value);
                        }
                    }
                }
            }

            /// <summary>
            /// (Recursivly) Parses a match
            /// </summary>
            /// <param name="match"></param>
            /// <returns></returns>
            private String Parse(Match match)
            {
                Char key = match.Value[0];
                String[] groups = Data[key].Split(' ');

                // Pick replacement group
                String group = groups[Lidgren.Network.NetRandom.Instance.Next(groups.Length)];

                // Parse recursive
                String result = Regex.Replace(group, "([A-Z])", new MatchEvaluator(Parse));
                // Capitalize with !
                result = Regex.Replace(result, @"([a-z]+?)!\s?", new MatchEvaluator(Capitalize));
                // Add ! with \!
                result = Regex.Replace(result, @"\\!", "!");
                // Add spaces with _
                result = result.Replace("_", " ");
                

                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="match"></param>
            /// <returns></returns>
            private String Capitalize(Match match)
            {
                String result = match.ToString();
                Char upper = result[0].ToString().ToUpperInvariant()[0];
                result = upper + result.Remove(0, 1).Remove(result.Length - 2);
               
                return result;
            }

            /// <summary>
            /// Fetches a processed line
            /// </summary>
            /// <returns></returns>
            internal String Fetch()
            {
                return Regex.Replace(KeyOnFetch.ToString(), "([A-Z])", new MatchEvaluator(Parse));
            }
        }
    }
}