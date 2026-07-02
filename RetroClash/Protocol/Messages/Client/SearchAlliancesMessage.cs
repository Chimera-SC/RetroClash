using System.Threading.Tasks;
using RetroClash.Logic;
using RetroClash.Protocol.Messages.Server;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Client
{
    public class SearchAlliancesMessage : PiranhaMessage
    {
        public SearchAlliancesMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        public string SearchString { get; set; }
        public int WarFrequency { get; set; }
        public int AllianceOrigin { get; set; }
        public int MinimumAllianceMembers { get; set; }
        public int MaximumAllianceMembers { get; set; }
        public int AllianceScore { get; set; }
        public int ShowOnlyJoinableAlliances { get; set; }
        public int MinimumAllianceLevel { get; set; }

        public override void Decode()
        {
            SearchString = Reader.ReadString();
            WarFrequency = Reader.ReadInt32();
            AllianceOrigin = Reader.ReadInt32();
            MinimumAllianceMembers = Reader.ReadInt32();
            MaximumAllianceMembers = Reader.ReadInt32();
            AllianceScore = Reader.ReadInt32();
            ShowOnlyJoinableAlliances = Reader.ReadByte();
            Reader.ReadInt32();
            MinimumAllianceLevel = Reader.ReadInt32();
        }

        public override async Task Process()
        {
            await Resources.Gateway.Send(new AllianceListMessage(Device)
            {
                SearchString = SearchString,
                WarFrequency = WarFrequency,
                AllianceOrigin = AllianceOrigin,
                MinimumAllianceMembers = MinimumAllianceMembers,
                MaximumAllianceMembers = MaximumAllianceMembers,
                AllianceScore = AllianceScore,
                ShowOnlyJoinableAlliances = ShowOnlyJoinableAlliances,
                MinimumAllianceLevel = MinimumAllianceLevel
            });
        }
    }
}