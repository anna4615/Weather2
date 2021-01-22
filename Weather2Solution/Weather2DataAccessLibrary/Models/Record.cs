using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Weather2DataAccessLibrary.Models
{
    public class Record
    {
        public int Id { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        public DateTime Time { get; set; }

        public double? Temperature { get; set; }
        public int? Humidity { get; set; }

        public Sensor Sensor { get; set; }


        public override string ToString()
        {
            return $"Id: {Id}\tSensorId: {SensorId}\tTid: {Time}\tTemperatur: {Temperature}" +
                   $"\tFuktighet: {Humidity}";
        }
    }
}

