using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaheDownloadDemo
{
    public class Configer
    {
        #region  字段,属性
        //配置参数 缓存字典
        private static readonly StringDictionary ConfigCacheDictionary = new StringDictionary();
        #endregion

        #region 公共函数

        /// <summary>
        ///     读取配置文件
        /// </summary>
        /// <param name="key">配置的键</param>
        /// <returns></returns>
        public static string Read(string key)
        {
            lock (ConfigCacheDictionary)
            {
                try
                {
                    //如果缓存中有参数,直接从缓存中读取
                    if (ConfigCacheDictionary.ContainsKey(key))
                    {
                        return ConfigCacheDictionary[key];
                    }
                    var innerText = ConfigurationManager.AppSettings[key];
                    ConfigCacheDictionary.Add(key, innerText);
                    return innerText;
                }
                catch (Exception)
                {
                    // ignored
                }
                return null;
            }
        }


        /// <summary>
        /// 刷新配置缓存字典
        /// </summary>
        public static void RefreshToCache()
        {
            lock (ConfigCacheDictionary)
            {
                try
                {
                    var settings = ConfigurationManager.AppSettings;
                    foreach (var key in settings.AllKeys)
                    {
                        if (ConfigCacheDictionary.ContainsKey(key))
                        {
                            ConfigCacheDictionary[key] = settings[key];
                        }
                        else
                        {
                            ConfigCacheDictionary.Add(key, settings[key]);
                        }
                    }
                }
                catch (Exception)
                {
                    //Logger.Error("WeMew.WinFormOsr RefreshToCache:" + ex.Message);
                }
            }
        }

        /// <summary>
        ///     读取配置文件
        /// </summary>
        /// <param name="configs">配置列表 string[]{key,value}</param>
        /// <returns></returns>
        public static bool Write(List<string[]> configs)
        {
            lock (ConfigCacheDictionary)
            {
                try
                {
                    //更新App.config
                    var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    foreach (var tuple in configs)
                    {
                        var key = tuple[0];
                        var innerText = tuple[1] ?? "";

                        if (!configuration.AppSettings.Settings.AllKeys.Contains(key))
                        {
                            configuration.AppSettings.Settings.Add(key, innerText);
                        }
                        else
                        {
                            configuration.AppSettings.Settings[key].Value = innerText;
                        }
                    }

                    configuration.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");     //重新加载新的配置文件


                    //更新配置参数 缓存字典
                    foreach (var tuple in configs)
                    {
                        var key = tuple[0];
                        var innerText = tuple[1];

                        if (ConfigCacheDictionary.ContainsKey(key))
                        {
                            ConfigCacheDictionary[key] = innerText;
                        }
                        else
                        {
                            ConfigCacheDictionary.Add(key, innerText);
                        }
                    }
                    return true;
                }
                catch (Exception)
                {
                    //Logger.Error("WeMew.WinFormOsr Write:" + ex.Message);
                }
                return false;
            }
        }

        public static bool Write(string key, string value)
        {
            lock (ConfigCacheDictionary)
            {
                try
                {
                    //更新App.config
                    var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    if (!configuration.AppSettings.Settings.AllKeys.Contains(key))
                    {
                        configuration.AppSettings.Settings.Add(key, value);
                    }
                    else
                    {
                        configuration.AppSettings.Settings[key].Value = value;
                    }
                    configuration.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("appSettings");     //重新加载新的配置文件

                    //更新配置参数 缓存字典
                    if (ConfigCacheDictionary.ContainsKey(key))
                    {
                        ConfigCacheDictionary[key] = value;
                    }
                    else
                    {
                        ConfigCacheDictionary.Add(key, value);
                    }
                    return true;
                }
                catch (Exception)
                {
                    //Logger.Error("WeMew.WinFormOsr Write:" + ex.Message);
                }
                return false;
            }
        }
        #endregion
    }
}
