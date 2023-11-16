using System.Collections.Generic;

namespace MechJebLib.Utils
{
    public class DictOfLists<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _dict;

        public DictOfLists(int capacity)
        {
            _dict = new Dictionary<TKey, List<TValue>>(capacity);
        }

        public List<TValue> this[TKey key]
        {
            get
            {
                if (_dict.TryGetValue(key, out List<TValue> val))
                    return val;

                _dict.Add(key, val = new List<TValue>());
                return val;
            }
        }

        public void Clear()
        {
            // careful:  not every value in this dict is always valid and we don't clear them out here.
            // not implemeting IDictionary and the ability to iterate over Keys is deliberate.
            foreach (List<TValue> list in _dict.Values)
                list.Clear();
        }

        public int Count(TKey key) => _dict[key].Count;
    }
}
