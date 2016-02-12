using System;
using System.Collections.Generic;
using System.Net;
using BeeSenseUI.Json;
using Newtonsoft.Json;

namespace BeeSenseUI.Tests
{
    public class JsonTest
    {
        // Note here NAN is replaced with NaN for double to parse
        
        public static bool DeserializeBeeJSON(string json)
        {
            // Need to convert NaN to something double parser
            
            var collData = JsonConvert.DeserializeObject <List<BeeData>>(json);

            foreach(var data in collData)
            {
                Console.WriteLine(data.timestamp + ": Temperature1: " + data.temperature_1 + ", Humidity1: " + data.humidity_1 + ", Temperature2: " + data.temperature_2 + ", Humidity2: " + data.humidity_2);
            }
            return true;
        }

        public static bool DownloadAndDeserialiseJSON(string url)
        {
            var json = new WebClient().DownloadString(url);

            // Fixup NAN -> NaN
            json = json.Replace("NAN", "NaN");

            return DeserializeBeeJSON(json);
        }
    }
}
