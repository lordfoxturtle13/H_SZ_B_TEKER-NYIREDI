namespace Adatkezelo;

public class Okos_szoba
{
    //Változók
    public int Ido { get; set; } // ora_perc_masodperc
    public double Homerseklet { get; set; } // celsius
    public double Legnyomas { get; set; } // Bar
    public double Paratartalom { get; set; } // %
    // tárolók (listák)
    public List<int> IdoLista = new List<int>();
    public List<double> HomersekletLista = new List<double>();
    public List<double> LegnyomasLista = new List<double>();
    public List<double> ParatartalomLista = new List<double>();
    // random szám
    private Random r = new Random();
    private const double MAX_HOMERSEKLET_VALTOZAS = 0.5; // °C
    private const double MAX_LEGNYOMAS_VALTOZAS = 0.15;
    private const double MAX_PARATARTALOM_VALTOZAS = 0.23;
    
    //Eseménykezelés
    private const double MAX_PARATARTALOM = 80.0;
    public event Action<double> KritikusParatartalomElerve;
    
    
    //Konstruktor
    public Szoba(int Ido, double Homerseklet, double Legnyomas, double Paratartalom)
    {
        this.Ido = Ido;
        this.Homerseklet = Homerseklet;
        this.Legnyomas = Legnyomas;
        this.Paratartalom = Paratartalom;
        // hozzáadjuk ezeket 
        this.IdoLista.Add(Ido);
        this.HomersekletLista.Add(Homerseklet);
        this.LegnyomasLista.Add(Legnyomas);
        this.ParatartalomLista.Add(Paratartalom);
    }
    
    //Értékbeállítások
    public void HomersekletBeAllitas()
    {
        double random = MAX_HOMERSEKLET_VALTOZAS * r.NextDouble();
        bool novekszik = r.Next(2) == 1;
        Homerseklet += novekszik ? random : -random;
        HomersekletLista.Add(Homerseklet);
        
    }
    public void LegnyomasBeAllitas()
    {
        double random = MAX_LEGNYOMAS_VALTOZAS * r.NextDouble();
        bool novekszik = r.Next(2) == 1;
        Legnyomas += novekszik ? random : -random;
        LegnyomasLista.Add(Legnyomas);
    }
    // Itt kezelünk az eseményt, hogya a páratartalom megnövekszik
    public void ParatartalomBeAllitas()
    {
        double random = MAX_PARATARTALOM_VALTOZAS * r.NextDouble();
        bool novekszik = r.Next(2) == 1;
        Paratartalom += novekszik ? random : -random;
        if (Paratartalom >= MAX_PARATARTALOM) { KritikusParatartalomElerve?.Invoke(Paratartalom); }
        ParatartalomLista.Add(Paratartalom);
    }

    public void Kiir()
    {
        System.Console.WriteLine($"Ido: {Ido}, Hőmérséklet: {Homerseklet}, Légnyomás: {Legnyomas}, Páratartalom: {Paratartalom}");
    }
    
    //Delegált
    public delegate void Valtozas();
    public void Delegalt()
    {
        Valtozas del = new Valtozas(Homersekletallitas);
        del += LegnyomasBeAllitas;
        del += ParatartalomBeAllitas;
        del();
    }
}