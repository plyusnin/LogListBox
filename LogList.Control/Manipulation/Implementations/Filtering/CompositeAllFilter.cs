using System.Collections.Generic;
using System.Linq;

namespace LogList.Control.Manipulation.Implementations.Filtering
{
    public class CompositeAllFilter<TItem> : IFilter<TItem>
        where TItem : ILogItem
    {
        private ICollection<IFilter<TItem>> _children;
        
        public CompositeAllFilter(ICollection<IFilter<TItem>> Children)
        {
            _children = Children;
        }

        public bool Check(TItem Item)
        {
            return _children.All(f => f.Check(Item));
        }

        public bool IsSubFilterFor(IFilter<TItem> Another)
        {
            if (Another == this)
                return true;
            
            switch (Another)
            {
                case EmptyFilter<TItem> empty:
                    return true;
                
                case CompositeAllFilter<TItem> composite:
                    return composite._children.All(another => _children.Any(our => our.IsSubFilterFor(another)));
                
                default:
                    return false;
            }
        }
    }
}