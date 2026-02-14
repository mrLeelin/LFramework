using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS && UNITY_EDITOR
using UnityEditor.iOS.Xcode;
#endif

using Debug = UnityEngine.Debug;


public class ProjectBuilder_iOS_ProcressBuild
{
#if UNITY_IOS

    [MenuItem("Tool/TestPod")]
    public static void testPod()
    {
        RunPodInstall(
            "/Users/daxingxing/.jenkins/workspace/BuildPartyGame/JellybeanUnity/Assets/../Builds/IOS/Project");
    }

    private const int ProjectBuilder_iOS_ProcressBuildPriority = 80;

    [PostProcessBuild(ProjectBuilder_iOS_ProcressBuildPriority)]
    public static void IOSBuildPostProcess(BuildTarget target, string pathToBuiltProject)
    {
        //修复 Facebook deep link 造成崩溃
        FixColdStartFacebook(pathToBuiltProject); // Call this function from your IOSBuildPostProcess 
        //修复 apple face book deep link Unity 原生收不到
        FixColdStartDeeplink(pathToBuiltProject);
        //修复 Firebase + Facebook 造成崩溃
        FixUseFrameworksBug(pathToBuiltProject);
    }

    /// <summary>
    /// 最后一部手动执行
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pathToBuiltProject"></param>
    [PostProcessBuild(Int32.MaxValue)]
    public static void RunPod(BuildTarget target, string pathToBuiltProject)
    {
        //手动执行 pod文件
        RunPodInstall(pathToBuiltProject);
    }


    private const string IsBackgroundLaunchOptions =
        @"(?x)(isBackgroundLaunchOptions.+(?:.*\n)+?\s*return\ )YES(\;\n\})# }";

    private static void FixColdStartFacebook(string path)
    {
        string fullPath = Path.Combine(path, Path.Combine("Classes", "UnityAppController.mm"));
        string data = Load(fullPath);

        data = Regex.Replace(
            data,
            IsBackgroundLaunchOptions,
            "$1NO$2");

        Save(fullPath, data);
    }

    private static string Load(string fullPath)
    {
        string data;
        FileInfo projectFileInfo = new FileInfo(fullPath);
        StreamReader fs = projectFileInfo.OpenText();
        data = fs.ReadToEnd();
        fs.Close();

        return data;
    }

    private static void Save(string fullPath, string data)
    {
        System.IO.StreamWriter writer = new System.IO.StreamWriter(fullPath, false);
        writer.Write(data);
        writer.Close();
    }


    private static void FixUseFrameworksBug(string pathToBuiltProject)
    {
        string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");
        if (!File.Exists(podfilePath))
        {
            return;
        }
        string content = File.ReadAllText(podfilePath);
        // 使用正则表达式替换 use_frameworks! :linkage => :static 为 use_frameworks!
        string pattern = @"use_frameworks!\s*:linkage\s*=>\s*:static";
        string replaced = Regex.Replace(content, pattern, "use_frameworks!");
        replaced += "\n";
        replaced +=
            "# 在安装 CocoaPods 依赖时，抑制未使用 Master Specs 仓库的警告\ninstall! 'cocoapods', :warn_for_unused_master_specs_repo => false\n";
        File.WriteAllText(podfilePath, replaced);
    }

    /// <summary>
    /// 修复Deeplink 
    /// </summary>
    /// <param name="pathToBuiltProject"></param>
    private static void FixColdStartDeeplink(string pathToBuiltProject)
    {
        //自定义 MyAppController.mm
        //继承 UnityAppController
        //实现Deeplinks 储存
        const string CustomAppControllerName = "MyAppController";

        // 1. 修改 Info.plist
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;
        rootDict.SetString("UnityAppControllerClass", "MyAppController");
        //AppsFlyer插件启用 Swizzle
        rootDict.SetBoolean("AppsFlyerShouldSwizzle", true);

        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log("✅ Set UnityAppControllerClass=MyAppController in Info.plist.");

        // 2. 可选：修改 main.mm（保险一点）
        var mainMMPath = Path.Combine(pathToBuiltProject, "Classes/main.mm");

        if (File.Exists(mainMMPath))
        {
            string content = File.ReadAllText(mainMMPath);
            string pattern = @"const char\* AppControllerClassName\s*=\s*""UnityAppController"";";
            string replacement = $@"const char* AppControllerClassName = ""{CustomAppControllerName}"";";

            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
                File.WriteAllText(mainMMPath, content);

                UnityEngine.Debug.Log(
                    "✅ Updated AppControllerClassName to use GetAppControllerClassName() in main.mm.");
            }
        }
    }


    /// <summary>
    /// 使用脚本执行 Pod install 
    /// </summary>
    /// <param name="path"></param>
    private static void RunPodInstall(string path)
    {
        var podFilePath = Path.Combine(path, "Podfile");
        Debug.Log($"Pod file {podFilePath}");
        if (!File.Exists(podFilePath))
        {
            Debug.LogError("None Podfile");
        }

        string shPath = Path.Combine(path, "podinstall.sh");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#!/bin/sh");
        sb.AppendLine("export LANG=en_US.UTF-8");
        sb.AppendLine("cd " + path);
        sb.AppendLine("/opt/homebrew/bin/pod install");
        using FileStream file = new FileStream(shPath, FileMode.Create);
        var bts = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        file.Write(bts, 0, bts.Length);
        file.Flush();


        //执行权限shell        
        Process chmodProcess = new Process();
        chmodProcess.StartInfo.FileName = "chmod";
        chmodProcess.StartInfo.Arguments = "+x podinstall.sh";
        chmodProcess.StartInfo.CreateNoWindow = true;
        chmodProcess.StartInfo.UseShellExecute = false;
        chmodProcess.StartInfo.WorkingDirectory = path;

        // Start the process
        chmodProcess.Start();
        chmodProcess.WaitForExit();
        chmodProcess.Close();

        //执行生成 shell
        Process process = new Process();
        process.StartInfo.FileName = "sh";
        process.StartInfo.Arguments = "podinstall.sh";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        // 启用标准输出重定向
        process.StartInfo.RedirectStandardError = true;
        // 启用错误输出重定向
        process.StartInfo.WorkingDirectory = path;


        // Start the process
        process.Start();
        // Read the output and error streams (optional)
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        // Wait for the process to exit
        process.WaitForExit();

        // Print the output and error messages
        UnityEngine.Debug.Log("Output: " + output);
        UnityEngine.Debug.LogError("Error: " + error);

        // Close the process
        process.Close();
    }


#endif
}