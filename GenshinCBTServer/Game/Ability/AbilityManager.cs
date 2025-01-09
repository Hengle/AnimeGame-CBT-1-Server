using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Game.Ability
{
    public class AbilityManager
    {
        public YPlayer player;
        public AbilityManager(YPlayer player)
        {
            this.player=player;
        }
        public void Invoke(AbilityInvokeEntry invoke)
        {
            switch(invoke.ArgumentType)
            {
                case AbilityInvokeArgument.AbilityMetaAddNewAbility:
                    HandleAddNewAbility(invoke);
                    break;
                default:

                    break;
            }
        }

        private void HandleAddNewAbility(AbilityInvokeEntry invoke)
        {
            var entity = this.player.world.GetEntityById(invoke.Head.TargetId);

            if (entity == null) return;
            AbilityMetaAddAbility addAbility = AbilityMetaAddAbility.Parser.ParseFrom(invoke.AbilityData);
            string abilityName = addAbility.Ability.AbilityName.Str;
            //TODO implement ability data
        }
    }
}
