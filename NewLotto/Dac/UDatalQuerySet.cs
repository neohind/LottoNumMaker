using System;
using System.Collections.Generic;
using System.Data;
using System.Text;


namespace NewLotto.Dac
{
    /// <summary>
    /// 쿼리 값을 담기 위한 개체 클래스
    /// </summary>
    public class UDataQuerySet
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// 파라미터 정보를 위한 개체 클래스
        /// </summary>
        class ParamterInfo
        {
            /// <summary>
            /// 파라미터 이름을 가져오거나 설정한다.
            /// </summary>
            public string ParamName
            {
                get;
                set;
            }

            /// <summary>
            /// 파라미터의 크기를 가져오거나 설정한다.
            /// </summary>
            public int Size
            {
                get;
                set;
            }

            /// <summary>
            /// 파라미터 값의 유형을 가져오거나 설정한다.
            /// </summary>
            public Type ParamType
            {
                get;
                set;
            }

            /// <summary>
            /// 파라미터 값을 가져오거나 설정한다.
            /// </summary>
            public object Value
            {
                get;
                set;
            }


            public string Save()
            {
                string sSavedData = string.Format("{0}|{1}|{2}|{3}", this.ParamName, this.Size, this.ParamType.FullName, this.Value);

                return sSavedData;
;
            }

            public void Load(string sSavedData)
            {
                if(string.IsNullOrEmpty(sSavedData) == false)
                {
                    string[] aryToken = sSavedData.Split("|".ToCharArray()); ;
                    if(aryToken.Length == 4)
                    {
                        this.ParamName = aryToken[0];
                        if(string.IsNullOrEmpty(aryToken[1]) == false)
                            this.Size = Convert.ToInt32(aryToken[1]);
                        this.ParamType = Type.GetType(aryToken[2]);
                        this.Value = Convert.ChangeType(aryToken[3], this.ParamType);
                    }
                }
            }
        }

        /// <summary>
        /// 호출시 사용할 쿼리값을 가져오거나 설정한다.
        /// </summary>
        public string Query
        {
            get;
            set;
        }

        /// <summary>
        /// 입력 파라미터들을 가져오거나 설정한다.
        /// </summary>
        Dictionary<string, ParamterInfo> Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// 프로시저 파라미터 정보를 가져오거나 설정한다.
        /// </summary>
        public string ParamterData
        {
            get
            {
                StringBuilder sbAllParamters = new StringBuilder();
                foreach(string sKey in Parameters.Keys)
                {
                    sbAllParamters.Append(Parameters[sKey].Save());
                    sbAllParamters.Append("^");
                }
                return sbAllParamters.ToString();
            }
            set
            {
                string[] aryParamters = value.Split("^".ToCharArray());
                foreach(string sParamData in aryParamters)
                {
                    if (string.IsNullOrEmpty(sParamData) == false)
                    {
                        ParamterInfo info = new ParamterInfo();
                        info.Load(sParamData);
                        if(Parameters.ContainsKey(info.ParamName) == false)
                            Parameters.Add(info.ParamName, info);
                    }
                }
            }

        }

        /// <summary>
        /// 입력 파라미터들의 이름들을 모두 가져온다.
        /// </summary>
        public string[] ParametersKeys
        {
            get
            {
                string[] aryResult = new string[Parameters.Keys.Count];
                Parameters.Keys.CopyTo(aryResult, 0);
                return aryResult;
            }
        }

        /// <summary>
        /// 출력 파라미터들을 가져오거나 설정한다.
        /// </summary>
        Dictionary<string, ParamterInfo> OutParameters
        {
            get;
            set;
        }

        /// <summary>
        /// 출력 파라미터들의 이름들을 모두 가져온다.
        /// </summary>
        public string[] OutParametersKeys
        {
            get
            {
                string[] aryResult = new string[OutParameters.Keys.Count];
                OutParameters.Keys.CopyTo(aryResult, 0);
                return aryResult;
            }
        }

        /// <summary>
        /// 호출 유형(Text, Store Procedure Name)을 가져오거나 설정한다.
        /// </summary>
        public CommandType CmdType
        {
            get;
            set;
        }

        

        /// <summary>
        /// 실행 Timeout 시간을 가져오거나 설정한다.
        /// </summary>
        public int Timeout
        {
            get;
            set;
        }

    

        /// <summary>
        /// 생성자
        /// </summary>
        public UDataQuerySet()
        {
            this.Parameters = new Dictionary<string, ParamterInfo>();
            this.OutParameters = new Dictionary<string, ParamterInfo>();
            CmdType = CommandType.Text; 
            Timeout = 30;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="sQuery">실행할 쿼리</param>
        public UDataQuerySet(string sQuery)
            : this()
        {
            this.Query = sQuery;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="sQuery">실행할 쿼리</param>
        /// <param name="type">쿼리 유형</param>
        public UDataQuerySet(string sQuery, CommandType type) : this(sQuery)
        {
            this.CmdType = type;
        }

        /// <summary>
        /// 파라미터를 추가한다.
        /// </summary>
        /// <param name="sName">파라미터 이름</param>
        /// <param name="objValue">파라미터 값</param>
        public void AddParam(string sName, object objValue)
        {
            ParamterInfo paramInfo = new ParamterInfo();
            paramInfo.ParamName = sName;

            if (objValue == null)
            {
                paramInfo.ParamType = typeof(object);
                paramInfo.Value = DBNull.Value;
            }
            else{
                paramInfo.ParamType = objValue.GetType();
                paramInfo.Value = objValue;
            }

            if (Parameters.ContainsKey(sName) == false)
                Parameters.Add(sName, paramInfo);
            else
                Parameters[sName] = paramInfo;
        }

        

        /// <summary>
        /// 파라미터에서 값을 가져온다.
        /// </summary>
        /// <param name="sName">파라미터 이름</param>
        /// <returns>해당 파라미터의 값</returns>
        public object GetParamValue(string sName)
        {
            if (Parameters.ContainsKey(sName))
                return Parameters[sName].Value;
            return null;
        }

        /// <summary>
        /// 출력 파라미터를 추가한다.
        /// </summary>
        /// <param name="sName">파라미터 이름</param>
        /// <param name="paramType">파라미터 값 유형</param>
        /// <param name="nSize">파라미터 값 크기</param>
        public void AddOutParam(string sName, Type paramType, int nSize)
        {
            ParamterInfo paramInfo = new ParamterInfo();
            paramInfo.ParamName = sName;
            paramInfo.ParamType = paramType;
            paramInfo.Size = nSize;

            if (OutParameters.ContainsKey(sName) == false)
                OutParameters.Add(sName, paramInfo);
        }

        /// <summary>
        /// 출력 파라미터의 크기를 가져온다.
        /// </summary>
        /// <param name="sName">파라미터 이름</param>
        /// <returns>해당 파라미터의 크기</returns>
        public int GetOutParamSize(string sName)
        {
            if (OutParameters.ContainsKey(sName))
                return OutParameters[sName].Size;
            return 0;
        }

        /// <summary>
        /// 출력 파라미터의 값 유형을 가져온다.
        /// </summary>
        /// <param name="sName">파라미터 이름</param>
        /// <returns>해당 파라미터의 값 유형</returns>
        public Type GetOutParamType(string sName)
        {
            if (OutParameters.ContainsKey(sName))
                return OutParameters[sName].ParamType;
            return typeof(object);
        }

        /// <summary>
        /// 출력 파라미터에 값을 설정한다.
        /// </summary>
        /// <param name="sName">출력 파라미터 이름</param>
        /// <param name="value">출력 파라미터 값</param>
        public void SetOutParam(string sName, object value)
        {
            if (OutParameters.ContainsKey(sName))
                OutParameters[sName].Value = value;
            else
            {
                ParamterInfo paramInfo = new ParamterInfo();
                paramInfo.ParamName = sName;                
                paramInfo.Value = value;
                OutParameters.Add(sName, paramInfo);
            }
        }

        /// <summary>
        /// 출력 파라미터에서 값을 가져온다.
        /// </summary>
        /// <param name="sName">출력 파라미터 이름</param>
        /// <returns>해당 파라미터의 값</returns>
        public object GetOutParam(string sName)
        {
            object result = null;
            if (OutParameters.ContainsKey(sName))
                result = OutParameters[sName].Value;
            return result;
        }

        /// <summary>
        /// 현재 개체의 모든 Property 값을 로그로 나타낼 수 있도록 문자열을 돌려준다.
        /// </summary>
        /// <returns>모든 Property들이 담긴 결과 문자열</returns>
        public override string ToString()
        {
            StringBuilder sbLogForQuery = new StringBuilder();            
            sbLogForQuery.AppendLine("Query : ");
            sbLogForQuery.AppendLine(this.Query);
            sbLogForQuery.AppendLine("Params : ");
            foreach (string sKey in Parameters.Keys)
            {
                sbLogForQuery.AppendFormat("  {0} -> {1}, ", sKey, Parameters[sKey]);
            }

            if(OutParameters.Count > 0)
            {
                sbLogForQuery.AppendLine("Output Params : ");
                foreach (string sKey in OutParameters.Keys)
                {
                    sbLogForQuery.AppendFormat("  {0} -> {1}, ", sKey, OutParameters[sKey]);
                }
            }
            sbLogForQuery.AppendLine();
            return sbLogForQuery.ToString();
        }
    }
}
