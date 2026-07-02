using System;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroClash.Logic.StreamEntry.Alliance;
using RetroClash.Protocol.Messages.Server;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Client
{
    public class ChangeAllianceMemberRoleMessage : PiranhaMessage
    {
        public ChangeAllianceMemberRoleMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        public long AllianceId { get; set; }
        public long TargetId { get; set; }
        public int TargetRole { get; set; }

        public override void Decode()
        {
            TargetId = Reader.ReadInt64();
            TargetRole = Reader.ReadInt32();
        }

        public override async Task Process()
        {
            AllianceId = Device.Player?.AllianceId ?? 0;

            var alliance = await Resources.AllianceCache.GetAlliance(AllianceId);
            var sender = alliance?.Members.Find(x => x.AccountId == Device.Player.AccountId);
            var target = alliance?.Members.Find(x => x.AccountId == TargetId);

            if (alliance == null || sender == null || target == null)
            {
                return;
            }

            if (sender.Role != (int) Enums.Role.Leader && sender.Role != (int) Enums.Role.CoLeader)
            {
                return;
            }

            if (TargetRole < (int) Enums.Role.Member || TargetRole > (int) Enums.Role.CoLeader)
            {
                return;
            }

            var previousRole = target.Role;
            var senderRole = sender.Role;
            var leaderTransfer = false;

            if (TargetRole == (int) Enums.Role.Leader)
            {
                if (senderRole != (int) Enums.Role.Leader)
                    return;

                if (target.Role != (int) Enums.Role.CoLeader)
                    return;

                leaderTransfer = true;
                target.Role = (int) Enums.Role.Leader;
                sender.Role = (int) Enums.Role.CoLeader;
            }
            else
            {
                target.Role = TargetRole;
            }

            await AllianceDb.Save(alliance);
            Resources.AllianceCache.AddAlliance(alliance);

            var entryIdBase = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            if (previousRole != TargetRole)
            {
                var entry = new AllianceEventStreamEntry
                {
                    CreationDateTime = DateTime.Now,
                    Id = entryIdBase,
                    EventType = leaderTransfer ? Enums.AllianceEvent.Promoted : (TargetRole > previousRole ? Enums.AllianceEvent.Promoted : Enums.AllianceEvent.Demoted),
                    AvatarId = Device.Player.AccountId,
                    AvatarName = Device.Player.Name,
                    SenderRole = senderRole
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
            }

            if (leaderTransfer)
            {
                var demotionEntry = new AllianceEventStreamEntry
                {
                    CreationDateTime = DateTime.Now,
                    Id = entryIdBase + 1,
                    EventType = Enums.AllianceEvent.Demoted,
                    AvatarId = Device.Player.AccountId,
                    AvatarName = Device.Player.Name,
                    SenderRole = senderRole
                };

                demotionEntry.SetSender(Device.Player);
                alliance.AddEntry(demotionEntry);

                foreach (var memberInfo in alliance.Members)
                {
                    var player = await Resources.PlayerCache.GetPlayer(memberInfo.AccountId, true);
                    if (player != null)
                        await Resources.Gateway.Send(new AllianceStreamEntryMessage(player.Device)
                        {
                            AllianceStreamEntry = demotionEntry
                        });
                }
            }

            var targetPlayer = await Resources.PlayerCache.GetPlayer(TargetId, true);
            if (targetPlayer != null)
                await Resources.Gateway.Send(new OwnHomeDataMessage(targetPlayer.Device));

            await Resources.Gateway.Send(new ChangeAllianceMemberRoleOkMessage(Device)
            {
                TargetId = TargetId,
                TargetRole = TargetRole
            });
        }
    }
}