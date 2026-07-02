using System;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroClash.Logic.StreamEntry.Alliance;
using RetroClash.Protocol.Messages.Server;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Client
{
    public class LeaveAllianceMessage : PiranhaMessage
    {
        public LeaveAllianceMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        public long AllianceId { get; set; }

        public override void Decode()
        {
        }

        public override async Task Process()
        {
            AllianceId = Device.Player?.AllianceId ?? 0;

            var alliance = await Resources.AllianceCache.GetAlliance(AllianceId);

            if (alliance == null || !alliance.IsMember(Device.Player.AccountId))
            {
                await Resources.Gateway.Send(new LeaveAllianceOkMessage(Device)
                {
                    AllianceId = AllianceId
                });
                return;
            }

            var member = alliance.Members.Find(x => x.AccountId == Device.Player.AccountId);
            var leftRole = member?.Role ?? 0;
            if (member != null)
                alliance.Members.Remove(member);

            Device.Player.AllianceId = 0;
            await PlayerDb.Save(Device.Player);
            await Device.Player.Update();

            var entry = new AllianceEventStreamEntry
            {
                CreationDateTime = DateTime.Now,
                Id = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                EventType = Enums.AllianceEvent.Left,
                AvatarId = Device.Player.AccountId,
                AvatarName = Device.Player.Name,
                SenderRole = 1
            };

            entry.SetSender(Device.Player);
            alliance.AddEntry(entry);

            foreach (var memberInfo in alliance.Members)
            {
                var player = await Resources.PlayerCache.GetPlayer(memberInfo.AccountId, true);
                if (player != null)
                    await Resources.Gateway.Send(new AllianceStreamEntryMessage(player.Device)
                    {
                        AllianceStreamEntry = entry
                    });
            }

            if (alliance.Members.Count == 0)
            {
                await AllianceDb.Delete(AllianceId);
                Resources.AllianceCache.TryRemove(AllianceId, out _);
            }
            else
            {
                if (leftRole == (int) Enums.Role.Leader)
                    alliance.PromoteNextLeader();

                await AllianceDb.Save(alliance);
                Resources.AllianceCache.AddAlliance(alliance);
            }

            await Resources.Gateway.Send(new LeaveAllianceOkMessage(Device)
            {
                AllianceId = AllianceId
            });
        }
    }
}