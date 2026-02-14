using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public enum UpdateResultType
    {
        Successful,
        NoneDownload = 1,
        
        CheckCatalogsFailure,

        //没有网络
        NotReachable,
        GetDownloadSizeFailure,
       
    }
}

