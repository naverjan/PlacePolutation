using Newtonsoft.Json.Linq;
using PlacesPopulation.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace PlacesPopulation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Establishing connection...");
            SqlConnection conn = GetConnection();            

            try
            {
                //Query coordinates
                var locations = GetCoordinates(conn);
                //Get data geo by location
                var locationsComplete = GetDataGeo(locations);
                //update information in db
                UpdateInformation(locationsComplete, conn);
                Console.WriteLine("Programa finalizado");
            }
            catch(Exception e)
            {
                Console.WriteLine("Occurred exception while the program was executed: "+e.Message);
            }
            Console.ReadLine();
        }   
        
        /// <summary>
        /// Realiza conexion a base de datos
        /// </summary>
        /// <returns></returns>
        private  static SqlConnection GetConnection()
        {            
            //connection data
            var host = "climateconnector.canalclima.com,1845";
            var user = "CCLIMATE_DEV";
            var pwd = "}N{IR73l\\F1M";
            var database = "CCLIMATE_DEV";

            //connection string
            var connString = @"Data source=" + host + ";Initial Catalog=" + database + "; Persist Security Info=True; User ID=" + user + ";Password=" + pwd;
            SqlConnection conn = new SqlConnection(connString);
            try
            {            
                conn.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Ocurrio un error al conectarse a la db");
                throw e.InnerException;
            }

            return conn;                 
        }

        /// <summary>
        /// Consulta las cordenadas
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static List<Location>  GetCoordinates(SqlConnection conn)
        {
            List<Location> locations = new List<Location>();
            SqlCommand command = new SqlCommand("SELECT * FROM forecast.cc_locationColombia WHERE revision = 0", conn);
            SqlDataReader data = command.ExecuteReader();            
            while (data.Read())
            {
                var location = new Location(
                    data.GetInt32("id"),
                    data.GetDouble("latitude"),
                    data.GetDouble("longitude")); ;
                locations.Add(location);
                
            }
            return locations;            
        }

        /// <summary>
        /// Get aditional information of interest points
        /// </summary>
        /// <param name="locations">points</param>

        private static List<Location> GetDataGeo(List<Location> locations)
        {
            List<Location> result = new List<Location>();
            int cantLocations = 600;//CANT LOCATIONS TO PROCCESS

            try
            {
                //Scroll list
                int row = 0;
                foreach (Location location in locations)
                {
                    if (row >= cantLocations)
                        break;

                    //url of api positionstack
                    string urlPositionStack = "http://api.positionstack.com/v1/reverse?access_key=26ff29d9ce43c4e413e102e148846438&query=";


                    string latitude = Convert.ToString(location.latitude);
                    string longitude = Convert.ToString(location.longitude);
                    latitude = latitude.Replace(',', '.');
                    longitude = longitude.Replace(',', '.');


                    urlPositionStack = urlPositionStack + latitude + "," + longitude;

                    //request
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlPositionStack);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    //Now, create the Stream  
                    Stream responseStream = response.GetResponseStream();
                    //Seting Up the Stream Reader  
                    StreamReader readerStream = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));

                    string json = readerStream.ReadToEnd();
                    readerStream.Close();
                    var jsonObject = JObject.Parse(json);
                    var arrayObject = JArray.Parse(jsonObject["data"].ToString());
                    if (arrayObject.Count == 0)
                        continue;

                    var place = jsonObject["data"][0];

                    //add information to location
                    location.distance = (place["distance"] == null || place["distance"].ToString() == "") ? 0 : Convert.ToDouble(place["distance"].ToString());
                    location.countryCode = place["country_code"].ToString();
                    location.county = place["county"].ToString();
                    location.type = place["type"].ToString();
                    location.name = place["name"].ToString();
                    location.number = place["number"].ToString();
                    location.label = place["label"].ToString();
                    location.revision = true;
                    result.Add(location);
                    Console.WriteLine("Informacion consulta de positionstack con id " + location.id.ToString());
                    row++;
                }
            }
            catch (Exception e)
            {
                return result;                
            }

            return result;
        }

        /// <summary>
        /// Actualiza la informacion en la base de datos
        /// </summary>
        /// <param name="locations"></param>
        /// <param name="conn"></param>
        private static void UpdateInformation(List<Location> locations, SqlConnection conn)
        {
            foreach(Location location in locations)
            {
                string query = "UPDATE forecast.cc_locationColombia SET distance = " + location.distance.ToString().Replace(',','.') + ","
                 + "countryCode = '" + location.countryCode + "', county = '" + location.county + "', type = '" + location.type + "'," +
                 " name = '" + location.name + "', number = '" + location.number + "', label = '" + location.label + "', revision = " +
                 Convert.ToInt32(location.revision) + " WHERE id =" + location.id;
                
                //ADD QUERY AND EXECUTED
                SqlCommand command = new SqlCommand(query, conn);
                int cant;
                cant = command.ExecuteNonQuery();
                if(cant == 1)
                    Console.WriteLine("Actualizacion de luagar con id "+location.id.ToString());
                else
                    Console.WriteLine("Ocurrio un problema al insertar la informacion con id "+location.id.ToString());
            }                        
        }
    }    
}
