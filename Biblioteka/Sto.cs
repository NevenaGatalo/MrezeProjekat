using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public enum StatusSto { ZAUZET, SLOBODAN, REZERVISAN }
    [Serializable]
    public class Sto
    {
        public int brStola {  get; set; }
        public int brGostiju { get; set; }
        public StatusSto status { get; set; }
        public List<Porudzbina> porudzbine { get; set; }
        
    }
}
