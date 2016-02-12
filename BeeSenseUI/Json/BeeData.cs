using System;

namespace BeeSenseUI.Json
{
    public class BeeData
    {
        public double temperature_2 { get; set; }
        public double temperature_1 { get; set; }
        public double humidity_1 { get; set; }
        public double humidity_2 { get; set; }
        public DateTime timestamp { get; set; }
    }
}
