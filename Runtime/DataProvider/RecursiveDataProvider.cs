using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public class RecursiveDataProvider : DataProviderBase, IDataReceiver
    {
        public override IDictionary<string, object> GetData()
        {
            var src = GetComponent<DynamicPrefab>();
            if (src)
            {
                return src.ChildData;
            }
            return null;
        }

        public void Receive(IDictionary<string, object> data)
        {
            var trans = transform;
            while (trans)
            {
                var parent = trans.parent;
                if (parent)
                {
                    var par = parent.GetComponent<DynamicPrefab>();
                    if (par)
                    {
                        if (par.DynamicChild == trans.gameObject)
                        {
                            par.SyncChildData();
                            break;
                        }
                    }
                }
                trans = parent;
            }
        }
    }
}