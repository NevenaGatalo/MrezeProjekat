using Domen.Enumi;
using Domen.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domen.Repozitorijumi.StoloviRepozitorijum
{
    public class StoRepozitorijum : IStoRepozitorijum
    {
        private static List<Sto> stolovi;
        static StoRepozitorijum()
        {
            stolovi =
                [
                    new(1, 0, StatusStola.SLOBODAN),
                    new(2, 0, StatusStola.SLOBODAN),
                    new(3, 0, StatusStola.SLOBODAN)
                ];
        }
        public bool OslobodiSto(int brojStola)
        {
            foreach (Sto sto in stolovi)
            {
                if(sto.BrojStola == brojStola)
                {
                    sto.Status = StatusStola.SLOBODAN;
                    sto.BrojGostiju = 0;
                    return true;
                }
            }
            return false;
        }

        public bool ZauzmiSto(int brojStola, int brojGostiju)
        {
            foreach(Sto sto in stolovi)
            {
                if(sto.BrojStola == brojStola)
                {
                    sto.Status = StatusStola.ZAUZET;
                    sto.BrojGostiju = brojGostiju;
                    return true;
                }
            }
            return false;
        }
    }
}
