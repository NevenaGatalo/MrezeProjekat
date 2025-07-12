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
using System.Runtime.Remoting.Messaging;

namespace Konobar
{
    public class Konobar
    {
        static void Main(string[] args)
        {

            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint destinationEPTcp = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50001);

            //udp socket za stanje stolova
            Socket clientSocketStanjeStolovaUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEPStanjeStolova = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50002);
           
            EndPoint posiljaocStanjeStolovaEP = new IPEndPoint(IPAddress.Any, 0);

            //udp socket za slanje rezervacija
            Socket clientSocketRezevacijeUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEPRezervacije = new IPEndPoint(IPAddress.Parse("192.168.100.8"), 50003);

            EndPoint posiljaocRezervacijeEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Konobar je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocketTCP.Connect(destinationEPTcp);

            //konobar salje poruku da je konobar
            int brBajtaPorudzbine = clientSocketTCP.Send(Encoding.UTF8.GetBytes("TIP:KONOBAR"));
            Console.WriteLine("Konobar je uspesno povezan sa serverom!");

            Console.WriteLine("==============================KONOBAR================================");
            while (true)
            {
                //meni
                int opcija = Meni();


                BinaryFormatter formatter = new BinaryFormatter();
                switch (opcija)
                {
                    case 1:
                        int brStola = UnosRezervacije();
                        StoloviRepozitorijum.IspisiStolove();
                        Console.WriteLine(RezervacijeStolova.RezervacijeToString());

                        //treba da posaljem rezervaciju
                        #region Slanje Rezervacije
                        using (MemoryStream ms = new MemoryStream())
                        {
                            formatter.Serialize(ms, RezervacijeStolova.rezervacije);
                            byte[] data = ms.ToArray();

                            int bytesSent = clientSocketRezevacijeUDP.SendTo(data, 0, data.Length, SocketFlags.None, destinationEPRezervacije);

                            Console.WriteLine($"Poslato {bytesSent} bajtova rezervacija preko UDP.");
                        }
                        #endregion

                        break;
                    case 2:

                        byte[] buffer = new byte[1024];
                        byte[] prijemniBafer = new byte[1024];
                        //pita ga da li imate rez
                            //ne -> unos stanja stolova
                            //da -> unesite na cije ime je rez
                        Console.WriteLine("\n------------------------Unos stanja stolova--------------------------");
                        //ispise sve stolove
                        Console.Write("Unesite broj stola: ");
                        int brojStola;
                        int.TryParse(Console.ReadLine(), out brojStola);
                        //provera da li je unet slobodan sto
                        Console.Write("Unesite broj gostiju: ");
                        int brojGostiju;
                        int.TryParse(Console.ReadLine(), out brojGostiju);


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

                            //salje stanje stola
                            using (MemoryStream ms = new MemoryStream())
                            {
                                formatter.Serialize(ms, sto);
                                byte[] data = ms.ToArray();

                                int brBajta = clientSocketStanjeStolovaUDP.SendTo(data, 0, data.Length, SocketFlags.None, destinationEPStanjeStolova);
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
                            Console.WriteLine($"{"[KUSUR]",-18} Kusur stola broj " + brojStola + " iznosi > " + kusur + " dinara.");

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
                        break;
                }
            }
            Console.WriteLine("Klijent zavrsava sa radom");
            clientSocketTCP.Close();
            clientSocketStanjeStolovaUDP.Close();
            Console.ReadKey();
        }
        private static int Meni()
        {
            //treba do while
            Console.WriteLine("------------------MENI----------------");
            Console.WriteLine("1. Napravi rezervaciju\n2. Usluzi gosta");
            int opcija;
            while(!Int32.TryParse(Console.ReadLine(), out opcija) || opcija < 1)
            {
                Console.WriteLine("Niste uneli validan broj");
            }
            return opcija;
        }
        private static int UnosRezervacije()
        {
            Console.WriteLine("trenutno stanje stolova:");
            StoloviRepozitorijum.IspisiStolove();
            Console.WriteLine("izaberite sto koji zelite da rezervisete:");
            int brStola;

            //treba do while
            while(!Int32.TryParse(Console.ReadLine(), out brStola))
            {
                Console.WriteLine("unesite validan broj stola");
            }
            while (!DaLiJeStoSlobodan(brStola))
            {
                Console.WriteLine("unesite broj slobodnog stola");
            }
            Console.WriteLine("za koliko sati je rezervacija");
            int satRezervacije = int.Parse(Console.ReadLine());
            Console.WriteLine("na cije ime je rezervacija:");
            string imeRezervacije = Console.ReadLine();
            //popuni rezervaciju
            RezervacijeStolova.rezervacije.Add(brStola, new Tuple<int, string> (  satRezervacije, imeRezervacije  ));
            return brStola;
        }
        private static bool DaLiJeStoSlobodan(int brStola)
        {
            foreach(Sto s in StoloviRepozitorijum.stolovi)
            {
                if (brStola == s.brStola && s.status == StatusSto.SLOBODAN)
                    return true;

            }
            return false;
        }
    }
    
}
