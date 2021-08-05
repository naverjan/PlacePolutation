using System;
using System.Collections.Generic;
using System.Text;

namespace PlacesPopulation.Models
{
    public class Location
    {
        public Location(int id, Double lat, Double lon)
        {
            this.id = id;
            this.latitude = lat;
            this.longitude = lon;
        }

        public int id { get; set; }
        public string code { get; set; }
        public Double latitude { get; set; }
        public Double longitude { get; set; }        
        public Double distance { get; set; }
        public string countryCode { get; set; }
        public string county { get; set; }
        public string type { get; set; }
        public string name { get; set; }        
        public string number { get; set; }        
        public string label { get; set; }
        public bool isActive { get; set; }
        public bool revision { get; set; }
    }
}
