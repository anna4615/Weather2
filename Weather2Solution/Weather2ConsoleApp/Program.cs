using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Weather2DataAccessLibrary.DataAccess;
using Weather2DataAccessLibrary.Models;

namespace Weather2ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] fileContent = File.ReadAllLines("TemperaturData.txt");
            //string[] fileContent = File.ReadAllLines("TestData.txt");

            Console.WriteLine("Skapar sensorer...");
            int newSensors = NewMethods.CreateSensors(fileContent);

            Console.WriteLine(newSensors != 0 ? $"{newSensors} nya sensorer skapades i databasen." :
                                                 "Sensorer finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            Console.WriteLine("Laddar upp data...");
            int newRecords = NewMethods.LoadData(fileContent);
            Console.WriteLine(newRecords != 0 ? $"{newRecords} nya avläsningar registrerades." :
                                                 "Avläsningarna finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            Console.WriteLine("Tryck på Enter för att gå vidare.");
            Console.ReadLine();

            Console.Clear();

            bool newSelection = true;

            while (newSelection)
            {
                int selectedService = SelectFromMainMenu();

                switch (selectedService)
                {
                    case 1:
                        Console.Clear();
                        int inOrOut = SelectInOrOut();
                        Sensor sensor = NewMethods.GetSensor(inOrOut);
                        DateTime date = SelectDate();
                        Console.Clear();
                        Console.WriteLine("\nHämtar data...");
                        PrintAveragesForSelectedSensorAndDay(sensor, date);
                        break;
                    case 2:
                        Console.Clear();
                        inOrOut = SelectInOrOut();
                        Console.WriteLine("\nHämtar data...");
                        sensor = NewMethods.GetSensor(inOrOut);
                        PrintSortedDailyAverages(sensor);
                        break;
                    case 3:
                        Console.Clear();
                        Console.WriteLine("\nHämtar resultat för höst...");
                        sensor = NewMethods.GetSensor(6); // Sensor 6 är "Ute"
                        PrintStartOfSeason(sensor, "Höst");
                        Console.WriteLine("Hämtar resultat för vinter...");
                        PrintStartOfSeason(sensor, "Vinter");
                        Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
                        Console.ReadLine();
                        break;
                    case 4:
                        Console.Clear();
                        Console.WriteLine("\nHämtar data...");
                        PrintDifferenceInTempInAndOut(5, 6); // Sensor 5 är "Inne", sensor 6 är "Ute"
                        break;
                    case 5:
                        Console.Clear();
                        Console.WriteLine("\nHar börjat på GetTimeForOpenBalconyDoor() men har inte lyckats\n");                        
                        Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
                        Console.ReadLine();
                        break;
                    case 6:
                        Console.WriteLine("\nAvslutat");
                        newSelection = false;
                        break;
                    default:
                        break;
                }
            }
        }

        static int SelectFromMainMenu()
        {
            Console.Clear();

            Console.WriteLine("\nHuvudmeny\n*********\n" +
                "1. Medelvärden för en dag.\n" +
                "2. Lista med medelvärden för alla dagar.\n" +
                "3. Datum för meteoroligisk höst och vinter 2016.\n" +
                "4. Lista med temperaturskillnad mellan inne och ute.\n" +
                "5. Öppen balkongdörr.\n" +
                "6. Avsluta.\n");

            Console.Write("Välj tjänst med siffra 1-5 eller avsluta med 6  ");
            Console.WriteLine();

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 0 && number < 7)
                {
                    selection = number;
                    Console.WriteLine($"Val: {selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("Ange nummer 1-6");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return selection;
        }

        static int SelectInOrOut()
        {
            string heading = "\nVälj värden från inne eller ute";

            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n" +
               "5. Inne.\n" +
               "6. Ute.\n");

            Console.Write("Välj inne eller ute med siffra 5 eller 6  ");

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 4 && number < 7)
                {
                    selection = number;
                    Console.WriteLine($"\nVald plats: {selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("\nAnge nummer 5 eller 6");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return selection;
        }

        private static DateTime SelectDate()
        {
            DateTime selectedDate = new DateTime(2016, 5, 31);

            Console.WriteLine($"Tryck \"j\" + Enter för att välja förinställt datum (2016-05-31) eller Enter för att välja datum");
            string input = Console.ReadLine();
            Console.WriteLine();

            if (input == "j")
            {
                return selectedDate;
            }

            else
            {
                Console.Write("Välj en dag under 2016 med formatet yyyy-mm-dd, bekräfta med Enter ");
                string selectedDay = Console.ReadLine();

                while (true)
                {
                    if (DateTime.TryParse(selectedDay, out DateTime d))
                    {
                        selectedDate = d;
                        Console.Clear();
                        Console.WriteLine($"Valt datum: {selectedDate.ToShortDateString()}\n\n");

                        if (NewMethods.DateHasRecords(selectedDate))
                        {
                            break;
                        }

                        else
                        {
                            Console.Clear();
                            Console.Write($"Det finns inga inmatningar för {selectedDate.ToShortDateString()}, välj ett annat datum: ");
                            selectedDay = Console.ReadLine();
                        }
                    }

                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Felaktig inmatning. Välj en dag under 2016 med formatet yyyy-mm-dd");
                        selectedDay = Console.ReadLine();
                    }
                }
            }

            return selectedDate;
        }

        static void PrintAveragesForSelectedSensorAndDay(Sensor sensor, DateTime date)
        {
            List<Record> records = NewMethods.GetRecordsForSensor(sensor);

            var dailyAverages = records
                .GroupBy(r => r.Time.Date)
                .Where(g => g.Key == date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature),
                    hum = g.Average(r => r.Humidity),
                    fungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity))
                })
                .FirstOrDefault();

            string heading = $"\nDygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";

            Console.Clear();
            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}");
            Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");
            Console.Write($"{dailyAverages.day.ToShortDateString()}\t{Math.Round((decimal)dailyAverages.temp, 1)}\t\t" +
                $"{Math.Round((decimal)dailyAverages.hum)}\t\t");
            Console.WriteLine(dailyAverages.fungusRisk > 0 ? $"{Math.Round((decimal)dailyAverages.fungusRisk)}" : "0");
            Console.WriteLine();

            Console.WriteLine("Tryck på Enter för att komma till huvudmenyn");
            Console.ReadLine();
        }

        static void PrintSortedDailyAverages(Sensor sensor)
        {
            List<Record> records = NewMethods.GetRecordsForSensor(sensor);

            var dailyAveragesList = records
                .GroupBy(r => r.Time.Date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature),
                    hum = g.Average(r => r.Humidity),
                    fungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity))
                });

            bool newSelection = true;

            while (newSelection)
            {
                var sorton = SelectSorting();

                switch (sorton)
                {
                    case Sortingselection.Date:
                        dailyAveragesList = dailyAveragesList.OrderBy(a => a.day);
                        break;
                    case Sortingselection.Temperature:
                        dailyAveragesList = dailyAveragesList.OrderByDescending(a => a.temp);
                        break;
                    case Sortingselection.Humidity:
                        dailyAveragesList = dailyAveragesList.OrderBy(a => a.hum);
                        break;
                    case Sortingselection.FungusRisk:
                        dailyAveragesList = dailyAveragesList.OrderByDescending(a => a.fungusRisk);
                        break;
                    case Sortingselection.Quit:
                        newSelection = false;
                        break;
                    default:
                        break;
                }

                if (sorton != Sortingselection.Quit)
                {

                    string heading = GetHeadingForSortedList(sorton, sensor.SensorName);

                    Console.Clear();
                    Console.WriteLine($"n{heading}\n{Utils.GetUnderline(heading)}");
                    Console.WriteLine($"Visar resultat för {dailyAveragesList.Count()} dygn.\n");
                    Console.WriteLine("Tryck Enter för att göra ett nytt sorteringsval\n");
                    Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");

                    foreach (var item in dailyAveragesList)
                    {
                        Console.Write($"{item.day.ToShortDateString()}\t{Math.Round((decimal)item.temp, 1)}\t\t{Math.Round((decimal)item.hum)}\t\t");
                        Console.WriteLine(item.fungusRisk > 0 ? $"{Math.Round((decimal)item.fungusRisk)}" : "0");
                    }



                    Utils.ScrollToTop(dailyAveragesList.Count() + 5);
                    Console.Clear();
                }
            }
        }

        enum Sortingselection
        {
            [Display(Name = "Datum")]
            Date = 1,

            [Display(Name = "Temperatur")]
            Temperature,

            [Display(Name = "Fuktighet")]
            Humidity,

            [Display(Name = "Mögelrisk")]
            FungusRisk,

            [Display(Name = "Avsluta")]
            Quit
        }

        private static Sortingselection SelectSorting()
        {
            string heading = "Välj sorteringsordning";

            Console.WriteLine($"\n{heading}\n{Utils.GetUnderline(heading)}\n" +
                "1. Datum, tidigt till sent.\n" +
                "2. Temperatur, varmast till kallast." +
                "3. Fuktighet, torrast till fuktigast.\n" +
                "4. Risk för mögel, högst till lägst risk.\n" +
                "5. Huvudmeny.\n");

            Console.Write("Välj med siffra 1-4 eller gå tillbaka till huvudmenyn med 5  ");
            Console.WriteLine();

            ConsoleKeyInfo input = Console.ReadKey(true);
            string inputToString = input.KeyChar.ToString();
            int selection = 0;

            while (true)
            {
                if (int.TryParse(inputToString, out int number) &&
                    number > 0 && number < 6)
                {
                    selection = number;
                    Console.WriteLine($"Vald sortering: {(Sortingselection)selection}\n\n");
                    break;
                }
                else
                {
                    Console.WriteLine("Ange nummer 1-5");
                    inputToString = Console.ReadKey(true).KeyChar.ToString();
                }
            }

            return (Sortingselection)selection;
        }

        // Villkor för höst:
        // - Första dygnet av 5 dygn i rad med medeltemperatur under 10,0C
        // - Kan starta tidigast 1:a augusti

        // Villkor för vinter:
        // - Första dygnet av 5 dygn i rad med medeltemperatur under 0,0C
        // - Kan starta tidigast 1:a augusti

        // Det finns inte data för alla dagar, kollar 5 dagar tillbaka av de som har data
        static void PrintStartOfSeason(Sensor sensor, string season)
        {
            List<Record> records = NewMethods.GetRecordsForSensor(sensor);

            DateTime earliestStart = new DateTime(2016, 8, 1);

            var dailyAveragesArray = records
               .GroupBy(r => r.Time.Date)
               .Where(g => g.Key >= earliestStart)
               .Select(g => new
               {
                   day = g.Key,
                   temp = g.Average(r => r.Temperature),
                   hum = g.Average(r => r.Humidity),
                   fungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity))
               })
               .OrderBy(a => a.day)
               .ToArray();

            double startTemp = 0.0;

            switch (season.ToLower())
            {
                case "höst":
                    startTemp = 10.0;
                    break;
                case "vinter":
                    startTemp = 0.0;
                    break;
                default:
                    break;
            }

            DateTime seasonStart = new DateTime();

            for (int i = 4; i < dailyAveragesArray.Length; i++)
            {
                if (dailyAveragesArray[i].temp < startTemp &&
                    dailyAveragesArray[i - 1].temp < startTemp &&
                    dailyAveragesArray[i - 2].temp < startTemp &&
                    dailyAveragesArray[i - 3].temp < startTemp &&
                    dailyAveragesArray[i - 4].temp < startTemp)
                {
                    seasonStart = dailyAveragesArray[i - 4].day;
                    break;
                }
            }

            string printString = seasonStart != default ?
                   $"\n{season} blev det {seasonStart.ToShortDateString()}.\n" :
                   $"\nDet blev inte {season.ToLower()} innan 2016 års slut.\n";

            Console.WriteLine(printString);


        }

        public static double? GetFungusRisk(double? temp, double? humidity)
        {
            double? risk = (humidity - 78) * (temp / 15) / 0.22;

            return risk;
        }

        private static string GetHeadingForSortedList(Sortingselection sortOn, string sensorName)
        {
            string heading = $"Dygnsmedelvärden från sensor \"{sensorName}\" sorterat på";

            switch (sortOn)
            {
                case Sortingselection.Date:
                    heading += " datum";
                    break;
                case Sortingselection.Temperature:
                    heading += " temperatur, varmast till kallast";
                    break;
                case Sortingselection.Humidity:
                    heading += " fuktighet, torrast till fuktigast";
                    break;
                case Sortingselection.FungusRisk:
                    heading += $" risk för mögel, högst till lägst risk";
                    break;
                default:
                    heading = "Ingen tabell tillgänglig, felaktigt sorteringsval";
                    break;
            }

            return heading;
        }

        private static void PrintDifferenceInTempInAndOut(int insideSensorId, int outsideSensorId)
        {
            Sensor insideSensor = NewMethods.GetSensor(insideSensorId);

            var insideDailyTemp = NewMethods.GetRecordsForSensor(insideSensor)
                .GroupBy(r => r.Time.Date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature)
                });


            Sensor outsideSensor = NewMethods.GetSensor(outsideSensorId);

            var outsideDailyTemp = NewMethods.GetRecordsForSensor(outsideSensor)
                .GroupBy(r => r.Time.Date)
                .Select(g => new
                {
                    day = g.Key,
                    temp = g.Average(r => r.Temperature)
                });


            var differenceList = insideDailyTemp
                .Select(inside => new
                {
                    day = inside.day.Date,
                    insideTemp = inside.temp,
                    outsideTemp = outsideDailyTemp
                        .Where(outside => outside.day.Date == inside.day.Date)
                        .FirstOrDefault()
                        .temp
                })
                .OrderByDescending(a => a.insideTemp - a.outsideTemp)
                .ToList();


            string table = "";

            foreach (var item in differenceList)
            {
                double? tempDiff = item.insideTemp - item.outsideTemp;

                table += $"{item.day.ToShortDateString()}\t";
                table += item.insideTemp != null ? $"{Math.Round((double)item.insideTemp, 1)}\t" : $"*\t";
                table += item.outsideTemp != null ? $"{Math.Round((double)item.outsideTemp, 1)}\t" : $"*\t";
                table += tempDiff != null ? $"{Math.Round((double)tempDiff, 1)}\n" : $"*\n";
            }

            string heading = "\nDygnsmedeltemperaturer och skillnad mellan inne och ute, sorterat på skillnad";

            Console.Clear();
            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");
            Console.WriteLine("Tryck på Enter för att komma till huvudmenyn\n");
            Console.WriteLine("Datum\t\tInne\tUte\tSkillnad");
            Console.WriteLine(table);
            Utils.ScrollToTop(differenceList.Count() + 5);
        }

        static void GetTimeForOpenBalconyDoor(int insideSensorId, int outsideSensorId, DateTime date)
        {

            //Jag tänkte man kunde göra något sådant här:
            // Medelvärde med 5 min intervall för inne och ute i varsin array.
            // Hitta tid för inne där temp sjunker
            // Kolla om temp ökar vid samma tid för ute
            // I så fall är balkongdörren öppen
            // Sedan omvänt för att hitta tid när dörren stängs

            // Har startat lite men inte fått det klart


            Sensor insideSensor = NewMethods.GetSensor(insideSensorId);

            var insideRecordsForDay = NewMethods.GetRecordsForSensor(insideSensor)
                .Where(r => r.Time.Date == date.Date)
                .OrderBy(r => r.Time)
                .GroupBy(g =>                   // gruppera records med 5 min intervall
                {
                    DateTime t = g.Time;
                    t = t.AddMinutes(-(t.Minute % 5));
                    t = t.AddMilliseconds(-t.Millisecond - 1000 * t.Second);
                    return t;
                })
               .Select(g => new
               {
                   time = g.Key,
                   temp = g.Average(r => r.Temperature)
               })
               .OrderBy(a => a.time)
               .ToArray();


            Sensor outsideSensor = NewMethods.GetSensor(outsideSensorId);

            var outsideRecordsForDay = NewMethods.GetRecordsForSensor(outsideSensor)
                .Where(r => r.Time.Date == date.Date)
                .GroupBy(g =>                   // gruppera records med 5 min intervall
                {
                    DateTime t = g.Time;
                    t = t.AddMinutes(-(t.Minute % 5));
                    t = t.AddMilliseconds(-t.Millisecond - 1000 * t.Second);
                    return t;
                })
                .Select(g => new
                {
                    time = g.Key,
                    temp = g.Average(r => r.Temperature)
                })
                .OrderBy(a => a.time)
                .ToArray();

            bool isOpen = false;
            List<DateTime> openTimes = new List<DateTime>();

            for (int i = 0; i < insideRecordsForDay.Length - 1; i++)
            {
                if (isOpen == true)
                {
                    break;
                }

                if (insideRecordsForDay[i].temp > insideRecordsForDay[i + 1].temp)
                {
                    for (int j = 0; j < outsideRecordsForDay.Length; j++)
                    {
                        if (outsideRecordsForDay[j].time.TimeOfDay.TotalMinutes == insideRecordsForDay[i].time.TimeOfDay.TotalMinutes &&
                            outsideRecordsForDay[j].time.TimeOfDay.TotalMinutes < insideRecordsForDay[i].time.TimeOfDay.TotalMinutes + 5 &&
                            outsideRecordsForDay[j].temp < outsideRecordsForDay[j + 1].temp)
                        {
                            openTimes.Add(insideRecordsForDay[i].time);
                        }
                    }
                }
            }

            for (int i = 0; i < openTimes.Count(); i++)
            {
                Console.WriteLine($"öppen {openTimes[i]}");
            }
        }

        static void PrintSensors()
        {
            List<Sensor> sensors = NewMethods.GetSensorList();

            foreach (var sensor in sensors)
            {
                Console.WriteLine(sensor);
            }
        }

        static void PrintRecords(List<Record> records)
        {
            foreach (var record in records)
            {
                Console.WriteLine(record);
            }
        }
    }
}
