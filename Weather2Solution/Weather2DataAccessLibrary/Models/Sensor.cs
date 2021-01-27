using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Weather2DataAccessLibrary.DataAccess;

namespace Weather2DataAccessLibrary.Models
{
    public class Sensor
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SensorName { get; set; }

        public ICollection<Record> Records { get; set; }


        public override string ToString()
        {
            return $"Id: {Id}\tSensornamn: {SensorName, -10}" +
                $"\tAntal avläsningar: {NewMethods.GetNumberOfRecordsForSensor(this)}";
        }
    }
}
