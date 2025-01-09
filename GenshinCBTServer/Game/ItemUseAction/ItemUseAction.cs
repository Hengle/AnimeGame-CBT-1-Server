using GenshinCBTServer.Data;
using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Game.ItemUseAction
{
    public abstract class ItemUseAction
    {
        public ItemUseAction()
        {

        }
        public abstract bool UseItem(UseItemParams param);
        public static ItemUseAction FromItemUseOp(UseItemParams param)
        {
            switch(param.itemUseConfig.useOp)
            {
                case ItemUseOp.ITEM_USE_GAIN_AVATAR:
                    return new ItemUseGainAvatar(param.itemUseConfig.useParam.ToArray());
                default:
                    return null;
            }
        }
    }


    public class UseItemParams
    {
        public YPlayer player;
        public ulong targetGuid;
        public ItemUseTarget useTarget;
        public ItemUseConfig itemUseConfig;
    }
}
