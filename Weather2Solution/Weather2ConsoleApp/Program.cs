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
            //double temp = 23.4;
            //int hum = 96;

            //Console.WriteLine($"Temp: {temp}\tFukt: {hum}\tMögelrisk: {CalculateFungusRisk(temp, hum)}%");

            //List<Record> r = AccessMethods.GetRecordsForSensor(3);
            //PrintRecords(r);

            //string[] fileContent = File.ReadAllLines("TemperaturData.txt");

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

            //DateTime date = new DateTime(2016, 11, 18);
            //int sensorId = 6;

            //PrintDataForDay(date, sensorId);
            //Console.WriteLine();
            //Console.WriteLine();

            //PrintSortedList(sensorId, (AccessMethods.Sortingselection)1);

            DateTime autumnDate = AccessMethods.GetFirstAutumnDay(6);
            Console.WriteLine($"Höst blev det {autumnDate.ToShortDateString()}");
            



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

        static void PrintDataForDay(DateTime date, int id)
        {
            (DailyData dailyData, int numberOfTemperatureRecords, int numberOfHumidityRecords) =
             AccessMethods.GetDataForSensorByDay(date, id);

            Sensor sensor = AccessMethods.GetSensor(id);

            if (sensor == null)
            {
                Console.WriteLine($"Det finns ingen sensor med Id {id}.");
            }

            else if (numberOfTemperatureRecords == 0 && numberOfHumidityRecords == 0)
            {
                Console.WriteLine($"Det finns inga data från {date.ToShortDateString()} för sensor \"{sensor.SensorName}\".");
            }

            else
            {
                string heading = $"Dygnsmedelvärden från sensor \"{sensor.SensorName}\" för valt datum";
                Console.WriteLine($"{heading}\n{Utils.GetUnderline(heading)}\n");

                Console.WriteLine($"Datum\t\tTemperatur (C)\tAntal avläsningar\tFuktighet (%)\tMögelrisk (%)\t\tAntal avläsningar");
                Console.WriteLine(dailyData);
            }
        }

        static void PrintSortedList(int id, AccessMethods.Sortingselection sortOn)
        {
            List<DailyData> dailyData = AccessMethods.SortDailyRecords(id, sortOn);

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
                Console.WriteLine($"Visar resultat för {dailyData.Count} valda dygn. Tryck på valfri tangent för att avsluta.\n");
                Console.WriteLine($"Datum\t\tTemperatur (C)\tAntal avläsningar\tFuktighet (%)\tMögelrisk (%)\t\tAntal avläsningar");
                Console.WriteLine(string.Join(Environment.NewLine, dailyData));
                Console.WriteLine();
                Console.WriteLine($"Visar resultat för {dailyData.Count} valda dygn. Tryck på valfri tangent för att avsluta.\n");
                Utils.ScrollToTop(dailyData.Count + 15);
            }
        }
    }
}
