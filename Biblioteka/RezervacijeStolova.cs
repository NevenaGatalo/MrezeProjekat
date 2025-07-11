using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public class RezervacijeStolova
    {
        //brstola       satRez  imeRez
        public static Dictionary<int, Dictionary<int, string>> rezervacije = new Dictionary<int, Dictionary<int, string>>();

        public static string RezervacijeToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var stoEntry in rezervacije)
            {
                int stoId = stoEntry.Key;
                var vremenskeRezervacije = stoEntry.Value;

                foreach (var rez in vremenskeRezervacije)
                {
                    int sat = rez.Key;
                    string ime = rez.Value;

                    sb.AppendLine($"Sto {stoId}: {sat}h - {ime}");
                }
            }

            return sb.ToString();
        }

    }
}
