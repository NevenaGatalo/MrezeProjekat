using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Biblioteka;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kuvar
{
    public class Kuvar
    {
        static void Main(string[] args)
        {
            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint destinationEPTcp = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50001);

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Kuvar je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocketTCP.Connect(destinationEPTcp);
            int brBajtaPorudzbine = clientSocketTCP.Send(Encoding.UTF8.GetBytes("TIP:KUVAR"));
            Console.WriteLine("Kuvar je uspesno povezan sa serverom!");

            Console.WriteLine("==============================KUVAR================================");
            while (true)
            {
                try
                {
                    Console.WriteLine($"\n{"[WAITING]",-12} Čeka se porudžbina...");
                    byte[] porudzbina = new byte[1024];
                    int brPrimljenihBajtova = clientSocketTCP.Receive(porudzbina);
                    if(brPrimljenihBajtova == 0)
                    {
                        break;
                    }
                    Porudzbina p = null;
                    using (MemoryStream ms = new MemoryStream(porudzbina, 0, brPrimljenihBajtova))
                    {
                        //prima porudzbinu koju treba da napravi
                        BinaryFormatter bf = new BinaryFormatter();
                        p = bf.Deserialize(ms) as Porudzbina;
                        Console.WriteLine($"\n{"[PRIMLJENO]", -12} Porudzbina: " + p.nazivArtikla);
                    }
                    
                    Console.WriteLine($"{"[OBRADA]",-12} Priprema jela...");
                    Thread.Sleep(2000);
                    p.status = StatusPorudzbina.SPREMNO;
                    Console.WriteLine($"{"[SPREMNO]",-12} Porudzbina je spremna.");
                    

                    using (MemoryStream msSend = new MemoryStream()) {
                        BinaryFormatter bfSend = new BinaryFormatter();
                        bfSend.Serialize(msSend, p);
                        byte[] data = msSend.ToArray();

                        clientSocketTCP.Send(data);
                        Console.WriteLine($"{"[SLANJE]",-12} Porudzbina prosledjena serveru.");
                    }
                    Console.WriteLine("===================================================================");
                    
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
