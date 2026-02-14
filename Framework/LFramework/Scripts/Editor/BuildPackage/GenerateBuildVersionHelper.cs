using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Editor
{
    public static class GenerateBuildVersionHelper
    {
        private static readonly string Template = "using System.Collections;\n" +
                                                  "using System.Collections.Generic;\n" +
                                                  "using GameFramework;\n" +
                                                  "using UnityEngine;\n" +
                                                  "\n" +
                                                  "namespace LFramework.Runtime\n" +
                                                  "{\n" +
                                                  "    public class BuildVersionHelper : Version.IVersionHelper\n" +
                                                  "    {\n" +
                                                  "        public string GameVersion => Application.dataPath;\n" +
                                                  "        public int InternalGameVersion => #InternalGameVersion#;\n" +
                                                  "        \n" +
                                                  "    }\n" +
                                                  "}\n";
        
        
        public static void Generate(int internalGameVersion)
        {
            string path = Application.dataPath + "/ThirdParty/Framework/LFramework/Scripts/Runtime/Helper/BuildVersionHelper.cs";
            string allText = Template.Replace("#InternalGameVersion#", internalGameVersion.ToString());
            System.IO.File.WriteAllText(path, allText);
        }
    }
}

