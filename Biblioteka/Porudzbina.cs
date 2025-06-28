using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public enum Kategorija { PICE, HRANA }
    public enum StatusPorudzbina { U_PRIPREMI, SPREMNO, DOSTAVLJENO }
    [Serializable]
    public class Porudzbina
    {
        public string nazivArtikla { get; set; }
        public Kategorija kategorija { get; set;}
        public int cena { get; set; }
        public StatusPorudzbina status { get; set; }
    }
}
