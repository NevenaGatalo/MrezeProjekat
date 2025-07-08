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
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.NetworkInformation;

namespace Server4
{
    public class Server4
    {
        static void Main(string[] args)
        {
            //tcp konekcija
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEPTCP = new IPEndPoint(IPAddress.Any, 50001);

            //udp konekcija
            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEPUDP = new IPEndPoint(IPAddress.Any, 50002);
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            serverSocketTCP.Bind(serverEPTCP);
            serverSocketUDP.Bind(serverEPUDP);

            serverSocketTCP.Blocking = false;
            int maxKlijenata = 3;
            serverSocketTCP.Listen(maxKlijenata);


            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEPTCP}");


            List<Sto> stolovi = new List<Sto>()
            {
                new Sto() {brStola=1, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=2, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=3, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=4, brGostiju = 0, status = StatusSto.SLOBODAN},
                new Sto() {brStola=5, brGostiju = 0, status = StatusSto.SLOBODAN}
            };

            List<Socket> acceptedSockets = new List<Socket>();
            Dictionary<Socket, Osoblje> osoblje = new Dictionary<Socket, Osoblje>();
            List<Porudzbina> porudzbine = new List<Porudzbina>();
            Dictionary<int, Socket> konobarPoStolu = new Dictionary<int, Socket>();
            //List<Porudzbina> neobradjenePorudzbine = new List<Porudzbina>();

            while (true)
            {
                if (serverSocketTCP.Poll(2000 * 1000, SelectMode.SelectRead))
                {
                    Socket acceptedSocket = serverSocketTCP.Accept();
                    IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
                    acceptedSockets.Add(acceptedSocket);
                    Osoblje o = KoSeObratio(acceptedSocket);
                    osoblje[acceptedSocket] = o;
                    Console.WriteLine("Povezao se "+ o.tip +"! Njegova adresa je " + clientEP);
                }
                if (acceptedSockets.Count < maxKlijenata)
                {
                    continue;
                }
                while (true)
                {
                    try
                    {
                        foreach (Socket s in osoblje.Keys)
                        {
                            if (s.Poll(1500 * 1000, SelectMode.SelectRead))
                            {

                                osoblje[s].status = StatusOsoblja.ZAUZET;
                                #region Obracanje konobara
                                if (osoblje[s].tip == TipOsoblja.KONOBAR)
                                {
                                    //primanje stanja stola
                                    byte[] prijemniBaferSto = new byte[1024];
                                    int brBajtaUDP = serverSocketUDP.ReceiveFrom(prijemniBaferSto, ref posiljaocEP);

                                    if (brBajtaUDP == 0)
                                    {
                                        break;
                                    }
                                    Sto zauzetSto = DeserializacijaStola(prijemniBaferSto, brBajtaUDP, stolovi);
                                    konobarPoStolu[zauzetSto.brStola] = s;
                                    Console.WriteLine($"Primljen sto br {zauzetSto.brStola} sa {zauzetSto.brGostiju} gostiju.");

                                    //primanje liste porudzbina od konobara
                                    byte[] prijemniBaferPorudzbina = new byte[1024];
                                    int brBajtaTCP = s.Receive(prijemniBaferPorudzbina);
                                    if (brBajtaTCP == 0)
                                    {
                                        Console.WriteLine("Konobar je zavrsio sa radom");
                                        break;
                                    }
                                    DeserializacijaPorudzbina(prijemniBaferPorudzbina, brBajtaTCP, stolovi, porudzbine);
                                    Console.WriteLine("Primljena porudzbina stola broj " + zauzetSto.brStola);
                                    for (int i = 0; i < porudzbine.Count; i++)
                                    {
                                        Console.WriteLine(i + 1 + ". " + porudzbine[i].nazivArtikla);
                                    }

                                    //obracun i slanje racuna konobaru
                                    int racun = ObracunRacuna(stolovi, zauzetSto);
                                    byte[] data = BitConverter.GetBytes(racun);
                                    s.Send(data);
                                    Console.WriteLine("Poslat racun konobaru za sto broj: " + zauzetSto.brStola);
                                }
                                #endregion
                                #region Obracanje Kuvara
                                else if (osoblje[s].tip == TipOsoblja.KUVAR)
                                {
                                    try
                                    {
                                        byte[] bufferPorudzbina = new byte[1024];
                                        if (s.Available > 0)
                                        {
                                            int brPrimljenihBajtova = s.Receive(bufferPorudzbina);
                                            PrimiGotovuPorudzbinu(brPrimljenihBajtova, stolovi, bufferPorudzbina);
                                            osoblje[s].status = StatusOsoblja.SLOBODAN;
                                        }
                                        else
                                        {
                                            // Nema podataka za čitanje, idi dalje (ne blokira)
                                            Console.WriteLine("Kuvar nema sta da posalje sada");
                                            continue;
                                        }
                                    }
                                    catch (SocketException ex)
                                    {
                                        Console.WriteLine("Greška prilikom primanja od kuvara: " + ex.Message);
                                    }
                                }
                                #endregion
                                else
                                {
                                    //pozovi metodu
                                }
                                //slanje porudzbina kuvaru/barmenu
                                #region Slanje porudzbine kuvaru/barmenu
                                if (porudzbine.Count > 0)
                                {
                                    Porudzbina poslataPorudzbina = null;
                                    foreach (var p in porudzbine)
                                    {
                                        if (p.kategorija == Kategorija.HRANA)
                                        {
                                            //nadji slobodnog kuvara
                                            if (NadjiSlobodnogKuvara(osoblje, p))
                                            {
                                                poslataPorudzbina = p;
                                                Console.WriteLine("Porudzbina prosledjena kuvaru!");
                                                Console.WriteLine("Naziv por: " + p.nazivArtikla);
                                            }
                                        }
                                        else
                                        {
                                            //nadji slobodnog barmena
                                            if (NadjiSlobodnogBarmena(osoblje, p))
                                            {
                                                poslataPorudzbina = p;
                                                Console.WriteLine("Status porudzbine: " + p.status);
                                                Console.WriteLine("Porudzbina prosledjena barmenu!");
                                            }
                                        }
                                        //dodaj u neobradjene porudzbine
                                        //if (p.status != StatusPorudzbina.U_PRIPREMI)
                                        //    neobradjenePorudzbine.Add(p);
                                    }
                                    if (poslataPorudzbina != null)
                                    {
                                        porudzbine.Remove(poslataPorudzbina);
                                    }
                                    //porudzbine = neobradjenePorudzbine
                                }
                                #endregion
                                //slanje porudzbine nazad kuvaru
                                SlanjeGotovePorudzbineKonobaru(konobarPoStolu, stolovi);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Doslo je do greske {ex}");
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
                            Console.WriteLine("Porudzbine poslate nazad konobaru");
                        }
                    }
                }
            }
            konobarPoStolu.Remove(zapamcenBrStola);
        }
        private static void PrimiGotovuPorudzbinu(int brPrimljenihBajtova, List<Sto> stolovi, byte[] bufferPorudzbina)
        {
            //byte[] bufferPorudzbina = new byte[1024];
            if (brPrimljenihBajtova > 0)
            {
                using (MemoryStream ms = new MemoryStream(bufferPorudzbina, 0, brPrimljenihBajtova))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    Porudzbina p = bf.Deserialize(ms) as Porudzbina;

                    Console.WriteLine("Porudzbina: " + p.nazivArtikla);

                    foreach (Sto st in stolovi)
                    {
                        if (st.brStola == p.brojStola)
                        {
                            foreach (Porudzbina por in st.porudzbine)
                            {
                                if (por.nazivArtikla == p.nazivArtikla)
                                    por.status = StatusPorudzbina.SPREMNO;
                            }
                        }
                    }
                }
            }
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
