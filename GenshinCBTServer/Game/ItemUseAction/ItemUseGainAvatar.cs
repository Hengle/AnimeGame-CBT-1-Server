using GenshinCBTServer.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Game.ItemUseAction
{
    public class ItemUseGainAvatar : ItemUseAction
    {
        private uint id = 0;
        private int level = 1;
        private int constellation = 0;
        public ItemUseGainAvatar(string[] useParam)
        {
            try
            {
                this.id = uint.Parse(useParam[0]);
            }
            catch (Exception e)
            {
            }
            try
            {
                this.level = int.Parse(useParam[1]);
            }
            catch (Exception e) {
            }
            try
            {
                this.constellation = int.Parse(useParam[2]);
            }
            catch (Exception e) {

            }
        }
        public override bool UseItem(UseItemParams param)
        {
            Avatar avatar = param.player.avatars.Find(av => av.id == id);
            if(avatar != null)
            {

            }
            else
            {
                avatar = new Avatar(param.player, id);
                avatar.level = level;
                param.player.AddAvatar(avatar);

                return true;
            }
            return false;
        }
    }
}
