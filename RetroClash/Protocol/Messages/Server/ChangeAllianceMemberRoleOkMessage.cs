using System.IO;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Server
{
    public class ChangeAllianceMemberRoleOkMessage : PiranhaMessage
    {
        public ChangeAllianceMemberRoleOkMessage(Device device) : base(device)
        {
            Id = 24306;
        }

        public long TargetId { get; set; }
        public int TargetRole { get; set; }

        public override async Task Encode()
        {
            await Stream.WriteLong(TargetId);
            await Stream.WriteInt(TargetRole);
        }
    }
}