using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace SteamBot
{
    class API
    {

        public static Dictionary<long, int> points;

        //Magical function to make multiples of 1/9 out of any decimal number
        //This is especially useful for turning the raw calculated prices into actual prices
        public static float ScrapifyPrice(float price)
        {
            float end = price - (float)Math.Truncate(price);
            end *= 10;
            end = (float)Math.Truncate(end);
            end *= 11;
            if (end == 99)
                end++;
            end /= 100;
            float final = (float)Math.Truncate(price) + end;
            return final;
        }

        //This method fetches the amount of Keys and Refined Metal from any given Steam inventory
        //As a rule, I try to keep all downloads into strings in order to reduce filesystem footprint. However, this causes a bit more RAM usage.
        //TODO Cache API Results
        public static Tuple<int, int> GetCurrencyStock(UInt64 ID, string steamapi)
        {
            var items = new List<KeyValuePair<int, int>>();
            //The first value is the amount of keys, and the second value is the amount of refined metal
            //In case of an error, the first is -1, and the second is the status code from the Steam API
            //If the Steam API completely failed, then both the first and second are -1
            try
            {
                WebClient client = new WebClient();
                String backpackFile = client.DownloadString("http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + steamapi + "&steamid=" + ID);
                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(backpackFile);

                if (result.result.status == 15)
                {
                    return new Tuple<int, int>(-1, 15);
                }
                else
                {
                    foreach (var item in result.result.items)
                    {
                        items.Add(new KeyValuePair<int, int>(Convert.ToInt32(item.defindex + ""), Convert.ToInt32(item.quality + "")));
                    }
                    int refcount = 0;
                    int keycount = 0;
                    foreach (var item in items)
                    {
                        if (item.Key == 5002)
                        {
                            refcount++;
                        }
                        if (item.Key == 5021 || item.Key == 5713)
                        {
                            keycount++;
                        }
                    }
                    return new Tuple<int, int>(keycount, refcount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new Tuple<int, int>(-1, -1);
        }

        //The loyalty point system works using a dictionary of SteamIDs and integers.
        //The dictionary is deserialized when the program starts, and serialized everytime points are added and every time there is a graceful shutdown
        public static void DeserializePoints()
        {
            if (!File.Exists("data/points.json"))
            {
                File.WriteAllText("data/points.json", "{\"76561198039982559\":0}");
            }
            string json = File.ReadAllText("data/points.json");
            points = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<long, int>>(json);
            Console.WriteLine();
        }

        public static void SerializePoints()
        {
            string serial = Newtonsoft.Json.JsonConvert.SerializeObject(points);
            File.WriteAllText("points.json", serial);
        }

        public static void AddPoints(long ID, int point)
        {
            int current = GetPoints(ID);
            points[ID] = current + point;
            SerializePoints();
        }

        public static int GetPoints(long ID)
        {
            if (points.ContainsKey(ID))
                return points[ID];
            else
            {
                points.Add(ID, 0);
                return 0;
            }
        }

        //This method gets the price for an item off of backpack.tf, given a defindex.
        //As of now, it only works for Craftable, Tradable, and the first quality of the given item.
        //The method was made primarily with keys in mind
        //TODO Cache API result
        public static double GetPrices(string defindex, string bptfapi)
        {
            WebClient client = new WebClient();
            string priceFile = client.DownloadString("http://backpack.tf/api/IGetPrices/v4/?key=" + bptfapi);
            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(priceFile);
            double keyprice = 0;
            //Big nasty JSON parse function. Cmon, Brad, fix your API. Perhaps a feature to fetch data for a defindex via the URL?
            try
            {
                foreach (var items in result.response.items)
                {
                    foreach (var item in items)
                    {
                        string indexstr = item.defindex.ToString();
                        if (indexstr.Contains(defindex))
                        {
                            foreach (var price in item.prices)
                            {
                                foreach (var quality in price)
                                {
                                    Console.WriteLine("Key price fetched: {0} {1}", quality.Tradable.Craftable[0].value, quality.Tradable.Craftable[0].currency);
                                    keyprice = Convert.ToDouble(quality.Tradable.Craftable[0].value.ToString());
                                    return keyprice;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Parse complete");
            return -1;
        }
    }
}
