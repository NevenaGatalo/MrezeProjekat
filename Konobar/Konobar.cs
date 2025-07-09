using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Biblioteka;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace Konobar
{
    public class Konobar
    {
        static void Main(string[] args)
        {

            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint destinationEPTcp = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 50001);


            Socket clientSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 50002); // Odredisni IPEndPoint, IP i port ka kome saljemo. U slucaju 8. tacke je potrebno uneti IP adresu server racunara
           
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Konobar je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocketTCP.Connect(destinationEPTcp);

            //konobar salje poruku da je konobar
            int brBajtaPorudzbine = clientSocketTCP.Send(Encoding.UTF8.GetBytes("TIP:KONOBAR"));
            Console.WriteLine("Konobar je uspesno povezan sa serverom!");

            Console.WriteLine("==============================KONOBAR================================");
            while (true)
            {
                byte[] buffer = new byte[1024];
                byte[] prijemniBafer = new byte[1024];
                Console.WriteLine("\n------------------------Unos stanja stolova--------------------------");
                Console.Write("Unesite broj stola: ");
                int brojStola;
                int.TryParse(Console.ReadLine(), out brojStola);
                Console.Write("Unesite broj gostiju: ");
                int brojGostiju;
                int.TryParse(Console.ReadLine(),out brojGostiju);


                Console.WriteLine("\n-----------------------Unos porudzbina za sto------------------------");
                
                List<Porudzbina> porudzbine = new List<Porudzbina>();
                for (int i = 1; i <= brojGostiju; i++)
                {
                    Porudzbina porudzbina = new Porudzbina();
                    Console.WriteLine("\nPorudzbina za gosta broj " + i);
                    Console.Write("Naziv artikla > ");
                    string nazivArtikla = Console.ReadLine();
                    Console.Write("Kategorija porudzbine: 0 - PICE 1 - HRANA > ");
                    string kategorijaString = Console.ReadLine();
                    int kategorija;
                    int.TryParse(kategorijaString, out kategorija);
                    Random rand = new Random();
                    int cena = rand.Next(100, 5001);

                    porudzbina.nazivArtikla = nazivArtikla;
                    if (kategorija == 0)
                        porudzbina.kategorija = Kategorija.PICE;
                    else
                        porudzbina.kategorija = Kategorija.HRANA;
                    porudzbina.cena = cena;
                    //porudzbina.status = StatusPorudzbina.U_PRIPREMI;
                    porudzbina.brojStola = brojStola;
                    porudzbine.Add(porudzbina);
                }

                Sto sto = new Sto()
                {
                    brStola = brojStola,
                    brGostiju = brojGostiju,
                    status = StatusSto.ZAUZET
                };

                try
                {

                    BinaryFormatter formatter = new BinaryFormatter();
                    //salje stanje stola
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, sto);
                        byte[] data = ms.ToArray();

                        int brBajta = clientSocketUDP.SendTo(data, 0, data.Length, SocketFlags.None, destinationEP);
                        Console.WriteLine($"\n{"[STANJE STOLA]",-18} Stanje stola prosledjeno serveru.");
                    }

                    //salje listu porudzbina serveru
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, porudzbine);
                        byte[] data = ms.ToArray();

                        clientSocketTCP.Send(data);
                        Console.WriteLine($"{"[PORUDZBINE]",-18} Porudzbine prosledjena serveru.");

                    }
                    porudzbine.Clear();

                    //konobar salje zahtev za racun
                    Console.WriteLine($"\n{"[ZAHTEV ZA RACUN]",-18} Saljem zahtev za racun serveru...");


                    byte[] racun = new byte[4]; // int zauzima 4 bajta
                    int brPrimljenihBajtova = clientSocketTCP.Receive(racun);

                    int broj = BitConverter.ToInt32(racun, 0); // pretvori bajtove nazad u int
                    Console.WriteLine($"{"[RACUN]",-18} Racun stola broj " + brojStola + " iznosi > " + broj + " dinara.");
                    
                    Random randNovac = new Random();
                    int kusur = randNovac.Next(broj, 2 * broj) - broj;
                    Console.WriteLine($"{"[KUSUR]",-18} Kusur stola broj " + brojStola+  " iznosi > " + kusur + " dinara.");

                    //konobar prima gotove porudzbine
                    byte[] brojStolaGotovePorudzbine = new byte[4]; // int zauzima 4 bajta
                    int brPrimljenihBajtovaGotovePorudzbine = clientSocketTCP.Receive(brojStolaGotovePorudzbine);
                    int konvertovanBrojStola = BitConverter.ToInt32(brojStolaGotovePorudzbine, 0);
                    Console.WriteLine($"\n{"[DOSTAVLJENO]",-18} Dostavljena porudzbina za sto broj > " + konvertovanBrojStola);

                    if (Console.ReadLine().Equals("stop"))
                        break;

                    Console.WriteLine("=====================================================================");
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
