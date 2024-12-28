using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Send
{
    public class PacketPlayerLoginRsp : Packet
    {
        public PacketPlayerLoginRsp(uint uid) 
        {
            PlayerLoginRsp proto = new()
            {
                DataVersion = 138541,
                ResVersion = 138541,
                TargetUid = uid,
                Retcode = 0,
            };
            SetData(CmdType.PlayerLoginRsp, proto);
        }
    }
}
