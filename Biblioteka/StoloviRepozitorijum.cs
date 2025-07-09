using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public class StoloviRepozitorijum
    {
        public static List<Sto> stolovi = new List<Sto>()
            {
                new Sto() {brStola=1, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=2, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=3, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=4, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=5, brGostiju = 0, status = StatusSto.SLOBODAN}
            };
    }
}
