using Domen.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domen.Modeli
{
    [Serializable]
    public class Osoblje
    {
        public StatusOsoblja Status {  get; set; }
        public TipOsoblja Tip { get; set; }
    }
}
