using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DiscordRPC;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Hearthstone_Deck_Tracker.Utility.BoardDamage;
using HearthDb.Enums;

namespace HearthstoneDiscordRichPresence
{
    public class Main
    {
        private const string ApplicationID = "565295209936715776";
        private static DiscordRpcClient discord;
        public static Boolean isRunning = false;
        internal static void Load()
        {
            discord = new DiscordRpcClient(ApplicationID);
            discord.Initialize();
        }
        internal static void Unload()
        {
            ClearPresence();
            discord.Dispose();
        }
        internal static void HandleUpdate(Mode mode)
        {
            HandleUpdate();
        }
        internal static void HandleUpdate(ActivePlayer player)
        {
            HandleUpdate();
        }
        internal static void HandleUpdate()
        {
            switch (Core.Game.CurrentMode)
            {
                case Mode.HUB:
                    UpdatePresence("In Main Menu");
                    break;
                case Mode.GAMEPLAY:
                    UpdatePresenceGameplay();
                    break;
                case Mode.COLLECTIONMANAGER:
                    UpdatePresence("Browsing Collection");
                    break;
                case Mode.TOURNAMENT:
                    UpdatePresence("Preparing for a battle");
                    break;
                case Mode.ADVENTURE:
                    UpdatePresence("Preparing for an Adventure");
                    break;
                case Mode.TAVERN_BRAWL:
                    UpdatePresence("Preparing for a Tavern Brawl");
                    break;
                case Mode.PACKOPENING:
                    UpdatePresence("Opening Packs!");
                    break;
                case Mode.DRAFT:
                    UpdatePresence("Preparing for an Arena");
                    break;
                case Mode.FATAL_ERROR:
                    ClearPresence();
                    break;
            }

        }

        private static void UpdatePresenceGameplay()
        {
            String detail = "";
            String state = "";

            switch (Core.Game.CurrentGameMode)
            {
                case GameMode.Ranked:
                case GameMode.Casual:
                    detail = GetDetail(Core.Game.CurrentFormat, Core.Game.CurrentGameMode);
                    break;
                case GameMode.Spectator:
                    detail = "Spectating a game";
                    break;
                case GameMode.Brawl:
                    detail = "Playing in Tavern Brawl";
                    break;
                case GameMode.Practice:
                    detail = "Playing against AI";
                    break;
                case GameMode.Arena:
                    detail = "Playing in Arena";
                    break;
                case GameMode.Friendly:
                    detail = "Playing with Friend";
                    break;
            }

            state = Core.Game.Player.Class + " vs. " + Core.Game.Opponent.Class;
            if (Core.Game.GetTurnNumber() > 0)
            {
                state += " - turn " + Core.Game.GetTurnNumber();
            }

            UpdatePresence(detail, state);

        }

        private static string GetDetail(Format? currentFormat, GameMode currentGameMode)
        {
            string detail = "Playing in " + currentGameMode + " " + currentFormat;
            if (currentGameMode == GameMode.Ranked)
            {
                if (currentFormat == Format.Standard)
                {
                    if (Core.Game.MatchInfo.LocalPlayer.StandardLegendRank > 0)
                    {
                        detail += " - Legend Rank " + Core.Game.MatchInfo.LocalPlayer.StandardLegendRank;
                    }
                    else
                    {
                        detail += " - Rank " + Core.Game.MatchInfo.LocalPlayer.StandardRank;
                    }
                }
                else if (currentFormat == Format.Wild)
                {
                    if (Core.Game.MatchInfo.LocalPlayer.WildLegendRank > 0)
                    {
                        detail += " - Legend Rank " + Core.Game.MatchInfo.LocalPlayer.WildLegendRank;
                    }
                    else
                    {
                        detail += " - Rank " + Core.Game.MatchInfo.LocalPlayer.WildRank;
                    }
                }
            }

            return detail;
        }

        private static void UpdatePresence(String detail)
        {
            UpdatePresence(detail, null);
        }
        private static void UpdatePresence(String detail, String state)
        {
            Assets assets = new Assets()
            {
                LargeImageKey = "hs-logo"
            };
            discord?.SetPresence(new RichPresence()
            {
                Details = detail,
                State = state,
                Assets = assets
            });
        }

        private static void ClearPresence()
        {
            discord?.ClearPresence();
        }
    }

    public class MainPlugin : IPlugin
    {
        public string Name => "Discord Rich Presence";

        public string Description => "Plugin to show detailed stats of your Hearthstone game on Discord.";

        public string ButtonText => null;

        public string Author => "MISI90";

        public Version Version => new Version(0, 0, 1);

        public MenuItem MenuItem => null;

        public void OnButtonPress()
        {
        }

        public void OnLoad()
        {
            Main.Load();
            GameEvents.OnGameStart.Add(Main.HandleUpdate);
            GameEvents.OnTurnStart.Add(Main.HandleUpdate);
            GameEvents.OnModeChanged.Add(Main.HandleUpdate);
        }

        public void OnUnload()
        {
            Main.Unload();
        }

        public void OnUpdate()
        {
            if (Main.isRunning && !Core.Game.IsRunning)
            {
                Main.isRunning = false;
                Main.Unload();
            }
            if (!Main.isRunning && Core.Game.IsRunning)
            {
                Main.isRunning = true;
            }
        }
    }
}
