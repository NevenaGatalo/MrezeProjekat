using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public enum TipOsoblja { KUVAR, BARMEN, KONOBAR }
    public enum StatusOsoblja { SLOBODAN, ZAUZET }
    [Serializable]
    public class Osoblje
    {
        public TipOsoblja tip {  get; set; }
        public StatusOsoblja status { get; set; }

        public Osoblje(TipOsoblja tip, StatusOsoblja status)
        {
            this.tip = tip;
            this.status = status;
        }
    }
}
