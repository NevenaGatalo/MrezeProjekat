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
        //public static Dictionary<int, Dictionary<int, string>> rezervacije = new Dictionary<int, Dictionary<int, string>>();
        public static Dictionary<int, Tuple<int, string>> rezervacije = new Dictionary<int, Tuple<int, string>>();

        public static string RezervacijeToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var kvp in rezervacije)
            {
                int stoId = kvp.Key;
                int sat = kvp.Value.Item1;
                string ime = kvp.Value.Item2;

                sb.AppendLine($"Sto {stoId}: {sat}h - {ime}");
            }
            return sb.ToString();
        }
        public static List<int> OčistiIstekleRezervacije(int trenutniSat)
        {

            var zaBrisanje = rezervacije
                .Where(kvp => kvp.Value.Item1 < trenutniSat)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sto in zaBrisanje)
            {
                rezervacije.Remove(sto);

                foreach (var s in StoloviRepozitorijum.stolovi)
                {
                    if (s.brStola == sto)
                    {
                        s.status = StatusSto.SLOBODAN;
                        //Console.WriteLine("oslobodjen sto br: " + s.brStola + " status: " + s.status);
                        break;
                    }
                }
            }
            return zaBrisanje;
        }

    }
}
