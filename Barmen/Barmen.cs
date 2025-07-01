using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Biblioteka;

namespace Barmen
{
    public class Barmen
    {
        static void Main(string[] args)
        {
            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint destinationEPTcp = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 50001);

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Barmen je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocketTCP.Connect(destinationEPTcp);
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            while (true)
            {
                try
                {
                    byte[] porudzbina = new byte[1024];
                    int brPrimljenihBajtova = clientSocketTCP.Receive(porudzbina);
                    if (brPrimljenihBajtova == 0)
                    {
                        break;
                    }
                    using (MemoryStream ms = new MemoryStream(porudzbina, 0, brPrimljenihBajtova))
                    {
                        //prima listu porudzbina koje treba da napravi
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Porudzbina> primljenePorudzbine = bf.Deserialize(ms) as List<Porudzbina>;
                        foreach (var p in primljenePorudzbine)
                        {
                            //proverava da li je porudzbina pice
                            if (p.kategorija == Kategorija.PICE)
                            {
                                p.status = StatusPorudzbina.SPREMNO;
                            }
                        }

                        //salje listu spremljenih porudzbina serveru
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream mst = new MemoryStream())
                        {
                            formatter.Serialize(mst, primljenePorudzbine);
                            byte[] data = mst.ToArray();

                            clientSocketTCP.Send(data);
                            Console.WriteLine("Porudzbine spremne i prosledjene serveru");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }
            }
            Console.WriteLine("Klijent zavrsava sa radom");
            clientSocketTCP.Close();
            Console.ReadKey();
        }
    }
}
