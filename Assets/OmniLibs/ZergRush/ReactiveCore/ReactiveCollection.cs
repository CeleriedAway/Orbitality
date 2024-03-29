using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TWL;
using Debug = ServerEngine.Debug;

namespace ZergRush.ReactiveCore
{
    public enum ReactiveCollectionEventType : byte
    {
        Reset,
        Insert,
        Remove,
        Set
    }

    public class ReactiveCollectionEvent
    {
        public ReactiveCollectionEventType type;
        public int position;
    }

    public class ReactiveCollectionEvent<T> : ReactiveCollectionEvent
    {
        public T newItem;
        public T oldItem;
        public IEnumerable<T> oldData;
        public IEnumerable<T> newData;
    }
    
    /*
          Reactive collection abstraction.
          Main usecase is presentation of some collections of data in tables.
     */

    public interface IReactiveCollection<T> : IEnumerable<T>
    {
        IEventReader<ReactiveCollectionEvent<T>> update { get; }
        List<T> current { get; }
    }

    [DebuggerDisplay("{this.ToString()}")]
    public class ReactiveCollection<T> : IReactiveCollection<T>, IList<T>, IConnectable
// Proposition: rename to ReactiveList. Reactive collection is too general. Also I want ReactiveDictionary.
    {
        protected EventStream<ReactiveCollectionEvent<T>> up;
        protected List<T> data;

        public List<T> current
        {
            get { return data; }
            set { Reset(value); }
        }

        public ReactiveCollection()
        {
            this.data = new List<T>();
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public ReactiveCollection(IEnumerable<T> list)
        {
            this.data = list.ToList();
        }

        public ReactiveCollection(List<T> list)
        {
            this.data = list;
        }

        public IEventReader<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < data.Count + arrayIndex; i++)
            {
                array[i] = data[i - arrayIndex];
            }
        }

        public void Add(T item)
        {
            data.Add(item);
            OnItemAdded(item, up, data);
        }
        public static void OnItemAdded(T item, EventStream<ReactiveCollectionEvent<T>> up, List<T> data)
        {
            OnItemInserted(item, up, data.Count - 1);
        }
        public static void OnItemInserted(T item, EventStream<ReactiveCollectionEvent<T>> up, int index)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Insert,
                    newItem = item,
                    position = index,
                });
        }


        public int IndexOf(T item)
        {
            return data.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            data.Insert(index, item);
            OnItemInserted(item, up, index);
        }

        public bool Remove(T item)
        {
            var index = data.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
        
        public void Clear()
        {
            Reset(new List<T>());
        }

        public int RemoveAll(Func<T, bool> predicate)
        {
            int removedCounter = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var item = data[i];
                if (predicate(item))
                {
                    removedCounter++;
                    RemoveAt(i);
                }
            }

            return removedCounter;
        }

        public void RemoveRange(int ind, int count)
        {
            for (int i = 0; i < count; i++)
                RemoveAt(ind);
        }

        public void RemoveAt(int index)
        {
            var item = data[index];
            data.RemoveAt(index);
            OnItemRemovedAt(index, up, item);
        }
        
        public void Reset(List<T> newData)
        {
            var oldData = data;
            data = newData;
            OnItemsReset(newData, oldData, up);
        }
        
        public static void OnItemRemovedAt(int index, EventStream<ReactiveCollectionEvent<T>> up, T item)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Remove,
                    position = index,
                    oldItem = item
                });
        }
        
        public void Reset(IEnumerable<T> val = null)
        {
            var oldData = data;
            data = val != null ? new List<T>(val) : new List<T>();
            if (oldData.Count == 0 && data.Count == 0) return;

            OnItemsReset(data, oldData, up);
        }

        public T this[int index]
        {
            get { return data[index]; }
            set
            {
                var oldItem = data[index];
                data[index] = value;
                OnItemSet(index, value, oldItem, up);
            }
        }

        public int Capacity
        {
            get { return data.Capacity; }
            set { data.Capacity = value; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return current.PrintCollection();
        }
        
        public int Count
        {
            get { return data.Count; }
        }

        public bool IsReadOnly { get; }
        public int getConnectionCount => up == null ? 0 : up.getConnectionCount;

        public static void OnItemsReset(List<T> newData, List<T> oldData, EventStream<ReactiveCollectionEvent<T>> up)
        {
            if (up != null)
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Reset,
                    oldData = oldData,
                    newData = newData
                });
        }

        public static void OnItemSet(int index, T newItem, T oldItem, EventStream<ReactiveCollectionEvent<T>> up)
        {
            if (up != null)
            {
                up.Send(new ReactiveCollectionEvent<T>
                {
                    type = ReactiveCollectionEventType.Set,
                    position = index,
                    newItem = newItem,
                    oldItem = oldItem
                });
            }
        }
    }

    public abstract class AbstractCollectionTransform<T> : IReactiveCollection<T>
    {
        int connectionCounter = 0;
        IDisposable collectionConnection;
        
        protected readonly ReactiveCollection<T> buffer = new ReactiveCollection<T>();
        
        bool connected
        {
            get { return connectionCounter != 0; }
        }

        void OnConnect()
        {
            if (connectionCounter == 0)
            {
                RefillBuffer();
                collectionConnection = StartListen(); 
            }
            //Debug.Log($"connection counter increased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            connectionCounter++;
        }

        protected abstract IDisposable StartListen();

        void OnDisconnect()
        {
            connectionCounter--;
            //Debug.Log($"connection counter decreased to {connectionCounter} bufferCounter {buffer.connectionCount}");
            if (connectionCounter == 0)
            {
                if (buffer.getConnectionCount != 0)
                {
                    //Debug.LogError("WTF is that");
                    throw new Exception("this should not happen");
                }
                collectionConnection.Dispose();
                collectionConnection = null;
                ClearBuffer();
            }
        }

        void ClearBuffer()
        {
            buffer.Reset();
        }

        protected void RefillBuffer()
        {
            Refill();
        }
        
        protected abstract void Refill();
        
        public IEnumerator<T> GetEnumerator()
        {
            return current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEventReader<ReactiveCollectionEvent<T>> updateWrapp;

        public IEventReader<ReactiveCollectionEvent<T>> update
        {
            get
            {
                return updateWrapp ?? (updateWrapp = new AnonymousEventReader<ReactiveCollectionEvent<T>>(act =>
                {
                    OnConnect();
                    var connection = buffer.update.Subscribe(act);
                    return new AnonymousDisposable(() =>
                    {
                        connection.Dispose();
                        OnDisconnect();
                    });
                }));
            }
        }

        public List<T> current
        {
            get 
            { 
                if (!connected) RefillBuffer();
                return buffer.current;
            }
        }

        public override string ToString()
        {
            return current.PrintCollection();
        }
    }

    public class SingleElementCollection<T> : ICell<T>, IReactiveCollection<T>
    {
        //[SerializeField]
        private T val;
        [NonSerialized] private EventStream<T> upVal;
        [NonSerialized] EventStream<ReactiveCollectionEvent<T>> up;

        public SingleElementCollection(T t)
        {
            val = t;
        }

        public SingleElementCollection()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, val) == false)
                {
                    var oldval = val;
                    val = value;
                    if (upVal != null) upVal.Send(val);
                    if (up != null) up.Send(new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Set,
                        oldItem = oldval,
                        newItem = value
                    });
                }
            }
        }

        public EventStream<T> updates
        {
            get { return upVal = upVal ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return value;
        }

        public IEventReader<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public List<T> current { get {return new List<T>{value};} }
    }
    
    public class SingleNullableElementCollection<T> : ICell<T>, IReactiveCollection<T> where T : class
    {
        //[SerializeField]
        private T val;
        [NonSerialized] private EventStream<T> upVal;
        [NonSerialized] EventStream<ReactiveCollectionEvent<T>> up;

        public SingleNullableElementCollection(T t)
        {
            val = t;
        }

        public SingleNullableElementCollection()
        {
        }

        public T value
        {
            get { return val; }
            set
            {
                if (object.ReferenceEquals(value, val) == false)
                {
                    var oldval = val;
                    val = value;
                    if (upVal != null) upVal.Send(val);
                    if (up != null) {
                        if (oldval == null)
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Insert,
                                newItem = val
                            });
                        }
                        else if (val == null)
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Remove,
                                oldItem = oldval,
                            });
                        }
                        else
                        {
                            up.Send(new ReactiveCollectionEvent<T> {
                                type = ReactiveCollectionEventType.Set,
                                oldItem = oldval,
                                newItem = val
                            });
                        }
                    }
                }
            }
        }

        public EventStream<T> updates
        {
            get { return upVal = upVal ?? new EventStream<T>(); }
        }

        public IDisposable ListenUpdates(Action<T> callback)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(callback);
        }

        public IDisposable OnChanged(Action action)
        {
            if (upVal == null) upVal = new EventStream<T>();
            return upVal.Subscribe(_ => action());
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public void SetValue(T v)
        {
            this.value = v;
        }
        

        public IEnumerator<T> GetEnumerator()
        {
            if (value == null) yield break;
            yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (value == null) yield break;
            yield return value;
        }

        public IEventReader<ReactiveCollectionEvent<T>> update
        {
            get { return up ?? (up = new EventStream<ReactiveCollectionEvent<T>>()); }
        }

        public List<T> current
        {
            get
            {
                var list = new List<T>();
                if (val != null) list.Add(val);
                return list;
            }
        }
    }
    
    class StaticCollection<T> : IReactiveCollection<T>
    {
        public List<T> list;

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEventReader<ReactiveCollectionEvent<T>> update
        {
            get { return AbandonedReader<ReactiveCollectionEvent<T>>.value; }
        }

        public List<T> current
        {
            get { return list; }
        }

        static readonly StaticCollection<T> def = new StaticCollection<T>{list = new List<T>()};
        public static IReactiveCollection<T> Empty()
        {
            return def;
        }
    }

    public static class ReactiveCollectionExtensions
    {

        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this List<T> coll)
        {
            return new StaticCollection<T> {list = coll};
        }
        
        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this IEnumerable<T> coll)
        {
            return new StaticCollection<T> {list = coll.ToList()};
        }

        public static List<T> ToList<T>(this ReactiveCollection<T> coll)
        {
            return coll.AsEnumerable().ToList();
        }

        public static ICell<int> CountCell<T>(this IReactiveCollection<T> coll)
        {
            return coll.AsCell().Map(c => c.Count);
        }

        public static IReactiveCollection<TMapped> Map<T, TMapped>(this IReactiveCollection<T> collection,
            Func<T, TMapped> mapFunc)
        {
            return new MappedCollection<T, TMapped>(collection, mapFunc);
        }
        
        public static ICell<bool> ContainsReactive<T>(this IReactiveCollection<T> collection,
            T item)
        {
            return collection.AsCell().Map(c => c.Contains(item));
        }

        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, bool> predicate)
        {
            return new FilteredCollection<T>(collection, predicate); 
        }
        
        // TODO Actually that is not difficult to implement this with simple filtered collection
        // but I forgot how
        public static IReactiveCollection<T> Filter<T>(this IReactiveCollection<T> collection,
            Func<T, ICell<bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
            return collection.update.Subscribe(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Set:
                        action(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in collection)
                        {
                            action(item);
                        }
                        break;
                }
            });
        }

        // Wont work well if collection has same elements multiple times
        public static IDisposable AffectEach<T>(this IReactiveCollection<T> collection, Action<IConnectionSink, T> affect) where T : class 
        {
            var itemConnectionsDict = new Dictionary<T, Connections>();

            collection.BindEach(item => {
                var itemConnections = new Connections();
                if (itemConnectionsDict.ContainsKey(item))
                {
                    Debug.LogError("it seems item is already loaded, this function wont work if elements repeated in the collection");
                    return;
                }
                affect(itemConnections, item);
                itemConnectionsDict[item] = itemConnections;
            }, item => {
                itemConnectionsDict.TakeKey(item).DisconnectAll();
            });

            return new AnonymousDisposable(() =>
            {
                foreach (var connections in itemConnectionsDict.Values) {
                    connections.DisconnectAll();
                }
            });
        }
        
        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> onInsert, Action<T> onRemove)
        {
            foreach (var item in collection)
            {
                onInsert(item);
            }
            return collection.update.Subscribe(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                        onInsert(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        onInsert(rce.newItem);
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in rce.oldData)
                        {
                            onRemove(item);
                        }
                        foreach (var item in rce.newData)
                        {
                            onInsert(item);
                        }
                        break;
                }
            });
        }
        
        public static IEventReader<T> MergeCollectionOfStreams<T>(this IReactiveCollection<IEventReader<T>> collection)
        {
            return new AnonymousEventReader<T>(action =>
            {
                var connections = new Connections();
                var disposable = new DoubleDisposable
                {
                    First = connections,
                };
                disposable.Second =
                    // TODO It can be done more effectively then asCell call but much more complex
                    collection.AsCell().Bind(coll =>
                    {
                        connections.DisconnectAll();
                        if (disposable.disposed) return;
                        connections.AddRange(coll.Select(item => item.Subscribe(action)));
                    });
                return disposable;
            });
        }

        public static ICell<T> AtIndex<T>(this IReactiveCollection<T> collection, int index)
        {
            return new AnonymousCell<T>(action =>
            {
                return collection.AsCell().ListenUpdates(coll =>
                {
                    action(coll.Count > index ? coll[index] : default(T));
                });
            }, () =>
            {
                var coll = collection.current;
                return coll.Count > index ? coll[index] : default(T);
            });
        }

        public static ICell<List<T>> AsCell<T>(this IReactiveCollection<T> collection)
        {
            return new AnonymousCell<List<T>>(action =>
            {
                return collection.update.Subscribe(_ => { action(collection.current); });
            }, () => collection.current);
        }

        public static IReactiveCollection<T> ToReactiveCollection<T>(this ICell<IEnumerable<T>> cell)
        {
            return new ReactiveCollectionFromCellOfArray<T>{cell = cell};    
        }

        /// <summary>
        /// TODO: Refactor this. Slow, but written fast.
        /// </summary>
        public static IReactiveCollection<T> Reverse<T>(this IReactiveCollection<T> original)
        {
            return original.AsCell().Map(list => list.GetReversed()).ToReactiveCollection();
        }

        public static IReactiveCollection<T> Join<T>(this ICell<IReactiveCollection<T>> cellOfCollection)
        {
            return new JoinCellOfCollection<T> {cellOfCollection = cellOfCollection};
        }
        
        public static IReactiveCollection<T> EnumerateRange<T>(this ICell<int> cellOfElemCount, Func<int, T> fill)
        {
            return new ReactiveRange<T> {fill = fill, cellOfCount = cellOfElemCount};
        }

        class ReactiveRange<T> : AbstractCollectionTransform<T>
        {
            public Func<int, T> fill;
            public ICell<int> cellOfCount;
            protected override IDisposable StartListen()
            {
                return cellOfCount.ListenUpdates(FillBuffer);
            }
            
            protected override void Refill()
            {
                FillBuffer(cellOfCount.value);
//                for (int i = 0; i < cellOfCount.value; i++)
//                {
//                    buffer.Add(fill(i));
//                }
            }

            void FillBuffer(int i)
            {
                while (buffer.Count != i)
                {
                    if (buffer.Count > i) buffer.RemoveAt(buffer.Count - 1);
                    else if (buffer.Count < i) buffer.Add(fill(buffer.Count));
                }
            }

        }

        class JoinCellOfCollection<T> : IReactiveCollection<T>
        {
            public ICell<IReactiveCollection<T>> cellOfCollection;
            public IEnumerator<T> GetEnumerator()
            {
                return cellOfCollection.value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEventReader<ReactiveCollectionEvent<T>> update
            {
                get
                {
                    var cellUpdates = cellOfCollection.BufferPreviousValue().Map(tuple => new ReactiveCollectionEvent<T>
                    {
                        type = ReactiveCollectionEventType.Reset,
                        newData = tuple.Item1,
                        oldData = tuple.Item2
                    });
                    return cellOfCollection.Map(coll => coll.update).Join().MergeWith(cellUpdates);
                }
            }

            public List<T> current
            {
                get { return cellOfCollection.value.current; }
            }
        }

        [DebuggerDisplay("{this.ToString()}")]
        public class MappedCollection<T, TMapped> : AbstractCollectionTransform<TMapped>
        {
            readonly Func<T, TMapped> mapFunc;
            readonly IReactiveCollection<T> collection;
            
            public MappedCollection(IReactiveCollection<T> collection, Func<T, TMapped> mapFunc)
            {
                this.collection = collection;
                this.mapFunc = mapFunc;
            }

            void Process(ReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillBuffer();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        var item = mapFunc(e.newItem);
                        buffer.Insert(e.position, item);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        buffer.RemoveAt(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        var newItem = mapFunc(e.newItem);
                        buffer[e.position] = newItem;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListen()
            {
                return collection.update.Subscribe(Process);
            }

            protected override void Refill()
            {
                buffer.Reset(collection.current.Select(mapFunc));
            }
        }
        
        [DebuggerDisplay("{this.ToString()}")]
        public class FilteredCollection<T> : AbstractCollectionTransform<T>
        {
            readonly Func<T, bool> predicate;
            readonly IReactiveCollection<T> collection;
            List<int> realIndexes = new List<int>();
            
            public FilteredCollection(IReactiveCollection<T> collection, Func<T, bool> predicate)
            {
                this.collection = collection;
                this.predicate = predicate;
            }

            void Insert(int realIndex, T item)
            {
                int newIndex = 0;
                if (realIndexes.Count > 0)
                {
                    newIndex = realIndexes.UpperBound(realIndex);
                    for (var i = newIndex; i < realIndexes.Count; ++i)
                    {
                        realIndexes[i]++;
                    }
                }
                if (predicate(item) == false) return;
                realIndexes.Insert(newIndex, realIndex);
                buffer.Insert(newIndex, item);
            }

            void Remove(int realIndex)
            {
                if (realIndexes.Count == 0) return;
                var oldIndex = realIndexes.BinarySearch(realIndex);
                for (var i = oldIndex >= 0 ? oldIndex : ~oldIndex; i < realIndexes.Count; ++i)
                {
                    realIndexes[i]--;
                }
                if (oldIndex < 0) return;
                realIndexes.RemoveAt(oldIndex);
                buffer.RemoveAt(oldIndex);
            }

            void Process(ReactiveCollectionEvent<T> e)
            {
                switch (e.type)
                {
                    case ReactiveCollectionEventType.Reset:
                        RefillBuffer();
                        break;
                    case ReactiveCollectionEventType.Insert:
                        Insert(e.position, e.newItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        Remove(e.position);
                        break;
                    case ReactiveCollectionEventType.Set:
                        //TODO make proper set event resolve if needed
                        Remove(e.position); 
                        Insert(e.position, e.newItem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected override IDisposable StartListen()
            {
                return collection.update.Subscribe(Process);
            }

            protected override void Refill()
            {
                realIndexes.Clear();
                var coll = collection.current;
                for (int i = 0; i < coll.Count; i++)
                {
                    var item = coll[i];
                    if (predicate(item))
                    {
                        realIndexes.Add(i);
                    }
                }
                buffer.Reset(coll.Where(predicate));
            }
        }
        
        class ReactiveCollectionFromCellOfArray<T> : AbstractCollectionTransform<T>
        {
            public ICell<IEnumerable<T>> cell;
            protected override IDisposable StartListen()
            {
                return cell.ListenUpdates(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    // TODO make smarter algorithm later
                    buffer.Reset(coll);
                    
                    // This algorithm does not work on simple types and same items in collection
//                    var newItems = coll as T[] ?? coll.ToArray();
//                    for (var index = 0; index < newItems.Length; index++)
//                    {
//                        var item = newItems[index];
//                        if (buffer.Contains(item)) continue;
//                        buffer.Add(item);
//                    }
//
//                    for (var index = buffer.Count - 1; index >= 0; index--)
//                    {
//                        var oldItem = buffer[index];
//                        if (newItems.Contains(oldItem)) continue;
//                        buffer.RemoveAt(index);
//                    }
                });
            }

            protected override void Refill()
            {
                buffer.Reset(cell.value);
            }
        }
        
        class ReactiveCollectionFromCellOfCollection<T> : AbstractCollectionTransform<T>
        {
            public ICell<IReactiveCollection<T>> cell;
            protected override IDisposable StartListen()
            {
                return cell.ListenUpdates(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    var newItems = coll as T[] ?? coll.ToArray();
                    for (var index = 0; index < newItems.Length; index++)
                    {
                        var item = newItems[index];
                        if (buffer.Contains(item)) continue;
                        buffer.Add(item);
                    }

                    for (var index = buffer.Count - 1; index >= 0; index--)
                    {
                        var oldItem = buffer[index];
                        if (newItems.Contains(oldItem)) continue;
                        buffer.RemoveAt(index);
                    }
                });
            }

            protected override void Refill()
            {
                buffer.Reset(cell.value);
            }
        }
    }
}