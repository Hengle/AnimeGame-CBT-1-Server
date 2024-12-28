using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Send
{
    public class PacketGetPlayerTokenRsp : Packet
    {
        public PacketGetPlayerTokenRsp(GetPlayerTokenReq req,YPlayer player) 
        {
            GetPlayerTokenRsp resp = new GetPlayerTokenRsp()
            {
                AccountUid = "1",
                AccountType = 0,
                Token = req.AccountToken,
                Retcode = 0,
                Uid = 1,
                Msg = "OK",
            };
            SetData(CmdType.GetPlayerTokenRsp, resp);
        }
    }
}
