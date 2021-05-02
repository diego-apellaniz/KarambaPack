using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace KarambaPack
{
    public static class Others
    {
        public static Dictionary<Tuple<int, string>, double> dictLFactors(List<string> Combos)
        //Extract the corresponding Load Factors from the definition of the Load Combinations
        {
            //Dictionary((LCombo, LCase), LFactor)
            var LFactors = new Dictionary<Tuple<int, string>, double>();
            string LCindex;
            double factor;
            for (int i = 0; i < Combos.Count; i++)
            {
                string string1 = Combos[i].Replace(" ", string.Empty).Replace("*", string.Empty).Replace("LF", "LC");
                string[] parts1 = Regex.Split(string1, @"(?=[+-])");
                for (int j = 0; j < parts1.Length; j++)
                {
                    if (parts1[j].Length > 0)
                    {
                        string[] parts2 = Regex.Split(parts1[j], @"LC");

                        if (parts2[0] == "" || parts2[0] == "+")
                        {
                            factor = 1;
                        }
                        else if (parts2[0] == "-")
                        {
                            factor = -1;
                        }
                        else
                        {
                            factor = Convert.ToDouble(parts2[0]);
                        }
                        LCindex = parts2[1];
                        LFactors.Add(new Tuple<int, string>(i, LCindex), factor);
                    }
                }
            }
            return LFactors;
        }
    }
}
