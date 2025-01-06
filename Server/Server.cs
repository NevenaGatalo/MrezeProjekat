using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEPTcp = new IPEndPoint(IPAddress.Any, 50001);
            IPEndPoint serverEPUdp = new IPEndPoint(IPAddress.Any, 50002);
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0); // Serverov IPEndPoint, IP i port na kom ce server soket primati poruke

            serverSocketTCP.Bind(serverEPTcp);
            serverSocketUDP.Bind(serverEPUdp);

            Console.WriteLine($"Server je pokrenut i ceka poruku na: {serverEPTcp}");

            serverSocketTCP.Listen(5);
            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEPTcp}");

            Socket acceptedSocket = serverSocketTCP.Accept();

            IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Povezao se novi klijent! Njegova adresa je {clientEP}");

            byte[] buffer = new byte[1024];
            byte[] prijemniBafer = new byte[1024];
            while (true)
            {
                try
                {
                    int brBajta = serverSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP); // Primamo poruku i podatke o posiljaocu
                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                    Console.WriteLine("\n----------------------------------------------------------------------------------------\n");
                    Console.WriteLine($"Stiglo je {brBajta} bajta od {posiljaocEP}, poruka:\n{poruka}");

                    byte[] binarnaPoruka = Encoding.UTF8.GetBytes("Stanje stolova: " + poruka); // Dopisujemo Server eho cisto da znamo koja je poruka

                    brBajta = serverSocketUDP.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, posiljaocEP); // 3.
                    Console.WriteLine($"Poslat je eho duzine {brBajta} ka {posiljaocEP}");
                    Console.WriteLine();

                    //tcp
                    int brBajtaTCP = acceptedSocket.Receive(buffer);
                    if (brBajta == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom");
                        break;
                    }
                    string porukaTCP = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine("Porudzbina: " + porukaTCP);


                    if (poruka == "kraj")
                        break;


                    Console.WriteLine("Unesite odgovor: "); 
                    string odgovor = Console.ReadLine();

                    brBajta = acceptedSocket.Send(Encoding.UTF8.GetBytes(odgovor));
                    if (odgovor == "kraj")
                        break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske {ex}");
                    break;
                }

            }
            Console.WriteLine("Server zavrsava sa radom");
            serverSocketTCP.Close(); // Zatvaramo soket na kraju rada
            acceptedSocket.Close();
            serverSocketUDP.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        }
    }
}
