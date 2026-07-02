using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RetroGames.Helpers;
using RetroRoyale.Database;
using RetroRoyale.Logic.Manager;
using RetroRoyale.Logic.StreamEntry;

namespace RetroRoyale.Logic
{
    public class Player : IDisposable
    {
        [JsonProperty("achievements")] public Achievements Achievements = new Achievements();

        [JsonProperty("stream")] public List<AvatarStreamEntry> Stream = new List<AvatarStreamEntry>(20);

        public Player(long id, string token)
        {
            AccountId = id;
            Name = "RetroRoyale";
            PassToken = token;
            ExpLevel = 12;
            TutorialSteps = 10;
            Language = "EN";
            Diamonds = 1000000;
            Gold = 500;
        }

        [JsonIgnore]
        public long AccountId
        {
            get => (long)HighId << 32 | (uint)LowId;
            set
            {
                HighId = Convert.ToInt32(value >> 32);
                LowId = (int)value;
            }
        }

        [JsonProperty("hi_id")]
        public int HighId { get; set; }

        [JsonProperty("lo_id")]
        public int LowId { get; set; }

        [JsonProperty("alliance_id")]
        public long AllianceId { get; set; }

        [JsonProperty("fb_id")]
        public string FacebookId { get; set; }

        [JsonProperty("account_name")]
        public string Name { get; set; }

        [JsonProperty("pass_token")]
        public string PassToken { get; set; }

        [JsonProperty("exp_level")]
        public int ExpLevel { get; set; }

        [JsonProperty("exp_points")]
        public int ExpPoints { get; set; }

        [JsonProperty("tutorial_steps")]
        public int TutorialSteps { get; set; }

        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonIgnore]
        public Device Device { get; set; }

        [JsonProperty("diamonds")]
        public int Diamonds { get; set; }

        [JsonProperty("gold")]
        public int Gold { get; set; }

        public void Dispose()
        {
            Device = null;
            Stream = null;
            Achievements = null;
        }

        public void AddEntry(AvatarStreamEntry entry)
        {
        }

        public async Task LogicClientHome(MemoryStream stream)
        {
            await stream.WriteLong(AccountId); 

            await stream.WriteVInt(1);
            await stream.WriteVInt(1);
            await stream.WriteVInt(239940);
            await stream.WriteVInt(346065);
            await stream.WriteVInt(Utils.GetCurrentTimestamp);
            await stream.WriteVInt(0);

            await stream.WriteVInt(3); // DECK COUNT

            await stream.WriteVInt(8); // CARDS IN DECK 1
            await stream.WriteVInt(26000001);
            await stream.WriteVInt(26000002);
            await stream.WriteVInt(26000003);
            await stream.WriteVInt(28000004);
            await stream.WriteVInt(28000005);
            await stream.WriteVInt(26000006);
            await stream.WriteVInt(26000007);
            await stream.WriteVInt(26000008);

            await stream.WriteVInt(8); // CARDS IN DECK 2
            await stream.WriteVInt(26000001);
            await stream.WriteVInt(26000002);
            await stream.WriteVInt(26000003);
            await stream.WriteVInt(26000004);
            await stream.WriteVInt(26000005);
            await stream.WriteVInt(26000006);
            await stream.WriteVInt(26000007);
            await stream.WriteVInt(26000008);

            await stream.WriteVInt(8); // CARDS IN DECK 3
            await stream.WriteVInt(26000001);
            await stream.WriteVInt(26000002);
            await stream.WriteVInt(26000003);
            await stream.WriteVInt(26000004);
            await stream.WriteVInt(26000005);
            await stream.WriteVInt(26000006);
            await stream.WriteVInt(26000007);
            await stream.WriteVInt(26000008);

            stream.WriteByte(0xFF);

            await stream.WriteVInt(26);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(2);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(3);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(4);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(5);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(6);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(7);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(26);
            await stream.WriteVInt(8);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(39); // COUNT

            for (var i = 0; i < 29; i++)
            {
                await stream.WriteVInt(26);
                await stream.WriteVInt(i);
                await stream.WriteVInt(12);
                await stream.WriteVInt(0);
                await stream.WriteVInt(1);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0); // Is new = 2, 0 = Old
            }

            for (var i = 0; i < 10; i++)
            {
                await stream.WriteVInt(27);
                await stream.WriteVInt(i);
                await stream.WriteVInt(12);
                await stream.WriteVInt(0);
                await stream.WriteVInt(1);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0); // Is new = 2, 0 = Old
            }

            await stream.WriteVInt(0); // SELECTED DECK

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteHex("7f");

            await stream.WriteVInt(33);
            await stream.WriteVInt(0); // CurrentTimestamp
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(1); // EventCount
            {
                await stream.WriteVInt(1109); // ID
                await stream.WriteString("2v2 Button");
                await stream.WriteVInt(8); // Type
                await stream.WriteVInt(0); // StartTime
                await stream.WriteVInt(2147483647); // EndTime
                await stream.WriteVInt(0); // VisibleOn
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteString("2v2 Button");
                await stream.WriteString("{\"HideTimer\":false,\"HidePopupTimer\":false}\"");
            }

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteHex("7f");

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // ChallengeEvents

            await stream.WriteVInt(1); // EventCount
            {
                await stream.WriteVInt(1109);
            }

            await stream.WriteVInt(2); // EventCount
            {
                await stream.WriteVInt(2);
                await stream.WriteString("{\"ID\":\"CARD_RELEASE\",\"Params\":{}})");
                await stream.WriteVInt(4);
                await stream.WriteString("{\"ID\":\"CLAN_CHEST\",\"Params\":{}}");
            }

            // CHESTS
            await stream.WriteVInt(4); // SLOT COUNT
            {
                await stream.WriteVInt(0);
                /*await stream.WriteVInt(1);
                await stream.WriteVInt(19); // ClassId
                await stream.WriteVInt(1); // CHEST Type (1 = Wooden Chest, 6 Magical...)
                await stream.WriteVInt(0); // CHEST READY TO OPEN
                await stream.WriteVInt(1);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);*/
            }

            await stream.WriteVInt(0); // FreeChestTimer
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // bool

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // CrownChestCount
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(-1);
            await stream.WriteVInt(1714640);
            await stream.WriteVInt(1726960);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(-1);

            await stream.WriteVInt(1); // 1 = SetNamePopup, 2 = Upgrade Card Tutorial, 3 = NameSet

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(2);
            await stream.WriteVInt(12); // Level
            await stream.WriteVInt(55);
            await stream.WriteVInt(10); // OldArena

            //shop
            await stream.WriteVInt(1); // ShopDay
            await stream.WriteVInt(1); // Seed
            await stream.WriteVInt(1); // DaySeen

            await stream.WriteVInt(0); // TicksUntilTomorrow
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // OfferCount

            await stream.WriteVInt(0); // Special

            for (var i = 0; i < 3; i++) ;
            {
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
                await stream.WriteVInt(0);
            }

            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // CardRequest?

            await stream.WriteVInt(0);

            await stream.WriteVInt(23);

            // Array
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteHex("F807");

            await stream.WriteVInt(1);
            await stream.WriteVInt(1);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(11);
            await stream.WriteVInt(0);

            await stream.WriteVInt(2);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(4);
            await stream.WriteVInt(3);
            await stream.WriteVInt(17);
            await stream.WriteVInt(1);

            await stream.WriteVInt(14);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(5);
            await stream.WriteVInt(4);
            await stream.WriteVInt(14);
            await stream.WriteVInt(1);

            await stream.WriteVInt(74);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(5);
            await stream.WriteVInt(4);
            await stream.WriteVInt(1);
            await stream.WriteVInt(1);

            await stream.WriteVInt(73);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(5);
            await stream.WriteVInt(0);

            await stream.WriteVInt(4);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(9);
            await stream.WriteVInt(0);

            await stream.WriteVInt(15);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(1);
            await stream.WriteVInt(6);
            await stream.WriteVInt(2);

            await stream.WriteVInt(16);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(1);
            await stream.WriteVInt(1);
            await stream.WriteVInt(6);
            await stream.WriteVInt(2);

            await stream.WriteVInt(0);

            // Missions
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(1);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // NewArenasSeenCount

            await stream.WriteVInt(0); // SessionReward = 2
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(7); // TrainingBattlesCompleted
        }

        public async Task LogicClientAvatar(MemoryStream stream)
        {
            await stream.WriteVInt(HighId); // HighId
            await stream.WriteVInt(LowId); // LowId 
            await stream.WriteVInt(HighId); // HighId
            await stream.WriteVInt(LowId); // LowId 
            await stream.WriteVInt(HighId); // HighId
            await stream.WriteVInt(LowId); // LowId

            await stream.WriteString("<c4>" + Name + "</c>"); // Name
            await stream.WriteVInt(1); // NameSetByUser
            await stream.WriteVInt(10); // Arena
            await stream.WriteVInt(3000); // Trophies

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // LegendaryTrophies

            await stream.WriteVInt(0); // CurrentSeasonTrophies
            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // DisplaysNearLeague // maybe never used

            await stream.WriteVInt(0); // BestSeasonTrophies
            await stream.WriteVInt(0); // Rank
            await stream.WriteVInt(0); // Trophies

            // League
            await stream.WriteVInt(0); // CurrentTrophies
            await stream.WriteVInt(0); // PastTrophies
            await stream.WriteVInt(1);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // set this 1 and it appears on the profile

            await stream.WriteVInt(8);

            // Game Variables
            await stream.WriteVInt(10);
            await stream.WriteVInt(5);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(5);
            await stream.WriteVInt(1);
            await stream.WriteVInt(500000); // Gold

            await stream.WriteVInt(5);
            await stream.WriteVInt(3);
            await stream.WriteVInt(2);

            await stream.WriteVInt(5); // NewCrowns
            await stream.WriteVInt(4);
            await stream.WriteVInt(0);

            await stream.WriteVInt(5);
            await stream.WriteVInt(5);
            await stream.WriteVInt(500000); // Gold

            await stream.WriteVInt(5);
            await stream.WriteVInt(13);
            await stream.WriteVInt(0); // NewGold

            await stream.WriteVInt(5);
            await stream.WriteVInt(14);
            await stream.WriteVInt(0);

            await stream.WriteVInt(5);
            await stream.WriteVInt(16);
            await stream.WriteVInt(51);

            await stream.WriteVInt(5);
            await stream.WriteVInt(28);
            await stream.WriteVInt(0);

            await stream.WriteVInt(5);
            await stream.WriteVInt(29);
            await stream.WriteVInt(72000006);

            await stream.WriteVInt(0); // CompletedAchievements

            // Achievements
            await stream.WriteVInt(0); // AchievementCount
            await stream.WriteVInt(0); // AchievementCount

            // Profile Statistics
            await stream.WriteVInt(6);
            await stream.WriteVInt(5);
            await stream.WriteVInt(6);
            await stream.WriteVInt(30);

            await stream.WriteVInt(5);
            await stream.WriteVInt(7);
            await stream.WriteVInt(0); // ThreeCrownWinCount

            await stream.WriteVInt(5);
            await stream.WriteVInt(8);
            await stream.WriteVInt(5); // CardsFound

            await stream.WriteVInt(5);
            await stream.WriteVInt(1); // Count
            await stream.WriteVInt(26000048); // CardID

            await stream.WriteVInt(5);
            await stream.WriteVInt(11);
            await stream.WriteVInt(32);

            await stream.WriteVInt(5);
            await stream.WriteVInt(27);
            await stream.WriteVInt(1);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // NPC? / Count?
            await stream.WriteVInt(0);

            await stream.WriteVInt(Diamonds); // Diamonds
            await stream.WriteVInt(Diamonds); // FreeDiamonds

            await stream.WriteVInt(0); // Experience
            await stream.WriteVInt(12); // Level

            await stream.WriteVInt(12); // AvatarUserLevelTier

            await stream.WriteVInt(0); // HasAlliance

            // Battle Statistics
            await stream.WriteVInt(0); // GamesPlayed
            await stream.WriteVInt(0); // TournamentMatchesPlayed
            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // Wins
            await stream.WriteVInt(0); // Losses

            await stream.WriteVInt(0);

            await stream.WriteVInt(7);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0); // Has Challenge
            //  await stream.WriteVInt(); // ID
            //  await stream.WriteVInt(0); // WINS
            //  await stream.WriteVInt(0); // LOSSES

            await stream.WriteVInt(0);
            await stream.WriteVInt(0);
            await stream.WriteVInt(0);

            await stream.WriteVInt(0);
            await stream.WriteVInt(0); // AccountCreated
            await stream.WriteVInt(0); // PlayTime
        }

        public async Task AvatarRankingEntry(MemoryStream stream)
        {
            await stream.WriteVInt(ExpLevel); // ExpLevel

            await stream.WriteVInt(0); // Unknown
            await stream.WriteVInt(0); // Unknown
            await stream.WriteVInt(0); // Unknown
            await stream.WriteVInt(0); // Unknown
            await stream.WriteVInt(0); // Unknown

            await stream.WriteString("EN"); // Language

            await stream.WriteLong(AccountId); // HomeId

            await stream.WriteVInt(0); // Unknown
            await stream.WriteVInt(0); // Unknown

            await stream.WriteVInt(0); // Unknown
        }

        public void AddDiamonds(int value)
        {
            Diamonds += value;
        }

        public bool UseDiamonds(int value)
        {
            if (Diamonds < value)
                return false;

            Diamonds -= value;

            return true;
        }

        public async Task Update()
        {
            await Redis.CachePlayer(this);
        }
    }
}
 