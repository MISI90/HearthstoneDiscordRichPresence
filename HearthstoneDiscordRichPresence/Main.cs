using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Hearthstone_Deck_Tracker.Utility.BoardDamage;
using HearthDb.Enums;
using Discord;
using System.IO;
using System.ComponentModel;
using Hearthstone_Deck_Tracker.Stats.CompiledStats;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using static HearthWatcher.ArenaWatcher;
//using static HearthWatcher.ArenaWatcher;

namespace HearthstoneDiscordRichPresence
{

    public class Main
    {
        private static Discord.Discord discord;
        private static Deck lastSelected;
        public static readonly string[] ranks = { "Bronze", "Silver", "Gold", "Platinum", "Diamond" };
        public static bool isRunning = false;
        private static bool isDiscordInstanceUp = false;
        private static ActivityManager.ClearActivityHandler ClearActivityHandlerCallback = ClearActivityCallback;
        private static ActivityManager.UpdateActivityHandler UpdateActivityHandlerCallback = UpdateActivityCallback;

        internal static void Load()
        {
            Log.Info("HSDiscordRP: Main.Load() starting...");

            // Use your client ID from Discord's developer site.
            //var clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            //if (clientID == null)
            //{
            var clientID = "841234753679130624";
            //}
            discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)CreateFlags.Default);
            isDiscordInstanceUp = true;

            discord.SetLogHook(LogLevel.Debug, (level, message) =>
            {
                Log.Info("HSDTDiscordRP from Discord, level: " + level, ": " + message);
            });

            ClearPresence();

            Log.Info("HSDiscordRP: Finished loading. Calling first HandleUpdate()...");

            HandleUpdate();
        }

        internal static void Unload()
        {
            Log.Info("HSDT DRP Unloading...");

            ClearPresence();
            isDiscordInstanceUp = false;
            discord.Dispose();

            Log.Info("HSDT DRP Successfully Unloaded");
        }

        private static void ClearActivityCallback(Discord.Result result)
        {
            Log.Info("Cleared Activity. Result: " + result);
        }

        private static void UpdateActivityCallback(Discord.Result result)
        {
            Log.Info("Updated Activity. Result: " + result);
        }

        internal static void OnOpponentDeckToPlay(Card obj)
        {
            Log.Info("HSDiscordRP: OnOpponentDeckToPlay triggered");

            if (Core.Game.CurrentGameMode == GameMode.Battlegrounds && obj.Name.StartsWith("Bob's Tavern"))
            {
                string toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                string bottomtext;
                var currentHero = Core.Game.Entities.FirstOrDefault(kvp => kvp.Value.IsHero && kvp.Value.Info.OriginalZone == Zone.HAND && !kvp.Value.Info.Discarded);
                if (currentHero.Value != null)
                {
                    bottomtext = "Recruitment Turn " + (Core.Game.GetTurnNumber() + 1) + " - As " + currentHero.Value.Card.Name;  // +1 neccesary as this triggers before the turn indicator does for somer reason
                }
                else
                {
                    bottomtext = "Recruitment Turn " + Core.Game.GetTurnNumber();
                }
                UpdatePresence(toptext, bottomtext, "baguh", "Battlegrounds");
            }
        }

        internal static void OnDeckSelected(Deck obj)
        {
            if (obj != null)
            {
                Log.Info("HSDiscordRP: OnDeckSelected triggered");

                lastSelected = obj;
                Log.Info("New Deck: " + obj);
                int sum = 0;
                foreach (Card x in obj.Cards)
                {
                    sum += x.Count;
                }
                if (obj.IsArenaDeck && sum < 30 && Core.Game.CurrentMode == Mode.DRAFT)
                {
                    UpdatePresenceShort("Drafting an Arena Deck");
                }
            }
        }

        public static void MyChoicesChangedEventHandler(object sender, HearthWatcher.EventArgs.ChoicesChangedEventArgs args)
        {
            Log.Info("HSDiscordRP: MyChoicesChangedEventHandler triggered");

            if (Core.Game.CurrentMode == Mode.DRAFT)
            {
                UpdatePresenceShort("Drafting an Arena Deck");
            }
        }

        public static void MyCardPickedEventHandler(object sender, HearthWatcher.EventArgs.CardPickedEventArgs args)
        {
            Log.Info("HSDiscordRP: MyCardPickedEventHandler triggered");

            if (Core.Game.CurrentMode == Mode.DRAFT)
            {
                UpdatePresenceShort("Drafting an Arena Deck");
            }
        }

        internal static void OnTurnStart(ActivePlayer x)
        {
            Log.Info("HSDiscordRP: OnTurnStart triggered");

            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds)
            {
                HandleUpdate();
            }
            else
            {
                if (Core.Game.GetTurnNumber() == 1)
                {
                    string toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                    string bottomtext;
                    var currentHero = Core.Game.Entities.FirstOrDefault(kvp => kvp.Value.IsHero && kvp.Value.Info.OriginalZone == Zone.HAND && !kvp.Value.Info.Discarded);
                    if (currentHero.Value != null)
                    {
                        bottomtext = "Recruitment Turn " + Core.Game.GetTurnNumber() + " - As " + currentHero.Value.Card.Name;  // +1 neccesary as this triggers before the turn indicator does for somer reason
                    }
                    else
                    {
                        bottomtext = "Recruitment Turn " + Core.Game.GetTurnNumber();
                    }
                    UpdatePresence(toptext, bottomtext, "baguh", "Battlegrounds");
                }

            }
        }

        internal static void OnOpponentCreateInPlay(Card obj)
        {
            Log.Info("HSDiscordRP: OnOpponentCreateInPlay triggered");

            if (obj != null && Core.Game.CurrentGameMode == GameMode.Battlegrounds && obj.Type.Equals("Hero"))
            {
                string toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                string bottomtext;
                var currentHero = Core.Game.Entities.FirstOrDefault(kvp => kvp.Value.IsHero && kvp.Value.Info.OriginalZone == Zone.HAND && !kvp.Value.Info.Discarded);
                if (currentHero.Value != null)
                {
                    bottomtext = "Combat Turn " + Core.Game.GetTurnNumber() + " - As " + currentHero.Value.Card.Name + " vs. " + obj.Name;
                }
                else
                {
                    bottomtext = "Turn " + Core.Game.GetTurnNumber();
                }
                UpdatePresence(toptext, bottomtext, "baguh", "Battlegrounds");

            }
        }

        internal static void OnPlayerPlay(Card obj)
        {
            Log.Info("HSDiscordRP: OnPlayerPlay triggered");

            if (obj != null && Core.Game.CurrentGameMode == GameMode.Battlegrounds && obj.Type.Equals("Hero"))
            {
                string toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                string bottomtext;
                var currentHero = Core.Game.Entities.FirstOrDefault(kvp => kvp.Value.IsHero && kvp.Value.Info.OriginalZone == Zone.HAND && !kvp.Value.Info.Discarded);
                if (currentHero.Value != null)
                {
                    bottomtext = "Starting Match - As " + currentHero.Value.Card.Name;
                }
                else
                {
                    bottomtext = "Starting Match";
                }
                UpdatePresence(toptext, bottomtext, "baguh", "Battlegrounds");

            }
        }

        public static void MyCompleteDeckEventHandler(object sender, HearthWatcher.EventArgs.CompleteDeckEventArgs args)
        {
            Log.Info("HSDiscordRP: MyCompleteDeckEventHandler triggered");

            HandleUpdate();
        }

        internal static void HandleGameEnd()
        {
            Log.Info("HSDiscordRP: HandleGameEnd triggered");

            switch (Core.Game.CurrentGameStats.Result)
            {
                case GameResult.Win:
                    UpdatePresenceShort("In the Victory Screen");
                    break;
                case GameResult.Loss:
                    UpdatePresenceShort("In the Defeat Screen");
                    break;
                case GameResult.Draw:
                    UpdatePresenceShort("In the Tie Screen");
                    break;
                case GameResult.None:
                default:
                    UpdatePresenceShort("In the Game Finished Screen");
                    break;
            }
        }
        internal static void HandleGameStart()
        {
            Log.Info("HSDiscordRP: HandleGameStart triggered");

            if (Core.Game.CurrentGameMode == GameMode.Battlegrounds)
            {
                string toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                string bottomtext = "Selecting a Hero";
                UpdatePresence(toptext, bottomtext, "baguh", "Battlegrounds");
            }
            else
            {
                HandleUpdate();
            }
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
            // TODO: Handle GameType

            if (Core.Game.CurrentGameMode != GameMode.None && !Core.Game.IsInMenu)
            {
                DisplayCurrentGamemode();
            }
            else
            {
                switch (Core.Game.CurrentMode)
                {
                    case Mode.STARTUP:
                    case Mode.LOGIN:
                        UpdatePresenceShort("Finding a Seat");
                        break;
                    case Mode.HUB:
                        UpdatePresenceShort("In Main Menu");
                        break;
                    case Mode.GAMEPLAY:  // No idea what this one is
                        UpdatePresenceShort("TODO - Gameplay");
                        break;
                    case Mode.COLLECTIONMANAGER:
                        UpdatePresenceShort("Browsing Collection");
                        break;
                    case Mode.PACKOPENING:
                        UpdatePresenceShort("Opening Packs!");
                        break;
                    case Mode.TOURNAMENT:
                        UpdatePresenceShort("Preparing for a battle");
                        break;
                    case Mode.FRIENDLY:
                        UpdatePresenceShort("Preparing to battle a friend");
                        break;
                    case Mode.DRAFT:
                        UpdatePresenceShort("Preparing to Enter the Arena");
                        break;
                    case Mode.ADVENTURE:
                        UpdatePresenceShort("Preparing for an Adventure");
                        break;
                    case Mode.TAVERN_BRAWL:
                        UpdatePresenceShort("Preparing for a Tavern Brawl");
                        break;
                    case Mode.FIRESIDE_GATHERING:
                        UpdatePresenceShort("Attending a Fireside Gathering");
                        break;
                    case Mode.BACON:
                        UpdatePresenceShort("Preparing to Enter the Battlegrounds");
                        break;
                    case Mode.GAME_MODE:
                        UpdatePresenceShort("Picking an Alternate Mode");
                        break;
                    case Mode.PVP_DUNGEON_RUN:
                        UpdatePresenceShort("Preparing for a Duel");
                        break;
                    case Mode.INVALID:
                    case Mode.FATAL_ERROR:
                    case Mode.CREDITS:
                    case Mode.RESET:
                        ClearPresence();
                        break;
                }
            }

        }

        private static string GetPlayInfo(GameMode currentGameMode)
        {
            switch (currentGameMode)
            {
                case GameMode.Ranked:
                case GameMode.Casual:
                case GameMode.Friendly:
                case GameMode.Arena:
                case GameMode.Brawl:
                case GameMode.Practice:
                case GameMode.Duels:
                case GameMode.Spectator:
                    if (!Core.Game.IsMulliganDone)
                    {
                        return "In the Mulligan";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Core.Game.Player.Class) && !string.IsNullOrEmpty(Core.Game.Opponent.Class))
                        {
                            string state = Core.Game.Player.Class + " vs. " + Core.Game.Opponent.Class;
                            if (Core.Game.GetTurnNumber() > 0)
                            {
                                state += " - Turn " + Core.Game.GetTurnNumber();
                            }
                            return state;
                        }
                        else
                        {
                            if (Core.Game.GetTurnNumber() > 0)
                            {
                                return "Turn " + Core.Game.GetTurnNumber();
                            }
                            else
                            {
                                return "";
                            }
                        }
                    }
                case GameMode.Battlegrounds:
                    var currentHero = Core.Game.Entities.FirstOrDefault(kvp => kvp.Value.IsHero && kvp.Value.Info.OriginalZone == Zone.HAND && !kvp.Value.Info.Discarded);
                    if (currentHero.Value != null)
                    {
                        return "Turn " + Core.Game.GetTurnNumber() + " - As " + currentHero.Value.Card.Name;
                    }
                    else
                    {
                        return "Turn " + Core.Game.GetTurnNumber();
                    }
                case GameMode.None:
                case GameMode.All:
                default:
                    return "";
            }
        }

        private static void DisplayCurrentGamemode()
        {
            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds)
            {
                string toptext = "";
                string bottomtext = GetPlayInfo(Core.Game.CurrentGameMode);
                string smallIcon = "";
                string smallText = "";

                switch (Core.Game.CurrentGameMode)
                {
                    case GameMode.Ranked:
                        toptext = "In a Ranked Game" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                        break;
                    case GameMode.Casual:
                        toptext = "In a Casual Game";
                        break;
                    case GameMode.Arena:
                        toptext = "Battling in the Arena" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                        smallIcon = "arena";
                        smallText = "Arena";
                        break;
                    case GameMode.Brawl:
                        toptext = "Partaking in a Brawl";
                        break;
                    case GameMode.Battlegrounds:
                        toptext = "In the Battlegrounds" + EllipsesWrapper(GetRank(Core.Game.CurrentFormat, Core.Game.CurrentGameMode));
                        smallIcon = "baguh";
                        smallText = "Battlegrounds";
                        break;
                    case GameMode.Friendly:
                        toptext = "Playing a Friend";
                        break;
                    case GameMode.Practice:
                        toptext = "Playing against an AI";
                        break;
                    case GameMode.Spectator:
                        toptext = "Spectating a Game";  // TODO: More here
                        break;
                    case GameMode.Duels:
                        toptext = "Partaking in a Duel";
                        smallIcon = "duels";
                        smallText = "Duels";
                        break;
                    case GameMode.None:
                    case GameMode.All:
                    default:
                        toptext = "";
                        break;
                }
                UpdatePresence(toptext, bottomtext, smallIcon, smallText);
            }
        }

        private static string EllipsesWrapper(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }
            else
            {
                return " (" + input + ")";
            }
        }

        private static string GetRank(Format? currentFormat, GameMode currentGameMode)
        {
            if (currentGameMode == GameMode.Ranked)
            {
                string medalInfo;
                switch (currentFormat)
                {
                    case Format.Standard:
                        medalInfo = InterpretMedalInfo(Core.Game.MatchInfo?.LocalPlayer?.Standard);
                        return "Standard" + (!string.IsNullOrEmpty(medalInfo) ? " " + medalInfo : "");
                    case Format.Wild:
                        medalInfo = InterpretMedalInfo(Core.Game.MatchInfo?.LocalPlayer?.Wild);
                        return "Wild" + (!string.IsNullOrEmpty(medalInfo) ? " " + medalInfo : "");
                    case Format.Classic:
                        medalInfo = InterpretMedalInfo(Core.Game.MatchInfo?.LocalPlayer?.Classic);
                        return "Classic" + (!string.IsNullOrEmpty(medalInfo) ? " " + medalInfo : "");
                    default:
                        return "";
                }
            }
            else if (currentGameMode == GameMode.Battlegrounds)
            {
                Log.Info("Starting rating!");
                string rating = Core.Game.BattlegroundsRatingInfo.Rating.ToString();
                Log.Info("Starting ending!");
                return rating;
            }
            else if (currentGameMode == GameMode.Arena)
            {
                ArenaRun first = ArenaStats.Instance.Runs.First();
                if (first.Deck.Equals(lastSelected))
                {
                    return first.Wins + " - " + first.Losses;
                }
                else
                {
                    return "0 - 0";
                }
            }
            else
            {
                return "";
            }
        }

        private static string InterpretMedalInfo(HearthMirror.Objects.MatchInfo.MedalInfo medalInfo)
        {
            if (medalInfo == null)
            {
                return "";
            }
            if (medalInfo.LegendRank > 0)
            {
                return "Legend " + medalInfo.LegendRank;
            }
            else
            {
                return ranks[((medalInfo.StarLevel ?? default) - 1) / 10] + " " + (1 + mod(11 - (medalInfo.StarLevel ?? default) - 1, 10)) + " - " + medalInfo.Stars + " Star" + (medalInfo.Stars != 1 ? "s" : "");

            }
        }

        private static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private static void UpdatePresenceShort(string detail, string smallIcon = "", string smallText = "")
        {
            UpdatePresence(detail, "", smallIcon, smallText);
        }

        public static void UpdatePresence(string detail, string state, string smallIcon = "", string smallText = "")
        {
            var activity = new Discord.Activity
            {
                State = state,
                Details = detail,
                Assets =
                  {
                      LargeImage = "hearthstone",
                      LargeText = "Hearthstone",
                      SmallImage = smallIcon,
                      SmallText = smallText
                },
                Instance = true,
            };

            Log.Info("HSDiscordRP: Updating presence to: {detail: " + detail + ", state: " + state + "}");

            discord.GetActivityManager().UpdateActivity(activity, UpdateActivityHandlerCallback);

        }
        

        private static void ClearPresence()
        {
            Log.Info("HSDiscordRP: About to Clear Presence...");
            discord.GetActivityManager().ClearActivity(ClearActivityHandlerCallback);
            Log.Info("HSDiscordRP: Sent Clear Presence");
        }

        internal static void OnUpdate()
        {
            if (isDiscordInstanceUp)
            {
                try
                {
                    discord.RunCallbacks();
                }
                catch (Discord.ResultException e)
                {
                    Log.Info("ResultException: " + e);
                }
            } 
        }
    }

    public class MainPlugin : IPlugin
    {
        public string Name => "Discord Rich Presence";

        public string Description => "Plugin to show detailed stats of your Hearthstone game on Discord.";

        public string ButtonText => null;

        public string Author => "MISI90 and supersam710";

        public Version Version => new Version(1, 0, 0);

        public MenuItem MenuItem => null;

        public void OnButtonPress()
        {
        }

        public void OnLoad()
        {
            Main.Load();

            Log.Info("HSDiscordRP: Finished Main.Load(). Adding Event listeners...");

            GameEvents.OnGameStart.Add(Main.HandleGameStart);
            GameEvents.OnTurnStart.Add(Main.OnTurnStart);
            GameEvents.OnModeChanged.Add(Main.HandleUpdate);
            //GameEvents.OnModeChanged.Add((Mode mode) => Main.UpdatePresence("a", "b"));
            GameEvents.OnGameEnd.Add(Main.HandleGameEnd);
            GameEvents.OnOpponentDeckToPlay.Add(Main.OnOpponentDeckToPlay);
            GameEvents.OnOpponentCreateInPlay.Add(Main.OnOpponentCreateInPlay);
            GameEvents.OnPlayerPlay.Add(Main.OnPlayerPlay);
            DeckManagerEvents.OnDeckSelected.Add(Main.OnDeckSelected);
            DeckManagerEvents.OnDeckUpdated.Add(Main.OnDeckSelected);

            Log.Info("HSDiscordRP: Finished Adding Event listeners. Adding ArenaWatcher Event Listeners...");

            Watchers.ArenaWatcher.OnChoicesChanged += new ChoicesChangedEventHandler(Main.MyChoicesChangedEventHandler);
            Watchers.ArenaWatcher.OnCardPicked += new CardPickedEventHandler(Main.MyCardPickedEventHandler);
            Watchers.ArenaWatcher.OnCompleteDeck += new CompleteDeckEventHandler(Main.MyCompleteDeckEventHandler);

            Log.Info("HSDiscordRP: Finished Adding Event listeners. Finished Loading HSDiscordRichPresence.");
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
                Main.Load();
            }

            if (Main.isRunning)
            {
                Main.OnUpdate();
            }
        }
    }
}
