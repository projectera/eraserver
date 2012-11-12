using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ERAServer.Generators
{
    /// <summary>
    /// Originated from lc: language confluxer (http://www.ruf.rice.edu/~pound/revised-lc)
    /// - Written by Christopher Pound (pound@rice.edu), July 1993.
    /// - Loren Miller suggested I make sure lc starts by picking a letter pair that was at the 
    ///   beginning of a data word, Oct 95.
    /// - Cleaned it up a little bit, March 95; more, September 01
    /// 
    /// The datafile should be a bunch of words from some language with minimal punctuation or 
    /// garbage (# starts a comment!). Try mixing and matching words from different languages to 
    /// get just the balance you like. The output offcourse needs some editing.
    /// 
    /// </summary>
    internal static class LanguageConfluxer
    {
        private static Dictionary<String, LanguageConfluxerData> _cache = new Dictionary<String, LanguageConfluxerData>();

        public static Int32 MinLength = 3;
        public static Int32 MaxLength = 12;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static String[] Run(String filename, Int32 number, Boolean overrideCache = false)
        {

            // Get data
            LanguageConfluxerData data = null;
            if (overrideCache || !_cache.TryGetValue(filename, out data))
            {
                _cache[filename] = new LanguageConfluxerData(filename);
                data = _cache[filename];
            }

            String[] results = new String[number];

            // Loop to generate new words, beginning with a start_pair; find a word,
            // then continue to the next word using the last two characters (the last
            // of which will be whitespace) from the previous word as a "seed" for the new;
            // oh, and only print the first $max_length characters of any words
            for (Int32 i = 0; i < number; )
            {
                Int32 key = Lidgren.Network.NetRandom.Instance.Next(data.Hash.Count);
                String word = data.Hash.Keys.ElementAt(key);
                word += data.Hash.Values.ElementAt(key)[Lidgren.Network.NetRandom.Instance.Next(data.Hash.Values.ElementAt(key).Length)];
                while (word.Contains(' ') == false)
                {
                    String letters = data.Pairs[word[word.Length - 2].ToString() + word[word.Length - 1]];
                    if (letters == null)
                        break;
                    word += letters[Lidgren.Network.NetRandom.Instance.Next(letters.Length)];
                }

                word.Trim();
                if (word.Length > MinLength && word.Length < MaxLength)
                {
                    results[i++] = word;
                }
            }
            return results;
        }

        public static Int32 vowelCutoff = 3;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public static String[] Prop(String[] names)
        {
            List<String> shorts = new List<String>();
            List<String> longs = new List<String>();
            foreach (String name in names)
            {
                if (name.Count(a => "aeiou".Contains(a)) < vowelCutoff)
                    longs.Add(name);
                else
                    shorts.Add(name);
            }

            Int32 count = Math.Min(shorts.Count, longs.Count);
            String[] results = new String[count];
            while (count-- > 0)
            {
                results[count] = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(longs[count]) +
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(shorts[count]);
            }

            return results;
        }

        /// <summary>
        /// 
        /// </summary>
        internal class LanguageConfluxerData
        {
            /// <summary>
            /// 
            /// </summary>
            public Dictionary<String, String> Hash
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            public Dictionary<String, String> Pairs
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fileName"></param>
            internal LanguageConfluxerData(String fileName)
            {
                Queue<String> data = new Queue<String>();
                using (StreamReader file = File.OpenText(fileName))
                {
                    String contents;
                    // Read in the data, cleaning it up as we go, and making it one long array
                    while ((contents = file.ReadLine()) != null)
                    {
                        String[] words = Regex.Split(contents.Trim(), @"\s+");

                        foreach (String word in words)
                        {
                            data.Enqueue(word);
                        }
                    }

                    // Let's assume the first letter could follow the last pair (loop around)
                    //data.Enqueue(data.Peek());
                }

                if (data == null)
                    return;

                Hash = new Dictionary<String, String>();
                Pairs = new Dictionary<String, String>();

                // Now, load our hash of character pairs and the letters that may follow them,
                // keeping track of which pairs can be at the start of a word
                while (data.Count > 0)
                {

                    String word = data.Dequeue() + ' ';
                    if (word.Length > 3)
                    {
                        String hashval = String.Empty;
                        Hash.TryGetValue(word[0].ToString() + word[1], out hashval);
                        Hash[word[0].ToString() + word[1]] = hashval + word[2];
                    }

                    Int32 pos = 0;
                    while (pos++ < word.Length - 2)
                    {
                        String hashval = String.Empty;
                        Pairs.TryGetValue(word[pos - 1].ToString() + word[pos], out hashval);
                        Pairs[word[pos - 1].ToString() + word[pos]] = word[pos + 1].ToString();
                    }
                }
            }
        }
    }
}