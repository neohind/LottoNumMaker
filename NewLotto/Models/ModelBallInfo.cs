using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLotto.Models
{
    public class ModelBallInfo
    {
        private int[] m_aryBalls = new int[] { 0, 0, 0, 0, 0, 0, 0 };

        public int Index
        {
            get;
            set;
        }

        public int B1
        {
            get
            {
                return m_aryBalls[0];
            }
        }

        public int B2
        {
            get
            {
                return m_aryBalls[1];
            }
        }

        public int B3
        {
            get
            {
                return m_aryBalls[2];
            }
        }

        public int B4
        {
            get
            {
                return m_aryBalls[3];
            }
        }

        public int B5
        {
            get
            {
                return m_aryBalls[4];
            }
        }

        public int B6
        {
            get
            {
                return m_aryBalls[5];
            }
        }

        public int B7
        {
            get
            {
                return m_aryBalls[6];
            }
        }

        public List<int> GetAllBalls
        {
            get
            {
                return new List<int>(m_aryBalls);
            }
        }

        public string GetUniqueKey(int nLevel)
        {
            StringBuilder sbResult = new StringBuilder();
            for (int i = 0; i < nLevel; i++)
                sbResult.AppendFormat("{0:00}", m_aryBalls[i]);
            for (int i = nLevel - 1; i < 5; i++)
                sbResult.AppendFormat("00");

            return sbResult.ToString();
        }

        public ModelBallInfo(DataRow rowBallInfo)
        {
            Index = Convert.ToInt32(rowBallInfo["idx"]);
            for(int i=1; i<8; i++)
            {
                m_aryBalls[i - 1] = Convert.ToInt32(rowBallInfo[$"b{i}"]);
            }
        }

        public int GetBall(int nLocation)
        {
            if (nLocation < 1 || nLocation > 7)
                return 0;
            return m_aryBalls[nLocation];
        }

        public bool ContainBall(int nBallNumber)
        {

            for (int i = 0; i < m_aryBalls.Length; i++)
            {
                int nCurBallNumber = m_aryBalls[i];
                if (nCurBallNumber == nBallNumber)
                    return true;
            }

            return false;
        }

        
    }
}
