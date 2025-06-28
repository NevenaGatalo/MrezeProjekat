using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domen.Repozitorijumi.StoloviRepozitorijum
{
    public interface IStoRepozitorijum
    {
        public bool ZauzmiSto(int brojStola, int brojGostiju);
        public bool OslobodiSto(int brojStola);
    }
}
