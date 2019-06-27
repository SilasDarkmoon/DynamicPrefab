using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public abstract class DataProviderBase : MonoBehaviour
    {
        public abstract IDictionary<string, object> GetData();
    }

    public class DataProvider : DataProviderBase
    {
        public DataDictionary Data;
        public override IDictionary<string, object> GetData()
        {
            return Data;
        }
    }
}