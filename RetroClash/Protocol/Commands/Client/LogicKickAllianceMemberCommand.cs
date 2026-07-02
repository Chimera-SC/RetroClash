using System;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroClash.Logic.StreamEntry.Alliance;
using RetroClash.Logic.StreamEntry.Avatar;
using RetroClash.Protocol.Messages.Server;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Commands.Client
{
    public class LogicKickAllianceMemberCommand : LogicCommand
    {
        public LogicKickAllianceMemberCommand(Device device, Reader reader) : base(device, reader)
        {
        }

        public int AvatarId { get; set; }
        public string Message { get; set; }

        public override void Decode()
        {
            AvatarId = Reader.ReadInt32();
            Reader.ReadByte();
            Message = Reader.ReadString();
            Reader.ReadInt32();
        }

        public override async Task Process()
        {
            var AllianceId = Device.Player?.AllianceId ?? 0;

            var alliance = await Resources.AllianceCache.GetAlliance(AllianceId);
            if (alliance == null)
            {
                return;
            }

            var sender = alliance.Members.Find(x => x.AccountId == Device.Player.AccountId);
            if (sender == null || (sender.Role != (int) Enums.Role.Leader && sender.Role != (int) Enums.Role.CoLeader && sender.Role != (int) Enums.Role.Elder))
            {
                return;
            }

            if (AvatarId == Device.Player.AccountId)
            {
                return;
            }

            var member = alliance.Members.Find(x => x.AccountId.Long == AvatarId);
            if (member == null)
            {
                return;
            }

            if (sender.Role == (int) Enums.Role.Elder && member.Role != (int) Enums.Role.Member)
            {
                return;
            }

            if (sender.Role == (int) Enums.Role.CoLeader && member.Role != (int) Enums.Role.Leader && member.Role != (int) Enums.Role.Elder)
            {
                return;
            }

            var leftRole = member.Role;
            alliance.Members.Remove(member);

            var targetPlayer = await Resources.PlayerCache.GetPlayer(AvatarId);
            if (targetPlayer != null)
            {
                targetPlayer.AllianceId = 0;
                await PlayerDb.Save(targetPlayer);

                if (targetPlayer.Device != null)
                {
                    await targetPlayer.Update();
                    await Resources.Gateway.Send(new LeaveAllianceOkMessage(targetPlayer.Device)
                    {
                        AllianceId = AllianceId,
                        Reason = 2
                    });
                }

                var kickEntry = new AllianceKickOutStreamEntry
                {
                    CreationDateTime = DateTime.Now,
                    Id = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                    Message = Message,
                    AllianceId = alliance.Id,
                    AllianceName = alliance.Name,
                    AllianceBadge = alliance.Badge,
                    SenderHomeId = Device.Player.AccountId
                };

                kickEntry.SetSender(Device.Player);
                targetPlayer.AddEntry(kickEntry);

                if (targetPlayer.Device != null)
                    await Resources.Gateway.Send(new AvatarStreamEntryMessage(targetPlayer.Device)
                    {
                        Entry = kickEntry
                    });
            }

            var entry = new AllianceEventStreamEntry
            {
                CreationDateTime = DateTime.Now,
                Id = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                EventType = Enums.AllianceEvent.Kicked,
                AvatarId = member.AccountId.Long,
                AvatarName = targetPlayer?.Name ?? string.Empty,
                SenderRole = sender.Role
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
                await AllianceDb.Save(alliance);
                Resources.AllianceCache.AddAlliance(alliance);
            }
        }
    }
}