using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLotto.Models
{
    public class ModelCalcResults
    {
        public double SummaryAvg
        {
            get;
            set;
        }

        public double SummaryStdDev
        {
            get;
            set;
        }

        public double Average
        {
            get;
            set;
        }

        public double AverageStdDev
        {
            get;
            set;
        }

        public ModelCalcResults(DataRow rowBallInfo)
        {
            if(rowBallInfo != null)
            {
                SummaryAvg = Convert.ToDouble(rowBallInfo["sumavg"]);
                SummaryStdDev = Convert.ToDouble(rowBallInfo["sumstd"]);
                Average = Convert.ToDouble(rowBallInfo["avg"]);
                AverageStdDev = Convert.ToDouble(rowBallInfo["stdev"]);
            }
        }
    }
}
