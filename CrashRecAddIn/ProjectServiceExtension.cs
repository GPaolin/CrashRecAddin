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
 *  Added MultiTRIGGER 
 *  
 *  Versione 1.3.0 - 08 / 07 / 2022
 *  Added UNITY Management
 *  
 *  
 *  Versione 1.3.1 - 11 / 11 / 2022
 *  Recompiled with new ZenAddInLibrary
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
        
        public const string AddInVersion = "1.3.1";
        public const string cLogFile = "CrashRecAddIn_LOG.txt";

        public bool _CycleMonitorEnable = true;
        public bool _CrashRecordinEnable = true;

        public string folderRepos = @"C:\Romaco\Prog\Data\CrashRecorder";
        //public string waitForVideo = "waitForVideo.txt";
        public string triggerFile = "triggerFile.txt";

        private OcValueContainer alarmTriggerVarContainer;
        private string alarmTriggerVarContainerName = "AlarmTriggerVarContainer";

        private int _filterTriggerMilliSeconds = 1000;

        OcMultiTriggerContainer ocMultiTriggerContainer;
        private string alarmContainerName = "AlarmContainer";

        private string _pathConfigFile = "CrashRecConfig.txt";
        private string AlarmCodeTriggerVar = "bINT_CrashRecorder_AlarmTriggerCode";
        private string AlarmCodeTriggerVar1 = "bINT_CrashRecorder_AlarmTriggerCode1";
        private string AlarmCodeTriggerVar2 = "bINT_CrashRecorder_AlarmTriggerCode2";
        private string AlarmCodeTriggerVar3 = "bINT_CrashRecorder_AlarmTriggerCode3";
        private string AlarmCodeTriggerVar4 = "bINT_CrashRecorder_AlarmTriggerCode4";

        //UNITY
        private string AlarmCodeTriggerVar_BM = "bINT_CrashRecorder_AlarmTriggerCode_BM";
        private string AlarmCodeTriggerVar1_BM  = "bINT_CrashRecorder_AlarmTriggerCode1_BM";
        private string AlarmCodeTriggerVar2_BM  = "bINT_CrashRecorder_AlarmTriggerCode2_BM";
        private string AlarmCodeTriggerVar3_BM  = "bINT_CrashRecorder_AlarmTriggerCode3_BM";
        private string AlarmCodeTriggerVar4_BM  = "bINT_CrashRecorder_AlarmTriggerCode4_BM";

        private string AlarmCodeTriggerVar_CM = "bINT_CrashRecorder_AlarmTriggerCode_CM";
        private string AlarmCodeTriggerVar1_CM = "bINT_CrashRecorder_AlarmTriggerCode1_CM";
        private string AlarmCodeTriggerVar2_CM = "bINT_CrashRecorder_AlarmTriggerCode2_CM";
        private string AlarmCodeTriggerVar3_CM = "bINT_CrashRecorder_AlarmTriggerCode3_CM";
        private string AlarmCodeTriggerVar4_CM = "bINT_CrashRecorder_AlarmTriggerCode4_CM";

        private string AlarmCodeTriggerVar_Main = "bINT_CrashRecorder_AlarmTriggerCode_Main";
        private string AlarmCodeTriggerVar1_Main = "bINT_CrashRecorder_AlarmTriggerCode1_Main";
        private string AlarmCodeTriggerVar2_Main = "bINT_CrashRecorder_AlarmTriggerCode2_Main";
        private string AlarmCodeTriggerVar3_Main = "bINT_CrashRecorder_AlarmTriggerCode3_Main";
        private string AlarmCodeTriggerVar4_Main = "bINT_CrashRecorder_AlarmTriggerCode4_Main";
        //END UNITY

        private int _alarmFirstVarNo = 16001;
        private int _alarmLastVarNo = 16480;
        private string _alarmNaming = "KABO";
        private string _BOStyleAddress = "[0096]";
        private bool _unity = false; //UNITY di merda di KA

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
                    Logger.AddLog($"AlarmFirstVar: " + _alarmFirstVarNo.ToString(), ProjectServiceExtension.cLogFile);



                    Logger.AddLog($"Looking for setting: AlarmLastVar", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("AlarmLastVar|"));
                    string alarmLastVar = line.Split(new char[] { '|' })[1];
                    _alarmLastVarNo = Convert.ToInt32(alarmLastVar);
                    Logger.AddLog($"AlarmLastVar: " + _alarmLastVarNo.ToString(), ProjectServiceExtension.cLogFile);

                    Logger.AddLog($"Looking for setting: AlarmNaming", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("AlarmNaming|"));
                    _alarmNaming = line.Split(new char[] { '|' })[1];
                    Logger.AddLog($"AlarmNaming: " + _alarmNaming.ToString(), ProjectServiceExtension.cLogFile);

                    Logger.AddLog($"Looking for setting: BOStyleAddress", ProjectServiceExtension.cLogFile);
                    line = File.ReadLines(pathConfigFile).FirstOrDefault(s => s.StartsWith("Address|"));
                    _BOStyleAddress = line.Split(new char[] { '|' })[1];
                    Logger.AddLog($"BOStyleAddress: " + _BOStyleAddress.ToString(), ProjectServiceExtension.cLogFile);

                    _unity = (_alarmNaming == "UNITY");
                    Logger.AddLog($"UNITY: " + _unity.ToString(), ProjectServiceExtension.cLogFile);
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

                Logger.AddLog($"Unity condition {_unity.ToString()}", ProjectServiceExtension.cLogFile);
                if (_unity)
                {
                    vars.Add(AlarmCodeTriggerVar_BM);
                    vars.Add(AlarmCodeTriggerVar1_BM);
                    vars.Add(AlarmCodeTriggerVar2_BM);
                    vars.Add(AlarmCodeTriggerVar3_BM);
                    vars.Add(AlarmCodeTriggerVar4_BM);
                    vars.Add(AlarmCodeTriggerVar_CM);
                    vars.Add(AlarmCodeTriggerVar1_CM);
                    vars.Add(AlarmCodeTriggerVar2_CM);
                    vars.Add(AlarmCodeTriggerVar3_CM);
                    vars.Add(AlarmCodeTriggerVar4_CM);
                    vars.Add(AlarmCodeTriggerVar_Main);
                    vars.Add(AlarmCodeTriggerVar1_Main);
                    vars.Add(AlarmCodeTriggerVar2_Main);
                    vars.Add(AlarmCodeTriggerVar3_Main);
                    vars.Add(AlarmCodeTriggerVar4_Main);
                }
                else
                {
                    vars.Add(AlarmCodeTriggerVar);
                    vars.Add(AlarmCodeTriggerVar1);
                    vars.Add(AlarmCodeTriggerVar2);
                    vars.Add(AlarmCodeTriggerVar3);
                    vars.Add(AlarmCodeTriggerVar4);
                }

                alarmTriggerVarContainer = new OcValueContainer(_Zenon, alarmTriggerVarContainerName, vars);
                //alarmTriggerVarContainer.CreateContainer();
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
                Logger.AddLog("Run control cycle", ProjectServiceExtension.cLogFile);

                Task.Run(() =>
                {
                    while (_CycleMonitorEnable)
                    {
                        try 
                        {
                            //string token = triggerVarValue.ToString();
                            List<string> tokens = new List<string>();

                            if (_unity)
                            {
                                int triggerVarValue_BM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar_BM, out triggerVarValue_BM);
                                Logger.AddLog($"{triggerVarValue_BM}", ProjectServiceExtension.cLogFile);
                                int triggerVarValue1_BM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar1_BM, out triggerVarValue1_BM);
                                Logger.AddLog($"{triggerVarValue1_BM}", ProjectServiceExtension.cLogFile); 
                                int triggerVarValue2_BM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar2_BM, out triggerVarValue2_BM);
                                Logger.AddLog($"{triggerVarValue2_BM}", ProjectServiceExtension.cLogFile);
                                int triggerVarValue3_BM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar3_BM, out triggerVarValue3_BM);
                                Logger.AddLog($"{triggerVarValue3_BM}", ProjectServiceExtension.cLogFile);
                                int triggerVarValue4_BM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar4_BM, out triggerVarValue4_BM);
                                Logger.AddLog($"{triggerVarValue4_BM}", ProjectServiceExtension.cLogFile);

                                int triggerVarValue_CM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar_CM, out triggerVarValue_CM);
                                int triggerVarValue1_CM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar1_CM, out triggerVarValue1_CM);
                                int triggerVarValue2_CM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar2_CM, out triggerVarValue2_CM);
                                int triggerVarValue3_CM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar3_CM, out triggerVarValue3_CM);
                                int triggerVarValue4_CM;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar4_CM, out triggerVarValue4_CM);

                                int triggerVarValue_Main;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar_Main, out triggerVarValue_Main);
                                int triggerVarValue1_Main;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar1_Main, out triggerVarValue1_Main);
                                int triggerVarValue2_Main;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar2_Main, out triggerVarValue2_Main);
                                int triggerVarValue3_Main;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar3_Main, out triggerVarValue3_Main);
                                int triggerVarValue4_Main;
                                alarmTriggerVarContainer.GetValue(AlarmCodeTriggerVar4_Main, out triggerVarValue4_Main);

                                tokens.Add("BM__" + triggerVarValue_BM.ToString("00000"));
                                tokens.Add("BM__" + triggerVarValue1_BM.ToString("00000"));
                                tokens.Add("BM__" + triggerVarValue2_BM.ToString("00000"));
                                tokens.Add("BM__" + triggerVarValue3_BM.ToString("00000"));
                                tokens.Add("BM__" + triggerVarValue4_BM.ToString("00000"));
                                tokens.Add("CM__" + triggerVarValue_CM.ToString("00000"));
                                tokens.Add("CM__" + triggerVarValue1_CM.ToString("00000"));
                                tokens.Add("CM__" + triggerVarValue2_CM.ToString("00000"));
                                tokens.Add("CM__" + triggerVarValue3_CM.ToString("00000"));
                                tokens.Add("CM__" + triggerVarValue4_CM.ToString("00000"));
                                tokens.Add("Main__" + triggerVarValue_Main.ToString("00000"));
                                tokens.Add("Main__" + triggerVarValue1_Main.ToString("00000"));
                                tokens.Add("Main__" + triggerVarValue2_Main.ToString("00000"));
                                tokens.Add("Main__" + triggerVarValue3_Main.ToString("00000"));
                                tokens.Add("Main__" + triggerVarValue4_Main.ToString("00000"));
                            }
                            else
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


                                tokens.Add(triggerVarValue.ToString());
                                tokens.Add(triggerVarValue1.ToString());
                                tokens.Add(triggerVarValue2.ToString());
                                tokens.Add(triggerVarValue3.ToString());
                                tokens.Add(triggerVarValue4.ToString());
                            }

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
                                    if (_unity)
                                    {
                                        string dummyTrigger = actualTrigger.Remove(0, 4);
                                        if (actualTrigger.StartsWith("BM__"))
                                        {
                                            triggerVar = $"xInt_BM_Exceptions{actualTrigger}";
                                        }
                                        if (actualTrigger.StartsWith("CM__"))
                                        {
                                            triggerVar = $"xInt_CA_Exceptions{actualTrigger}";
                                        }
                                        if (actualTrigger.StartsWith("Main__"))
                                        {
                                            triggerVar = $"xInt_Main_Exceptions{actualTrigger}";
                                        }
                                    }
                                    else
                                    {
                                        if (_alarmNaming == "KABO")
                                        {
                                            triggerVar = $"xInt_Exceptions{actualTrigger}";
                                        }
                                        else
                                        {
                                            //BO
                                            triggerVar = $"{_BOStyleAddress}.Application.P_HmiError.P_HmiMsg[{actualTrigger}]";
                                        } 
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
                            //throw;
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
                if (_unity)
                {
                    Logger.AddLog("Add unity alarm variables", ProjectServiceExtension.cLogFile);
                    for (int i = 1; i <= 5056; i++)
                    {
                        alarms.Add($"xInt_BM_Exceptions{i.ToString("00000")}");
                    }
                    for (int i = 1; i <= 1024; i++)
                    {
                        alarms.Add($"xInt_CA_Exceptions{i.ToString("00000")}");
                    }
                    for (int i = 12001; i <= 13536; i++)
                    {
                        alarms.Add($"xInt_CA_Exceptions{i.ToString("00000")}");
                    }
                    for (int i = 1; i <= 1024; i++)
                    {
                        alarms.Add($"xInt_Main_Exceptions{i.ToString("00000")}");
                    }
                    Logger.AddLog("Added unity alarm variables", ProjectServiceExtension.cLogFile);
                }
                else
                { 
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
                }

                Logger.AddLog("Creating container", ProjectServiceExtension.cLogFile);
                ocMultiTriggerContainer = new OcMultiTriggerContainer(_Zenon, alarmContainerName, new List<FuncVarBond>());
                //ocMultiTriggerContainer.VarList = alarms;
                ocMultiTriggerContainer.AddVarList(alarms);

                //ocMultiTriggerContainer.CreateContainer();
                Logger.AddLog("Created container", ProjectServiceExtension.cLogFile);
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