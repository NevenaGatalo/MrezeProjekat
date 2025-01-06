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
        private string NazivArtikla = "";
        private KategorijaPorudzbine Kategorija {  get; set; }
        private int Cena {  get; set; }
        private StatusPorudzbine Status {  get; set; }
    }
}
