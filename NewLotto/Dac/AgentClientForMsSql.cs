using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NewLotto.Dac
{
    /// <summary>
    /// MS SQL을 접속할 때 사용할 DB 연결 Agent 클래스
    /// </summary>
    public class AgentClientForMsSql 
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        
        
        private string m_sConnectionString = string.Empty;

        
        public AgentClientForMsSql(string sConnectionString)
        {
            m_sConnectionString = sConnectionString;
        }


        /// <summary>
        /// 쿼리를 실행 한뒤 DataTable 형식으로 값을 가져온다.
        /// </summary>
        /// <param name="set">쿼리값이 담긴 개체</param>
        /// <returns>DataTable 형식의 결과값</returns>
        public DataTable GetDataTable(UDataQuerySet set)
        {
            DataTable dtResult = new DataTable();

            SqlConnection connection = null;
            SqlCommand sqlCommand = null;
            SqlDataAdapter sqlAdapter = null;
            try
            {
                using (connection = new SqlConnection(m_sConnectionString))
                using (sqlCommand = new SqlCommand())
                using (sqlAdapter = new SqlDataAdapter())
                {
                    connection.Open();

                    sqlCommand.Connection = connection;
                    sqlCommand.CommandText = set.Query;
                    sqlCommand.CommandTimeout = set.Timeout;
                    sqlCommand.CommandType = set.CmdType;
                    sqlAdapter.SelectCommand = sqlCommand;

                    MakeParamter(sqlCommand.Parameters, set);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    sqlAdapter.Fill(dtResult);
                    stopwatch.Stop();
                    //log.DebugFormat("GetDataTable : {0}ms - {1}", stopwatch.ElapsedMilliseconds, set.Query);

                    RetriveOutParamter(sqlCommand.Parameters, set);
                }
            }
            catch (SqlException ex)
            {
                if (set != null)
                {
                    StringBuilder sbErrorMessage = new StringBuilder();
                    sbErrorMessage.AppendLine($"Query : {set.Query}");
                    if (set.ParametersKeys.Length > 0)
                        foreach (string sKey in set.ParametersKeys)
                            sbErrorMessage.AppendLine($"  Param : {sKey} = {set.GetParamValue(sKey)}");
                    log.Error(sbErrorMessage.ToString());
                }
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dtResult;
        }

        

        /// <summary>
        /// 쿼리를 실행 한뒤 DataSet 형식으로 값을 가져온다.
        /// </summary>
        /// <param name="set">쿼리값이 담긴 개체</param>
        /// <returns>DataSet 형식의 결과값</returns>        
        public DataSet GetDataSet(UDataQuerySet set)
        {
            DataSet dsResult = new DataSet();

            SqlConnection connection = null;
            SqlDataAdapter sqlAdapter = null;
            SqlCommand sqlCommand = null;
            try
            {
                using (connection = new SqlConnection(m_sConnectionString))
                using (sqlCommand = new SqlCommand())
                using (sqlAdapter = new SqlDataAdapter())
                {
                    connection.Open();

                    sqlCommand.CommandTimeout = set.Timeout;
                    sqlCommand.CommandType = set.CmdType;
                    sqlCommand.CommandText = set.Query;
                    sqlCommand.Connection = connection;

                    sqlAdapter.SelectCommand = sqlCommand;


                    MakeParamter(sqlCommand.Parameters, set);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    sqlAdapter.Fill(dsResult);
                    stopwatch.Stop();
                   // log.DebugFormat("GetDataSet : {0}ms - {1}", stopwatch.ElapsedMilliseconds, set.Query);

                    RetriveOutParamter(sqlCommand.Parameters, set);
                }
            }
            catch (SqlException ex)
            {             
                if (set != null)
                {
                    StringBuilder sbErrorMessage = new StringBuilder();
                    sbErrorMessage.AppendLine($"Query : {set.Query}");
                    if (set.ParametersKeys.Length > 0)
                        foreach (string sKey in set.ParametersKeys)
                            sbErrorMessage.AppendLine($"  Param : {sKey} = {set.GetParamValue(sKey)}");
                    log.Error(sbErrorMessage.ToString());
                }
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

           
            return dsResult;
        }


        /// <summary>
        /// 쿼리를 실행 한다.
        /// </summary>
        /// <param name="aryQuerySet">쿼리 셋 목록</param>
        /// <returns>실행 결과 변경된 Row 갯수</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public int ExecuteQuery(UDataQuerySet set)
        {
            int affectedRows = 0;

            SqlConnection connection = null;
            SqlCommand sqlCommand = null;            
            try
            {
                using (connection = new SqlConnection(m_sConnectionString))
                using (sqlCommand = new SqlCommand())
                {
                    connection.Open();

                    sqlCommand.CommandTimeout = set.Timeout;
                    sqlCommand.CommandType = set.CmdType;
                    sqlCommand.CommandText = set.Query;
                    sqlCommand.Connection = connection;

                    MakeParamter(sqlCommand.Parameters, set);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    affectedRows += sqlCommand.ExecuteNonQuery();
                    stopwatch.Stop();
                    //log.DebugFormat("ExecuteQuery : {0}ms - {1}", stopwatch.ElapsedMilliseconds, set.Query);

                    RetriveOutParamter(sqlCommand.Parameters, set);
                }
            }
            catch (SqlException ex)
            {
                if (set != null)
                {
                    StringBuilder sbErrorMessage = new StringBuilder();
                    sbErrorMessage.AppendLine($"Query : {set.Query}");
                    if (set.ParametersKeys.Length > 0)
                        foreach (string sKey in set.ParametersKeys)
                            sbErrorMessage.AppendLine($"  Param : {sKey} = {set.GetParamValue(sKey)}");
                    log.Error(sbErrorMessage.ToString());
                }
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (connection != null)
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                    connection.Dispose();
                }
            }

            return affectedRows;
        }
       

        /// <summary>
        /// 쿼리를 실행 한 뒤 단일 값을 가져온다.
        /// </summary>
        /// <typeparam name="T">결과값 단일 값을 위한 템플릿</typeparam>
        /// <param name="set">쿼리값이 담긴 개체</param>
        /// <returns>T 로 캐스팅된 결과 값</returns>
        public T GetValue<T>(UDataQuerySet set)
        {
            T value = default(T);

            SqlConnection connection = null;
            SqlCommand sqlCommand = null;
            try
            {
                using (connection = new SqlConnection(m_sConnectionString))
                using (sqlCommand = new SqlCommand())
                {
                    connection.Open();
                    sqlCommand.Connection = connection;
                    sqlCommand.CommandType = set.CmdType;
                    sqlCommand.CommandText = set.Query;
                    sqlCommand.CommandTimeout = set.Timeout;

                    MakeParamter(sqlCommand.Parameters, set);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    object objResult = sqlCommand.ExecuteScalar();
                    stopwatch.Stop();
                    //log.DebugFormat("GetValue : {0}ms - {1}", stopwatch.ElapsedMilliseconds, set.Query);

                    if (objResult != null && DBNull.Value.Equals(objResult) == false)
                    {
                        value = (T)Convert.ChangeType(objResult, typeof(T));
                    }
                }
            }
            catch (SqlException ex)
            {
                if (set != null)
                {
                    StringBuilder sbErrorMessage = new StringBuilder();
                    sbErrorMessage.AppendLine($"Query : {set.Query}");
                    if (set.ParametersKeys.Length > 0)
                        foreach (string sKey in set.ParametersKeys)
                            sbErrorMessage.AppendLine($"  Param : {sKey} = {set.GetParamValue(sKey)}");
                    log.Error(sbErrorMessage.ToString());
                }
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return value;
        }

        /// <summary>
        /// UDataQuery에 담긴 Paramter를 DB 개체에 맞게 Paramter를 구성한다.
        /// </summary>
        /// <param name="parameters">DB 개체에 담긴 Paramter Collection 개체</param>
        /// <param name="set">쿼리값이 담긴 개체</param>
        private void MakeParamter(SqlParameterCollection parameters, UDataQuerySet set)
        {
            foreach (string sKey in set.ParametersKeys)
            {
                SqlParameter param = new SqlParameter(sKey, set.GetParamValue(sKey));
                parameters.Add(param);
            }

            foreach (string sKey in set.OutParametersKeys)
            {
                SqlParameter param = new SqlParameter();
                param.ParameterName = sKey;
                param.Size = set.GetOutParamSize(sKey);
                param.Direction = ParameterDirection.Output;
                parameters.Add(param);
            }
        }

        /// <summary>
        /// 쿼리 실행 후 Return 된 Output 파라미터를 추출한다.
        /// </summary>
        /// <param name="parameter">결과값이 담긴 모든 DB 개체의Paramter Collection 개체</param>
        /// <param name="set">쿼리값이 담긴 개체</param>
        private void RetriveOutParamter(SqlParameterCollection parameter, UDataQuerySet set)
        {
            foreach (SqlParameter param in parameter)
            {
                if (param.Direction == ParameterDirection.Output
                    || param.Direction == ParameterDirection.InputOutput)
                {
                    set.SetOutParam(param.ParameterName, param.Value);
                }
            }
        }
    }
}
