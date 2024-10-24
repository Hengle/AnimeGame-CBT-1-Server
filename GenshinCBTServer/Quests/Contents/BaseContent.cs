using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Contents
{
    public abstract class BaseContent
    {
        public virtual bool IsEvent(QuestData questData, QuestCondition condition, QuestContent type, string paramStr, params uint[] parameters)
        {
            return condition.type == (uint)type && condition.param[0] == parameters[0];
        }

        public virtual uint InitialCheck(GameQuest quest, QuestData questData, QuestCondition condition)
        {
            return 0;
        }

        public virtual uint UpdateProgress(GameQuest quest, uint currentProgress, QuestCondition condition, string paramStr, params uint[] parameters)
        {
            return currentProgress + 1;
        }

        public virtual bool CheckProgress(GameQuest quest, QuestCondition condition, uint currentProgress)
        {
            uint target = condition.count > 0 ? condition.count : 1;
            return currentProgress >= target;
        }
    }

    public class ContentFinishPlot : BaseContent
    {

    }
    public class ContentTriggerFire : BaseContent
    {

    }
}
