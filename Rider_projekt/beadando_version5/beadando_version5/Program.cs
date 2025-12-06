using Adatkezelo; // Hivatkozás a DLL-re (ahol a SzobaSzenzor van)
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace beadando_version5
{
    // JSON és Adatbázis osztály (a DLL-ből származó adatok tárolásához)
    // Felelős: Zoli
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
        // Database connection string is a constant
        static readonly string connectionString = "Data Source=szoba_adatok.db;"; 

        static void Main(string[] args)
        {
            // KÖTELEZŐ FIX: Az SQLite natív szolgáltatójának inicializálása.
            // Ez kell a Microsoft.Data.Sqlite csomaghoz.
            SQLitePCL.Batteries.Init(); 

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
        
        /// <summary> 
        /// Inizializálja az adatbázist, futatja a szimulációt és menti az adatokat (1. Fázis).
        /// Felelős: Kristóf
        /// </summary>
        static void AdatokatGeneralEsMent()
        {
            const int SIMULACIO_HOSSZA = 1440; // 24 óra * 60 perc

            Console.WriteLine("--- 1. Fázis: Adatok generálása és adatbázisba mentése ---");

            // Adatbázis inicializálása (az SqliteAdatkezelo osztály már be van illesztve a kódban)
            SqliteAdatkezelo.InitializeDatabase(); 

            // Random kezdőértékek generálása
            Random r = new Random();

            // 1. A SzobaSzenzor példányosítása (a DLL-ből)
            var szoba = new Adatkezelo.SzobaSzenzor(
                Ido: 0,
                Homerseklet: r.Next(20, 23), // 20-23 °C
                Legnyomas: r.Next(1000, 1030), // 1000-1030 Bar
                Paratartalom: r.Next(40, 55) // 40-55 %
            );

            // 2. Esemény Hozzáadása/Feliratkozás (KÖTELEZŐ KÖVETELMÉNY!)
            szoba.KritikusParatartalomElerve += Szoba_KritikusParatartalomElerve;

            // Kezdőállapot mentése
            SqliteAdatkezelo.AdatBeszuras(
                szoba.Homerseklet, 
                szoba.Legnyomas, 
                szoba.Paratartalom);

            // Szimuláció futtatása 1440 lépésen (percen) keresztül
            for (int i = 1; i <= SIMULACIO_HOSSZA; i++)
            {
                // Mivel független véletlen változást választottunk, a Delegalt paraméter nélkül hívódik
                szoba.Delegalt(); 

                // Kiírás (opcionális)
                // szoba.Kiir(); 

                // Mentés
                SqliteAdatkezelo.AdatBeszuras(
                    szoba.Homerseklet, 
                    szoba.Legnyomas, 
                    szoba.Paratartalom);
            }
            Console.WriteLine($"Az adatgenerálás és mentés {SIMULACIO_HOSSZA} lépés után befejeződött.");
        }
        
        /// <summary>
        /// Eseménykezelő metódus, ami akkor fut le, ha a DLL-ből jövő páratartalom kritikus értéket ér el.
        /// Felelős: Zoli
        /// </summary>
        static void Szoba_KritikusParatartalomElerve(double kritikusErtek)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n*** FIGYELEM! KRITIKUS PÁRATARTALOM ELÉRVE! ***");
            Console.WriteLine($"Jelenlegi Páratartalom: {kritikusErtek:F2} %"); 
            Console.ResetColor();
        }

        /// <summary>
        /// Betölti az adatokat, elvégzi a LINQ elemzést és kiírja a JSON fájlt (2. Fázis).
        /// Felelős: Zoli
        /// </summary>
        static void AdatokatElemezAdatbazisbol()
        {
            Console.WriteLine("\n--- 2. Fázis: Adatok betöltése és elemzése az adatbázisból ---");

            List<MertAdat> meresiAdatok = SqliteAdatkezelo.AdatokatBetolt();

            if (meresiAdatok.Count == 0)
            {
                Console.WriteLine("Nem található adat az adatbázisban az elemzéshez.");
                return;
            }

            // --- LINQ Lekérdezések ---

            // 1. LINQ: Óránkénti átlagok (GroupBy, Average)
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
                Console.WriteLine($"{atlag.Ora + 1,-3} | {atlag.HomersekletAtlag,18:F2} °C | {atlag.ParatartalomAtlag,18:F2} % | {atlag.LegnyomasAtlag,14:F2} Bar");
            }

            // 2. LINQ: Páratartalom extrémumok elemzése (Where, Count)
            Console.WriteLine("\nMásodik LINQ: Páratartalom extrémumok elemzése");
            const double KRITIKUS_PARATARTALOM_HATAR = 75.0; 
            var tullepettMeresek = meresiAdatok
                .Where(adat => adat.Paratartalom > KRITIKUS_PARATARTALOM_HATAR)
                .ToList();

            int kritikusPercSzam = tullepettMeresek.Count;
            double kritikusIdotartamOra = kritikusPercSzam / 60.0;

            Console.WriteLine($"A kritikus ({KRITIKUS_PARATARTALOM_HATAR:F1} % feletti) páratartalom összesen {kritikusPercSzam} percen át volt mérhető.");
            Console.WriteLine($"Ez {kritikusIdotartamOra:F2} órás időtartamot jelentett.");

            // 3. LINQ: Hőmérséklet és Légnyomás korreláció elemzése (OrderByDescending, ThenBy)
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
            Console.WriteLine("\nJSON fájl létrehozása a betöltött adatokból...");
            string jsonString = JsonConvert.SerializeObject(meresiAdatok, Formatting.Indented);
            File.WriteAllText("SzobaMeresek.json", jsonString); 
            Console.WriteLine("JSON mentés sikeres a SzobaMeresek.json fájlba.");
        }
        
        // --- Database Helper Methods (SqliteAdatkezelo) ---
        // A kód tisztasága érdekében ez a statikus osztály a Program osztály alá kerül beillesztésre.

        public static class SqliteAdatkezelo
        {
            // Felelős: Zoli
            static readonly string connectionString = "Data Source=szoba_adatok.db;";

            // Ez a fix a Main-ben van, itt nincs rá szükség, csak a logika miatt hagyom bent:
            // public static void InitializeProvider() { SQLitePCL.Batteries.Init(); }

            /// <summary>
            /// Inicializálja az adatbázist (létrehozza a táblát) és törli a korábbi adatokat.
            /// Felelős: Zoli
            /// </summary>
            public static void InitializeDatabase()
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MeresiAdatok (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Homerseklet REAL,
                        Paratartalom REAL,
                        Legnyomas REAL,
                        Idopont DATETIME DEFAULT CURRENT_TIMESTAMP
                    );"; 
                    command.ExecuteNonQuery();
                    command.CommandText = "DELETE FROM MeresiAdatok;"; 
                    command.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// Egyetlen mérési pont beszúrása az adatbázisba.
            /// Felelős: Kristóf
            /// </summary>
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

            /// <summary>
            /// Összes mérési adat betöltése az adatbázisból List&lt;MertAdat&gt; formátumban.
            /// Felelős: Zoli
            /// </summary>
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
