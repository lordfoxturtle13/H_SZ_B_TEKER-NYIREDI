//using Adatkezelo; // Hivatkozás a DLL-re, de felesleges mert az IDE érzékeli
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using SQLitePCL;

// feleslegesnek bizonyultak a futáshoz, lehet hogy vissza kell állítani !!!
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

namespace beadando_version5
{
    // JSON és Adatbázis osztály
    // nyiredi
    public class MertAdat
    {
        public int Ido { get; set; } // A szimuláció lépésszáma (perc)
        public double Homerseklet { get; set; } // celsius
        public double Paratartalom { get; set; } // %
        public double Legnyomas { get; set; } // Bar
        public DateTime MentesIdeje { get; set; } // DB adatomatikusan generálja
    }

    internal class Program
    {
        // felesleges beállítás:
        //static readonly string connectionString = "Data Source=szoba_adatok.db;"; 

        static void Main(string[] args)
        {
            // Ez kell a Microsoft.Data.Sqlite csomaghoz.
            // SQLitePCL.Batteries.Init(); 
            Batteries.Init(); 

            try
            {
                // 1. Fázis: Adatok generálása, szimuláció futtatása, eseménykezelés, DB mentés
                AdatokatGeneralEsMent(); 

                // 2. Fázis: Adatok betöltése, LINQ elemzés, JSON fájlba írás
                AdatokatElemezAdatbazisbol();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kritikus hiba történt a szimuláció során: {ex.Message}");
            }

            Console.WriteLine("\n--- A szimuláció befejeződött. ---");
            Console.WriteLine("Az eredmények megtekinthetők: szoba_adatok.db és SzobaMeresek.json fájlokban.");

            Console.WriteLine("\nNyomj meg egy gombot a kilépéshez.");
            Console.ReadKey();
        }
        // Inizializálja az adatbázist, futatja a szimulációt és menti az adatokat.
        // teker
        static void AdatokatGeneralEsMent()
        {
            const int SZIMULACIO_HOSSZA = 1440; // 24 óra * 60 perc

            Console.WriteLine("--- 1. Fázis: Adatok generálása és adatbázisba mentése ---");

            // Adatbázis inicializálása
            SqliteAdatkezelo.InitializeDatabase(); 

            // Random kezdőértékek generálása
            Random r = new Random();

            // 1. A SzobaSzenzor példányosítása (a DLL-ből)
            var szoba = new Adatkezelo.Okosszoba(
                ido: 0,
                homerseklet: r.Next(20, 23), // 20-23 °C
                legnyomas: r.Next(1000, 1030), // 1000-1030 Bar
                paratartalom: r.Next(40, 85) // 40-85 %
            );

            // 2. Esemény Hozzáadása/Feliratkozás
            szoba.KritikusParatartalomElerve += Szoba_KritikusParatartalomElerve;

            // Kezdőállapot mentése
            SqliteAdatkezelo.AdatBeszuras(
                szoba.Homerseklet, 
                szoba.Paratartalom, 
                szoba.Legnyomas);

            // Szimuláció futtatása 1440 lépésen (percen) keresztül
            Console.WriteLine("\nAz első 5 elem:\n");
            for (int i = 1; i <= SZIMULACIO_HOSSZA; i++)
            {
                szoba.Delegalt();
                if (i < 6)
                {
                    szoba.Kiir(); 
                }
                // Mentés
                SqliteAdatkezelo.AdatBeszuras(
                    szoba.Homerseklet, 
                    szoba.Paratartalom, 
                    szoba.Legnyomas);
            }
            Console.WriteLine($"Az adatgenerálás és mentés {SZIMULACIO_HOSSZA} lépés után befejeződött.");
        }
        // Eseménykezelő metódus, ami akkor fut le, ha a DLL-ből jövő páratartalom kritikus értéket ér el.
        // nyiredi
        static void Szoba_KritikusParatartalomElerve(double kritikusErtek)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n*** FIGYELEM! KRITIKUS PÁRATARTALOM ELÉRVE! ***");
            Console.WriteLine($"Jelenlegi Páratartalom: {kritikusErtek:F2} %"); 
            Console.ResetColor();
        }
        // Betölti az adatokat, elvégzi a LINQ elemzést és kiírja a JSON fájlt.
        //közös
        static void AdatokatElemezAdatbazisbol()
        {
            Console.WriteLine("\n--- 2. Fázis: Adatok betöltése és elemzése az adatbázisból ---");

            List<MertAdat> meresiAdatok = SqliteAdatkezelo.AdatokatBetolt();

            if (meresiAdatok.Count == 0)
            {
                Console.WriteLine("Nem található adat az adatbázisban az elemzéshez.");
                return;
            }

            //LINQ Lekérdezések

            // 1. LINQ: Óránkénti átlagok
            // teker
            Console.WriteLine("\nElső LINQ: Óránkénti átlagok (Hőmérséklet, Páratartalom, Légnyomás)");
            var orankentiAtlagok = meresiAdatok
                .GroupBy(adat => (adat.Ido - 1) / 60)
                .Select(group => new
                {
                    Ora = group.Key,
                    HomersekletAtlag = group.Average(a => a.Homerseklet),
                    ParatartalomAtlag = group.Average(a => a.Paratartalom),
                    LegnyomasAtlag = group.Average(a => a.Legnyomas)
                })
                .ToList();
            Console.WriteLine("Óra | Hőmérséklet Átlag | Páratartalom Átlag | Légnyomás Átlag");
            Console.WriteLine("------------------------------------------------------------------");
            foreach (var atlag in orankentiAtlagok)
            {
                Console.WriteLine($"{atlag.Ora,-3} | {atlag.HomersekletAtlag,18:F2} °C | {atlag.ParatartalomAtlag,18:F2} % | {atlag.LegnyomasAtlag,14:F2} Bar");
            }

            // 2. LINQ: Páratartalom extrémumok elemzése
            // nyiredi
            Console.WriteLine("\nMásodik LINQ: Páratartalom extrémumok elemzése");
            const double KRITIKUS_PARATARTALOM_HATAR = 75.0; 
            var tullepettMeresek = meresiAdatok
                .Where(adat => adat.Paratartalom > KRITIKUS_PARATARTALOM_HATAR)
                .ToList();

            int kritikusPercSzam = tullepettMeresek.Count;
            double kritikusIdotartamOra = kritikusPercSzam / 60.0;

            Console.WriteLine($"A kritikus ({KRITIKUS_PARATARTALOM_HATAR:F1} % feletti) páratartalom összesen {kritikusPercSzam} percen át volt mérhető.");
            Console.WriteLine($"Ez {kritikusIdotartamOra:F2} órás időtartamot jelentett.");

            // 3. LINQ: Hőmérséklet és Légnyomás korreláció elemzése
            // nyiredi
            Console.WriteLine("\nHarmadik LINQ: Hőmérséklet és Légnyomás korreláció elemzése");
            var legmagasabbHomersekletLegalacsonyabbNyomassal = meresiAdatok
                .OrderByDescending(adat => adat.Homerseklet)
                .ThenBy(adat => adat.Legnyomas)
                .Select(adat => new { adat.Ido, adat.Homerseklet, adat.Legnyomas })
                .First(); 

            Console.WriteLine($"Legmagasabb hőmérséklet a legalacsonyabb légnyomással együtt mérve:");
            Console.WriteLine($"Időpont (perc): {legmagasabbHomersekletLegalacsonyabbNyomassal.Ido}");
            Console.WriteLine($"Hőmérséklet: {legmagasabbHomersekletLegalacsonyabbNyomassal.Homerseklet:F2} °C");
            Console.WriteLine($"Légnyomás: {legmagasabbHomersekletLegalacsonyabbNyomassal.Legnyomas:F2} Bar");
            
            // JSON Fájlba írás
            // teker
            Console.WriteLine("\nJSON fájl létrehozása a betöltött adatokból...");
            string jsonString = JsonConvert.SerializeObject(meresiAdatok, Formatting.Indented);
            File.WriteAllText("SzobaMeresek.json", jsonString); 
            Console.WriteLine("JSON mentés sikeres a SzobaMeresek.json fájlba.");
        }
        
        //Database Helper Methods
        //közös
        public static class SqliteAdatkezelo
        {
            static readonly string connectionString = "Data Source=szoba_adatok.db;";
            // Inicializálja az adatbázist (létrehozza a táblát) és törli a korábbi adatokat.
            public static void InitializeDatabase()
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
        
                    // 1. Teljesen eldobjuk a táblát (ez reseteli az ID-t is)
                    command.CommandText = "DROP TABLE IF EXISTS MeresiAdatok;"; 
                    command.ExecuteNonQuery();

                    // 2. Újra létrehozzuk
                    command.CommandText = @"
                    CREATE TABLE MeresiAdatok (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Homerseklet REAL,
                        Paratartalom REAL,
                        Legnyomas REAL,
                        Idopont DATETIME DEFAULT CURRENT_TIMESTAMP
                     );";
                    command.ExecuteNonQuery();
                }
            }
            // Egyetlen mérési pont beszúrása az adatbázisba.
            public static void AdatBeszuras(double homerseklet, double paratartalom, double legnyomas)
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO MeresiAdatok (Homerseklet, Paratartalom, Legnyomas) VALUES (@homerseklet, @paratartalom, @legnyomas);";
                    command.Parameters.AddWithValue("@homerseklet", homerseklet);
                    command.Parameters.AddWithValue("@paratartalom", paratartalom);
                    command.Parameters.AddWithValue("@legnyomas", legnyomas);
                    command.ExecuteNonQuery();
                }
            }
            // Összes mérési adat betöltése az adatbázisból
            public static List<MertAdat> AdatokatBetolt()
            {
                var adatok = new List<MertAdat>();
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Id, Homerseklet, Paratartalom, Legnyomas, Idopont FROM MeresiAdatok ORDER BY Id;";
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            adatok.Add(new MertAdat
                            {
                                Ido = reader.GetInt32(0),
                                Homerseklet = reader.GetDouble(1),
                                Paratartalom = reader.GetDouble(2),
                                Legnyomas = reader.GetDouble(3),
                                MentesIdeje = reader.GetDateTime(4)
                            });
                        }
                    }
                }
                Console.WriteLine($"{adatok.Count} adat betöltve az adatbázisból.");
                return adatok;
            }
        }
    }
}
