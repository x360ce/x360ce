using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Configuration
{
    /// <summary>
    /// Represents a case-insensitive command-line arguments parser built on Dictionary<string,string>.
    /// Splits parameters by -, --, /, =, or :, and removes enclosing quotes from values.
    /// </summary>
    /// <remarks>
    /// Shares parsing logic with <see cref="InstallContext"/>, but outputs to this dictionary and does not include logging or flag helper methods.
    /// </remarks>
    public class Arguments : Dictionary<string, string>
    //public class Arguments : StringDictionary // Case insensitive.
    {
        /// <summary>
        /// Initializes and parses the specified command-line arguments.
        /// </summary>
        /// <param name="args">Array of command-line arguments to parse.</param>
        /// <param name="comparer">Comparer for key matching; defaults to InvariantCultureIgnoreCase if null.</param>
        public Arguments(string[] args, StringComparer comparer = null) : base(comparer ?? StringComparer.InvariantCultureIgnoreCase)
        {
            Regex spliter = new Regex(@"^-{1,2}|^/|=|:",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

			Regex remover = new Regex(@"^['""]?(.*?)['""]?$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // (-|--|/)param( |=|:)[("|')]value[("|')]
            // Examples: 
            //   -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (string Txt in args)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                Parts = spliter.Split(Txt, 3);
                switch (Parts.Length)
                {
                    // Found a value (for the last parameter 
                    // found using space separator)
                    case 1:
                        if (Parameter != null)
                        {
                            if (!base.ContainsKey(Parameter))
                            {
                                Parts[0] = remover.Replace(Parts[0], "$1");
                                base.Add(Parameter, Parts[0]);
                            }
                            Parameter = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;

                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!base.ContainsKey(Parameter))
                                base.Add(Parameter, null);
                        }
                        Parameter = Parts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!base.ContainsKey(Parameter))
                                base.Add(Parameter, null);
                        }

                        Parameter = Parts[1];

                        // Remove possible enclosing characters (",')
                        if (!base.ContainsKey(Parameter))
                        {
                            Parts[2] = remover.Replace(Parts[2], "$1");
                            base.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!base.ContainsKey(Parameter))
                    base.Add(Parameter, null);
            }
        }

        /// <summary>
        /// Retrieves the value associated with the specified key, or null if not found.
        /// </summary>
        /// <param name="key">The key to locate in the arguments.</param>
        /// <param name="ignoreCase">True to ignore case during key comparison; otherwise, false.</param>
        /// <returns>The value associated with the key; or null if not present.</returns>
        public string GetValue(string key, bool ignoreCase = false)
        {
            var keyValue = Keys.Cast<string>().FirstOrDefault(x => string.Compare(x, key, ignoreCase) == 0);
            return keyValue is null ? null : this[keyValue];
        }

        /// <summary>
        /// Determines whether an entry with the specified key exists, using optional case-insensitive comparison.
        /// </summary>
        /// <param name="key">The key to locate in the arguments.</param>
        /// <param name="ignoreCase">True to ignore case during key comparison; otherwise, false.</param>
        /// <returns>True if an entry with the specified key exists; otherwise, false.</returns>
        public bool ContainsKey(string key, bool ignoreCase)
        {
            return Keys
                .Cast<string>()
                .Any(x => string.Compare(x, key, ignoreCase) == 0);
        }

        //public T GetValue<T>(string key, bool ignoreCase = false, T defaultValue = default) where T : struct
        //{
        //    var valueString = GetValue(key, ignoreCase);
        //    return RuntimeHelper.TryParse<T>(valueString, defaultValue);
        //}
    }
}
