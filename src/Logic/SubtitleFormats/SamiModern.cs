﻿using System.Collections.Generic;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    class SamiModern : Sami
    {

        public override string Name
        {
            get { return "SAMI modern"; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }
    }
}
