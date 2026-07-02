using System.Threading.Tasks;
using RetroClash.Logic;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Commands.Server
{
    public class LogicLeaveAllianceCommand : LogicCommand
    {
        public LogicLeaveAllianceCommand(Device device) : base(device)
        {
            Type = 2;
        }

        public long AllianceId { get; set; }
        public int Reason { get; set; } = 1;

        public override async Task Encode()
        {
            await Stream.WriteLong(AllianceId);
            await Stream.WriteInt(Reason);
            await Stream.WriteInt(-1);
        }
    }
}
