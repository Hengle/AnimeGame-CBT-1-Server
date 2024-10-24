using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Contents
{
    public class ContentCompleteTalk : BaseContent
    {

        public override uint InitialCheck(GameQuest quest, QuestData questData, QuestCondition condition)
        {
            var talkId = condition.param[0];
            var checkMainQuest = quest.GetOwner().GetQuestManager().GetMainQuestByTalkId(talkId);
            if (checkMainQuest == null || checkMainQuest.parentQuestId != questData.mainId)
            {
                return 0;
            }

            var talkData = checkMainQuest.talks[talkId];
            return talkData != null ? (uint)1 : 0;
        }
    }
}
