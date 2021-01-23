using System;
using System.Collections.Generic;
using System.IO;
using Weather2DataAccessLibrary.DataAccess;
using Weather2DataAccessLibrary.Models;

namespace Weather2ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Record> r = AccessMethods.GetRecordsForSensor(3);
            PrintRecords(r);

            string[] fileContent = File.ReadAllLines("TemperaturData.txt");
            //string[] fileContent = File.ReadAllLines("TestData.txt");

            Console.WriteLine("Skapar sensorer...");
            int newSensors = AccessMethods.CreateSensors(fileContent);

            Console.WriteLine(newSensors != 0 ? $"{newSensors} nya sensorer skapades i databasen." :
                                                 "Sensorer finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            Console.WriteLine("Laddar upp data...");
            int newRecords = AccessMethods.LoadData(fileContent);
            Console.WriteLine(newRecords != 0 ? $"{newRecords} nya avläsningar registrerades." :
                                                 "Avläsningarna finns redan i databasen");
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Alla sensorer med antal avläsningar:");
            PrintSensors();
            Console.WriteLine("\n");

            int sensorId = 6;

            string season = "Höst";
            PrintStartOfAutumnOrWinter(sensorId, season);

            season = "Vinter";
            PrintStartOfAutumnOrWinter(sensorId, season);

            Console.WriteLine();
            Console.WriteLine();

            DateTime date = new DateTime(2016, 11, 18);

            PrintDataForDay(date, sensorId);
            Console.WriteLine();
            Console.WriteLine();

            PrintSortedList(sensorId, (AccessMethods.Sortingselection)4);
           


        }

        private static void PrintStartOfAutumnOrWinter(int sensorId, string season)
        {
            DateTime seasonDate = AccessMethods.GetFirstDayOfSeason(sensorId, season);

            string printString = seasonDate != default ?
                $"{season} blev det {seasonDate.ToShortDateString()}." :
                $"Det blev inte {season.ToLower()} innan 2016 års slut.";

            Console.WriteLine(printString);
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

        static void PrintDataForDay(DateTime date, int sensorId)
        {

            Sensor sensor = AccessMethods.GetSensor(sensorId);

            while (true)
            {
                if (sensor == null)
                {
                    Console.WriteLine($"Det finns ingen sensor med Id {sensorId}.");
                    break;
                }

                DailyData dailyData = AccessMethods.GetDataForSensorByDay(date, sensorId);

                if (dailyData.NumberOfTemperatureRecords == 0 && dailyData.NumberOfHumidityRecords == 0)
                {
                    Console.WriteLine($"Det finns inga data från {date.ToShortDateString()} för sensor \"{sensor.SensorName}\".");
                    break;
                }

                else
                {
                    string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";
                    Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");

                    Console.WriteLine($"Datum\t\tTemperatur (C)\tAntal avläsningar\tFuktighet (%)\tMögelrisk (%)\t\tAntal avläsningar");
                    Console.WriteLine(dailyData);
                    break;
                }
            }
        }

        static void PrintSortedList(int id, AccessMethods.Sortingselection sortOn)
        {
            List<DailyData> dailyData = AccessMethods.SortDailyRecordsForSensor(id, sortOn);

            Sensor sensor = AccessMethods.GetSensor(id);

            if (sensor == null)
            {
                Console.WriteLine($"Det finns ingen sensor med Id {id}.");
            }

            else
            {
                string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" sorterat på";

                switch (sortOn)
                {
                    case AccessMethods.Sortingselection.Date:
                        heading += " datum";
                        break;
                    case AccessMethods.Sortingselection.Temperature:
                        heading += " temperatur, varmast till kallast";
                        break;
                    case AccessMethods.Sortingselection.Humidity:
                        heading += " fuktighet, torrast till fuktigast";
                        break;
                    case AccessMethods.Sortingselection.FungusRisk:
                        heading += $" risk för mögel, lägst till högst risk";
                        break;
                    default:
                        heading = "Ingen tabell tillgänglig, felaktigt sorteringsval";
                        break;
                }

                PrintDailyDataTable(dailyData, heading);
            }
        }

        private static void PrintDailyDataTable(List<DailyData> dailyData, string heading)
        {
            Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");

            if (dailyData.Count != 0)
            {
                Console.WriteLine($"Visar resultat för {dailyData.Count} valda dygn. Tryck på valfri tangent för att komma till slutet av listan.\n");
                Console.WriteLine($"Datum\t\tTemperatur (C)\tAntal avläsningar\tFuktighet (%)\tMögelrisk (%)\t\tAntal avläsningar");
                Console.WriteLine(string.Join(Environment.NewLine, dailyData));
                Console.WriteLine();
                Utils.ScrollToTop(dailyData.Count + 15);
            }
        }
    }
}
