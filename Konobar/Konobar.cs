using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Konobar
{
    public class Konobar
    {
        static void Main(string[] args)
        {

            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket clientSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50002); // Odredisni IPEndPoint, IP i port ka kome saljemo. U slucaju 8. tacke je potrebno uneti IP adresu server racunara
            IPEndPoint destinationEPTcp = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50001); // Odredisni IPEndPoint, IP i port ka kome saljemo. U slucaju 8. tacke je potrebno uneti IP adresu server racunara
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Klijent je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocketTCP.Connect(destinationEPTcp);
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            while (true) // 1.
            {
                byte[] buffer = new byte[1024];
                byte[] prijemniBafer = new byte[1024];
                Console.WriteLine("Unesite stanje stolova:");
                string poruka = Console.ReadLine();

                if (poruka == "stop client") // 2.
                    break;

                byte[] binarnaPoruka = Encoding.UTF8.GetBytes(poruka);
                try
                {
                    int brBajta = clientSocketUDP.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destinationEP); // Poruka koju saljemo u binarnom zapisu, pocetak poruke, duzina, flegovi, odrediste

                    Console.WriteLine($"Uspesno poslato {brBajta} ka {destinationEP}");

                    brBajta = clientSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP);

                    string ehoPoruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                    Console.WriteLine($"Stigao je odgovor od {posiljaocEP}, duzine {brBajta}, eho glasi:\n{ehoPoruka}"); // 4
                    //tcp
                    Console.WriteLine("Unesite porudzbinu: ");
                    string porudzbina = Console.ReadLine();
                    int brBajtaPorudzbine = clientSocketTCP.Send(Encoding.UTF8.GetBytes(porudzbina));

                    if (poruka == "kraj")
                        break;

                    brBajta = clientSocketTCP.Receive(buffer);

                    if (brBajta == 0)
                    {
                        Console.WriteLine("Server je zavrsio sa radom");
                        break;
                    }

                    string odgovor = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine(odgovor);
                    if (odgovor == "kraj")
                        break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }
            }
            Console.WriteLine("Klijent zavrsava sa radom");
            clientSocketTCP.Close();
            clientSocketUDP.Close();
            Console.ReadKey();
        }

    }
}
