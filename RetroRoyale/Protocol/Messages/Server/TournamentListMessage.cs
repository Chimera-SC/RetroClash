using System.Threading.Tasks;
using RetroRoyale.Logic;
using RetroGames.Helpers;

namespace RetroRoyale.Protocol.Messages.Server
{
    public class TournamentListMessage : PiranhaMessage
    {
        public TournamentListMessage(Device device) : base(device)
        {
            Id = 26101;
        }

        public override async Task Encode()
        {
            await Stream.WriteVInt(0);
        }
    }
}