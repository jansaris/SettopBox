using System.Collections.Generic;

namespace WebUi.api.Logging
{
    public class LimitedList<T> : List<T>
    {
        readonly int _maxLength;

        public LimitedList(int max)
        {
            _maxLength = max;
        } 

        public new void Add(T item)
        {
            if(Count > _maxLength) RemoveAt(0);
            base.Add(item);
        }
    }
}