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
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            while (true)
            {
                byte[] buffer = new byte[1024];
                byte[] prijemniBafer = new byte[1024];
                Console.WriteLine("----------Unos stanja stolova-------------");
                Console.WriteLine("Unesite broj stola: ");
                int brojStola;
                int.TryParse(Console.ReadLine(), out brojStola);
                Console.WriteLine("Unesite broj gostiju: ");
                int brojGostiju;
                int.TryParse(Console.ReadLine(),out brojGostiju);


                Console.WriteLine("\nUnos porudzbina za sto:");
                
                List<Porudzbina> porudzbine = new List<Porudzbina>();
                for (int i = 0; i < brojGostiju; i++)
                {
                    Porudzbina porudzbina = new Porudzbina();
                    Console.WriteLine("Porudzbina za gosta br. " + i+1);
                    Console.WriteLine("Naziv artikla: ");
                    string nazivArtikla = Console.ReadLine();
                    Console.WriteLine("Kategorija porudzbine: 0 - PICE 1 - HRANA");
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
                    //salje poruku SALJEM PORUDZBINU
                    int brBajtaInfoPoruke = clientSocketTCP.Send(Encoding.UTF8.GetBytes("SLANJE PORUDZBINE"));

                    BinaryFormatter formatter = new BinaryFormatter();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, sto);
                        byte[] data = ms.ToArray();

                        int brBajta = clientSocketUDP.SendTo(data, 0, data.Length, SocketFlags.None, destinationEP);
                        Console.WriteLine("Stanje stola prosledjeno serveru");
                       
                    }

                    //salje listu porudzbina serveru
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, porudzbine);
                        byte[] data = ms.ToArray();

                        clientSocketTCP.Send(data);
                        Console.WriteLine("Porudzbina prosledjena serveru");

                    }
                    porudzbine.Clear();


                    //konobar prima gotove porudzbine
                    byte[] brojStolaGotovePorudzbine = new byte[4]; // int zauzima 4 bajta
                    int brPrimljenihBajtovaGotovePorudzbine = clientSocketTCP.Receive(brojStolaGotovePorudzbine);
                    int konvertovanBrojStola = BitConverter.ToInt32(brojStolaGotovePorudzbine, 0);
                    Console.WriteLine("Dostavljena porudzbina za sto broj" + konvertovanBrojStola);

                    //konobar salje zahtev za racun
                    //salje poruku RACUN
                    int brBajtaInfoPorukeRacun = clientSocketTCP.Send(Encoding.UTF8.GetBytes("OBRACUN RACUNA"));

                    Console.WriteLine("Sto broj " + brojStola + " zeli da plati racun. Saljem zahtev za racun serveru...");

                    var tuple = new Tuple<int, string>(brojStola, "Zahtev za racun");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, tuple);
                        byte[] data = ms.ToArray();

                        clientSocketTCP.Send(data);
                    }

                    byte[] racun = new byte[4]; // int zauzima 4 bajta
                    int brPrimljenihBajtova = clientSocketTCP.Receive(racun);

                    int broj = BitConverter.ToInt32(racun, 0); // pretvori bajtove nazad u int
                    Console.WriteLine($"Racun stola" + brojStola + " iznosi: " + broj);
                    
                    Random randNovac = new Random();
                    int kusur = randNovac.Next(broj, 2 * broj) - broj;
                    clientSocketTCP.Send(BitConverter.GetBytes(kusur));
                    Console.WriteLine("Kusur stola broj " + brojStola+  "iznosi: " + kusur);

                    if (Console.ReadLine().Equals("stop"))
                        break;

                    //int brBajta = clientSocketUDP.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destinationEP); // Poruka koju saljemo u binarnom zapisu, pocetak poruke, duzina, flegovi, odrediste

                    ////Console.WriteLine($"Uspesno poslato {brBajta} ka {destinationEP}");

                    //brBajta = clientSocketUDP.ReceiveFrom(prijemniBafer, ref posiljaocEP);

                    //string ehoPoruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);

                    //Console.WriteLine($"Stigao je odgovor od {posiljaocEP}, duzine {brBajta}, eho glasi:\n{ehoPoruka}"); // 4
                    ////tcp
                    //Console.WriteLine("Unesite porudzbinu: ");
                    //string porudzbina = Console.ReadLine();
                    //int brBajtaPorudzbine = clientSocketTCP.Send(Encoding.UTF8.GetBytes(porudzbina));

                    //if (poruka == "kraj")
                    //    break;

                    //brBajta = clientSocketTCP.Receive(buffer);

                    //if (brBajta == 0)
                    //{
                    //    Console.WriteLine("Server je zavrsio sa radom");
                    //    break;
                    //}

                    //string odgovor = Encoding.UTF8.GetString(buffer);

                    //Console.WriteLine(odgovor);
                    //if (odgovor == "kraj")
                    //    break;
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
