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
       

        /// <summary>
        /// Main Method
        /// </summary>
        public void Send(string name, string token)
        {
            if (MyPlayers.DoesNameExist(name))
            {
                Clients.All.duplicateName(name,token);
                return;
            }
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

        public static bool DoesNameExist(string name)
        {
            if (PalyersCount() > 0)
            {
              return  Players.Any(player => player.UserName == name);
            }
            return false;
        }
    }
}