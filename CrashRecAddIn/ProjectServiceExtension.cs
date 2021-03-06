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
 *  Version 1.2.0 - 08 / 03 / 2022
 *  Added MultiTRIGGER - TO TEST!
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

        OcMultiTriggerContainer ocMultiTriggerContainer;
        private string alarmContainerName = "AlarmContainer";

        private string _pathConfigFile = "CrashRecConfig.txt";
        private string _pathBaslerCommander = @"C:\Romaco\Prog\Program\BaslerCommander\BaslerCommander.exe";
        private string AlarmCodeTriggerVar = "bINT_CrashRecorder_AlarmTriggerCode";
        private string AlarmCodeTriggerVar1 = "bINT_CrashRecorder_AlarmTriggerCode1";
        private string AlarmCodeTriggerVar2 = "bINT_CrashRecorder_AlarmTriggerCode2";
        private string AlarmCodeTriggerVar3 = "bINT_CrashRecorder_AlarmTriggerCode3";
        private string AlarmCodeTriggerVar4 = "bINT_CrashRecorder_AlarmTriggerCode4";

        private int _alarmFirstVarNo = 16001;
        private int _alarmLastVarNo = 16480;
        private string _alarmNaming = "KABO";
        private string _BOStyleAddress = "[0096]";

        private List<string> _tokens;
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
                vars.Add(AlarmCodeTriggerVar1);
                vars.Add(AlarmCodeTriggerVar2);
                vars.Add(AlarmCodeTriggerVar3);
                vars.Add(AlarmCodeTriggerVar4);
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
                            int triggerVarValue1;
                            alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar1, out triggerVarValue1);
                            int triggerVarValue2;
                            alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar2, out triggerVarValue2);
                            int triggerVarValue3;
                            alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar3, out triggerVarValue3);
                            int triggerVarValue4;
                            alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar4, out triggerVarValue4);

                            //string token = triggerVarValue.ToString();
                            List<string> tokens = new List<string>();
                            tokens.Add(triggerVarValue.ToString());
                            tokens.Add(triggerVarValue1.ToString());
                            tokens.Add(triggerVarValue2.ToString());
                            tokens.Add(triggerVarValue3.ToString());
                            tokens.Add(triggerVarValue4.ToString());

                            bool rebuild = false;
                            foreach (string t in tokens)
                            {
                                if (!_tokens.Contains(t))
                                {
                                    //Logger.AddLog("Rebuild triggers", ProjectServiceExtension.cLogFile);
                                    rebuild = true;
                                    break;
                                }
                            }

                            if (rebuild)
                            {
                                _tokens.Clear();
                                Logger.AddLog("TOKENS CLEAR", ProjectServiceExtension.cLogFile);
                                //string token = triggerVarValue.ToString("00000");
                                List<FuncVarBond> triggers = new List<FuncVarBond>();

                                foreach (string actualTrigger in tokens)
                                {
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
                                    //ocMultiTriggerContainer.TriggerVar = triggerVar;

                                    _tokens.Add(actualTrigger);
                                    Logger.AddLog($"TOKENS ADD {triggerVar}", ProjectServiceExtension.cLogFile);
                                    FuncVarBond fvb = new FuncVarBond(triggerVar, () =>
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
                                    triggers.Add(fvb);
                                }
                                ocMultiTriggerContainer.Triggers = triggers;
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
                _tokens = new List<string>();
                if (_alarmNaming == "KABO")
                {
                    for (int i = _alarmFirstVarNo; i < _alarmLastVarNo; i++)
                    {
                        alarms.Add($"xInt_Exceptions{i.ToString()}");
                    }
                    //xInt_Exceptions16000 - xInt_Exceptions16480
                    //alarm = $"xInt_Exceptions00000"; //fake var name just to create the container
                }
                else // "BO"
                {
                    for (int i = _alarmFirstVarNo; i < _alarmLastVarNo; i++)
                    {
                        alarms.Add($"{_BOStyleAddress}.Application.P_HmiError.P_HmiMsg[{i.ToString()}]");
                    }
                    //xInt_Exceptions16000 - xInt_Exceptions16480
                }

                ocMultiTriggerContainer = new OcMultiTriggerContainer(_Zenon, alarmContainerName, new List<FuncVarBond>());
                ocMultiTriggerContainer.VarList = alarms;
                ocMultiTriggerContainer.CreateContainer();
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