using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Biblioteka;
using System.Runtime.Serialization;
using System.Collections;

namespace Server2
{
    public class Server2
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



            //List<Socket> klijenti = new List<Socket>(); // Pravimo posebnu listu za klijentske sokete kako nam je ne bi obrisala Select funkcija

            Dictionary<Socket, Osoblje> osoblje = new Dictionary<Socket, Osoblje>();
            Dictionary<int, Socket> konobarPoStolu = new Dictionary<int, Socket>();

            List<Porudzbina> porudzbine = new List<Porudzbina>();
            List<Porudzbina> neobradjenePorudzbine = new List<Porudzbina>();



            try
            {
                Console.WriteLine("Server je pokrenut! Za zavrsetak rada pritisnite Escape");

                byte[] buffer = new byte[1024];
                byte[] typeBuffer = new byte[1024];
                byte[] prijemniBaferPorudzbina = new byte[1024];
                byte[] prijemniBaferSto = new byte[1024];
                byte[] infoKonobarBuffer = new byte[1024];

                BinaryFormatter formatter = new BinaryFormatter();
                
                int iteracije2 = 0;
                while (true)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    Array.Clear(typeBuffer, 0, typeBuffer.Length);
                    Array.Clear(prijemniBaferPorudzbina, 0, prijemniBaferPorudzbina.Length);
                    Array.Clear(prijemniBaferSto, 0, prijemniBaferSto.Length);
                    Array.Clear(infoKonobarBuffer, 0, infoKonobarBuffer.Length);

                    List<Socket> checkRead = new List<Socket>();
                    List<Socket> checkError = new List<Socket>();

                    if (osoblje.Count < maxKlijenata)
                    {
                        checkRead.Add(serverSocketTCP);

                    }
                    checkError.Add(serverSocketTCP);

                    foreach (Socket s in osoblje.Keys)
                    {
                        checkRead.Add(s);
                        checkError.Add(s);
                    }


                    Socket.Select(checkRead, null, checkError, 1000);


                    if (checkRead.Count > 0)
                    {
                        Console.WriteLine($"Broj dogadjaja je: {checkRead.Count}");
                        foreach (Socket s in checkRead)
                        {
                            iteracije2++;
                            Console.WriteLine("Iteracije2 " + iteracije2);
                            //ako neko zeli da se konektuje
                            if (s == serverSocketTCP)
                            {

                                Socket client = serverSocketTCP.Accept();
                                client.Blocking = false;
                                Console.WriteLine($"Server se povezao sa {client.RemoteEndPoint}");

                                //primamo vrstu klijenta koji se konektovao

                                int bytes = client.Receive(typeBuffer);
                                if (Encoding.UTF8.GetString(typeBuffer, 0, bytes).Equals("TIP:KONOBAR"))
                                {
                                    Osoblje o = new Osoblje(TipOsoblja.KONOBAR, StatusOsoblja.SLOBODAN);
                                    osoblje[client] = o;
                                }
                                else if (Encoding.UTF8.GetString(typeBuffer, 0, bytes).Equals("TIP:KUVAR"))
                                {
                                    Osoblje o = new Osoblje(TipOsoblja.KUVAR, StatusOsoblja.SLOBODAN);
                                    osoblje[client] = o;
                                }
                                else
                                {
                                    Osoblje o = new Osoblje(TipOsoblja.BARMEN, StatusOsoblja.SLOBODAN);
                                    osoblje[client] = o;
                                }
                            }
                            //neko ko je vec konektovan sa serverom salje poruku
                            else
                            {

                                while (s.Available > 0)
                                {
                                    int brBajta = s.Receive(buffer);
                                    if (brBajta == 0)
                                    {
                                        Console.WriteLine("Klijent je prekinuo komunikaciju");
                                        s.Close();
                                        osoblje.Remove(s);

                                        break;
                                    }
                      
                                    
                                        //ako se konobar obratio
                                        string poruka = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                        #region Obracanje Konobara
                                        if (osoblje[s].tip == TipOsoblja.KONOBAR)
                                        {
                                                if (poruka == "OBRACUN RACUNA")
                                                {
                                                    byte[] bufferZahtevRacun = new byte[1024];
                                                    int primljeno =  s.Receive(bufferZahtevRacun);
                                                    Console.WriteLine("Primljen zahtev za racun");
                                                    using (MemoryStream ms = new MemoryStream(bufferZahtevRacun, 0, primljeno))
                                                    {
                                                        var tuple = (Tuple<int, string>)formatter.Deserialize(ms);
                                                        int brSt = tuple.Item1;
                                                        string zahtev = tuple.Item2;
                                                        int racun = 0;
                                                        if (zahtev.Equals("Zahtev za racun"))
                                                        {
                                                            foreach (var st in stolovi)
                                                            {
                                                                if (st.brStola == brSt)
                                                                {
                                                                    foreach (var p in st.porudzbine)
                                                                    {
                                                                        racun += p.cena;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        byte[] data = BitConverter.GetBytes(racun);
                                                        s.Send(data);

                                                        byte[] kusur = new byte[4];
                                                        s.Receive(kusur);
                                                        Console.WriteLine("Vracen kusur stolu broj " + brSt);
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Konobar se obratio");
                                                    osoblje[s].status = StatusOsoblja.ZAUZET;
                                                    #region Primanje poruke koji sto je zauzet
                                                    //primljena poruka: sto koji je zauzet
                                                    int brBajtaUDP = serverSocketUDP.ReceiveFrom(prijemniBaferSto, ref posiljaocEP); // Primamo poruku i podatke o posiljaocu

                                                    if (brBajtaUDP == 0)
                                                    {
                                                        break;
                                                    }

                                                    using (MemoryStream ms = new MemoryStream(prijemniBaferSto, 0, brBajtaUDP))
                                                    {
                                                        Sto sto = (Sto)formatter.Deserialize(ms);
                                                        foreach (var st in stolovi)
                                                        {
                                                            if (st.brStola == sto.brStola)
                                                            {
                                                                st.brGostiju = sto.brGostiju;
                                                                st.status = StatusSto.ZAUZET;
                                                            }
                                                        }
                                                        Console.WriteLine($"Primljen sto br {sto.brStola} sa {sto.brGostiju} gostiju.");
                                                        konobarPoStolu[sto.brStola] = s;
                                                    }
                                                    #endregion
                                                    #region Primanje porudzbine za sto
                                                    //tcp
                                                    //primljena poruka: porudzbina za sto
                                                    int brBajtaTCP = s.Receive(prijemniBaferPorudzbina);
                                                    if (brBajtaTCP == 0)
                                                    {
                                                        Console.WriteLine("Konobar je zavrsio sa radom");
                                                        break;
                                                    }
                                                    using (MemoryStream ms = new MemoryStream(prijemniBaferPorudzbina, 0, brBajtaTCP))
                                                    {
                                                        BinaryFormatter bf = new BinaryFormatter();
                                                        List<Porudzbina> porudzbineNove = bf.Deserialize(ms) as List<Porudzbina>;
                                                        Console.WriteLine("Primljena porudzbina stola broj " + porudzbineNove[0].brojStola);
                                                        foreach (var st in stolovi)
                                                        {
                                                            if (st.brStola == porudzbineNove[0].brojStola)
                                                            {
                                                                st.porudzbine = porudzbineNove;
                                                            }
                                                        }
                                                        foreach (var p in porudzbineNove)
                                                        {
                                                            Console.WriteLine(p.nazivArtikla);
                                                            porudzbine.Add(p);
                                                        }
                                                    }
                                                    #endregion
                                                    #region Slanje porudzbine kuvaru/barmenu
                                                    ////slanje porudzbine kuvaru (za testiranje)
                                                    using (MemoryStream ms = new MemoryStream())
                                                    {
                                                        foreach (var p in porudzbine)
                                                        {
                                                            if (p.kategorija == Kategorija.HRANA)
                                                            {
                                                                //nadji slobodnog kuvara
                                                                foreach (Socket klijent in osoblje.Keys)
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
                                                                        Console.WriteLine("Porudzbina prosledjena kuvaru!");
                                                                        Console.WriteLine("Naziv por: " + p.nazivArtikla);
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //nadji slobodnog barmena
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
                                                                        Console.WriteLine("Porudzbina prosledjena barmenu!");
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            //dodaj u neobradjene porudzbine
                                                            if (p.status != StatusPorudzbina.U_PRIPREMI)
                                                                neobradjenePorudzbine.Add(p);
                                                        }
                                                        porudzbine = neobradjenePorudzbine;
                                                    }
                                                }
                                            



                                        }
                                        #endregion
                                        #endregion
                                        #region Obracanje Kuvara
                                        //kuvar
                                        else if (osoblje[s].tip == TipOsoblja.KUVAR)
                                        {
                                            //primanje porudzbine
                                            Console.WriteLine("Kuvar se obratio!");
                                            byte[] porudzbina = new byte[1024];
                                        //try
                                        //{
                                        //    int brPrimljenihBajtova = s.Receive(porudzbina);
                                        //    if (brPrimljenihBajtova > 0)
                                        //    {
                                        //        using (MemoryStream ms = new MemoryStream(porudzbina, 0, brPrimljenihBajtova))
                                        //        {
                                        //            BinaryFormatter bf = new BinaryFormatter();
                                        //            Porudzbina p = bf.Deserialize(ms) as Porudzbina;
                                        //            foreach (Sto st in stolovi)
                                        //            {
                                        //                if (st.brStola == p.brojStola)
                                        //                {
                                        //                    foreach (Porudzbina por in st.porudzbine)
                                        //                    {
                                        //                        if (por.nazivArtikla == p.nazivArtikla)
                                        //                            por.status = StatusPorudzbina.SPREMNO;
                                        //                    }
                                        //                }
                                        //            }
                                        //            osoblje[s].status = StatusOsoblja.SLOBODAN;
                                        //        }
                                        //    }
                                        //}
                                        //catch (SocketException ex)
                                        //{
                                        //    if (ex.SocketErrorCode == SocketError.WouldBlock)
                                        //    {
                                        //        // Poruka još nije stigla, ne paniči, idi dalje
                                        //        Console.WriteLine("Ovde pada!!!!!!");
                                        //        continue;
                                        //    }
                                        //    else
                                        //    {
                                        //        Console.WriteLine("Greška prilikom primanja od kuvara: " + ex.Message);
                                        //    }
                                        //}
                                        try
                                        {
                                            Console.WriteLine("Usao ponovo");
                                            if (s.Available > 0)
                                            {
                                                int brPrimljenihBajtova = s.Receive(porudzbina);
                                                if (brPrimljenihBajtova > 0)
                                                {
                                                    using (MemoryStream ms = new MemoryStream(porudzbina, 0, brPrimljenihBajtova))
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
                                                                        por.status = StatusPorudzbina.SPREMNO;
                                                                }
                                                            }
                                                        }
                                                        osoblje[s].status = StatusOsoblja.SLOBODAN;
                                                    }
                                                }
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
                                        #region Obracanje Barmena
                                        //barmen
                                        else
                                        {
                                            Console.WriteLine("Barmen se obratio");
                                        }
                                        #endregion
                                    
                                }
                            }

                            //vracanje porudzbina nazad konobaru zaduzenom za taj sto
                            #region Vracanje porudzbina nazad zaduzenom konobaru
                            int brojacSpremnihPorudzbina = 0;
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
                                            //sto je slobodan
                                            sto.status = StatusSto.SLOBODAN;
                                            //posalji za koji sto su porudzbine gotove
                                            byte[] data = BitConverter.GetBytes(sto.brStola);
                                            konobarPoStolu[brojStola].Send(data);
                                            Console.WriteLine("Porudzbine poslate nazad konobaru");
                                        }
                                    }
                                }
                            }
                            #endregion

                            if (Console.KeyAvailable)
                            {
                                if (Console.ReadKey().Key == ConsoleKey.Escape)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    checkRead.Clear();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske {ex}");
            }


            foreach (Socket s in osoblje.Keys)
            {
                s.Send(Encoding.UTF8.GetBytes("Server je zavrsio sa radom"));
                s.Close();
            }

            Console.WriteLine("Server zavrsava sa radom");
            Console.ReadKey();
            serverSocketTCP.Close();

        }
    }
}
