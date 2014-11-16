using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Timers;
using System.Linq;
using System.Media;
using System.Net;
using SteamKit2;

namespace SteamBot
{

    class MainClass
    {
        static SteamClient client;
        static CallbackManager manager;
        static SteamUser steamuser;
        static SteamFriends friends;
        static SteamTrading trade;
        static bool isRunning;
        static string user, pass;
        static string code, auth;

        static string name = "ʢ"; //Steam persona name of the bot

        static float sellmult = .05F; //Amount to increase price based off of backpack.tf price when selling
        static float buymult = .04F; //Amount to decrease price basd off of backpack.tf price when buying

        //static UInt64 ownerID = 76561198039982559; //Steam ID of the bot admin
        static UInt64 ownerID = 76561198103724342; //Temporary

        static Dictionary<string, string> apikeys; //API Keys

        public static void Main(string[] args)
        {
            Console.WriteLine("Fork me on GitHub! http://bit.ly/1uABGoK");
            if (!File.Exists("data/apikeys.json"))
            {
                Console.WriteLine("Your API info needs to be cached.");
                Console.Write("Backpack.TF? ");
                string bptf = Console.ReadLine();
                Console.Write("Steam? ");
                string steam = Console.ReadLine();
                var dict = new Dictionary<string, string>();
                dict.Add("BPTF", bptf);
                dict.Add("STEAM", steam);
                File.WriteAllText("data/apikeys.json", Newtonsoft.Json.JsonConvert.SerializeObject(dict));
            }
            apikeys = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("data/apikeys.json"));
            API.DeserializePoints();
            if (!File.Exists("data/login.json"))
            {
                Console.WriteLine("Your login info needs to be cached.");
                Console.Write("Username? ");
                user = Console.ReadLine();
                Console.Write("Password? ");
                pass = Console.ReadLine();
                File.WriteAllText("data/login.json", Newtonsoft.Json.JsonConvert.SerializeObject(new KeyValuePair<string, string>(user, pass)));
            }
            var login = Newtonsoft.Json.JsonConvert.DeserializeObject<KeyValuePair<string, string>>(File.ReadAllText("data/login.json"));
            user = login.Key;
            pass = login.Value;
            client = new SteamClient();
            manager = new CallbackManager(client);
            steamuser = client.GetHandler<SteamUser>();
            friends = client.GetHandler<SteamFriends>();
            trade = client.GetHandler<SteamTrading>();
            InitCallbacks();
            Console.WriteLine("Connecting to Steam...");
            client.Connect();
            isRunning = true;
            while (isRunning)
            {
                manager.RunCallbacks();
            }
        }

        public static void InitCallbacks()
        {
            //SteamClient
            new Callback<SteamClient.ConnectedCallback>(OnConnect, manager);
            new Callback<SteamClient.DisconnectedCallback>(OnDisconnect, manager);
            //SteamUser
            new Callback<SteamUser.LoggedOnCallback>(OnLogin, manager);
            new Callback<SteamUser.LoggedOffCallback>(OnLogout, manager);
            new Callback<SteamUser.UpdateMachineAuthCallback>(OnAuth, manager);
            new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, manager);
            //SteamFriends
            new Callback<SteamFriends.FriendsListCallback>(OnFriendsList, manager);
            new Callback<SteamFriends.FriendMsgCallback>(OnMsg, manager);
        }

        static void OnConnect(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Steam connection failed: {0}.", callback.Result);
                isRunning = false;
                return;
            }

            Console.WriteLine("Connected to Steam. Logging in as {0}.", user);
            byte[] sentryhash = null;
            if (File.Exists("data/sentry.bin"))
            {
                byte[] sentryfile = File.ReadAllBytes("data/sentry.bin");
                sentryhash = CryptoHelper.SHAHash(sentryfile);
            }
            steamuser.LogOn(new SteamUser.LogOnDetails
            {
                Username = user,
                Password = pass,
                AuthCode = auth,
                TwoFactorCode = code,
                SentryFileHash = sentryhash,
            });
        }

        static void OnDisconnect(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Reconnecting to Steam...");
            client.Connect();
        }

        static void OnLogin(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLogonDeniedNeedTwoFactorCode;

            if (isSteamGuard || is2FA)
            {
                Console.WriteLine("This account is SteamGuard protected!");

                if (is2FA)
                {
                    Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                    code = Console.ReadLine();
                }
                else
                {
                    Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                    auth = Console.ReadLine();
                }

                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                isRunning = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");
        }

        static void OnLogout(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged out of Steam. {0}", callback.Result);
        }

        static void OnAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentryfile...");
            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);
            File.WriteAllBytes("data/sentry.bin", callback.Data);
            steamuser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

            Console.WriteLine("Done!");
        }

        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            friends.SetPersonaState(EPersonaState.Online);
        }

        static void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            friends.SetPersonaName(name);
            foreach (var friend in callback.FriendList)
            {
                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                {
                    // this user has added us, let's add him back
                    friends.AddFriend(friend.SteamID);
                }
            }
        }

        static void OnMsg(SteamFriends.FriendMsgCallback callback)
        {
            string msg = callback.Message;
            if (msg == "stock")
            {
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Geting Steam backpack data...");
                var stock = API.GetCurrencyStock(steamuser.SteamID, apikeys["STEAM"]);
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("Keys: {0}, Refined Metal: {1}", stock.Item1, stock.Item2));
            }
            if (msg == "price")
            {
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Getting price data from backpack.tf...");
                float baseprice = (float)API.GetPrices(6, "5021", apikeys["BPTF"]).Item1;
                float sell = baseprice + (baseprice * sellmult);
                float buy = baseprice - (baseprice * buymult);
                sell = API.ScrapifyPrice(sell);
                buy = API.ScrapifyPrice(buy);
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("Selling keys for: {0} refined\nBuying keys for: {1} refined", sell, buy));
            }
            if (msg == "keyval")
            {
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Geting Steam backpack data...");
                var stock = API.GetCurrencyStock(callback.Sender, apikeys["STEAM"]);
                int keys = stock.Item1;
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Getting price data from backpack.tf...");
                float baseprice = (float)API.GetPrices(6, "5021", apikeys["BPTF"]).Item1;
                float buy = baseprice - (baseprice * buymult);
                buy = API.ScrapifyPrice(buy);
                float totalPay = keys * buy;
                //Grammar is cool!
                switch (keys)
                {
                    case -1:
                        if(stock.Item2 == 15)
                            friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Your Steam inventory is private. Please make sure it is public in order to trade.");
                        else
                            friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "There was an error getting your inventory. Please try again.");
                        break;
                    case 0:
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "You have no keys.");
                        break;
                    case 1:
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("I can buy your key for {0} refined.", totalPay));
                        break;
                    case 2:
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("I can buy both of your keys for {0} refined.", totalPay));
                        break;
                    default:
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("I can buy all {0} of your keys for {1} refined.", keys, totalPay));
                        break;
                }
            }
            if (msg == "points")
            {
                int points = API.GetPoints((long) callback.Sender.ConvertToUInt64());
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("You have {0} loyalty points.", points));
            }
            if (msg == "help")
            {
                if (!File.Exists("data/help.txt"))
                    File.WriteAllText("data/help.txt", "Tell the bot owner to fill out their help.txt!");
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, File.ReadAllText("data/help.txt"));
            }
            if (msg == "offer")
            {
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Hold on a second while I check the trade offer...");
                float offertotal = API.GetTradeOfferRefinedMetal(apikeys["STEAM"], apikeys["BPTF"], callback.Sender.ConvertToUInt64().ToString());
                float sellingtotal = API.GetTradeOfferSellingRefinedMetal(apikeys["STEAM"], apikeys["BPTF"], callback.Sender.ConvertToUInt64().ToString());
                if(offertotal == -1F)
                    friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "There was an error with the internet on my end. Please try again.");
                else{
                    friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("The items you offered me in your trade offer are worth {0} refined, and the items you are asking for are worth {1} refined.", offertotal, sellingtotal));
                    if (offertotal < sellingtotal)
                    {
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Please add more items");
                        API.DeclineTradeOffer(apikeys["STEAM"], callback.Sender.ConvertToUInt64().ToString());
                    }
                    else {
                        
                    }
                }
            }
            if(msg == "id")
            {
                friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("When making a trade offer, please set the message as {0}.", callback.Sender.ConvertToUInt64().ToString()));
            }
            if (callback.Sender.ConvertToUInt64() == ownerID)
            {
                string[] cmds = msg.Split();
                if(cmds[0] == "sellmult" && cmds.Length > 1){
                    try
                    {
                        sellmult = (float)Convert.ToDouble(cmds[1]);
                        float baseprice = (float)API.GetPrices(6, "5021", apikeys["BPTF"]).Item1;
                        float sell = baseprice + (baseprice * sellmult);
                        sell = API.ScrapifyPrice(sell);
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("Selling multiplier changed to {0}. Now selling keys for {1} refined", Convert.ToDouble(cmds[1]), sell));
                    }
                    catch (Exception e)
                    {
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Bad formatting!");
                        Console.WriteLine(e.Message);
                    }
                }
                if (cmds[0] == "buymult" && cmds.Length > 1)
                {
                    try
                    {
                        buymult = (float)Convert.ToDouble(cmds[1]);
                        float baseprice = (float)API.GetPrices(6, "5021", apikeys["BPTF"]).Item1;
                        float buy = baseprice - (baseprice * buymult);
                        buy = API.ScrapifyPrice(buy);
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("Buying multiplier changed to {0}. Now Buying keys for {1} refined", Convert.ToDouble(cmds[1]), buy));
                    }
                    catch (Exception e)
                    {
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Bad formatting!");
                        Console.WriteLine(e.Message);
                    }
                }
                if (cmds[0] == "profit")
                {
                    friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Getting price data from backpack.tf...");
                    float baseprice = (float)API.GetPrices(6, "5021", apikeys["BPTF"]).Item1;
                    float sell = baseprice + (baseprice * sellmult);
                    float buy = baseprice - (baseprice * buymult);
                    sell = API.ScrapifyPrice(sell);
                    buy = API.ScrapifyPrice(buy);
                    friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, String.Format("We are making {0} refined off of every sold key, {1} refined off of every purchased key, and {2} refined off of every key bought and sold.", API.ScrapifyPrice(sell - baseprice), API.ScrapifyPrice(baseprice - buy), API.ScrapifyPrice((sell - baseprice) + (baseprice - buy))));
                }
                if (cmds[0] == "shutdown")
                {
                    API.SerializePoints();
                    Environment.Exit(0);
                }
                if (cmds[0] == "addpoint")
                {
                    try
                    {
                        API.AddPoints((long)Convert.ToUInt64(cmds[1]), Convert.ToInt32(cmds[2]));
                    }
                    catch (Exception e)
                    {
                        friends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Bad formatting!");
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}