using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RetroGames.Helpers;
using RetroGames.Logic;

namespace RetroClash.Logic.Slots
{
    public class AllianceMember
    {
        public AllianceMember(LogicLong id, Enums.Role role, int score)
        {
            AccountId = id;
            Role = (int) role;
            Score = score;
        }

        [JsonIgnore]
        public LogicLong AccountId { get; set; }

        [JsonProperty("account_id")]
        public long AccountIdValue
        {
            get => AccountId.Long;
            set => AccountId = new LogicLong(value);
        }

        [JsonProperty("role")]
        public int Role { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("donations")]
        public int Donations { get; set; }

        [JsonProperty("donations_received")]
        public int DonationsReceived { get; set; }

        [JsonIgnore]
        public bool IsOnline => Resources.PlayerCache.ContainsKey(AccountId.Long);

        public async Task AllianceMemberEntry(MemoryStream stream, int order)
        {
            var player = await Resources.PlayerCache.GetPlayer(AccountId.Long);
            var name = player?.Name ?? string.Empty;
            var expLevel = player?.ExpLevel ?? 0;

            await AccountId.Encode(stream); // Avatar Id
            await stream.WriteString(null); // FacebookId
            await stream.WriteString(name); // Name
            await stream.WriteInt(Role); // Role
            await stream.WriteInt(expLevel); // Exp Level
            await stream.WriteInt(LogicUtils.GetLeagueByScore(Score)); // League Type
            await stream.WriteInt(Score); // Score
            await stream.WriteInt(Donations); // Donations
            await stream.WriteInt(DonationsReceived); // Donations Received
            await stream.WriteInt(order); // Order
            await stream.WriteInt(order); // Previous Order

            stream.WriteByte(0); // IsNewMember

            stream.WriteByte(1); // HasHomeId
            await AccountId.Encode(stream); // Home Id
        }
    }
}