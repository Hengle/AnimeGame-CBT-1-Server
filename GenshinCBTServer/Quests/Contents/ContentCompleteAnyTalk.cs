using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Contents
{
    public class ContentCompleteAnyTalk : BaseContent
    {


        public override uint InitialCheck(GameQuest quest, QuestData questData, QuestCondition condition)
        {
            if (condition.param_str == null)
            {
                Server.Print($"Quest {questData.subId} has no param string for QUEST_CONTENT_COMPLETE_ANY_TALK!");
                return 0;
            }
            var conditionTalk = condition.param_str
                .Split(',')
                .Select(uint.Parse)
                .ToArray();

            return conditionTalk.Any(talkId =>
            {
                var checkMainQuest = quest.GetOwner().GetQuestManager().GetMainQuestByTalkId(talkId);
                if (checkMainQuest == null || checkMainQuest.parentQuestId != questData.mainId)
                {
                    return false;
                }
                var talkData = checkMainQuest.talks.GetValueOrDefault(talkId);
                return talkData != null;
            }) ? (uint)1 : 0;
        }

        public override bool IsEvent(QuestData questData, QuestCondition condition, QuestContent type, string paramStr, params uint[] parameters)
        {
            if (condition.type != (uint)type)
            {
                return false;
            }
            if (condition.param_str == null)
            {
                Server.Print($"Quest {questData.subId} has no param string for QUEST_CONTENT_COMPLETE_ANY_TALK!");
                return false;
            }
            var talkId = parameters[0];
            var conditionTalk = condition.param_str
            .Split(',')
            .Select(int.Parse)
            .ToArray();

            return conditionTalk.Any(tids => tids == talkId);
        }
    }
}
