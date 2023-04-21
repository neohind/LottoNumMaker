using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using NewLotto.Dac;
using NewLotto.Models;
using System.Threading;
using System.Diagnostics;

namespace NewLotto
{
    public class AppMain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Regex m_regex = null;
        private DacLotto m_dac = null;
        private List<ModelBallInfo> m_aryAllBalls = new List<ModelBallInfo>();
        private int m_nLastIndex = 0;
        private Dictionary<int, List<ModelBallInfo>> m_dicContainBalls = new Dictionary<int, List<ModelBallInfo>>();

        public AppMain(string sConnectionString)
        {
            m_regex = new Regex("\\<span class=\"ball_645 lrg ball[0-9]\"\\>(?<num>[0-9]+)");
            m_dac = new DacLotto(sConnectionString);

            DateTime dtBaseDate = new DateTime(2002, 12, 7);
            m_nLastIndex = Convert.ToInt32((DateTime.Now - dtBaseDate).TotalDays / 7);
        }

        public void LoadBalls()
        {
            m_aryAllBalls = m_dac.GetAllBallInfo();
            m_aryAllBalls.Sort((m, n) => m.Index - n.Index);
            List<int> aryAllIndex = m_aryAllBalls.Select(m => m.Index).ToList<int>();

            for (int nIndex = 1; nIndex <= m_nLastIndex; nIndex++)
            {
                if (aryAllIndex.Contains(nIndex) == false)
                {
                    string sUrl = $"https://dhlottery.co.kr/gameResult.do?method=byWin&drwNo={nIndex}&dwrNoList={nIndex}";
                    string sResult = LoadHtml(sUrl);

                    List<int> aryBalls = new List<int>();
                    MatchCollection matches = m_regex.Matches(sResult);
                    foreach (Match match in matches)
                    {
                        int nCurBall = Convert.ToInt32(match.Groups["num"].Value);
                        aryBalls.Add(nCurBall);
                    }
                    m_dac.InsertNewBalls(nIndex, aryBalls);
                }
                else
                {
                    log.Info($"Index {nIndex} is already exist!");
                }
            }
        }

        public List<byte> GenerateNumbers()
        {
            int nSeed = (int)(DateTime.Now.Ticks);
            Random rnd = new Random(nSeed);

            List<byte> aryResult = new List<byte>();
            ModelCalcResults calcData = m_dac.GetCalcResult();
            bool bIsFound = false;

            while (bIsFound == false)
            {
                aryResult.Clear();

                while (aryResult.Count < 6)
                {
                    byte b = Convert.ToByte(rnd.Next(1, 45));
                    if (aryResult.Contains(b))
                        continue;
                    aryResult.Add(b);                    
                }
                Console.Write(".");

                int nSum = aryResult[0] + aryResult[1] + aryResult[2] + aryResult[3] + aryResult[4] + aryResult[5];
                double nAvg = nSum / 6.0;


                bIsFound = (nSum > (calcData.SummaryAvg - calcData.SummaryStdDev) 
                            && nSum < (calcData.SummaryAvg + calcData.SummaryStdDev))
                            || (nAvg > (calcData.Average - calcData.AverageStdDev) 
                            && nAvg < (calcData.Average + calcData.AverageStdDev));

                bIsFound = bIsFound && m_dac.CheckAlreadyExistBallSet(aryResult);

            }

            aryResult.Sort();

            Console.WriteLine();
            return aryResult;
        }

        public void AnalysisCalculator()
        {
            List<ModelBallInfo> aryBalls = m_dac.GetAllBallInfo();

            foreach(ModelBallInfo info in aryBalls)
            {
                int nSum = info.B1 + info.B2 + info.B3 + info.B4 + info.B5 + info.B6;
                double dAvg = nSum / 6.0;
                int nAvg = Convert.ToInt32(Math.Round(dAvg));


                int nSumOfDev =  info.B1 * info.B1 + info.B2 * info.B2 + info.B3 * info.B3 + info.B4 * info.B4 + info.B5 * info.B5 + info.B6 * info.B6;
                double dDev = nSumOfDev / 6.0;
                int nDev = Convert.ToInt32(Math.Round(dDev));

                dDev = Math.Sqrt(nSumOfDev - (dAvg * dAvg));

                log.Info($"{info.Index} - {nSum} , {nAvg}({dAvg}) , {nDev}({dDev})");

                m_dac.InsertAnalysisCalcResult(info.Index, nSum, dAvg, nAvg, dDev, nDev);
            }
        }

        public void AnalysisCountLevel()
        {
            foreach (ModelBallInfo info in m_aryAllBalls)
            {
                for (int i = 1; i < 46; i++)
                {
                    if (info.ContainBall(i))
                    {
                        if (m_dicContainBalls.ContainsKey(i) == false)
                            m_dicContainBalls.Add(i, new List<ModelBallInfo>());
                        m_dicContainBalls[i].Add(info);
                    }
                }
            }

            Thread thread = null;
            List <AutoResetEvent> aryAllTasks = new List<AutoResetEvent>();

            g_aryAllBallPatterns = m_dac.GetAllBallPatterns();


            for (byte i1 = 1; i1 < 16; i1++)
            {
                thread = new Thread(new ParameterizedThreadStart(WorkerMultiInsert));
                AutoResetEvent evt = new AutoResetEvent(false);
                aryAllTasks.Add(evt);

                List<object> aryParams = new List<object>();
                aryParams.Add(evt);
                aryParams.Add(i1);
                thread.Start(aryParams.ToArray());
            }
            AutoResetEvent.WaitAll(aryAllTasks.ToArray());
        }

        public List<byte[]> GetAllPatterns()
        {
            return m_dac.GetAllBallPatterns();
        }

        object m_locker = new object();
        object m_locker2 = new object();

        private bool AddBallSet(ref List<byte[]> aryAllBallPatterns, ref List<byte []> aryTempBallPatterns, byte [] aryBallSets)
        {
            bool bResult = false;
            lock (m_locker2)
            {
                byte[] result = aryAllBallPatterns.Find(m =>
                                m != null
                                && m[0] == aryBallSets[0]
                                && m[1] == aryBallSets[1]
                                && m[2] == aryBallSets[2]
                                && m[3] == aryBallSets[3]
                                && m[4] == aryBallSets[4]
                                && m[5] == aryBallSets[5]
                                && m[6] == aryBallSets[6]);

                if (result == null)
                {
                    aryTempBallPatterns.Add(aryBallSets);
                    aryAllBallPatterns.Add(aryBallSets);
                    bResult = true;
                }


                return bResult;
            }
        }


        static List<byte[]> g_aryAllBallPatterns = new List<byte[]>();

        private void WorkerMultiInsert(object oParam)
        {
            object[] aryParams = (object[])oParam;

            AutoResetEvent evt = (AutoResetEvent)aryParams[0];
            byte nBall1 = (byte)aryParams[1];

            DateTime dtBase = DateTime.Now;
            long nAllCount = 0;            
            
            List<byte[]> aryTempBallPatterns = new List<byte[]>();

            StringBuilder sbKey = new StringBuilder();
            List<int> aryContainBall = new List<int>();
            aryContainBall.Add(nBall1);
            byte[] aryValuesL1 = new byte[] { nBall1, 255, 255, 255, 255, 255, 1 };
            aryTempBallPatterns.Add(aryValuesL1);

            for (byte nBall2 = nBall1; nBall2 < 46; nBall2++)
            {
                if (aryContainBall.Contains(nBall2))
                    continue;
                byte[] aryValuesL2 = MakeBall(2, aryValuesL1, nBall2);
                AddBallSet(ref g_aryAllBallPatterns, ref aryTempBallPatterns, aryValuesL2);
                aryContainBall.Add(nBall2);

                for (byte nBall3 = nBall2; nBall3 < 46; nBall3++)
                {
                    if (aryContainBall.Contains(nBall3))
                        continue;
                    byte[] aryValuesL3 = MakeBall(3, aryValuesL2, nBall3);
                    AddBallSet(ref g_aryAllBallPatterns, ref aryTempBallPatterns, aryValuesL3);
                    aryContainBall.Add(nBall3);

                    for (byte nBall4 = nBall3; nBall4 < 46; nBall4++)
                    {
                        if (aryContainBall.Contains(nBall4))
                            continue;
                        byte[] aryValuesL4 = MakeBall(4, aryValuesL3, nBall4);
                        AddBallSet(ref g_aryAllBallPatterns, ref aryTempBallPatterns, aryValuesL4);
                        aryContainBall.Add(nBall4);

                        for (byte nBall5 = nBall4; nBall5 < 46; nBall5++)
                        {
                            if (aryContainBall.Contains(nBall5))
                                continue;
                            byte[] aryValuesL5 = MakeBall(5, aryValuesL4, nBall5);
                            AddBallSet(ref g_aryAllBallPatterns, ref aryTempBallPatterns, aryValuesL5);
                            aryContainBall.Add(nBall5);

                            for (byte nBall6 = nBall5; nBall6 < 46; nBall6++)
                            {
                                if (aryContainBall.Contains(nBall6))
                                    continue;
                                byte[] aryValuesL6 = MakeBall(6, aryValuesL5, nBall6);
                                AddBallSet(ref g_aryAllBallPatterns, ref aryTempBallPatterns, aryValuesL6);
                            }
                            aryContainBall.Remove(nBall5);
                            nAllCount = nAllCount + aryTempBallPatterns.Count;
                            if (aryTempBallPatterns.Count > 50)
                            {
                                int nResult = m_dac.InsertAnalysisDataTemplate(nBall1, aryTempBallPatterns.ToArray());
                                if (nResult > 0)
                                    log.Info($"< {nBall1,2:00} > INSERT Result : affected rows count is {nResult}");
                                aryTempBallPatterns.Clear();
                            }
                        }                        
                        aryContainBall.Remove(nBall4);                        
                    }
                    aryContainBall.Remove(nBall3);
                }
                aryContainBall.Remove(nBall2);
            }
            aryContainBall.Clear();
            aryContainBall = null;
            log.Debug($"[{nBall1}] CurCount: {nAllCount} / {g_aryAllBallPatterns.Count}");
            evt.Set();
        }

        private byte[] MakeBall(byte nLevel, byte [] aryPrevBalls, byte nBallNo)
        {
            List<byte> aryNewBalls = new List<byte>();

            for (int i = 0; i < nLevel - 1; i++)
                aryNewBalls.Add(aryPrevBalls[i]);
            aryNewBalls.Add(nBallNo);
            aryNewBalls.Sort();

            while (aryNewBalls.Count < 6)
                aryNewBalls.Add(255);

            aryNewBalls.Add(nLevel);

            return aryNewBalls.ToArray();
        }
        
  
        private string LoadHtml(string sUrl)
        {
            string result = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = client.GetAsync(sUrl).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        result = content.ReadAsStringAsync().Result;
                    }
                }
            }
            return result;
        }
    }
}
