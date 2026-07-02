using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using RetroClash.Core.Database;
using RetroClash.Logic.Slots;
using RetroClash.Logic.StreamEntry;
using RetroGames.Helpers;
using RetroGames.Logic;

namespace RetroClash.Logic
{
    public class Alliance
    {
        [JsonProperty("members")] public List<AllianceMember> Members = new List<AllianceMember>(50);

        [JsonProperty("stream")] public List<AllianceStreamEntry> Stream = new List<AllianceStreamEntry>(40);

        [JsonIgnore] public Timer Timer = new Timer(5000)
        {
            AutoReset = true
        };

        public Alliance(long id)
        {
            Id = id;
            Name = "RetroClash";
            Badge = 13000000;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonIgnore]
        public bool IsFull => Members.Count == 50;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("badge")]
        public int Badge { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("r_score")]
        public int RequiredScore { get; set; }

        [JsonProperty("score")]
        public int Score => Members?.Where(m => m != null).Sum(m => m.Score) / 2 ?? 0;

        public async Task AllianceRankingEntry(MemoryStream stream)
        {
            await stream.WriteInt(Badge); // Badge
            await stream.WriteInt(Members?.Count ?? 0); // Member Count
        }

        public async Task AllianceFullEntry(MemoryStream stream)
        {
            await AllianceHeaderEntry(stream);

            await stream.WriteString(Description); // Description

            await stream.WriteInt(Members?.Count ?? 0); // Member Count

            if (Members != null)
            {
                for (var i = 0; i < Members.Count; i++)
                    if (Members[i] != null)
                        await Members[i].AllianceMemberEntry(stream, i + 1);
            }
        }

        public async Task AllianceHeaderEntry(MemoryStream stream)
        {
            await stream.WriteLong(Id); // Id
            await stream.WriteString(Name); // Name
            await stream.WriteInt(Badge); // Badge
            await stream.WriteInt(Type); // Type
            await stream.WriteInt(Members.Count); // Member Count
            await stream.WriteInt(Score); // Score
            await stream.WriteInt(RequiredScore); // Required Score
        }

        public void AddEntry(AllianceStreamEntry entry)
        {
            while (Stream.Count >= 40)
                Stream.RemoveAt(0);

            Stream.Add(entry);
        }

        public int GetRole(LogicLong id)
        {
            if (Members == null)
                return 1;

            var index = Members.FindIndex(x => x != null && x.AccountId == id);

            return index > -1 ? Members[index].Role : 1;
        }

        public bool IsMember(long id)
        {
            return Members.FindIndex(x => x.AccountId == id) != -1;
        }

        public AllianceMember PromoteNextLeader()
        {
            if (Members.Count == 0)
                return null;

            var nextLeader = Members.OrderByDescending(x => x.Role).FirstOrDefault();
            if (nextLeader == null)
                return null;

            nextLeader.Role = (int) Enums.Role.Leader;
            return nextLeader;
        }

        public async void SaveCallback(object state, ElapsedEventArgs args)
        {
            if (Redis.IsConnected)
                await Redis.CacheAlliance(this);

            await AllianceDb.Save(this);
        }
    }
}