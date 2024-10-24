using GenshinCBTServer.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinCBTServer.Quests.Contents
{
    public class ContentNone : BaseContent
    {


        public override uint UpdateProgress(GameQuest quest, uint currentProgress, QuestCondition condition, string paramStr, params uint[] parameters)
        {
            return 1;
        }


        public override bool CheckProgress(GameQuest quest, QuestCondition condition, uint currentProgress)
        {
            return true;
        }
    }
}
