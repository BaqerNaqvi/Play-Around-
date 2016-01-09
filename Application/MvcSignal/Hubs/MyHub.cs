using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using MvcSignal.Models;
using Microsoft.AspNet.SignalR.Hubs;

namespace MvcSignal
{
    public class MyHub : Hub
    {
        static List<UserInfo> UsersList = new List<UserInfo>();
        static List<MessageInfo> MessageList = new List<MessageInfo>();

        //-->>>>> ***** Receive Request From Client [  Connect  ] *****
        public void Connect(string userName, string password)
        {
            var id = Context.ConnectionId;
            string userGroup="";
            //Manage Hub Class
            //if freeflag==0 ==> Busy
            //if freeflag==1 ==> Free

            //if tpflag==0 ==> User
            //if tpflag==1 ==> Admin


            var ctx = new TestEntities();

            var userInfo =
                 (from m in ctx.tbl_User
                  where m.UserName == userName && m.Password == password
                  select new { m.UserID, m.UserName, m.AdminCode }).FirstOrDefault();


            try
            {
                //You can check if user or admin did not login before by below line which is an if condition
                //if (UsersList.Count(x => x.ConnectionId == id) == 0)

                //Here you check if there is no userGroup which is same DepID --> this is User otherwise this is Admin
                //userGroup = DepID
               
               
                if ((int)userInfo.AdminCode == 0)
                {
                    //now we encounter ordinary user which needs userGroup and at this step, system assigns the first of free Admin among UsersList
                    var strg = (from s in UsersList where (s.tpflag == "1") && (s.freeflag == "1") select s).First();
                    userGroup = strg.UserGroup;

                    //Admin becomes busy so we assign zero to freeflag which is shown admin is busy
                    strg.freeflag = "0";

                    //now add USER to UsersList
                    UsersList.Add(new UserInfo { ConnectionId = id, UserID = userInfo.UserID, UserName = userName, UserGroup = userGroup, freeflag = "0", tpflag = "0", });
                    //whether it is Admin or User now both of them has userGroup and I Join this user or admin to specific group 
                    Groups.Add(Context.ConnectionId, userGroup);
                    Clients.Caller.onConnected(id, userName, userInfo.UserID, userGroup);

                }
                else
                {
                    //If user has admin code so admin code is same userGroup
                    //now add ADMIN to UsersList
                    UsersList.Add(new UserInfo { ConnectionId = id, AdminID = userInfo.UserID, UserName = userName, UserGroup = userInfo.AdminCode.ToString(), freeflag = "1", tpflag = "1" });
                    //whether it is Admin or User now both of them has userGroup and I Join this user or admin to specific group 
                    Groups.Add(Context.ConnectionId, userInfo.AdminCode.ToString());
                    Clients.Caller.onConnected(id, userName, userInfo.UserID, userInfo.AdminCode.ToString());

                }

                
               

            }

            catch
            {
                string msg = "All Administrators are busy, please be patient and try again";
                //***** Return to Client *****
                Clients.Caller.NoExistAdmin();

            }


        }
        // <<<<<-- ***** Return to Client [  NoExist  ] *****



        //--group ***** Receive Request From Client [  SendMessageToGroup  ] *****
        public void SendMessageToGroup(string userName, string message)
        {

            if (UsersList.Count != 0)
            {
                var strg = (from s in UsersList where (s.UserName == userName) select s).First();
                MessageList.Add(new MessageInfo { UserName = userName, Message = message, UserGroup = strg.UserGroup });
                string strgroup = strg.UserGroup;
                // If you want to Broadcast message to all UsersList use below line
                // Clients.All.getMessages(userName, message);

                //If you want to establish peer to peer connection use below line so message will be send just for user and admin who are in same group
                //***** Return to Client *****
                Clients.Group(strgroup).getMessages(userName, message);
            }

        }
        // <<<<<-- ***** Return to Client [  getMessages  ] *****


        //--group ***** Receive Request From Client ***** { Whenever User close session then OnDisconneced will be occurs }
        public override System.Threading.Tasks.Task OnDisconnected()
        {

            var item = UsersList.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                UsersList.Remove(item);

                var id = Context.ConnectionId;

                if (item.tpflag == "0")
                {
                    //user logged off == user
                    try
                    {
                        var stradmin = (from s in UsersList where (s.UserGroup == item.UserGroup) && (s.tpflag == "1") select s).First();
                        //become free
                        stradmin.freeflag = "1";
                    }
                    catch
                    {
                        //***** Return to Client *****
                        Clients.Caller.NoExistAdmin();
                    }
                    
                }

                //save conversation to dat abase


            }

            return base.OnDisconnected();
        }

        /// <summary>
        /// Main Method
        /// </summary>
        public void Send(string name, string token)
        {
            // New Player
            var player = new User
            {
                IsUserTurn = false,
                Token = token,
                UserName = name
            };

            if (MyPlayers.Players == null)
            {
                MyPlayers.Init();
            }
            MyPlayers.Players.Add(player);

            if (MyPlayers.PalyersCount() >= 2 && MyPlayers.IsPlayingGame == false)
            {
                MyPlayers.QueueTopTwoPlayers();
                MyPlayers.IsPlayingGame = true;
            }
            var players = MyPlayers.GetAllPlayers();
            // Brodcasr all players
            Clients.All.broadcastMessage(players, "");
        }

        /// <summary>
        /// Updates & Schedule Players
        /// </summary>
        public void Update(string groupToken)
        {
            MyPlayers.IsPlayingGame = false;
            MyPlayers.HaveReturenedFromGame = true;
            var listOfPlayersJustPlayed = MyPlayers.GetPlayersByGroupToken();
            if (listOfPlayersJustPlayed.Count > 0)
            {
                var players = MyPlayers.Players;
                // Removes Played Users 
                players.RemoveAll(player => player.IsUserTurn);
                MyPlayers.QueueTopTwoPlayers();
                Clients.All.broadcastMessage(players, "");
            }
        }

        public void GetPlayers()
        {
            var players = MyPlayers.GetAllPlayers();
            Clients.All.broadcastMessage(players, "");
        }
    }
    /// <summary>
    /// Game Player
    /// </summary>
    public static class MyPlayers
    {
        /// <summary>
        /// Init of Players
        /// </summary>
        public static void Init()
        {
            Players = new List<User>();
            HaveReturenedFromGame = true;
            IsPlayingGame = false;
        }

        /// <summary>
        /// List of Players
        /// </summary>
        public static List<User> Players { get; set; }

        /// <summary>
        /// Token of Group Playing
        /// </summary>
        public static string PlayingGroupToken { get; set; }

        /// <summary>
        /// Status Of Game
        /// </summary>
        public static Boolean HaveReturenedFromGame { get; set; }

        /// <summary>
        /// Status Of Game
        /// </summary>
        public static Boolean IsPlayingGame { get; set; }

        /// <summary>
        /// Returns List of All Players
        /// </summary>
        public static List<User> GetAllPlayers()
        {
            int count = PalyersCount();
            var list = new List<User>();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    list.Add(Players[i]);
                }
            }
            return list;
        }

        /// <summary>
        /// Returns Top Players
        /// </summary>
        public static void QueueTopTwoPlayers()
        {
            int count = PalyersCount();
            if (count >= 2)
            {
                // Fetching
                var first = Players[0];
                var second = Players[1];

                // Random Group Token
                PlayingGroupToken = GenerateRandomToken();

                // Make True and update localy
                first.IsUserTurn = true;
                first.GroupToken = PlayingGroupToken;

                second.IsUserTurn = true;
                second.GroupToken = PlayingGroupToken;

                // Updating list
                Players[0] = first;
                Players[1] = second;
            }
        }

        /// <summary>
        /// Current Number Of Players
        /// </summary>
        public static int PalyersCount()
        {
            if (Players == null)
            {
                Players = new List<User>();
            }
            return Players.Count;
        }

        /// <summary>
        /// Generates Random Token 
        /// </summary>
        public static string GenerateRandomToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        /// <summary>
        /// Returns Players who have just played
        /// </summary>
        public static List<User> GetPlayersByGroupToken()
        {
            if (HaveReturenedFromGame)
            {
                int count = PalyersCount();
                if (count > 0)
                {
                    return Players.Where(player => player.IsUserTurn).ToList();
                }
            }
            return new List<User>();
        }

    }
}