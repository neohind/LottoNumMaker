using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NewLotto
{
    public class KnoieAppender : RollingFileAppender
    {
        private static FieldInfo _loggingEventm_dataFieldInfo = null;
        private static ICryptoTransform m_desdecrypt = null;

        public KnoieAppender()
        {
            Init();
        }

        private void Init()
        {
            if (_loggingEventm_dataFieldInfo == null)
                _loggingEventm_dataFieldInfo = typeof(LoggingEvent).GetField("m_data", BindingFlags.Instance | BindingFlags.NonPublic);

            if (m_desdecrypt == null)
            {
                byte[] arySomeData = new byte[] { 0x48, 0x49, 0x4E, 0x44, 0x4E, 0x45, 0x54, 0x37 };
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                des.Key = arySomeData;
                des.IV = arySomeData;
                m_desdecrypt = des.CreateDecryptor();
            }
        }


        protected override void Append(log4net.Core.LoggingEvent loggingEvent)
        {
            //string newMessage = Encrypt(loggingEvent.RenderedMessage);

            //LoggingEventData loggingEventData = (LoggingEventData)_loggingEventm_dataFieldInfo.GetValue(loggingEvent);
            //loggingEventData.Message = newMessage;
            //_loggingEventm_dataFieldInfo.SetValue(loggingEvent, loggingEventData);

            base.Append(loggingEvent);
        }




        #region DES암복호화        

        //문자열 암호화
        private string Encrypt(string str)
        {
            byte[] arySomeData = new byte[] { 0x48, 0x49, 0x4E, 0x44, 0x4E, 0x45, 0x54, 0x37 };

            //소스 문자열
            byte[] btSrc = Encoding.Default.GetBytes(str);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            des.Key = arySomeData;
            des.IV = arySomeData;

            ICryptoTransform desencrypt = des.CreateEncryptor();

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, desencrypt, CryptoStreamMode.Write);
            cs.Write(btSrc, 0, btSrc.Length);
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }


        //문자열 복호화
        private string Decrypt(byte[] arySource)
        {
            byte[] btEncData = arySource;
            byte[] btSrc = new byte[0];
            using (MemoryStream ms = new MemoryStream())
            {
                CryptoStream cs = new CryptoStream(ms, m_desdecrypt, CryptoStreamMode.Write);

                cs.Write(btEncData, 0, btEncData.Length);

                cs.FlushFinalBlock();

                btSrc = ms.ToArray();
            }

            if(btSrc.Length > 0)
                return Encoding.Default.GetString(btSrc);
            return string.Empty;
        }//end of func DesDecrypt
        #endregion

    }
}
