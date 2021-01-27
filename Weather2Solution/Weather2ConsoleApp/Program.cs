using System;
using System.Collections.Generic;
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
            //List<Record> r = AccessMethods.GetRecordsForSensor(5);
            //PrintRecords(r);

            //string[] fileContent = File.ReadAllLines("TemperaturData.txt");
            ////string[] fileContent = File.ReadAllLines("TestData.txt");

            //Console.WriteLine("Skapar sensorer...");
            //int newSensors = AccessMethods.CreateSensors(fileContent);

            //Console.WriteLine(newSensors != 0 ? $"{newSensors} nya sensorer skapades i databasen." :
            //                                     "Sensorer finns redan i databasen");
            //Console.WriteLine();
            //Console.WriteLine("Alla sensorer med antal avläsningar:");
            //PrintSensors();
            //Console.WriteLine("\n");

            //Console.WriteLine("Laddar upp data...");
            //int newRecords = AccessMethods.LoadData(fileContent);
            //Console.WriteLine(newRecords != 0 ? $"{newRecords} nya avläsningar registrerades." :
            //                                     "Avläsningarna finns redan i databasen");
            //Console.WriteLine();
            //Console.WriteLine();

            //Console.WriteLine("Alla sensorer med antal avläsningar:");
            //PrintSensors();
            //Console.WriteLine("\n");

            //int sensorId = 6;

            //string season = "Höst";
            //PrintStartOfAutumnOrWinter(sensorId, season);

            //season = "Vinter";
            //PrintStartOfAutumnOrWinter(sensorId, season);

            //Console.WriteLine();
            //Console.WriteLine();

            //DateTime date = new DateTime(2016, 11, 10);

            //PrintSelectedDayForSensor(sensorId, date);
            //Console.WriteLine();
            //Console.WriteLine();
            //PrintAllDaysForSensorSortedBySelection(sensorId, (Sortingselection)4);


            //PrintDifferenceInTempInAndOut(5, 6);
            //------------------------------------------------------------------------------------------------------
            int sensorId = 6;
            DateTime date = new DateTime(2016, 10, 04);
            Sensor sensor = NewMethods.GetSensor(sensorId);  //Lägg till felhantering

            string season = "Höst";
            PrintStartOfSeason(sensor, season);

            season = "Vinter";
            PrintStartOfSeason(sensor, season);
            Console.WriteLine();

            PrintSelectedDay(sensor, date);
            Console.WriteLine();

            PrintSortedDailyAverages(sensor);

        }

        static void PrintSelectedDay(Sensor sensor, DateTime date)
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

            string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";

            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}");
            Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");
            Console.Write($"{dailyAverages.day.ToShortDateString()}\t{Math.Round((decimal)dailyAverages.temp, 1)}\t\t" +
                $"{Math.Round((decimal)dailyAverages.hum)}\t\t");
            Console.WriteLine(dailyAverages.fungusRisk > 0 ? $"{Math.Round((decimal)dailyAverages.fungusRisk)}" : "0");
        }


        enum Sortingselection
        {
            Date = 1,
            Temperature,
            Humidity,
            FungusRisk
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

            var sorton = (Sortingselection)1;

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
                default:
                    break;
            }


            string heading = GetHeadingForSortedList(sorton, sensor.SensorName);

            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}");
            Console.WriteLine($"Visar resultat för {dailyAveragesList.Count()} dygn.\n");
            Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");

            foreach (var item in dailyAveragesList)
            {
                Console.Write($"{item.day.ToShortDateString()}\t{Math.Round((decimal)item.temp, 1)}\t\t{Math.Round((decimal)item.hum)}\t\t");
                Console.WriteLine(item.fungusRisk > 0 ? $"{Math.Round((decimal)item.fungusRisk)}" : "0");
            }

            Utils.ScrollToTop(dailyAveragesList.Count() + 5);
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
                   $"{season} blev det {seasonStart.ToShortDateString()}." :
                   $"Det blev inte {season.ToLower()} innan 2016 års slut.";

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








        // -------------------------------------------------------------------------------------------------------------

        private static void PrintStartOfAutumnOrWinter(int sensorId, string season)
        {
            Sensor sensor = NewMethods.GetSensor(sensorId);

            if (sensor == null)
            {
                Console.WriteLine($"Det finns ingen sensor med ID {sensorId}.");
            }

            else if (sensor.SensorName.ToLower() == "inne")
            {
                Console.WriteLine($"Sensor \"{sensor.SensorName}\" sitter inomhus oah kan inte användas för att beräkna start av {season.ToLower()}.");
            }

            else
            {
                DateTime seasonStartDate = NewMethods.GetFirstDayOfSeason(sensor, season);

                string printString = seasonStartDate != default ?
                    $"{season} blev det {seasonStartDate.ToShortDateString()}." :
                    $"Det blev inte {season.ToLower()} innan 2016 års slut.";

                Console.WriteLine(printString);
            }
        }

        static void PrintSensors()
        {
            List<Sensor> sensors = AccessMethods.GetSensorList();

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



        static void PrintSelectedDayForSensor(int sensorId, DateTime date)
        {
            Sensor sensor = NewMethods.GetSensor(sensorId);

            while (true)
            {
                if (sensor == null)
                {
                    Console.WriteLine($"Det finns ingen sensor med ID {sensorId}.");
                    break;
                }

                List<IGrouping<DateTime, Record>> groupedRecords = NewMethods.GetLIstOfRecordsForSensorGroupedByDay(sensor);

                IGrouping<DateTime, Record> selectedRecords = NewMethods.GetRecordsForDate(groupedRecords, date);

                if (selectedRecords == null)
                {
                    Console.WriteLine($"Det finns inga avläsningar från {date.ToShortDateString()} för sensor \"{sensor.SensorName}\".");
                    break;
                }

                (double? aveTemp, double? aveHum, double? aveFungus) = NewMethods.GetAveragesForDayAndSensor(selectedRecords);

                DailyAverage dailyAverage = new DailyAverage()
                {
                    Day = selectedRecords.Key,
                    AverageTemperature = aveTemp,
                    AverageHumidity = aveHum,
                    FungusRisk = aveFungus
                };

                string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";
                Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}");
                Console.WriteLine($"  * = Medelvärde kunde inte beräknas pga otillräcklig data\n");
                Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");
                Console.WriteLine(dailyAverage);
                Console.WriteLine();
                break;
            }
        }

        static void PrintAllDaysForSensorSortedBySelection(int sensorId, Sortingselection sortOn)
        {
            Sensor sensor = NewMethods.GetSensor(sensorId);

            while (true)
            {
                if (sensor == null)
                {
                    Console.WriteLine($"Det finns ingen sensor med ID {sensorId}.");
                    break;
                }

                List<IGrouping<DateTime, Record>> groupedRecords = NewMethods.GetLIstOfRecordsForSensorGroupedByDay(sensor);

                if (groupedRecords == null)
                {
                    Console.WriteLine($"Det finns inga avläsningar för sensor \"{sensor.SensorName}\".");
                    break;
                }

                List<DailyAverage> DailyAverageList = new List<DailyAverage>();

                foreach (var group in groupedRecords)
                {
                    (double? aveTemp, double? aveHum, double? aveFungus) = NewMethods.GetAveragesForDayAndSensor(group);

                    DailyAverage dailyAverage = new DailyAverage()
                    {
                        Day = group.Key,
                        AverageTemperature = aveTemp,
                        AverageHumidity = aveHum,
                        FungusRisk = aveFungus
                    };

                    DailyAverageList.Add(dailyAverage);
                }

                var sortedList = new List<DailyAverage>();

                switch (sortOn)
                {
                    case Sortingselection.Date:
                        sortedList = DailyAverageList
                                     .OrderBy(d => d.Day)
                                     .ToList();
                        break;
                    case Sortingselection.Temperature:
                        sortedList = DailyAverageList
                                    .OrderByDescending(d => d.AverageTemperature)
                                    .ToList();
                        break;
                    case Sortingselection.Humidity:
                        sortedList = DailyAverageList
                                    .OrderBy(d => d.AverageHumidity)
                                    .ToList();
                        break;
                    case Sortingselection.FungusRisk:
                        sortedList = DailyAverageList
                             .OrderBy(d => d.FungusRisk)
                            .ToList();
                        break;
                    default:
                        break;
                }


                string heading = GetHeadingForSortedList(sortOn, sensor.SensorName);

                Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");
                Console.WriteLine($"Visar resultat för {sortedList.Count} dygn. Tryck på valfri tangent för att komma till slutet av listan.");
                Console.WriteLine($"  * = Medelvärde kunde inte beräknas pga otillräcklig data\n");
                Console.WriteLine($"Datum\t\tTemperatur (C)\tFuktighet (%)\tMögelrisk (%)");
                Console.WriteLine(string.Join(Environment.NewLine, sortedList));
                Console.WriteLine();
                Utils.ScrollToTop(sortedList.Count + 20);

                break;
            }
        }




        private static void PrintDifferenceInTempInAndOut(int insideSensorId, int outsideSensorId)
        {
            Sensor insideSensor = NewMethods.GetSensor(insideSensorId);

            List<IGrouping<DateTime, Record>> insideGroupedRecords = NewMethods.GetLIstOfRecordsForSensorGroupedByDay(insideSensor);

            List<DailyAverage> insideDailyAverageList = new List<DailyAverage>();

            foreach (var group in insideGroupedRecords)
            {
                (double? aveTemp, double? aveHum, double? aveFungus) = NewMethods.GetAveragesForDayAndSensor(group);

                DailyAverage dailyAverage = new DailyAverage()
                {
                    Day = group.Key,
                    AverageTemperature = aveTemp,
                    AverageHumidity = aveHum,
                    FungusRisk = aveFungus
                };

                insideDailyAverageList.Add(dailyAverage);
            }

            Sensor outsideSensor = NewMethods.GetSensor(outsideSensorId);

            List<IGrouping<DateTime, Record>> outsideGroupedRecords = NewMethods.GetLIstOfRecordsForSensorGroupedByDay(outsideSensor);

            List<DailyAverage> outsideDailyAverageList = new List<DailyAverage>();

            foreach (var group in outsideGroupedRecords)
            {
                (double? aveTemp, double? aveHum, double? aveFungus) = NewMethods.GetAveragesForDayAndSensor(group);

                DailyAverage dailyAverage = new DailyAverage()
                {
                    Day = group.Key,
                    AverageTemperature = aveTemp,
                    AverageHumidity = aveHum,
                    FungusRisk = aveFungus
                };

                outsideDailyAverageList.Add(dailyAverage);
            }


            var q = insideDailyAverageList
                .Select(id => new
                {
                    day = id.Day.Date,
                    insideTemp = id.AverageTemperature,
                    outsideTemp = outsideDailyAverageList.Where(od => od.Day.Date == id.Day.Date).FirstOrDefault().AverageTemperature
                })
                .OrderByDescending(a => a.insideTemp)   // Det blir snyggare tabell om alla dagar som saknar innetemp hamnar sist
                .OrderByDescending(a => a.insideTemp - a.outsideTemp);


            string heading = "Dygnsmedeltemperaturer";
            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}");
            Console.WriteLine("* = Otillräcklig data för att beräkna dygnsmedeltemperatur");
            Console.WriteLine("Datum\t\tInne\tUte\tSkillnad");

            foreach (var item in q)
            {
                double? tempDiff = item.insideTemp - item.outsideTemp;

                Console.Write($"{item.day.ToShortDateString()}\t");
                Console.Write(item.insideTemp != null ? $"{Math.Round((double)item.insideTemp, 1)}\t" : $"*\t");
                Console.Write(item.outsideTemp != null ? $"{Math.Round((double)item.outsideTemp, 1)}\t" : $"*\t");
                Console.Write(tempDiff != null ? $"{Math.Round((double)tempDiff, 1)}\n" : $"*\n");
            }

            Utils.ScrollToTop(q.Count() + 5);
        }



    }
}
