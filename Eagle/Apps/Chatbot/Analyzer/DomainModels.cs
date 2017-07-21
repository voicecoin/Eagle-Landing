﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Apps.Chatbot.DomainModels
{
    public class DmDeepParsed
    {
        public List<DmIntentExpressionItem> Tags { get; set; }
    }

    public class NlpirResult
    {
        public List<NlpirSegment> WordSplit { get; set; }
    }

    public class NlpirSegment
    {
        public string Word { get; set; }
        public string Entity { get; set; }
        public string Characteristic
        {
            get
            {
                var segs = Entity.Split(':');
                if (segs.Count() > 0)
                {
                    return segs[0];
                }

                return String.Empty;
            }
        }

        public string Category
        {
            get
            {
                /*MatchCollection mc = Regex.Matches(seg.Category, "\".+\"");
                foreach (Match m in mc)
                {
                    seg.Entity = seg.Entity.Replace(m.Value, String.Empty);
                }*/

                var segs = Entity.Split(':');
                if (segs.Count() == 1)
                {
                    return segs[0];
                }
                else if (segs.Count() == 2)
                {
                    return segs[1];
                }
                else if (segs.Count() == 3)
                {
                    return segs[2];
                }

                return String.Empty;
            }

        }
    }
}
