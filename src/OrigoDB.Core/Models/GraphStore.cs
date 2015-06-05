using System;
using System.Collections.Generic;
using OrigoDB.Core.Proxy;

namespace OrigoDB.Core.Models
{
    [Serializable]
    public class GraphStore : Model
    {
        /// <summary>
        /// Unique id generator, shared by nodes and edges
        /// </summary>
        private long _lastId;

        //Nodes and edges
        private readonly SortedDictionary<long, Node> _nodesById;
        private readonly SortedDictionary<long, Edge> _edgesById;

        [NonSerialized]
        private SortedDictionary<string, SortedSet<Node>> _nodesByLabel;

        [NonSerialized]
        private SortedDictionary<string, SortedSet<Edge>> _edgesByLabel;
        

        public GraphStore()
        {
            var ignoreCase = StringComparer.InvariantCultureIgnoreCase;
            _edgesById = new SortedDictionary<long, Edge>();
            _edgesByLabel = new SortedDictionary<string, SortedSet<Edge>>(ignoreCase);
            _nodesById = new SortedDictionary<long, Node>();
            _nodesByLabel = new SortedDictionary<string, SortedSet<Node>>(ignoreCase);
        }

        public abstract class Item
        {
            public readonly long Id;
            public readonly string Label;
            public readonly SortedDictionary<string, object> Props;

            protected Item(long id, string label)
            {
                Id = id;
                Label = label;
                Props = new SortedDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            }

            public object Get(string key)
            {
                object result;
                Props.TryGetValue(key, out result);
                return result;
            }

            public void Set(string key, object value)
            {
                Props[key] = value;
            }
        }

        [Command]
        public long CreateNode(string label)
        {
            var id = ++_lastId;
            var node = new Node(id, label);
            _nodesById[id] = node;
            AddByLabel(_nodesByLabel, node, label);
            return id;
        }

        [Command]
        public long CreateEdge(long fromId, long toId, string label)
        {
            Node from = NodeById(fromId);
            Node to = NodeById(toId);
            var id = ++_lastId;
            var edge = new Edge(id, label) {From = from, To = to};
            _edgesById[id] = edge;
            AddByLabel(_edgesByLabel, edge, label);
            from.Out.Add(edge);
            to.In.Add(edge);
            return id;
        }

        public void RemoveEdge(long id)
        {
            var edge = EdgeById(id);
            _edgesById.Remove(id);
            edge.From.Out.Remove(edge);
            edge.To.In.Remove(edge);
            _edgesByLabel[edge.Label].Remove(edge);
        }

        public void RemoveNode(long id)
        {
            var node = NodeById(id);
            foreach(var edge in node.Out) RemoveEdge(edge.Id);
            foreach (var edge in node.In) RemoveEdge(edge.Id);
            _nodesById.Remove(id);
            _nodesByLabel[node.Label].Remove(node);
        }

        private Node NodeById(long id)
        {
            return GetById(_nodesById, id);
        }

        private Edge EdgeById(long id)
        {
            return GetById(_edgesById, id);
        }

        private T GetById<T>(IDictionary<long,T> items, long id)
        {
            T item;
            if (items.TryGetValue(id, out item)) return item;
            throw new CommandAbortedException("No such node: " + id);

        }

        private static void AddByLabel<T>(IDictionary<string, SortedSet<T>> index, T item, string label)
        {
            SortedSet<T> set;
            if (!index.TryGetValue(label, out set))
            {
                set = new SortedSet<T>();
                index[label] = set;
            }
            set.Add(item);
        }

        protected internal override void SnapshotRestored()
        {
            var ignoreCase = StringComparer.InvariantCultureIgnoreCase;
            _edgesByLabel = new SortedDictionary<string, SortedSet<Edge>>(ignoreCase);
            _nodesByLabel = new SortedDictionary<string, SortedSet<Node>>(ignoreCase);
            foreach(var node in _nodesById.Values) AddByLabel(_nodesByLabel, node, node.Label);
            foreach(var edge in _edgesById.Values) AddByLabel(_edgesByLabel, edge, edge.Label);
        }

        public class Node : Item
        {
            public Node(long id, string label) : base(id,label){}

            internal SortedSet<Edge> Out;
            internal SortedSet<Edge> In;
        }

        public class Edge : Item
        {
            public Edge(long id, string label) : base(id,label){}

            internal Node From;
            internal Node To;
        }

    }

}