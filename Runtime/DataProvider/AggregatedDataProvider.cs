using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public class AggregatedDataProvider : DataProviderBase
    {
        public List<DataProviderBase> Children;
        public override IDictionary<string, object> GetData()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Children != null)
            {
                for (int i = 0; i < Children.Count; ++i)
                {
                    var child = Children[i];
                    if (child && child != this)
                    {
                        var part = child.GetData();
                        if (part != null)
                        {
                            foreach (var kvp in part)
                            {
                                result[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}