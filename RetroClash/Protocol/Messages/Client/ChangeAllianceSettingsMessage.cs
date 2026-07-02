using System;
using System.Threading.Tasks;
using RetroClash.Core.Database;
using RetroClash.Logic;
using RetroClash.Logic.StreamEntry.Alliance;
using RetroClash.Protocol.Messages.Server;
using RetroGames.Helpers;

namespace RetroClash.Protocol.Messages.Client
{
    public class ChangeAllianceSettingsMessage : PiranhaMessage
    {
        public ChangeAllianceSettingsMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        public int AllianceBadgeData;
        public string AllianceDescription;
        public int AllianceOrigin;
        public int AllianceType;
        public int RequiredScore;
        //public int WarFrequency;
        //public byte WarLogPublic;
        //public int Unknown;

        public override void Decode()
        {
            AllianceDescription = Reader.ReadString();
            //Unknown = Reader.ReadInt32();
            AllianceBadgeData = Reader.ReadInt32();
            AllianceType = Reader.ReadInt32();
            RequiredScore = Reader.ReadInt32();
            //WarFrequency = Reader.ReadInt32();
            //AllianceOrigin = Reader.ReadInt32();
            //WarLogPublic = Reader.ReadByte();
        }

        public override async Task Process()
        {
            var AllianceId = Device.Player?.AllianceId ?? 0;

            var alliance = await Resources.AllianceCache.GetAlliance(AllianceId);
            var member = alliance?.Members.Find(x => x.AccountId == Device.Player.AccountId);

            if (alliance == null || member == null)
            {
                await Resources.Gateway.Send(new AllianceChangeFailedMessage(Device));
                return;
            }

            if (member.Role != (int) Enums.Role.Leader && member.Role != (int) Enums.Role.CoLeader)
            {
                await Resources.Gateway.Send(new AllianceChangeFailedMessage(Device));
                return;
            }

            alliance.Description = AllianceDescription;
            alliance.Badge = AllianceBadgeData;
            alliance.Type = AllianceType;
            alliance.RequiredScore = RequiredScore;

            await AllianceDb.Save(alliance);
            Resources.AllianceCache.AddAlliance(alliance);

            var entry = new AllianceEventStreamEntry
            {
                CreationDateTime = DateTime.Now,
                Id = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                EventType = Enums.AllianceEvent.Edited,
                AvatarId = Device.Player.AccountId,
                AvatarName = Device.Player.Name,
                SenderRole = member.Role
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

            await Resources.Gateway.Send(new OwnHomeDataMessage(Device));
            await Resources.Gateway.Send(new AllianceDataMessage(Device)
            {
                AllianceId = AllianceId
            });
        }
    }
}