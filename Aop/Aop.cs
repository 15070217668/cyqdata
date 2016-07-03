using CYQ.Data.Cache;
using System.Configuration;
using System;
using CYQ.Data.Table;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// �ڲ�Ԥ��ʵ�ֿյ�Aop
    /// </summary>
    internal class Aop : IAop
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache����
        private static readonly object lockObj = new object();
        #region IAop ��Ա

        public AopResult Begin(AopEnum action, AopInfo aopInfo)
        {
            return AopResult.Default;
        }

        public void End(AopEnum action, AopInfo aopInfo)
        {

        }

        public void OnError(string msg)
        {

        }
        static bool _CallOnLoad = false;
        public IAop GetFromConfig()
        {

            IAop aop = null;

            string aopApp = AppConfig.Aop;
            if (!string.IsNullOrEmpty(aopApp))
            {
                if (_Cache.Contains("Aop_Instance"))
                {
                    aop = _Cache.Get("Aop_Instance") as IAop;
                }
                else
                {
                    #region AOP����

                    string[] aopItem = aopApp.Split(',');
                    if (aopItem.Length == 2)//��������,����(dll)����
                    {
                        try
                        {
                            System.Reflection.Assembly ass = System.Reflection.Assembly.Load(aopItem[1]);
                            if (ass != null)
                            {
                                object instance = ass.CreateInstance(aopItem[0]);
                                if (instance != null)
                                {
                                    _Cache.Add("Aop_Instance", instance, AppConst.RunFolderPath + aopItem[1].Replace(".dll", "") + ".dll", 1440);
                                    aop = instance as IAop;
                                    if (!_CallOnLoad)
                                    {
                                        lock (lockObj)
                                        {
                                            if (!_CallOnLoad)
                                            {
                                                _CallOnLoad = true;
                                                aop.OnLoad();
                                            }
                                        }
                                    }
                                    return aop;
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            string errMsg = err.Message + "--Web.config need add a config item,for example:<add key=\"Aop\" value=\"Web.Aop.AopAction,Aop\" />(value format:namespace.Classname,Assembly name) ";
                            Error.Throw(errMsg);
                        }
                    }
                    #endregion
                }
            }
            if (aop != null)
            {
                return aop.Clone();
            }
            return null;
        }
        public IAop Clone()
        {
            return new Aop();
        }
        public void OnLoad()
        {
        }
        #endregion

        #region �ڲ�����
        public static Aop Instance
        {
            get
            {
                return Shell.instance;
            }
        }

        class Shell
        {
            internal static readonly Aop instance = new Aop();
        }
        #endregion
    }
}
