using Biblioteka;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server4
{
    public class Server4
    {
        static void Main(string[] args)
        {
            //tcp konekcija
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEPTCP = new IPEndPoint(IPAddress.Any, 50001);

            //udp konekcija Stanje stolova
            Socket serverSocketStanjeStolovaUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEPStanjeStolovaUDP = new IPEndPoint(IPAddress.Any, 50002);
            EndPoint posiljaocStanjeStolovaEP = new IPEndPoint(IPAddress.Any, 0);

            //udp konekcija rezervacije
            Socket serverSocketRezervacijeUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEPRezervacijeUDP = new IPEndPoint(IPAddress.Any, 50003);
            EndPoint posiljaocRezervacijeEP = new IPEndPoint(IPAddress.Any, 0);

            serverSocketTCP.Bind(serverEPTCP);
            serverSocketRezervacijeUDP.Bind(serverEPRezervacijeUDP);
            serverSocketStanjeStolovaUDP.Bind(serverEPStanjeStolovaUDP);

            serverSocketTCP.Blocking = false;
            int maxKlijenata = 5;
            serverSocketTCP.Listen(maxKlijenata);


            Console.WriteLine("=============================================SERVER===============================================");
            Console.WriteLine($"{"[INFO]",-18} Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEPTCP}\n");


            List<Socket> acceptedSockets = new List<Socket>();
            //Dictionary<Socket, Osoblje> osoblje = new Dictionary<Socket, Osoblje>();
            List<Porudzbina> porudzbine = new List<Porudzbina>();
            Dictionary<int, Socket> konobarPoStolu = new Dictionary<int, Socket>();

            //lista endpointova konobara koji su se obratili
            List<EndPoint> konobariEndPoints = new List<EndPoint>();

            //proverava da li su rezervacije istekle i cisti ih
            Task.Run(() =>
            {
                DateTime vremePocetka = DateTime.Now;
                while (true)
                {
                    Thread.Sleep(5000); // proverava svakih 5 sekundi
                    int simuliraniSat = (int)(DateTime.Now - vremePocetka).TotalMinutes;
                    List<int> zaBrisanje = RezervacijeStolova.OčistiIstekleRezervacije(simuliraniSat);
                    //posalji listu isteklih rezervacija ako postoji neka istekla rezervacija
                    if (zaBrisanje.Count != 0)
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            formatter.Serialize(ms, zaBrisanje);
                            byte[] data = ms.ToArray();

                            foreach (var ep in konobariEndPoints)
                            {
                                int bytesSent = serverSocketRezervacijeUDP.SendTo(data, 0, data.Length, SocketFlags.None, ep);
                            }
                            //ispis stolova
                            Console.WriteLine($"{"[INFO]",-18} Istekla rezervacija za sto: {string.Join(", ", zaBrisanje)}\n");

                            StoloviRepozitorijum.IspisiStolove();
                        }
                    }
                    //Console.WriteLine($"[SIMULACIJA VREMENA] Trenutni simulirani sat: {simuliraniSat}h");
                }
            });


            while (true)
            {

                if (serverSocketTCP.Poll(2000 * 1000, SelectMode.SelectRead))
                {
                    Socket acceptedSocket = serverSocketTCP.Accept();
                    IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
                    acceptedSockets.Add(acceptedSocket);
                    Osoblje o = KoSeObratio(acceptedSocket);
                    OsobljeRepozitorijum.osoblje[acceptedSocket] = o;
                    Console.WriteLine($"{"[KLIJENT]",-18} Povezao se " + o.tip.ToString().ToLower() + "! Njegova adresa je " + clientEP);
                }
                int konobarCount = 0;
                int kuvarCount = 0;
                int barmenCount = 0;
                foreach (Osoblje o in OsobljeRepozitorijum.osoblje.Values)
                {
                    if (o.tip == TipOsoblja.KONOBAR) konobarCount++;
                    else if (o.tip == TipOsoblja.KUVAR) kuvarCount++;
                    else barmenCount++;
                }
                if (konobarCount == 0 || kuvarCount == 0 || barmenCount == 0 || acceptedSockets.Count < maxKlijenata)
                {
                    continue;
                }
                while (true)
                {
                    try
                    {
                        //
                        foreach (Socket s in OsobljeRepozitorijum.osoblje.Keys)
                        {
                            if (s.Poll(1500 * 1000, SelectMode.SelectRead))
                            {

                                OsobljeRepozitorijum.osoblje[s].status = StatusOsoblja.ZAUZET;
                                #region Obracanje konobara
                                if (OsobljeRepozitorijum.osoblje[s].tip == TipOsoblja.KONOBAR)
                                {
                                    //primanje stanja stola
                                    byte[] prijemniBaferSto = new byte[1024];
                                    int brBajtaUDP = serverSocketStanjeStolovaUDP.ReceiveFrom(prijemniBaferSto, ref posiljaocStanjeStolovaEP);

                                    if (brBajtaUDP == 0)
                                    {
                                        break;
                                    }
                                    Sto zauzetSto = DeserializacijaStola(prijemniBaferSto, brBajtaUDP, StoloviRepozitorijum.stolovi);
                                    konobarPoStolu[zauzetSto.brStola] = s;
                                    Console.WriteLine($"\n{"[STO]",-18} Zauzet sto broj {zauzetSto.brStola}, sa brojem gostiju: {zauzetSto.brGostiju}.");
                                    //ispisuje listu stolova
                                    StoloviRepozitorijum.IspisiStolove();

                                    //primanje liste porudzbina od konobara
                                    byte[] prijemniBaferPorudzbina = new byte[1024];
                                    int brBajtaTCP = s.Receive(prijemniBaferPorudzbina);
                                    if (brBajtaTCP == 0)
                                    {
                                        OsobljeRepozitorijum.osoblje.Remove(s);
                                        Console.WriteLine("Konobar je zavrsio sa radom");
                                        break;
                                    }
                                    DeserializacijaPorudzbina(prijemniBaferPorudzbina, brBajtaTCP, StoloviRepozitorijum.stolovi, porudzbine);
                                    Console.WriteLine($"\n{"[PORUDZBINE]",-18} Pristigle porudzbine");
                                    for (int i = 0; i < porudzbine.Count; i++)
                                    {
                                        Console.WriteLine("\t\t\t" + (i + 1) + ". " + porudzbine[i].nazivArtikla + " - " + porudzbine[i].status);
                                    }

                                    //obracun i slanje racuna konobaru
                                    int racun = ObracunRacuna(StoloviRepozitorijum.stolovi, zauzetSto);
                                    byte[] data = BitConverter.GetBytes(racun);
                                    s.Send(data);
                                    Console.WriteLine($"{"[RACUN]",-18} Poslat racun konobaru za sto broj: " + zauzetSto.brStola + ". Racun iznosi " + racun + " dinara.\n");

                                    //ispis stolova
                                    //StoloviRepozitorijum.IspisiStolove();

                                }
                                #endregion
                                #region Obracanje kuvara
                                else if (OsobljeRepozitorijum.osoblje[s].tip == TipOsoblja.KUVAR)
                                {
                                    try
                                    {
                                        byte[] bufferPorudzbina = new byte[1024];
                                        if (s.Available > 0)
                                        {
                                            int brPrimljenihBajtova = s.Receive(bufferPorudzbina);
                                            if (!PrimiGotovuPorudzbinu(brPrimljenihBajtova, StoloviRepozitorijum.stolovi, bufferPorudzbina))
                                                OsobljeRepozitorijum.osoblje.Remove(s);
                                            else
                                                OsobljeRepozitorijum.osoblje[s].status = StatusOsoblja.SLOBODAN;
                                        }
                                        else
                                        {
                                            // Nema podataka za čitanje, idi dalje (ne blokira)
                                            Console.WriteLine($"{"[ZAUZET]",-18} Kuvar nema sta da posalje sada");
                                            continue;
                                        }
                                    }
                                    catch (SocketException ex)
                                    {
                                        Console.WriteLine($"{"[GRESKA]",-18} Greška prilikom primanja od kuvara: " + ex.Message);
                                    }
                                }
                                #endregion
                                #region Obracanje barmena
                                else
                                {
                                    try
                                    {
                                        byte[] bufferPorudzbina = new byte[1024];
                                        if (s.Available > 0)
                                        {
                                            int brPrimljenihBajtova = s.Receive(bufferPorudzbina);
                                            if (!PrimiGotovuPorudzbinu(brPrimljenihBajtova, StoloviRepozitorijum.stolovi, bufferPorudzbina))
                                                OsobljeRepozitorijum.osoblje.Remove(s);
                                            else
                                                OsobljeRepozitorijum.osoblje[s].status = StatusOsoblja.SLOBODAN;
                                        }
                                        else
                                        {
                                            // Nema podataka za čitanje, idi dalje (ne blokira)
                                            Console.WriteLine($"{"[ZAUZET]",-18} Barmen nema sta da posalje sada");
                                            continue;
                                        }
                                    }
                                    catch (SocketException ex)
                                    {
                                        Console.WriteLine($"{"[GRESKA]",-18} Greška prilikom primanja od barmena: " + ex.Message);
                                    }
                                }
                                #endregion
                                //slanje porudzbina kuvaru/barmenu
                                #region Slanje porudzbine kuvaru/barmenu
                                if (porudzbine.Count > 0)
                                {
                                    List<Porudzbina> neobradjenePorudzbine = new List<Porudzbina>();
                                    foreach (var p in porudzbine)
                                    {
                                        if (p.kategorija == Kategorija.HRANA)
                                        {
                                            //nadji slobodnog kuvara
                                            if (NadjiSlobodnogKuvara(OsobljeRepozitorijum.osoblje, p))
                                            {
                                                Console.WriteLine($"{"[SALJEM KUVARU]",-18} Porudzbina <" + p.nazivArtikla + "> prosledjena kuvaru!");
                                            }
                                            else
                                            {
                                                neobradjenePorudzbine.Add(p);
                                            }
                                        }
                                        else
                                        {
                                            //nadji slobodnog barmena
                                            if (NadjiSlobodnogBarmena(OsobljeRepozitorijum.osoblje, p))
                                            {
                                                Console.WriteLine($"{"[SALJEM BARMENU]",-18} Porudzbina <" + p.nazivArtikla + "> prosledjena barmenu!");
                                            }
                                            else
                                            {
                                                neobradjenePorudzbine.Add(p);
                                            }
                                        }
                                    }
                                    porudzbine = neobradjenePorudzbine;
                                }
                                #endregion
                                //slanje porudzbine nazad kuvaru
                                SlanjeGotovePorudzbineKonobaru(konobarPoStolu, StoloviRepozitorijum.stolovi);
                            }
                        }
                        #region Primanje rezervacija
                        if (serverSocketRezervacijeUDP.Poll(1500 * 1000, SelectMode.SelectRead))
                        {
                            //primi rezervaciju
                            byte[] rezervacijaBytes = new byte[4096];
                            //EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

                            // primanje poruke
                            int primljeno = serverSocketRezervacijeUDP.ReceiveFrom(rezervacijaBytes, ref posiljaocRezervacijeEP);
                            konobariEndPoints.Add(posiljaocRezervacijeEP);

                            // deserijalizacija rezervacije
                            using (MemoryStream ms = new MemoryStream(rezervacijaBytes, 0, primljeno))
                            {
                                BinaryFormatter formatter = new BinaryFormatter();
                                var rezervacije = (Dictionary<int, Tuple<int, string>>)formatter.Deserialize(ms);

                                foreach (var r in rezervacije)
                                {
                                    RezervacijeStolova.rezervacije[r.Key] = r.Value;
                                    foreach (var sto in StoloviRepozitorijum.stolovi)
                                    {
                                        if (sto.brStola == r.Key)
                                        {
                                            sto.status = StatusSto.REZERVISAN;
                                            Console.WriteLine($"{"[INFO]",-18} Rezervacija uspešno primljena za sto broj: {r.Key}\n");
                                        }
                                    }
                                    //ispis stolova
                                    StoloviRepozitorijum.IspisiStolove();
                                }
                            }
                        }
                        #endregion
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"{"[GRESKA]",-18} Doslo je do greske: " + ex.Message);
                        break;
                    }
                }
                //potencijalno dodaj brisanje iz dictinary osoblje
                acceptedSockets.Clear();
            }

        }
        private static void SlanjeGotovePorudzbineKonobaru(Dictionary<int, Socket> konobarPoStolu, List<Sto> stolovi)
        {
            int brojacSpremnihPorudzbina = 0;
            int zapamcenBrStola = 0;
            foreach (int brojStola in konobarPoStolu.Keys)
            {
                foreach (Sto sto in stolovi)
                {
                    if (sto.brStola == brojStola)
                    {
                        foreach (Porudzbina p in sto.porudzbine)
                        {
                            if (p.status == StatusPorudzbina.SPREMNO)
                            {
                                brojacSpremnihPorudzbina++;
                            }
                        }
                        if (brojacSpremnihPorudzbina == sto.brGostiju)
                        {
                            zapamcenBrStola = brojStola;
                            //sto je slobodan
                            sto.status = StatusSto.SLOBODAN;
                            sto.porudzbine.Clear();
                            //posalji za koji sto su porudzbine gotove
                            byte[] data = BitConverter.GetBytes(sto.brStola);
                            konobarPoStolu[brojStola].Send(data);
                            Console.WriteLine($"{"[SALJEM KONOBARU]",-18} Porudzbine poslate nazad konobaru za sto broj: " + brojStola);
                            StoloviRepozitorijum.IspisiStolove();
                        }
                    }
                }
            }
            konobarPoStolu.Remove(zapamcenBrStola);
        }
        private static bool PrimiGotovuPorudzbinu(int brPrimljenihBajtova, List<Sto> stolovi, byte[] bufferPorudzbina)
        {
            if (brPrimljenihBajtova > 0)
            {
                using (MemoryStream ms = new MemoryStream(bufferPorudzbina, 0, brPrimljenihBajtova))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    Porudzbina p = bf.Deserialize(ms) as Porudzbina;

                    foreach (Sto st in stolovi)
                    {
                        if (st.brStola == p.brojStola)
                        {
                            foreach (Porudzbina por in st.porudzbine)
                            {
                                if (por.nazivArtikla == p.nazivArtikla)
                                {
                                    por.status = StatusPorudzbina.SPREMNO;
                                    Console.WriteLine($"\n{"[SPREMNA PORUDZ.]",-18} " + p.nazivArtikla + " - " + p.status);

                                }
                            }
                        }
                    }
                }
                return true;
            }
            else { return false; }
        }
        private static bool NadjiSlobodnogBarmena(Dictionary<Socket, Osoblje> osoblje, Porudzbina p)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                foreach (Socket klijent in osoblje.Keys)
                {
                    if (osoblje[klijent].tip == TipOsoblja.BARMEN && osoblje[klijent].status == StatusOsoblja.SLOBODAN)
                    {
                        osoblje[klijent].status = StatusOsoblja.ZAUZET;
                        p.status = StatusPorudzbina.U_PRIPREMI;
                        //posalji barmenu
                        ms.SetLength(0);
                        formatter.Serialize(ms, p);
                        byte[] data = ms.ToArray();
                        klijent.Send(data);
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool NadjiSlobodnogKuvara(Dictionary<Socket, Osoblje> osoblje, Porudzbina p)
        {
            foreach (Socket klijent in osoblje.Keys)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream())
                {
                    if (osoblje[klijent].tip == TipOsoblja.KUVAR && osoblje[klijent].status == StatusOsoblja.SLOBODAN)
                    {
                        osoblje[klijent].status = StatusOsoblja.ZAUZET;
                        p.status = StatusPorudzbina.U_PRIPREMI;
                        //posalji kuvaru
                        //ms.SetLength(0);
                        formatter.Serialize(ms, p);
                        //ToArray -> pretvara serializovani objekat iz ms u niz bajtova kako bi se slalo preko uticnice
                        byte[] data = ms.ToArray();
                        klijent.Send(data);
                        return true;
                    }
                }
            }
            return false;
        }
        private static int ObracunRacuna(List<Sto> stolovi, Sto zauzetSto)
        {
            int racun = 0;
            foreach (var s in stolovi)
            {
                if (s.brStola == zauzetSto.brStola)
                {
                    foreach (var p in s.porudzbine)
                    {
                        racun += p.cena;
                    }
                }
            }
            return racun;
        }
        private static void DeserializacijaPorudzbina(byte[] buffer, int brBajtaTCP, List<Sto> stolovi, List<Porudzbina> porudzbine)
        {
            using (MemoryStream ms = new MemoryStream(buffer, 0, brBajtaTCP))
            {
                BinaryFormatter bf = new BinaryFormatter();
                List<Porudzbina> porudzbineNove = bf.Deserialize(ms) as List<Porudzbina>;
                foreach (var st in stolovi)
                {
                    if (st.brStola == porudzbineNove[0].brojStola)
                    {
                        st.porudzbine = porudzbineNove;
                        break;
                    }
                }
                foreach (var p in porudzbineNove)
                {
                    porudzbine.Add(p);
                }
            }
        }
        private static Sto DeserializacijaStola(byte[] buffer, int brBajtaUDP, List<Sto> stolovi)
        {

            using (MemoryStream ms = new MemoryStream(buffer, 0, brBajtaUDP))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                Sto sto = (Sto)formatter.Deserialize(ms);
                foreach (var st in stolovi)
                {
                    if (st.brStola == sto.brStola)
                    {
                        st.brGostiju = sto.brGostiju;
                        st.status = StatusSto.ZAUZET;
                    }
                }
                //potencijalno ce trebati ako treba da se vraca porudzbina istom konobaru
                //konobarPoStolu[sto.brStola] = s;
                return sto;
            }
        }
        private static Osoblje KoSeObratio(Socket client)
        {
            byte[] typeBuffer = new byte[1024];
            while (true)
            {
                if (client.Poll(0, SelectMode.SelectRead))
                {
                    int bytes = client.Receive(typeBuffer);
                    if (Encoding.UTF8.GetString(typeBuffer, 0, bytes).Equals("TIP:KONOBAR"))
                    {
                        Osoblje o = new Osoblje(TipOsoblja.KONOBAR, StatusOsoblja.SLOBODAN);
                        return o;
                    }
                    else if (Encoding.UTF8.GetString(typeBuffer, 0, bytes).Equals("TIP:KUVAR"))
                    {
                        Osoblje o = new Osoblje(TipOsoblja.KUVAR, StatusOsoblja.SLOBODAN);
                        return o;
                    }
                    else
                    {
                        Osoblje o = new Osoblje(TipOsoblja.BARMEN, StatusOsoblja.SLOBODAN);
                        return o;
                    }
                }
            }
        }

    }

}
