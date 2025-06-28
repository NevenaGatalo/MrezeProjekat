using Domen.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domen.Modeli
{
    [Serializable]
    public class Porudzbina
    {
        public string NazivArtikla = "";
        public KategorijaPorudzbine Kategorija {  get; set; }
        public int Cena {  get; set; }
        public StatusPorudzbine Status {  get; set; }
    }
}
