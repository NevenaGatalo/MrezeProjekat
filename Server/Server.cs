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
            List<Sto> stolovi = new List<Sto>()
            {
                new Sto() {brStola=1, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=2, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=3, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=4, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=5, brGostiju = 0, status = StatusSto.SLOBODAN}
            };
            //mozda je dovoljna i samo lista porudzbina unutar svakog stola, bez ove globalne liste, promeniti kasnije
            List<Porudzbina> porudzbineServera = new List<Porudzbina>();

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

            //povezivanje konobara sa serverom
            Socket socketKonobar = serverSocketTCP.Accept();
            IPEndPoint clientEPKonobar = socketKonobar.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Povezao se novi konobar! Njegova adresa je {clientEPKonobar}");

            //povezivanje kuavra sa serverom
            Socket socketKuvar = serverSocketTCP.Accept();
            IPEndPoint clientEPKuvar = socketKuvar.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Povezao se novi kuvar! Njegova adresa je {clientEPKuvar}");

            //povezivanje barmena sa serverom
            Socket socketBarmen = serverSocketTCP.Accept();
            IPEndPoint clientEPBarmen = socketBarmen.RemoteEndPoint as IPEndPoint;
            Console.WriteLine($"Povezao se novi barmen! Njegova adresa je {clientEPBarmen}");


            BinaryFormatter formatter = new BinaryFormatter();

            byte[] prijemniBaferPorudzbina = new byte[1024];
            byte[] prijemniBaferSto = new byte[1024];
            while (true)
            {
                try
                {
                    //primljena poruka: sto koji je zauzet
                    int brBajta = serverSocketUDP.ReceiveFrom(prijemniBaferSto, ref posiljaocEP); // Primamo poruku i podatke o posiljaocu
                    
                    if (brBajta == 0) break;

                    using (MemoryStream ms = new MemoryStream(prijemniBaferSto, 0, brBajta))
                    {
                        Sto sto = (Sto)formatter.Deserialize(ms);
                        foreach (var s in stolovi)
                        {
                            if (s.brStola == sto.brStola)
                            {
                                s.brGostiju = sto.brGostiju;
                                s.status = StatusSto.ZAUZET;
                            }
                        }
                        Console.WriteLine($"Primljen sto br {sto.brStola} sa {sto.brGostiju} gostiju.");
                    }

                    //tcp
                    //primljena poruka: porudzbina za sto
                    int brBajtaTCP = socketKonobar.Receive(prijemniBaferPorudzbina);
                    if (brBajtaTCP == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom");
                        break;
                    }
                    using (MemoryStream ms = new MemoryStream(prijemniBaferPorudzbina, 0, brBajtaTCP))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Porudzbina> porudzbineNove = bf.Deserialize(ms) as List<Porudzbina>;
                        Console.WriteLine("Primljena porudzbina stola broj " + porudzbineNove[0].brojStola);
                        foreach (var s in stolovi)
                        {
                            if (s.brStola == porudzbineNove[0].brojStola)
                            {
                                s.porudzbine = porudzbineNove;
                            }
                        }
                        foreach (var p in porudzbineNove)
                        {
                            Console.WriteLine(p.nazivArtikla);
                            porudzbineServera.Add(p);
                        }
                    }
                    //slanje porudzbine kuvaru (za testiranje)
                    using (MemoryStream ms = new MemoryStream())
                    {
                        foreach (var s in stolovi)
                        {
                            if(s.porudzbine.Count != 0)
                            {
                                formatter.Serialize(ms, stolovi[0].porudzbine);
                                byte[] data = ms.ToArray();

                                socketKuvar.Send(data);
                                Console.WriteLine("Porudzbina prosledjena kuvaru!");
                                break;
                            }

                        }
                    }

                    //slanje porudzbine barmenu
                    using (MemoryStream ms = new MemoryStream())
                    {
                        foreach (var s in stolovi)
                        {
                            if (s.porudzbine.Count != 0)
                            {
                                formatter.Serialize(ms, stolovi[0].porudzbine);
                                byte[] data = ms.ToArray();

                                socketBarmen.Send(data);
                                Console.WriteLine("Porudzbina prosledjena barmenu!");
                                break;
                            }

                        }
                    }

                    //server nazad prima porudzbine od kuvara
                    int brBajtaTCPPrimljenePorudzbine = socketKuvar.Receive(prijemniBaferPorudzbina);
                    if (brBajtaTCPPrimljenePorudzbine == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom");
                        break;
                    }
                    using (MemoryStream ms = new MemoryStream(prijemniBaferPorudzbina, 0, brBajtaTCPPrimljenePorudzbine))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Porudzbina> porudzbineGotove = bf.Deserialize(ms) as List<Porudzbina>;
                        Console.WriteLine("Primljene gotove porudzbine stola broj " + porudzbineGotove[0].brojStola);
                        //trazenje za koji sto su gotove porudzbine
                        foreach (var s in stolovi)
                        {
                            if (s.brStola == porudzbineGotove[0].brojStola)
                            {
                                //dodeljivanje gotovih porudzbina stolu
                                s.porudzbine = porudzbineGotove;
                                //stavljanje da su porudzbine dostavljene
                                foreach(var p in s.porudzbine)
                                {
                                    p.status = StatusPorudzbina.DOSTAVLJENO;
                                }
                            }
                        }
                    }

                    //server prima nazad porudzbine od barmena
                    int brBajtaTCPPrimljenePorudzbineBarmen = socketBarmen.Receive(prijemniBaferPorudzbina);
                    if (brBajtaTCPPrimljenePorudzbineBarmen == 0)
                    {
                        Console.WriteLine("Klijent je zavrsio sa radom");
                        break;
                    }
                    using (MemoryStream ms = new MemoryStream(prijemniBaferPorudzbina, 0, brBajtaTCP))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Porudzbina> porudzbineGotove = bf.Deserialize(ms) as List<Porudzbina>;
                        Console.WriteLine("Primljene gotove porudzbine stola broj " + porudzbineGotove[0].brojStola);
                        //trazenje za koji sto su gotove porudzbine
                        foreach (var s in stolovi)
                        {
                            if (s.brStola == porudzbineGotove[0].brojStola)
                            {
                                //dodeljivanje gotovih porudzbina stolu
                                s.porudzbine = porudzbineGotove;
                                //stavljanje da su porudzbine dostavljene
                                foreach (var p in s.porudzbine)
                                {
                                    p.status = StatusPorudzbina.DOSTAVLJENO;
                                }
                            }
                        }
                        //saljem konobaru broj stola za koje su spremne porudzbine
                        byte[] data = BitConverter.GetBytes(porudzbineGotove[0].brojStola);
                        socketKonobar.Send(data);
                    }

                    //proverava da li je svaki status porudzbina istog stola == DOSTAVLJENO pa tek onda salje poruku konobaru da dostavi

                    // int brBajtaTCP = acceptedSocket.Receive(prijemniBaferPorudzbina);
                    byte[] bufferZahtevRacun = new byte[1024];
                    int primljeno = socketKonobar.Receive(bufferZahtevRacun);
                    Console.WriteLine("Primljen zahtev za racun");
                    using (MemoryStream ms = new MemoryStream(bufferZahtevRacun, 0, primljeno))
                    {
                        var tuple = (Tuple<int, string>)formatter.Deserialize(ms);
                        int brSt = tuple.Item1;
                        string zahtev = tuple.Item2;
                        int racun = 0;
                        if (zahtev.Equals("Zahtev za racun"))
                        {
                            foreach (var s in stolovi)
                            {
                                if (s.brStola == brSt)
                                {
                                    foreach (var p in s.porudzbine)
                                    {
                                        racun += p.cena;
                                    }
                                }
                            }
                        }
                        byte[] data = BitConverter.GetBytes(racun);
                        socketKonobar.Send(data);

                        byte[] kusur = new byte[4];
                        socketKonobar.Receive(kusur);
                        Console.WriteLine("Vracen kusur stolu broj " + brSt);
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
            socketKonobar.Close();
            socketKuvar.Close();
            socketBarmen.Close();

            serverSocketUDP.Close(); // Zatvaramo soket na kraju rada
            Console.ReadKey();
        }
    }
}
