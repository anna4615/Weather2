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
            //List<Record> r = AccessMethods.GetRecordsForSensor(3);
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

            int sensorId = 6;

            //PrintAverages2(sensorId); // ta bort

            string season = "Höst";
            PrintStartOfAutumnOrWinter(sensorId, season);

            season = "Vinter";
            PrintStartOfAutumnOrWinter(sensorId, season);

            Console.WriteLine();
            Console.WriteLine();

            DateTime date = new DateTime(2016, 11, 10);
            //Sensor sensor = AccessMethods.GetSensor(7);

            //PrintDataForDay(date, sensorId);
            //Console.WriteLine();
            //Console.WriteLine();

            //double? aveTemp = AccessMethods.GetAverageTemperatureForDayAndSensor(sensorId, date, AccessMethods.GetEkholmModenCoefficients());

            //Console.WriteLine($"Datum: {date.ToShortDateString()}\tMedeltemp: {aveTemp}");

            //PrintSortedList(sensor, (AccessMethods.Sortingselection)1);

            PrintSelectedDayForSensor(sensorId, date);
            Console.WriteLine();
            Console.WriteLine();
            PrintAllDaysForSensorSortedBySelection(sensorId, (Sortingselection)1);

            //double? averageTemperature = AccessMethods.GetAverageTemperatureForDayAndSensor(sensorId, new DateTime(2016, 10, 04), AccessMethods.GetEkholmModenCoefficients()); // ta bort

            //Console.WriteLine($"Datum: {new DateTime(2016, 10, 04).ToShortDateString()}\tMedeltemp: {averageTemperature}");
        }

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

        enum Sortingselection
        {
            Date = 1,
            Temperature,
            Humidity,
            FungusRisk
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


                string heading = GetHeadingforSortedList(sortOn, sensor.SensorName);

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

        private static string GetHeadingforSortedList(Sortingselection sortOn, string sensorName)
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
                    heading += $" risk för mögel, lägst till högst risk";
                    break;
                default:
                    heading = "Ingen tabell tillgänglig, felaktigt sorteringsval";
                    break;
            }

            return heading;
        }
    }
}
