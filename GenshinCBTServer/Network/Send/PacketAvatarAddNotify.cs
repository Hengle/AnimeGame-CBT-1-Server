using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Send
{
    public class PacketAvatarAddNotify : Packet
    {
        public PacketAvatarAddNotify(Avatar avatar) 
        {
            AvatarAddNotify proto = new()
            {
                Avatar=avatar.toProto(),
                IsInTeam=false,
            };
            SetData(CmdType.AvatarAddNotify, proto);
        }
    }
}
