using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Send
{
    public class PacketStoreItemDelNotify : Packet
    {
        public PacketStoreItemDelNotify(GameItem item) 
        {
            StoreItemDelNotify proto = new()
            {
                GuidList = {item.guid}
            };
            SetData(CmdType.StoreItemDelNotify, proto);
        }
    }
}
