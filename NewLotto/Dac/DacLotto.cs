using NewLotto.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NewLotto.Dac
{
    public class DacLotto
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private AgentClientForMsSql m_agent = null;

        public DacLotto(string sConnectionString)
        {
            m_agent = new AgentClientForMsSql(sConnectionString);
            //m_agent = new AgentClientForMsSql("Server=KNOIE;Database=Lotto;User Id=lottouser;Password=NJSdhQynvNhKJ4;");
        }

        public void InsertNewBalls(int nIndex, List<int> aryBalls)
        {
            if (aryBalls.Count == 7)
            {
                UDataQuerySet set = new UDataQuerySet(
@"INSERT INTO [dbo].[TB_BALLRESULTS]
           ([idx]
           ,[b1]
           ,[b2]
           ,[b3]
           ,[b4]
           ,[b5]
           ,[b6]
           ,[b7])
     VALUES
           (@idx,
			@b1,
			@b2,
			@b3,
			@b4,
			@b5,
			@b6,
			@b7)");
                set.AddParam("@idx", nIndex);
                for (int i = 0; i < 7; i++)
                {
                    set.AddParam($"@b{i + 1}", aryBalls[i]);
                }

                try
                {
                    m_agent.ExecuteQuery(set);
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    if (ex.Number == 2601)
                    {
                        log.Error($"Already Inserted this Index={nIndex}.\r\n Detail Message : {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
            else
            {
                log.Error($"Invalid Ball Count : Index={nIndex}, Ball Counts={aryBalls.Count}");
            }
        }

        public bool CheckAlreadyExist(int nIndex)
        {
            try
            {
                UDataQuerySet set = new UDataQuerySet(
 @"IF (SELECT TOP 1 idx FROM TB_BALLRESULTS WHERE idx = @idx) = @idx
		SELECT 'Y'
	ELSE
		SELECT 'N'");
                set.AddParam("@idx", nIndex);
                string sResult = m_agent.GetValue<string>(set);

                return "Y".Equals(sResult);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            return false;
        }

        public List<ModelBallInfo> GetAllBallInfo()
        {
            List<ModelBallInfo> aryResult = new List<ModelBallInfo>();
            DataTable dtBallInfos = null;

            UDataQuerySet set = new UDataQuerySet("SELECT idx, b1,b2,b3,b4,b5,b6, b7 FROM TB_BALLRESULTS", CommandType.Text);

            try
            {
                dtBallInfos = m_agent.GetDataTable(set);

                foreach (DataRow row in dtBallInfos.Rows)
                {
                    ModelBallInfo info = new ModelBallInfo(row);
                    aryResult.Add(info);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally            
            {
                if (dtBallInfos != null)
                    dtBallInfos.Dispose();
            }
            return aryResult;
        }

        public void InsertAnalysisCalcResult(int index, int nSum, double dAvg, int nAvg, double dDev, int nDev)
        {
            UDataQuerySet set = new UDataQuerySet("IF (SELECT idx FROM TB_ANALY_CALC WHERE idx=@idx) IS NULL INSERT INTO TB_ANALY_CALC(idx,avg_int,average,stddev_int,stddev,summary)VALUES(@idx,@avg_int,@average,@stddev_int,@stddev,@summary)"
                , CommandType.Text);
            set.AddParam("@idx", index);
            set.AddParam("@summary", nSum);
            set.AddParam("@avg_int", nAvg);
            set.AddParam("@average", dAvg);
            set.AddParam("@stddev_int", nDev);
            set.AddParam("@stddev", dDev);


            try
            {
                if (m_agent.ExecuteQuery(set) < 1)
                    log.Debug("Skip Insert to calc results");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public bool CheckAlreadyExistBallSet(List<byte> aryResult)
        {
            bool bResult = false;
            UDataQuerySet set = new UDataQuerySet("SELECT idx FROM TB_BALLRESULTS WHERE b1=@b1 AND b2=@b2 AND b3=@b3 AND b4=@b4 AND b5=@b5 AND b6=@b6", CommandType.Text);

            for(int i=0; i<6; i++)
                set.AddParam($"@b{i+1}", aryResult[i]);

            DataTable tbBallInfos = null;
            try
            {
                tbBallInfos = m_agent.GetDataTable(set);
                bResult = (tbBallInfos != null && tbBallInfos.Rows.Count == 0);
            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                if(tbBallInfos != null)
                    tbBallInfos.Dispose();
            }

            return bResult;
        }

        public Dictionary<int, List<ModelBallInfo>> GetAllSameValues()
        {
            Dictionary<int, List<ModelBallInfo>> dicResults = new Dictionary<int, List<ModelBallInfo>>();

            UDataQuerySet set = new UDataQuerySet(@"SELECT * 
                    from (SELECT count(seqid) as cnt , b1, b2, b3, b4, b5, b6
	                    FROM 
		                    [Lotto].[dbo].[TB_ANALY_CNT] WITH (NOLOCK)  
	                    Group BY 
		                    b1, b2, b3, b4, b5, b6) as src
                    WHERE 
	                    cnt > 1", CommandType.Text);
            DataTable tbResults = null;
            try
            {
                tbResults = m_agent.GetDataTable(set);
                if(tbResults != null)
                {
                    foreach(DataRow row in tbResults.Rows)
                    {
                        ModelBallInfo info = new ModelBallInfo(row);
                        if (dicResults.ContainsKey(info.Index) == false)
                            dicResults.Add(info.Index, new List<ModelBallInfo>());
                        dicResults[info.Index].Add(info);
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                if (tbResults != null)
                    tbResults.Dispose();
            }
            return dicResults;
        }

        public List<byte[]> GetAllBallPatterns()
        {
            List<byte[]> aryResults = new List<byte[]>();
            UDataQuerySet set = new UDataQuerySet("SELECT MAX(seqid) FROM TB_ANALY_CNT WHERE lvl=6", CommandType.Text);
            long nAllCount = 0;
            try
            {
                nAllCount = m_agent.GetValue<long>(set);
            }
            catch(Exception ex)
            {
                log.Error(ex);
            }

            if(nAllCount > 0)
            {
                long nCurIndex = 0;             
                int nBulkIndex = 0;

                while(nCurIndex <= nAllCount)
                {
                    UDataQuerySet setData = new UDataQuerySet("SELECT lvl, b1, b2, b3, b4, b5, b6 FROM TB_ANALY_CNT WITH (NOLOCK) WHERE lvl=6 AND (seqid > @r1 and seqid <= @r2)", CommandType.Text);
                    setData.AddParam("@r1", nCurIndex);
                    nCurIndex = nCurIndex + 50000 * nBulkIndex;
                    setData.AddParam("@r2", nCurIndex);

                    DataTable tbResult = null;

                    try
                    {
                        tbResult = m_agent.GetDataTable(setData);
                        if(tbResult != null)
                        {
                            foreach(DataRow row in tbResult.Rows)
                            {
                                byte[] aryData = new byte[7];
                                aryData[0] = Convert.ToByte(row["lvl"]);
                                aryData[1] = Convert.ToByte(row["b1"]);
                                aryData[2] = Convert.ToByte(row["b2"]);
                                aryData[3] = Convert.ToByte(row["b3"]);
                                aryData[4] = Convert.ToByte(row["b4"]);
                                aryData[5] = Convert.ToByte(row["b5"]);
                                aryData[6] = Convert.ToByte(row["b6"]);

                                aryResults.Add(aryData);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        log.Error(ex);
                    }
                    finally
                    {
                        if (tbResult != null)
                        {
                            tbResult.Dispose();
                            tbResult = null;
                        }
                    }

                    nBulkIndex++;
                }
            }



            return aryResults;
        }

        public ModelCalcResults GetCalcResult()
        {
            UDataQuerySet set = new UDataQuerySet(
@"DECLARE @avg real
DECLARE @stdev real
DECLARE @summaryAvg real
DECLARE @summaryStdev real

SET @summaryAvg = (SELECT AVG(summary) FROM [TB_ANALY_CALC])
SET @summaryStdev = (SELECT STDEV(summary) FROM [TB_ANALY_CALC])
SET @avg = (SELECT AVG(average) FROM [TB_ANALY_CALC])
SET @stdev = (SELECT STDEV(average) FROM [TB_ANALY_CALC])

SELECT @summaryAvg as sumavg, @summaryStdev as sumstd, @avg as avg, @stdev as stdev");
            set.CmdType = CommandType.Text;
            ModelCalcResults result = null;
            DataTable tbResult = null;

            try
            {
                tbResult = m_agent.GetDataTable(set);
                if (tbResult != null && tbResult.Rows.Count > 0)
                    result = new ModelCalcResults(tbResult.Rows[0]);
            }
            catch(Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                if (tbResult != null)
                    tbResult.Dispose();
            }

            return result;
        }

        private string MakeQuery(int nLevel, int nCurLevel)
        {
            if (nLevel == 1)
            {
                return "SELECT COUNT(seqid) FROM TB_BALLRESULTS WHERE b1=@b1 OR b2=@b1 OR b3=@b1 OR b4=@b1 OR b5=@b1 OR b6=@b1";
            }

            if (nCurLevel == 1)
            {
                return "SELECT seqid,b1,b2,b3,b4,b5,b6 FROM TB_BALLRESULTS WHERE b1=@b1 OR b2=@b1 OR b3=@b1 OR b4=@b1 OR b5=@b1 OR b6=@b1";
            }

            
            string sResult = MakeQuery(nLevel, nCurLevel - 1);

            StringBuilder sbResult = new StringBuilder();

            if (nLevel == nCurLevel)            
                sbResult.Append("SELECT COUNT(seqid) FROM (");
            else
                sbResult.Append("SELECT seqid,b1,b2,b3,b4,b5,b6 FROM (");

            sbResult.Append(sResult);
            sbResult.AppendFormat(") AS S{0} WHERE S{0}.b1=@b{1} OR S{0}.b2=@b{1} OR S{0}.b3=@b{1} OR S{0}.b4=@b{1} OR S{0}.b5=@b{1} OR S{0}.b6=@b{1}", nCurLevel - 1, nCurLevel);
            return sbResult.ToString();
        }

        internal int InsertAnalysisDataTemplate(int nBall1, byte[][] aryTest)
        {
            StringBuilder sbQuery = new StringBuilder();

            //sbQuery.AppendLine("SET NOCOUNT ON;");
            sbQuery.AppendLine("BEGIN TRANSACTION;");            
            foreach (byte[] aryValues in aryTest)
            {
                
                int nLevel = aryValues[6];
                
                sbQuery.AppendFormat("IF (SELECT COUNT(*) FROM TB_ANALY_CNT WITH (NOLOCK) WHERE lvl={0} AND b1={1} AND b2={2} AND b3={3} AND b4={4} AND b5={5} AND b6={6}) = 0  INSERT INTO TB_ANALY_CNT (lvl,b1,b2,b3,b4,b5,b6,cnt)VALUES({0}, {1}, {2}, {3}, {4}, {5}, {6}, 0);"
                    , nLevel, aryValues[0], aryValues[1], aryValues[2], aryValues[3], aryValues[4], aryValues[5]);
                sbQuery.AppendLine();
            }
            sbQuery.AppendLine("COMMIT");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UDataQuerySet set = new UDataQuerySet(sbQuery.ToString(), CommandType.Text);
            int nResult = m_agent.ExecuteQuery(set);
            stopwatch.Stop();
            

            return nResult;
        }
    }
}
