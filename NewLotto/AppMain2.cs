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
    public class AppMain2
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Regex m_regex = null;
        private DacLotto m_dac = null;
        private List<ModelBallInfo> m_aryAllBalls = new List<ModelBallInfo>();
        private int m_nLastIndex = 0;
        private Dictionary<int, List<ModelBallInfo>> m_dicContainBalls = new Dictionary<int, List<ModelBallInfo>>();

        public AppMain2()
        {
            m_regex = new Regex("\\<span class=\"ball_645 lrg ball[0-9]\"\\>(?<num>[0-9]+)");
            m_dac = new DacLotto();

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

            DateTime dtBase = DateTime.Now;
            long nAllCount = 0;
            byte[] aryValues;
            List<byte[]> aryTest = new List<byte[]>();
            
            for (byte i1 = 1; i1 < 46; i1++)
            {
                StringBuilder sbKey = new StringBuilder();
                List<int> aryContainBall = new List<int>();
                aryContainBall.Add(i1);                
                aryValues = new byte[] { i1, 255,255, 255, 255, 255 , 1};
                aryTest.Add(aryValues);

                for (byte i2 = 1; i2 < 46; i2++)
                {
                    if (aryContainBall.Contains(i2))
                        continue;                    
                    aryValues = new byte[] { i1, i2, 255, 255, 255, 255, 2};
                    aryTest.Add(aryValues);
                    aryContainBall.Add(i2);
                    for (byte i3 = 1; i3 <= 46; i3++)
                    {
                        if (aryContainBall.Contains(i3))
                            continue;                        
                        aryValues = new byte[] { i1, i2, i3, 255, 255, 255 , 3};
                        aryTest.Add(aryValues);
                        aryContainBall.Add(i3);
                        for (byte i4 = 1; i4 < 46; i4++)
                        {
                            if (aryContainBall.Contains(i4))
                                continue;                            
                            aryValues = new byte[] { i1, i2, i3, i4, 255, 255, 4};
                            aryTest.Add(aryValues);
                            aryContainBall.Add(i4);
                            for (byte i5 = 1; i5 < 46; i5++)
                            {
                                if (aryContainBall.Contains(i5))
                                    continue;                                
                                aryValues = new byte[] { i1, i2, i3, i4, i5, 255, 5};
                                aryTest.Add(aryValues);
                                aryContainBall.Add(i5);
                                for (byte i6 = 1; i6 < 26; i6++)
                                {
                                    if (aryContainBall.Contains(i6))
                                        continue;
                                    aryValues = new byte[] { i1, i2, i3, i4, i5, i6 , 6};
                                    aryTest.Add(aryValues);
                                }
                                aryContainBall.Remove(i5);
                                
                                if (aryTest.Count > 1500)
                                {
                                    nAllCount = nAllCount + aryTest.Count;
                                    TimeSpan timespan = DateTime.Now - dtBase;
                                    log.Info($"[{timespan.Hours}:{timespan.Minutes}:{timespan.Seconds}.{timespan.Milliseconds}] CurCount : {aryTest.Count} / {nAllCount} / {m_nAffectedRows}");

                                    m_queue.Enqueue(aryTest.ToArray());                                                                        
                                    aryTest.Clear();
                                    Trigger_WorkerInsertData();
                                }

                                if(m_queue.Count > 10000)
                                {
                                    Thread.Sleep(60 * 1000 * 2);
                                }
                            }
                            aryContainBall.Remove(i4);
                        }
                        aryContainBall.Remove(i3);                       
                    }                    
                    aryContainBall.Remove(i2);
                }
                aryContainBall.Clear();
                aryContainBall = null;
            }
        }


        Thread m_thread = null;
        Queue<byte[][]> m_queue = new Queue<byte[][]>();
        long m_nAffectedRows = 0;



        private void Trigger_WorkerInsertData()
        {
            if (m_thread == null)
            {
                m_thread = new Thread(new ParameterizedThreadStart(WorkerInsertData));
                //m_thread.Priority = ThreadPriority.AboveNormal;
            }

            switch (m_thread.ThreadState)
            {
                case System.Threading.ThreadState.Stopped:
                    m_thread = new Thread(new ParameterizedThreadStart(WorkerInsertData));
                    //m_thread.Priority = ThreadPriority.AboveNormal;
                    break;
                case System.Threading.ThreadState.Unstarted:
                    m_thread.Start();
                    break;
            }
        }

        private void WorkerInsertData(object param)
        {
            List<Task> aryTasks = new List<Task>();
            while (m_queue.Count > 0)
            {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    byte[][] arySets = m_queue.Dequeue();
                    m_nAffectedRows += m_dac.InsertAnalysisDataTemplate(0, arySets);
                    stopwatch.Stop();
                    log.Info($"Processing Elaptime is {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
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
