/*
 *  Romaco project 
 *   
 *  Crash Recorder AddIn Zenon 8.20
 *  
 *  Author Giovanni Paolin  
 *  
 *  Version 1.0.0 - 25 / Oct / 2021
 *  
 *  Version 1.0.1 - 20 / Dic / 2021
 *  Recompiled with ZenAddInStdLib updated
 *  
 *  
 *  Version 1.1.0 - 31 / 01 / 2022
 *  Added text config file management.
 *  Management of template BO

 *  Version 1.1.1 - 02 / 03 / 2022
 *  Fixed bug: naming of trigger variable in BO style.
 *  
 *  
 */

using Scada.AddIn.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ZenonAddInStdLib;

namespace CrashRecAddIn
{
    /// <summary>
    /// Description of Project Service Extension.
    /// </summary>
    [AddInExtension("Crash Recorder AddIn (" + ProjectServiceExtension.AddInVersion + ")", "AddIn for crash recorder external program management")]

    public class ProjectServiceExtension : IProjectServiceExtension
    {
        #region IProjectServiceExtension implementation
        IProject _Zenon;
        
        public const string AddInVersion = "1.1.1";
        public const string cLogFile = "CrashRecAddIn_LOG.txt";

        public bool _CycleMonitorEnable = true;
        public bool _CrashRecordinEnable = true;
        private string actualTrigger = "";

        public string folderRepos = @"C:\Romaco\Prog\Data\CrashRecorder";
        //public string waitForVideo = "waitForVideo.txt";
        public string triggerFile = "triggerFile.txt";

        private OcValueContainer alarmTriggerVarContainer;
        private string alarmTriggerVarContainerName = "AlarmTriggerVarContainer";

        private int _filterTriggerMilliSeconds = 1000;

        OcTriggerContainer ocTriggerContainer;
        private string alarmContainerName = "AlarmContainer";

        private string _pathConfigFile = "CrashRecConfig.txt";
        private string _pathBaslerCommander = @"C:\Romaco\Prog\Program\BaslerCommander\BaslerCommander.exe";
        private string AlarmCodeTriggerVar = "bINT_CrashRecorder_AlarmTriggerCode";

        private int _alarmFirstVarNo = 16001;
        private int _alarmLastVarNo = 16480;
        private string _alarmNaming = "KABO";
        private string _BOStyleAddress = "[0096]";

        public void Start(IProject context, IBehavior behavior)
        {
            // enter your code which should be executed when starting the service for the SCADA Service Engine
            try 
            {
                _Zenon = context;
                
                Logger.AddLog("Start Crash Recorder AddIn", ProjectServiceExtension.cLogFile);
                try 
                {
                    InitAlarmTriggerVarContainer();
                    CheckConfiguration();
                    InitTriggerContainer();
                    RunControlCycle();
                }
                catch (Exception ex)
                {
                    Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
                }
            }
            catch (Exception ex)
            {
                Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
            }
        }

        public void Stop()
        {
            // enter your code which should be executed when stopping the service for the SCADA Service Engine
        }

        private void CheckConfiguration()
        {
            try
            {
                Logger.AddLog("Checking configuration", ProjectServiceExtension.cLogFile);
                string pathConfigFile = Path.Combine(_Zenon.GetFolderPath(FolderPath.Others), _pathConfigFile);

                if (File.Exists(pathConfigFile))
                {
                    Logger.AddLog($"{pathConfigFile} is present", ProjectServiceExtension.cLogFile);
                    Logger.AddLog($"Looking for setting: AlarmFirstVar", ProjectServiceExtension.cLogFile);
                    string line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("AlarmFirstVar|"));
                    string alarmFirstVar = line.Split(new char[] { '|' })[1];
                    _alarmFirstVarNo = Convert.ToInt32(alarmFirstVar);

                    Logger.AddLog($"Looking for setting: AlarmLastVar", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("AlarmLastVar|"));
                    string alarmLastVar = line.Split(new char[] { '|' })[1];
                    _alarmLastVarNo = Convert.ToInt32(alarmLastVar);

                    Logger.AddLog($"Looking for setting: AlarmNaming", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("AlarmNaming|"));
                    _alarmNaming = line.Split(new char[] { '|' })[1];

                    Logger.AddLog($"Looking for setting: BOStyleAddress", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("Address|"));
                    _BOStyleAddress = line.Split(new char[] { '|' })[1];
                }
            }
            catch (Exception ex)
            {
                Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
            }
        }

        private void InitAlarmTriggerVarContainer()
         {
            try 
            {
                Logger.AddLog("Init Alarm Trigger Var Container", ProjectServiceExtension.cLogFile);
                List<string> vars = new List<string>();
                vars.Add(AlarmCodeTriggerVar);
                alarmTriggerVarContainer = new OcValueContainer(_Zenon, alarmTriggerVarContainerName, vars);
                alarmTriggerVarContainer.CreateContainer();
            }
            catch (Exception ex)
            {
                Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
            }
        }
        private void RunControlCycle()
        {
            try 
            {
                Task.Run(() =>
                {
                    Logger.AddLog("Start run control cycle", ProjectServiceExtension.cLogFile);
                    while (_CycleMonitorEnable)
                    {
                        try 
                        {
                            int triggerVarValue;
                            alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar, out triggerVarValue);

                            //string token = triggerVarValue.ToString("00000");
                            string token = triggerVarValue.ToString();
                            if (actualTrigger != token)
                            {
                                actualTrigger = token;
                                Logger.AddLog($"New alarm trigger: {actualTrigger}", ProjectServiceExtension.cLogFile);

                                //alarmContainer.Dispose();
                                //alarmContainer = new OcTriggerContainer(_Zenon, alarmContainerName, actualTrigger);

                                string triggerVar = "";
                                if (_alarmNaming == "KABO")
                                {
                                    triggerVar = $"xInt_Exceptions{actualTrigger}";
                                }
                                else
                                {
                                    //BO
                                    triggerVar = $"{_BOStyleAddress}.Application.P_HmiError.P_HmiMsg[{actualTrigger}]";
                                } 
                                Logger.AddLog($"TriggerVar: {triggerVar}", ProjectServiceExtension.cLogFile);
                                ocTriggerContainer.TriggerVar = triggerVar;

                            }
                        }
                        catch (Exception ex) 
                        {
                            //TO DO management
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
            }
        }
        private void InitTriggerContainer()
        {
            try
            {
                Logger.AddLog("Init trigger container", ProjectServiceExtension.cLogFile);
                List<string> alarms = new List<string>();
                string alarm = "";
                if (_alarmNaming == "KABO")
                {
                    for (int i = _alarmFirstVarNo; i < _alarmLastVarNo; i++)
                    {
                        alarms.Add($"xInt_Exceptions{i.ToString()}");
                    }
                    //xInt_Exceptions16000 - xInt_Exceptions16480
                    alarm = $"xInt_Exceptions00000"; //fake var name just to create the container
                }
                else // "BO"
                {
                    for (int i = _alarmFirstVarNo; i < _alarmLastVarNo; i++)
                    {
                        alarms.Add($"{_BOStyleAddress}.Application.P_HmiError.P_HmiMsg[{i.ToString()}]");
                    }
                    //xInt_Exceptions16000 - xInt_Exceptions16480
                }

                ocTriggerContainer = new OcTriggerContainer(_Zenon, alarmContainerName, alarm);
                ocTriggerContainer.VarList = alarms;
                ocTriggerContainer.TriggerFunc = (() => 
                {
                    Logger.AddLog("Alarm trigger received", ProjectServiceExtension.cLogFile);
                    if (_CrashRecordinEnable)
                    {
                        string triggerFilePath = Path.Combine(folderRepos, triggerFile);
                        if (!File.Exists(triggerFilePath))
                        { 
                            File.Create(triggerFilePath);
                            Logger.AddLog("TRIGGERED!", ProjectServiceExtension.cLogFile);
                        }
                        else
                            Logger.AddLog("trigger File already present", ProjectServiceExtension.cLogFile);
                    }
                });
                ocTriggerContainer.TriggerManagement = TriggerManagementEnum.LEAVE;
                ocTriggerContainer.CreateContainer();
            }
            catch (Exception ex)
            {
                Logger.AddLogError(MethodInfo.GetCurrentMethod().ToString(), ex.ToString(), ProjectServiceExtension.cLogFile);
            }
        }

        private void FilterTrigger()
        {
            System.Threading.Thread.Sleep(_filterTriggerMilliSeconds);
        }

        #endregion
    }
}