using System.IO;
using System.Threading.Tasks;
using RetroClash.Logic;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Server
{
    public class AvatarLocalRankingListMessage : PiranhaMessage
    {
        public AvatarLocalRankingListMessage(Device device) : base(device)
        {
            Id = 24404;
        }

        public override async Task Encode()
        {
            var count = 0;
            var players = Resources.LeaderboardCache?.LocalPlayers;
            var language = Device?.Player?.Language;

            using (var buffer = new MemoryStream())
            {
                if (!string.IsNullOrEmpty(language) && players != null && players.ContainsKey(language))
                {
                    foreach (var player in players[language])
                    {
                        if (player == null) continue;

                        try
                        {
                            await buffer.WriteLong(player.AccountId);
                            await buffer.WriteString(player.Name);

                            await buffer.WriteInt(count + 1);
                            await buffer.WriteInt(player.Score);
                            await buffer.WriteInt(200);

                            await player.AvatarRankingEntry(buffer);

                            count++;
                        }
                        catch
                        {
                            // Skip malformed local player entries without crashing the ranking message.
                        }
                    }
                }

                await Stream.WriteInt(count);
                await Stream.WriteBuffer(buffer.ToArray());
            }
        }
    }
}