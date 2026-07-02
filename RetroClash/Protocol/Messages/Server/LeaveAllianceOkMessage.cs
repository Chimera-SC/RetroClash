using System.IO;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Server
{
    public class LeaveAllianceOkMessage : PiranhaMessage
    {
        public LeaveAllianceOkMessage(Device device) : base(device)
        {
            Id = 24111;
            ServerCommandType = 2;
            Reason = 1;
            Unknown = -1;
        }

        public int ServerCommandType { get; set; }
        public long AllianceId { get; set; }
        public int Reason { get; set; }
        public int Unknown { get; set; }

        public override async Task Encode()
        {
            await Stream.WriteInt(ServerCommandType);
            await Stream.WriteLong(AllianceId);
            await Stream.WriteInt(Reason); // Reason (1 = Leave, 2 = Kick)
            await Stream.WriteInt(Unknown);
        }
    }
}