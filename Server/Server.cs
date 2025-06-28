using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Biblioteka;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEPTcp = new IPEndPoint(IPAddress.Any, 50001);

            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEPUdp = new IPEndPoint(IPAddress.Any, 50002);

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            serverSocketTCP.Bind(serverEPTcp);
            serverSocketUDP.Bind(serverEPUdp);

            ///ZA TCP KONEKCIJU
            Console.WriteLine($"Server je pokrenut i ceka poruku na: {serverEPTcp}");
            serverSocketTCP.Listen(5);
            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEPTcp}");
            Socket acceptedSocket = serverSocketTCP.Accept();
            IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Povezao se novi klijent! Njegova adresa je {clientEP}");

            BinaryFormatter formatter = new BinaryFormatter();
            List<Sto> stolovi = new List<Sto>();

            byte[] prijemniBaferPorudzbina = new byte[1024];
            byte[] prijemniBaferSto = new byte[1024];
            while (true)
            {
                try
                {
                    int brBajta = serverSocketUDP.ReceiveFrom(prijemniBaferSto, ref posiljaocEP); // Primamo poruku i podatke o posiljaocu
                    
                    if (brBajta == 0) break;

                    using (MemoryStream ms = new MemoryStream(prijemniBaferSto, 0, brBajta))
                    {
                        Sto sto = (Sto)formatter.Deserialize(ms);
                        stolovi.Add(sto);
                        Console.WriteLine($"Primljen sto br {sto.brStola} sa {sto.brGostiju} gostiju.");
                    }

                    //tcp
                    int brBajtaTCP = acceptedSocket.Receive(prijemniBaferPorudzbina);
                    if (brBajtaTCP == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom");
                        break;
                    }
                    using (MemoryStream ms = new MemoryStream(prijemniBaferPorudzbina, 0, brBajtaTCP))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Porudzbina> porudzbine = bf.Deserialize(ms) as List<Porudzbina>;
                        Console.WriteLine("Primljena porudzbina stola broj " + porudzbine[1].brojStola);
                        foreach (var p in porudzbine)
                        {
                            Console.WriteLine(p.nazivArtikla);
                        }
                    }

                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske {ex}");
                    break;
                }

            }
            Console.WriteLine("Server zavrsava sa radom");
            serverSocketTCP.Close(); // Zatvaramo soket na kraju rada
            //acceptedSocket.Close();

            serverSocketUDP.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        }
    }
}
