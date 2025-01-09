using GenshinCBTServer.Network.Send;
using GenshinCBTServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Network.Receive
{
    public static class HandleAbilityInvocationsNotify
    {
        [Server.Handler(CmdType.AbilityInvocationsNotify)]
        public static void Handle(YPlayer session, CmdType cmdId,Packet packet)
        {
            AbilityInvocationsNotify proto = packet.DecodeBody<AbilityInvocationsNotify>();
            foreach (AbilityInvokeEntry item in proto.Invokes)
            {
                session.GetAbilityManager().Invoke(item);
            }
        }
    }
}
