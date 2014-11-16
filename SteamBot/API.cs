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

        public static float GetTradeOfferRefinedMetal(string steamapi, string bptfapi, string msg)
        {
            try
            {
                var tradeItems = new List<string>();
                var itemdata = new List<Tuple<int, string>>();
                WebClient web = new WebClient();
                string offersjson = web.DownloadString("http://api.steampowered.com/IEconService/GetTradeOffers/v1/?get_received_offers=1&active_only=1&key=" + steamapi);
                dynamic offersresult = Newtonsoft.Json.JsonConvert.DeserializeObject(offersjson);
                foreach (var offer in offersresult.response.trade_offers_received)
                {
                    foreach (var item in offer.items_to_receive)
                    {
                        if (offer.message.ToString() == msg)
                            tradeItems.Add(item.classid.ToString());
                    }
                }
                foreach (var classid in tradeItems)
                {
                    string itemsjson = web.DownloadString("http://api.steampowered.com/ISteamEconomy/GetAssetClassInfo/v0001/?appid=440&classid0=" + classid + "&class_count=1&key=" + steamapi);
                    dynamic itemsresult = Newtonsoft.Json.JsonConvert.DeserializeObject(itemsjson);
                    foreach (var id in itemsresult.result)
                    {
                        foreach (var item in id)
                        {
                            try
                            {
                                itemdata.Add(new Tuple<int, string>(Convert.ToInt32(item.app_data.quality.ToString()), item.app_data.def_index.ToString()));
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                float total = 0;
                foreach (var itemdatum in itemdata)
                {
                    string defindex = itemdatum.Item2;
                    int quality = itemdatum.Item1;
                    if (defindex == "5002")
                        total++;
                    else if (defindex == "5021"){
                        float keyprice = GetPrices(6, "5021", bptfapi).Item1;
                        keyprice = ScrapifyPrice(keyprice - (keyprice * .04F));
                        total += keyprice;
                    }
                    else
                    {
                        var price = GetPrices(quality, defindex, bptfapi);
                        switch (price.Item2)
                        {
                            case "metal":
                                if (defindex == "5021")
                                    total += ScrapifyPrice(price.Item1 - (price.Item1 * .04F));
                                else
                                    total += ScrapifyPrice(price.Item1 * .75F);
                                break;
                            case "keys":
                                float keyprice = GetPrices(6, "5021", bptfapi).Item1;
                                keyprice = ScrapifyPrice(keyprice + (keyprice * .05F));
                                total += ScrapifyPrice((keyprice * price.Item1) * .75F);
                                break;
                        }
                    }
                }
                return ScrapifyPrice(total);
            }
            catch (System.Net.WebException)
            {
                return -1;
            }
        }

        public static float GetTradeOfferSellingRefinedMetal(string steamapi, string bptfapi, string msg)
        {
            try
            {
                var tradeItems = new List<string>();
                var itemdata = new List<Tuple<int, string>>();
                WebClient web = new WebClient();
                string offersjson = web.DownloadString("http://api.steampowered.com/IEconService/GetTradeOffers/v1/?get_received_offers=1&active_only=1&key=" + steamapi);
                dynamic offersresult = Newtonsoft.Json.JsonConvert.DeserializeObject(offersjson);
                foreach (var offer in offersresult.response.trade_offers_received)
                {
                    if (offer.items_to_give == null)
                        return 0;
                    foreach (var item in offer.items_to_give)
                    {
                        if (offer.message.ToString() == msg)
                            tradeItems.Add(item.classid.ToString());
                    }
                }
                foreach (var classid in tradeItems)
                {
                    string itemsjson = web.DownloadString("http://api.steampowered.com/ISteamEconomy/GetAssetClassInfo/v0001/?appid=440&classid0=" + classid + "&class_count=1&key=" + steamapi);
                    dynamic itemsresult = Newtonsoft.Json.JsonConvert.DeserializeObject(itemsjson);
                    foreach (var id in itemsresult.result)
                    {
                        foreach (var item in id)
                        {
                            try
                            {
                                itemdata.Add(new Tuple<int, string>(Convert.ToInt32(item.app_data.quality.ToString()), item.app_data.def_index.ToString()));
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                float total = 0;
                foreach (var itemdatum in itemdata)
                {
                    string defindex = itemdatum.Item2;
                    int quality = itemdatum.Item1;
                    if (defindex == "5002")
                        total++;
                    else
                    {
                        var price = GetPrices(quality, defindex, bptfapi);
                        switch (price.Item2)
                        {
                            case "metal":
                                if (defindex == "5021")
                                    total += ScrapifyPrice(price.Item1 + (price.Item1 * .05F));
                                else
                                    total += ScrapifyPrice(price.Item1 * 1.15F);
                                break;
                            case "keys":
                                float keyprice = GetPrices(6, "5021", bptfapi).Item1;
                                keyprice = ScrapifyPrice(keyprice + (keyprice * .05F));
                                total += ScrapifyPrice((keyprice * price.Item1) * 1.15F);
                                break;
                        }
                    }
                }
                return ScrapifyPrice(total);
            }
            catch (System.Net.WebException)
            {
                return -1;
            }
        }

        public static void DeclineTradeOffer(string steamapi, string msg)
        {
            try
            {
                var tradeItems = new List<string>();
                var itemdata = new List<Tuple<int, string>>();
                WebClient web = new WebClient();
                string offersjson = web.DownloadString("http://api.steampowered.com/IEconService/GetTradeOffers/v1/?get_received_offers=1&active_only=1&key=" + steamapi);
                dynamic offersresult = Newtonsoft.Json.JsonConvert.DeserializeObject(offersjson);
                foreach (var offer in offersresult.response.trade_offers_received)
                {
                    if (offer.message.ToString() == msg)
                    {
                        web.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        string param = "key=" + steamapi + "&tradeofferid=" + offer.tradeofferid;
                        string HtmlResult = web.UploadString("http://api.steampowered.com/IEconService/DeclineTradeOffer/v1/", param);
                    }
                }
            }
            catch (System.Net.WebException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

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
                string backpackFile = client.DownloadString("http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + steamapi + "&steamid=" + ID);
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
            File.WriteAllText("data/points.json", serial);
        }

        public static void AddPoints(long ID, int point)
        {
            int current = GetPoints(ID);
            points[ID] = current + point;
            SerializePoints();
        }

        public static int GetPoints(long ID)
        {
            DeserializePoints();
            if (points.ContainsKey(ID))
                return points[ID];
            else
            {
                points.Add(ID, 0);
                SerializePoints();
                return 0;
            }
        }

        //This method gets the price for an item off of backpack.tf, given a defindex.
        //As of now, it only works for Craftable, Tradable, and the first quality of the given item.
        //The method was made primarily with keys in mind
        //TODO Cache API result
        public static Tuple<float, string> GetPrices(int itemquality, string defindex, string bptfapi)
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
                                    if (price.Name == itemquality.ToString())
                                    {
                                        System.Threading.Thread.Sleep(3000); //For some reason, this fixes a bug where a null reference comes up
                                        Console.WriteLine("Price fetched: {0} {1}", quality.Tradable.Craftable[0].value, quality.Tradable.Craftable[0].currency);
                                        keyprice = Convert.ToDouble(quality.Tradable.Craftable[0].value.ToString());
                                        string currency = quality.Tradable.Craftable[0].currency.ToString();
                                        var tuple = new Tuple<float, string>((float)keyprice, currency);
                                        return tuple;
                                    }
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
            return new Tuple<float,string>(-1F, "");
        }
    }
}
