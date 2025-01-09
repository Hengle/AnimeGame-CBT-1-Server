using GenshinCBTServer.Player;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Send
{
    public class PacketStoreItemChangeNotify : Packet
    {
        public PacketStoreItemChangeNotify(GameItem item) 
        {
            StoreItemChangeNotify proto = new()
            {
               StoreType=StoreType.StorePack,
               ItemList = {item.toProtoItem()}
            };
            SetData(CmdType.StoreItemDelNotify, proto);
        }
    }
}
