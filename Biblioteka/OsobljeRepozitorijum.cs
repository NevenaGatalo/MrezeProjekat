using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public class OsobljeRepozitorijum
    {
        public static Dictionary<Socket, Osoblje> osoblje = new Dictionary<Socket, Osoblje>();
    }
}
