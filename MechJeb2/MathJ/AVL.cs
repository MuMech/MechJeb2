#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace MuMech.MathJ
{
    public interface IExtendedComparable<in T1, in T2> : IComparable<T1> where T2 : IComparable<T2>
    {
        public int CompareTo(T2 other);
    }

    // FIXME: look at the Dictionary interface in C# and implement it similarly with KeyValuePair<TKey,TValue> which is what this is like
    // see: https://github.com/xieqing/avl-tree
    // consider implementing:  ICollection<T>, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ISet<T>, ICollection
    public class AVL<TIndex, TStore> : IEnumerable<TStore> where TStore : IExtendedComparable<TStore, TIndex> where TIndex : IComparable<TIndex>
    {
        private   Node<TStore>? _lastLeft;
        private   Node<TStore>? _lastRight;
        protected Node<TStore>? First;
        protected Node<TStore>? Last;
        protected Node<TStore>? Root;

        public int Count { get; private set; }

        public TStore FirstData
        {
            get
            {
                if (First is null) throw new InvalidOperationException("AVL tree is empty");
                return First.Data;
            }
        }

        public TStore LastData
        {
            get
            {
                if (Last is null) throw new InvalidOperationException("AVL tree is empty");
                return Last.Data;
            }
        }

        protected Node<TStore> GetNode(TStore data)
        {
            return Node<TStore>.Get(data);
        }

        public bool TryGetValue(TIndex key, out TStore value)
        {
            value = default!;

            if (Root is null)
                return false;

            Node<TStore>? current = Root;
            while (current != null)
            {
                int cmp = current.Data.CompareTo(key);
                if (cmp == 0)
                {
                    value = current.Data;
                    return true;
                }

                current = cmp > 0 ? current.Left : current.Right;
            }

            return false;
        }

        public void Replace(TIndex key, TStore value)
        {
            if (Root is null)
                throw new ArgumentException("AVL tree is empty");

            Node<TStore>? current = Root;
            while (current != null)
            {
                int cmp = current.Data.CompareTo(key);
                if (cmp == 0)
                {
                    current.Data = value;
                    return;
                }

                current = cmp > 0 ? current.Left : current.Right;
            }

            throw new ArgumentException("Could not find item in AVL tree to replace");
        }

        public bool Contains(TIndex data)
        {
            if (Root is null)
                return false;

            Node<TStore>? current = Root;
            while (current != null)
            {
                int cmp = current.Data.CompareTo(data);
                if (cmp == 0)
                    return true;

                current = cmp > 0 ? current.Left : current.Right;
            }

            return false;
        }

        public bool Remove(TIndex data)
        {
            Mutated();
            throw new NotImplementedException("not implemented");
        }

        public void Add(TStore data)
        {
            if (Root is null)
            {
                Root  = GetNode(data);
                First = Root;
                Last  = Root;
                Count = 1;
                return;
            }

            Node<TStore> parent = Root;

            while (true)
            {
                // this tree doesn't support dups
                if (data.CompareTo(parent.Data) == 0)
                    return;

                int cmp = data.CompareTo(parent.Data);

                if (cmp < 0 && LeftIsNode(parent))
                    parent = parent.Left!;

                else if (cmp > 0 && RightIsNode(parent))
                    parent = parent.Right!;
                else
                    break;
            }

            Mutated();

            Node<TStore> current = GetNode(data);
            current.Parent = parent;
            Count++;

            if (data.CompareTo(parent.Data) < 0)
            {
                current.Left   = parent.Left;
                parent.Left    = current;
                parent.Lthread = false;
                current.Right  = parent;
            }
            else
            {
                current.Right  = parent.Right;
                parent.Right   = current;
                parent.Rthread = false;
                current.Left   = parent;
            }

            if (First is null || current.Data.CompareTo(First.Data) < 0)
                First = current;

            if (Last is null || current.Data.CompareTo(Last.Data) > 0)
                Last = current;

            while (true)
            {
                if (current == parent.Left)
                    switch (parent.BalanceFactor)
                    {
                        case 1:
                            parent.BalanceFactor = 0;
                            goto End;
                        case -1:
                            FixInsertLeftImbalance(parent);
                            goto End;
                        case 0:
                            parent.BalanceFactor = -1;
                            break;
                    }
                else
                    switch (parent.BalanceFactor)
                    {
                        case -1:
                            parent.BalanceFactor = 0;
                            goto End;
                        case 1:
                            FixInsertRightImbalance(parent);
                            goto End;
                        case 0:
                            parent.BalanceFactor = 1;
                            break;
                    }

                if (parent.Parent is null)
                    break;

                current = parent;
                parent  = current.Parent;
            }

            End: ;
        }

        private void Mutated()
        {
            _lastLeft  = null;
            _lastRight = null;
        }

        public (TStore min, TStore max) FindRange(TIndex data)
        {
            if (_lastLeft != null && _lastLeft.Data.CompareTo(data) <= 0)
                if (_lastRight!.Data.CompareTo(data) > 0)
                    return (_lastLeft.Data, _lastRight.Data);

            _lastLeft  = ClosestSmallerElementSearch(data);
            _lastRight = GetNextRightThread(_lastLeft);

            if (_lastRight == null)
            {
                _lastRight = _lastLeft;
                _lastLeft  = GetNextLeftThread(_lastRight);
            }

            // FIXME: somehow lastleft can be null here
            return (_lastLeft!.Data, _lastRight.Data);
        }

        private Node<TStore> ClosestSmallerElementSearch(TIndex data)
        {
            if (Root is null)
                throw new ArgumentException("empty tree");

            Node<TStore> current = Root;

            while (true)
            {
                int cmp = current.Data.CompareTo(data);
                if (cmp == 0)
                    return current;

                if (cmp > 0)
                {
                    if (LeftIsNode(current))
                        current = current.Left!;
                    else
                        return current.Left ?? current;
                }
                else
                {
                    if (RightIsNode(current))
                        current = current.Right!;
                    else
                        return current;
                }
            }
        }

        private void FixInsertLeftImbalance(Node<TStore> parent)
        {
            if (parent.Left!.BalanceFactor == parent.BalanceFactor) // -1, -1
            {
                parent               = RotateRight(parent);
                parent.BalanceFactor = parent.Right!.BalanceFactor = 0;
            }
            else // 1, -1
            {
                int oldBf = parent.Left!.Right!.BalanceFactor;
                RotateLeft(parent.Left!);
                parent               = RotateRight(parent);
                parent.BalanceFactor = 0;

                switch (oldBf)
                {
                    case -1:
                        parent.Left!.BalanceFactor  = 0;
                        parent.Right!.BalanceFactor = 1;
                        break;
                    case 1:
                        parent.Left!.BalanceFactor  = -1;
                        parent.Right!.BalanceFactor = 0;
                        break;
                    case 0:
                        parent.Left!.BalanceFactor = parent.Right!.BalanceFactor = 0;
                        break;
                }
            }
        }

        private void FixInsertRightImbalance(Node<TStore> parent)
        {
            if (parent.Right!.BalanceFactor == parent.BalanceFactor) // 1, 1
            {
                parent               = RotateLeft(parent);
                parent.BalanceFactor = parent.Left!.BalanceFactor = 0;
            }
            else // -1, 1
            {
                int oldBf = parent.Right!.Left!.BalanceFactor;
                RotateRight(parent.Right!);
                parent               = RotateLeft(parent);
                parent.BalanceFactor = 0;

                switch (oldBf)
                {
                    case -1:
                        parent.Left!.BalanceFactor  = 0;
                        parent.Right!.BalanceFactor = 1;
                        break;
                    case 1:
                        parent.Left!.BalanceFactor  = -1;
                        parent.Right!.BalanceFactor = 0;
                        break;
                    case 0:
                        parent.Left!.BalanceFactor = parent.Right!.BalanceFactor = 0;
                        break;
                }
            }
        }

        private Node<TStore> RotateRight(Node<TStore> x)
        {
            Node<TStore> y = x.Left!;

            if (!y.Rthread)
            {
                x.Left    = y.Right;
                x.Lthread = false;
            }
            else
            {
                x.Left    = y;
                x.Lthread = true;
            }

            if (LeftIsNode(x))
                x.Left!.Parent = x;

            y.Parent = x.Parent;

            if (x.Parent is null)
            {
                Root = y;
            }
            else if (x == x.Parent.Left)
            {
                x.Parent.Left    = y;
                x.Parent.Lthread = false;
            }
            else
            {
                x.Parent.Right   = y;
                x.Parent.Rthread = false;
            }

            y.Right   = x;
            y.Rthread = false;
            x.Parent  = y;

            return y;
        }

        private Node<TStore> RotateLeft(Node<TStore> x)
        {
            Node<TStore> y = x.Right!;

            if (!y.Lthread)
            {
                x.Right   = y.Left;
                x.Rthread = false;
            }
            else
            {
                x.Right   = y;
                x.Rthread = true;
            }

            if (RightIsNode(x))
                x.Right!.Parent = x;

            y.Parent = x.Parent;

            if (x.Parent is null)
            {
                Root = y;
            }
            else if (x == x.Parent.Left)
            {
                x.Parent.Left    = y;
                x.Parent.Lthread = false;
            }
            else
            {
                x.Parent.Right   = y;
                x.Parent.Rthread = false;
            }

            y.Left    = x;
            y.Lthread = false;
            x.Parent  = y;

            return y;
        }

        protected bool LeftIsNode(Node<TStore> node)
        {
            return node.Left != null && !node.Lthread;
        }

        protected bool RightIsNode(Node<TStore> node)
        {
            return node.Right != null && !node.Rthread;
        }

        private void CheckThreading()
        {
            Node<TStore>? current = First;

            int n;

            for (n = 0; current != null; n++)
            {
                bool rightThreaded = current.Rthread;
                current = current.Right;

                if (rightThreaded || current == null)
                    continue;

                // if right wasn't threaded, follow left to find leftmost subtree
                while (current.Left != null && !current.Lthread)
                    current = current.Left;
            }

            if (n != Count)
                throw new InvalidOperationException("threading traversal did not count enough elements");
        }

        protected Node<TStore>? GetNextRightThread(Node<TStore> current)
        {
            if (RightIsNode(current))
            {
                current = current.Right!;

                // follow left to find leftmost subtree
                while (current.Left != null && !current.Lthread)
                    current = current.Left;

                return current;
            }

            // threaded or null
            return current.Right;
        }

        protected Node<TStore>? GetNextLeftThread(Node<TStore> current)
        {
            if (LeftIsNode(current))
            {
                current = current.Left!;

                // follow right to find rightmost subtree
                while (current.Right != null && !current.Rthread)
                    current = current.Right;

                return current;
            }

            // threaded or null
            return current.Left;
        }

        private void CheckReverseThreading()
        {
            Node<TStore>? current = Last;

            int n;

            for (n = 0; current != null; n++)
            {
                bool leftThreaded = current.Lthread;
                current = current.Left;

                if (leftThreaded || current == null)
                    continue;

                // if right wasn't threaded, follow left to find leftmost subtree
                while (current.Right != null && !current.Rthread)
                    current = current.Right;
            }

            if (n != Count)
                throw new InvalidOperationException("threading traversal did not count enough elements");
        }

        private void CheckOrder(Node<TStore> node)
        {
            if (LeftIsNode(node) && node.Data.CompareTo(node.Left!.Data) <= 0)
                throw new InvalidOperationException("order is wrong on left node");

            if (RightIsNode(node) && node.Data.CompareTo(node.Right!.Data) >= 0)
                throw new InvalidOperationException("order is wrong on right node");

            if (LeftIsNode(node))
                CheckOrder(node.Left!);

            if (RightIsNode(node))
                CheckOrder(node.Right!);
        }

        private int CheckHeight(Node<TStore> node)
        {
            int lh = LeftIsNode(node) ? CheckHeight(node.Left!) : 0;

            int rh = RightIsNode(node) ? CheckHeight(node.Right!) : 0;

            if (rh - lh != node.BalanceFactor)
                throw new InvalidOperationException("computed balance factor isn't correct");

            if (node.BalanceFactor > 1 || node.BalanceFactor < -1)
                throw new InvalidOperationException("stored balance factor is out of range");

            return Math.Max(rh, lh) + 1;
        }

        private void CheckLinkages(Node<TStore> node)
        {
            if (LeftIsNode(node) && node.Left!.Parent != node)
                throw new InvalidOperationException("left node parent point does not point back");

            if (RightIsNode(node) && node.Right!.Parent != node)
                throw new InvalidOperationException("left node parent point does not point back");

            if (RightIsNode(node))
                CheckLinkages(node.Right!);

            if (LeftIsNode(node))
                CheckLinkages(node.Left!);
        }

        public bool CheckTree()
        {
            if (Root is null)
                return true;
            if (Root.Parent != null)
                throw new InvalidOperationException("root parent isn't null");
            CheckHeight(Root);
            CheckOrder(Root);
            CheckLinkages(Root);
            CheckThreading();
            CheckReverseThreading();
            return true;
        }

        public void Clear()
        {
            Node<TStore>? current = First;

            while (current != null)
            {
                bool rightThreaded = current.Rthread;
                current = current.Right;

                if (current == null)
                    break;

                if (rightThreaded)
                {
                    current.Dispose();
                    continue;
                }

                // if right wasn't threaded, follow left to find leftmost subtree
                while (current.Left != null && !current.Lthread)
                    current = current.Left;

                current.Dispose();
            }

            Reset();
        }

        private void Reset()
        {
            _lastLeft  = null;
            _lastRight = null;
            First      = null;
            Last       = null;
            Root       = null;
            Count      = 0;
        }

        protected class Node<T> : IDisposable where T : TStore
        {
            private static readonly ObjectPool<Node<T>> _nodePool = new ObjectPool<Node<T>>();

            public int      BalanceFactor;
            public T        Data = default!;
            public Node<T>? Left;
            public bool     Lthread = true;
            public Node<T>? Parent;
            public Node<T>? Right;
            public bool     Rthread = true;

            public static Node<T> Get(T data)
            {
                Node<T> node = _nodePool.Get();
                node.Reset();
                node.Data = data;
                return node;
            }

            private void Reset()
            {
                BalanceFactor = default;
                Data          = default!;
                Left          = default;
                Lthread       = true;
                Parent        = default;
                Right         = default;
                Rthread       = true;
            }

            public void Dispose()
            {
                _nodePool.Return(this);
            }
        }

        public IEnumerator<TStore> GetEnumerator()
        {
            Node<TStore>? current = First;

            while (current != null)
            {
                bool rightThreaded = current.Rthread;
                current = current.Right;

                if (current == null)
                    break;

                if (rightThreaded)
                {
                    yield return current.Data;
                    continue;
                }

                // if right wasn't threaded, follow left to find leftmost subtree
                while (current.Left != null && !current.Lthread)
                    current = current.Left;

                yield return current.Data;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
