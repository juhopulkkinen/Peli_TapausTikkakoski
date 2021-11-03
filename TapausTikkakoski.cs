using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using Jypeli.Effects;
using System.Threading.Tasks;

/// @author Juho Pulkkinen
/// @version 13.11.2020
/// <summary>
/// Ohjelmointi 1 -kurssin harjoitustyö eli fysiikkapeli. Kyseessä on peli, jossa
/// liikutetaan pelaajaa nuolinäppäimillä sekä kerätään ja väistetään ylhäältä päin
/// pelaajaa kohti liikkuvia kohteita. Peli päättyy, kun pelaaja osuu kentän lopussa
/// olevaan seinään. Pelaajan keräämän pistemäärän perusteella ruudulle tulostuu tämän
/// jälkeen lopputulos, joka kertoo kuinka pelaajan ohjaaman viruksen maailmanvalloitus 
/// sujui pelin päättymisen jälkeen. 
/// </summary>
public class TapausTikkakoski : PhysicsGame
{
    private static readonly Image taustaKuva = LoadImage("tausta");
    private static readonly Image olionKuva = LoadImage("virus");
    private static readonly Image vihunKuva1 = LoadImage("kasidesi");
    private static readonly Image vihunKuva2 = LoadImage("maski");
    private static readonly Image kerattavanKuva1 = LoadImage("tyyppi");
    private static readonly Image lopetus = LoadImage("seina");
    private List<GameObject> liikutettavat = new List<GameObject>();
    private double suunta = -4;
    private const int VIHOLLISTEN_MAARA = 20;
    private const int KERATTAVIEN_MAARA = 50;
    private double tuhoamisY;
    private IntMeter pisteLaskuri;
    private const int KOKO = 40;
    private const int NOPEUS = 1000;
    private const int MUUTOS = 10;
    public override void Begin()
    {
        LuoValikko();
    }


    /// <summary>
    /// Aloitetaan peli luomalla pelikenttä, asettamalla tausta, asetettamalla kameran näkymä,
    /// luomalla rajat kenttään, lisäämällä pistelaskuri kenttään, ja luomalla pelaaja sekä
    /// kerättävät ja viholliset.
    /// </summary>
    private void AloitaPeli()
    {
        ClearAll(); //Puhdistaa kentän, mikäli aloitetaan vanhan pelin jälkeen uusi peli
        Level.Height = 5000;
        LuoTausta();
        Camera.Zoom(1);
        Camera.Y = Level.Bottom + 400;
        Level.CreateBorders();
        LuoPistelaskuri();
        LuoPelaaja(this, KOKO*2, KOKO*2);


        for (int i = 0; i < VIHOLLISTEN_MAARA; i++)
        {
            LuoLiikkuva(this, KOKO, KOKO, vihunKuva1, "vihu"); //luo vihollisten määrä
            LuoLiikkuva(this, KOKO, KOKO, vihunKuva2, "vihu");
        }

        for (int i = 0; i < KERATTAVIEN_MAARA; i++)
        {
            LuoLiikkuva(this, KOKO*1.5, KOKO*1.5, kerattavanKuva1, "kerattava"); //luo kerattavien määrä
        }

        LuoLopetus(this);

        VieritaKenttaa();
    }


    /// <summary>
    /// Luodaan pelin lopettava elementti eli seinä, johon pelaajan osuttua
    /// peli päättyy ja lisätään se liikutettavien listaan.
    /// </summary>
    /// <param name="peli"></param>
    private void LuoLopetus(Game peli) 
    {   
        PhysicsObject loppu = new PhysicsObject(Level.Width, Screen.Height);
        loppu.X = 0;
        loppu.Y = Level.Top +700;
        loppu.Image = lopetus;
        loppu.Tag = "loppu";
        peli.Add(loppu);
        liikutettavat.Add(loppu);
        
    }


    /// <summary>
    /// Luodaan pelin loputtua ruudulle ilmestyvä näkymä. Riippuen pelaajan keräämästä
    /// pistemäärästä ruudulle tulostuu erilainen lopputulos. Lisäksi luodaan valikko, jossa
    /// pelaaja pystyy pelin päätyttyä aloittamaan joko uuden pelin tai lopettamaan pelin.
    /// </summary>
    void LopetaPeli()
    {
        ClearAll();
        LuoTausta();
        if (pisteLaskuri.Value < 10)
        {
            Label huonoTulos = new Label
                ("Virus pääsi juuri ja juuri karkaamaan ulos terminaalista, mutta valitettavasti \n" +
                "keräsit liian vähän pisteitä, joten virus sai aikaan vain muutamia kymmeniä \n" +
                "tartuntoja Jyväskylän alueella. Heikko suoritus. Yritäthän uudelleen!");
            huonoTulos.TextColor = Color.White;
            huonoTulos.Color = Color.Black;
            Add(huonoTulos);
        }
        if (pisteLaskuri.Value >= 10 && pisteLaskuri.Value < 30)
        {
            Label keskivertoTulos = new Label
                ("Virus pääsi karkaamaan ulos terminaalista! Matkallaan se vahvistui \n" +
                "ja sai aikaan kohtalaisen epidemian Suomessa. Harmi kyllä pisteesi eivät \n" +
                "riittäneet tuhoisan globaalin pandemian luomiseksi. Yritäthän uudelleen!");
            keskivertoTulos.TextColor = Color.White;
            keskivertoTulos.Color = Color.Black;
            Add(keskivertoTulos);
        }
        if (pisteLaskuri.Value >= 30)
        {
            Label huippuTulos = new Label
                ("Hienoa työtä! Virus pääsi karkaamaan ulos terminaalista ja aiheutti \n" +
                "hurjan maailmanlaajuisen pandemian! Avullasi virus tuhosi satojen \n" +
                "tuhansien perheiden elämät vuosikausiksi. Voit olla ylpeä itsestäsi!");

            huippuTulos.TextColor = Color.White;
            huippuTulos.Color = Color.Black;
            Add(huippuTulos);
        }

        MultiSelectWindow lopetusValikko = new MultiSelectWindow("Mitäs nyt?",
        "Yritä uudelleen", "Ei pysty enää");
        Add(lopetusValikko);

        lopetusValikko.AddItemHandler(0, AloitaPeli);
        lopetusValikko.AddItemHandler(1, Exit);
        lopetusValikko.DefaultCancel = -1;

        lopetusValikko.Y = Screen.Bottom + 200;
    }


    /// <summary>
    /// Määritetään pelaajan ominaisuudet ja liikekomennot.
    /// </summary>
    /// <param name="peli"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void LuoPelaaja (Game peli, double x, double y)
    {
        PhysicsObject pelaaja = new PhysicsObject(x, y);
        pelaaja.Image = olionKuva;
        pelaaja.Y = peli.Level.Bottom +100;
        peli.Add(pelaaja);
        pelaaja.Tag = "pelaaja";
        pelaaja.Restitution = 0; //poistaa kimmoisuuden
        pelaaja.LinearDamping = 0.96; //vaudin hidastaminen
        pelaaja.AngularDamping = 0.01; //pyörimisen hidastaminen

        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaa, "Liikuta pelaajaa vasemmalle", pelaaja, new Vector(-NOPEUS, 0));
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaa, "Liikuta pelaajaa oikealle", pelaaja, new Vector(NOPEUS, 0));
        Keyboard.Listen(Key.Up, ButtonState.Down, LiikutaPelaajaa, "Liikuta pelaajaa ylös", pelaaja, new Vector(0, NOPEUS));
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaPelaajaa, "Liikuta pelaajaa alas", pelaaja, new Vector(0, -NOPEUS));

        pelaaja.Collided += KasittelePelaajanTormays;
        
    }


    /// <summary>
    /// Aliohjelmassa luodaan liikutettavat viholliset sekä kerättävät ja lisätään ne liikutettavien listaan. 
    /// </summary>
    /// <param name="peli"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void LuoLiikkuva(Game peli, double x, double y, Image kuva, string tagi)
    {
        PhysicsObject liikkuva = new PhysicsObject(x, y);
        liikkuva.Image = kuva;
        liikkuva.Position = new Vector(RandomGen.NextDouble(Level.Left, Level.Right), RandomGen.NextDouble(Level.Bottom, Level.Top));
        liikkuva.IgnoresCollisionResponse = true;
        liikkuva.Tag = tagi;
        peli.Add(liikkuva);
        liikutettavat.Add(liikkuva);
        liikkuva.IgnoresExplosions = true;
    }


    /// <summary>
    /// Aliohjelmassa luodaan pistelaskuri ja pistenäyttö.
    /// </summary>
    private void LuoPistelaskuri()
    {       
        pisteLaskuri = new IntMeter(5);

        Label pisteNaytto = new Label();
        pisteNaytto.Title = "Pisteet";
        pisteNaytto.X = Screen.Left +100;
        pisteNaytto.Y = Screen.Bottom +50;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Color = Color.Black;

        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }


    /// <summary>
    /// Aliohjelmassa määritetään mitä tapahtuu, kun pelaaja törmää kentällä
    /// oleviin objekteihin.
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="kohde"></param>
    private void KasittelePelaajanTormays(IPhysicsObject pelaaja, IPhysicsObject kohde)
    {
        if (kohde.Tag.ToString() == "vihu")
        {
           
            pisteLaskuri.Value -= 1;
            pelaaja.Height -= MUUTOS;
            pelaaja.Width -= MUUTOS;
            kohde.Destroy();
            

            if (pisteLaskuri.Value <= 0) //Kun pistelaskuri tippuu nollaan niin pelaaja räjähtää
            {
                Explosion rajahdys = new Explosion(150);
                rajahdys.Position = pelaaja.Position;
                Add(rajahdys);
                pelaaja.Destroy();
                LuoValikko();
            }           
        }

        if (kohde.Tag.ToString() == "kerattava")
        {
            
            pisteLaskuri.Value += 1;
            pelaaja.Height += MUUTOS;
            pelaaja.Width += MUUTOS;
            kohde.Destroy();

        }
        
        if (kohde.Tag.ToString() == "loppu")
        {
            LopetaPeli();
        }
    }


    /// <summary>
    /// Aliohjelma liikuttaa pelaajaa pelaajan luonnissa määritettyyn suuntaan.
    /// </summary>
    /// <param name="pelaaja"></param>
    /// <param name="suunta"></param>
    private void LiikutaPelaajaa(PhysicsObject pelaaja, Vector suunta)
    {
        pelaaja.Push(suunta);
    }


    /// <summary>
    /// Aliohjelma liikuttaa kentällä olevia olioita,
    /// jolloin pelikenttä näyttää kulkevan eteenpäin.
    /// </summary>
    private void LiikutaOlioita()
    {
        for (int i = 0; i < liikutettavat.Count; i++)
        {
            GameObject olio = liikutettavat[i];
            olio.Y += suunta;
            if (olio.Y <= tuhoamisY)
            {
                olio.Destroy();
                liikutettavat.Remove(olio);
            }
        }
    }


    /// <summary>
    /// Määritetään kentällä olevien olioiden liikkumisen nopeus.
    /// </summary>
    private void VieritaKenttaa()
    {
        tuhoamisY = Level.Bottom;

        Timer liikutusAjastin = new Timer(); //määritetään kuinka nopeasti kohteet kentällä liikkuvat
        liikutusAjastin.Interval = 0.03;
        liikutusAjastin.Timeout += LiikutaOlioita;
        liikutusAjastin.Start();
    }


    /// <summary>
    /// Luodaan kentän tausta. 
    /// </summary>
    public void LuoTausta ()
    {
        GameObject tausta = new GameObject(Level.Width, Level.Height);
        tausta.Image = taustaKuva;
        Add(tausta, -3);
        Layers[-3].RelativeTransition = new Vector(0.2, 0.2);
    }


    /// <summary>
    /// Luodaan pelin alkuvalikko ja ohjetekstit.
    /// </summary>
    public void LuoValikko ()
    {
        LuoTausta();
        MultiSelectWindow alkuValikko = new MultiSelectWindow("Pelin alkuvalikko",
        "Uusi peli", "Lopeta");
        Add(alkuValikko);

        alkuValikko.AddItemHandler(0, AloitaPeli);
        alkuValikko.AddItemHandler(1, Exit);
        alkuValikko.DefaultCancel = -1;
        alkuValikko.Y = -50;

        Label tarina = new Label
                ("                                                  TAPAUS TIKKAKOSKI\n" +
                "On kulunut kaksi vuotta koronavirusepidemian ensimmäisestä aallosta\n" +
                "Ihmiskunta on tottunut elämään heikentyneen viruksen kanssa, joka on\n" +
                "jäänyt kiertämään maapalloa influenssan tavoin. Seuraava aalto on\n" +
                "kuitenkin erilainen. Kun koronavirus saavutti vuoden 2021 maaliskuussa\n" +
                "turistien mukana ensimmäistä kertaa Madagaskarin saaren, tapahtui jotain\n" +
                "yllättävää. Koronavirus tapasi saarella vuosikymmeniä kadoksissa olleen\n" +
                "serkkunsa CovSS-39, jonka tiedemiehet kehittivät toisen maailmansodan\n" +
                "kynnyksellä 1930-luvun natsi-Saksassa. Virukset yhdistyivät ja nyt\n" +
                "paluulento Madagaskarista Tikkakosken lentokentälle sisältää yllättävän\n" +
                "salamatkustajan. Pystytkö sinä ohjaamaan viruksen ulos Tikkakosken\n" +
                "terminaalista, läpi kasvomaskien  ja käsidesipisteiden?");
        tarina.TextColor = Color.White;
        tarina.Color = Color.Black;
        tarina.Y = Level.Top - 200;
        Add(tarina);

        Label ohjeet = new Label
            ("                                                  PELIN OHJEET\n" +
            "Pelissä ohjataan virusta nuolinäppäimillä ja tarkoituksena on kerätä \n" +
            "mahdollisimman paljon pisteitä. Keräämäsi pistemäärä ratkaisee \n" +
            "viruksen menestyksen ulkomaailmassa. Pisteitä saat tartuttamalla \n" +
            "matkalaukkujen kanssa kulkevia matkustajia. Pisteesi laskevat  \n" +
            "mikäli virus osuu käsidesipulloihin tai kasvomaskeihin. Vältä \n" +
            "pisteidesi laskemista nollaan saakka sillä silloin virus tuhoutuu!");
        ohjeet.TextColor = Color.White;
        ohjeet.Color = Color.Black;
        ohjeet.Y = Level.Bottom + 150;
        Add(ohjeet);
    }
}

