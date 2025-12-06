namespace Adatkezelo;

public class Okosszoba
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
    private Random _r = new Random();
    //konstans értékek
    private const double MAX_HOMERSEKLET_VALTOZAS = 0.5; // °C
    private const double MAX_LEGNYOMAS_VALTOZAS = 0.15;
    private const double MAX_PARATARTALOM_VALTOZAS = 0.23;
    //Eseménykezelés
    private const double MAX_PARATARTALOM = 80.0;
    public event Action<double> KritikusParatartalomElerve;
    
    
    //Konstruktor
    public Okosszoba(int ido, double homerseklet, double legnyomas, double paratartalom)
    {
        this.Ido = ido;
        this.Homerseklet = homerseklet;
        this.Legnyomas = legnyomas;
        this.Paratartalom = paratartalom;
        // hozzáadjuk ezeket 
        this.IdoLista.Add(Ido);
        this.HomersekletLista.Add(homerseklet);
        this.LegnyomasLista.Add(legnyomas);
        this.ParatartalomLista.Add(paratartalom);
    }
    
    //Értékbeállítások
    public void HomersekletBeAllitas()
    {
        double random = MAX_HOMERSEKLET_VALTOZAS * _r.NextDouble();
        bool novekszik = _r.Next(2) == 1;
        Homerseklet += novekszik ? random : -random;
        HomersekletLista.Add(Homerseklet);
        
    }
    public void LegnyomasBeAllitas()
    {
        double random = MAX_LEGNYOMAS_VALTOZAS * _r.NextDouble();
        bool novekszik = _r.Next(2) == 1;
        Legnyomas += novekszik ? random : -random;
        LegnyomasLista.Add(Legnyomas);
    }
    // Itt kezelünk az eseményt, hogya a páratartalom megnövekszik
    public void ParatartalomBeAllitas()
    {
        double random = MAX_PARATARTALOM_VALTOZAS * _r.NextDouble();
        bool novekszik = _r.Next(2) == 1;
        Paratartalom += novekszik ? random : -random;
        if (Paratartalom >= MAX_PARATARTALOM) { KritikusParatartalomElerve?.Invoke(Paratartalom); }
        ParatartalomLista.Add(Paratartalom);
    }

    public void Kiir()
    {
        Console.WriteLine($"Ido: {Ido}, Hőmérséklet: {Homerseklet}, Légnyomás: {Legnyomas}, Páratartalom: {Paratartalom}");
    }
    
    //Delegált
    public delegate void Valtozas();
    public void Delegalt()
    {
        Valtozas del = new Valtozas(HomersekletBeAllitas);
        del += LegnyomasBeAllitas;
        del += ParatartalomBeAllitas;
        del();
    }
}