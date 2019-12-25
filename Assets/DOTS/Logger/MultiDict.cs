using System;
using System.Collections.Generic;

namespace DOTS.Logger
{
    [Serializable]
    public class MultiDict<TKey, TValue>
    {
        private Dictionary<TKey, List<TValue>> _data =  new Dictionary<TKey,List<TValue>>();

        public void Add(TKey k, TValue v)
        {
            if (_data.ContainsKey(k))
                _data[k].Add(v);
            else
                _data.Add(k, new List<TValue> {v});
        }
    }
}