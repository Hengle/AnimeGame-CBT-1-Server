using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Contents
{
    public class ContentUnlockTransPoint : BaseContent
    {

        public override uint InitialCheck(GameQuest quest, QuestData questData, QuestCondition condition)
        {
            var sceneId = condition.param[0];
            var scenePointId = condition.param[1];

            return quest.GetOwner().unlockedPoints.Contains(scenePointId) ? (uint)1 : 0;
        }


        public override bool IsEvent(QuestData questData, QuestCondition condition, QuestContent type, string paramStr, params uint[] parameters)
        {
            if (condition.type != (uint)type)
            {
                return false;
            }
            var sceneId = condition.param[0];
            var scenePointId = condition.param[1];
            return parameters[0] == sceneId && parameters[1] == scenePointId;
        }
    }
}
