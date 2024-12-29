using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Jc.MediaImporter.Models;

public class SortedObservableCollection<T> : ObservableCollection<T>
{
    public SortedObservableCollection(IEnumerable<T> collection) : base(collection)
    {
    }
    
    public Func<T, object>? SortingSelector { get; init; }
    public bool Descending { get; init; }
    
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        if (SortingSelector is null
            || e.Action == NotifyCollectionChangedAction.Remove
            || e.Action == NotifyCollectionChangedAction.Reset)
        {
            return;
        }
                      
        var query = this
            .Select((item, index) => (Item: item, Index: index));
        query = Descending
            ? query.OrderByDescending(tuple => SortingSelector(tuple.Item))
            : query.OrderBy(tuple => SortingSelector(tuple.Item));

        var map = query.Select((tuple, index) => (OldIndex:tuple.Index, NewIndex:index))
            .Where(o => o.OldIndex != o.NewIndex);

        using var enumerator = map.GetEnumerator();
        if (enumerator.MoveNext())
        {
            Move(enumerator.Current.OldIndex, enumerator.Current.NewIndex);
        }
    }
}