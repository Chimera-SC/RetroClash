using System.Threading.Tasks;
using RetroClash.Logic;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Commands.Server
{
    public class LogicLeaveAlliance : LogicCommand
    {
        public LogicLeaveAlliance(Device device) : base(device)
        {
            Type = 2;
        }

        public long AllianceId { get; set; }
        public string Reason { get; set; }

        public override async Task Encode()
        {
            await Stream.WriteLong(AllianceId);
            await Stream.WriteString(Reason);
            await Stream.WriteInt(-1);
        }
    }
}