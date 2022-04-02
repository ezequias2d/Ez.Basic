using System;
using System.Collections.Generic;

namespace Ez.Basic.Compiler.Lexer
{
    internal readonly struct Trie<T>
    {
        private readonly Node m_root;
        public Trie(params (string, T)[] keywords)
        {
            m_root = MakeTrie(keywords);
        }

        public bool TryGet(ReadOnlySpan<char> keyword, out T value)
        {
            var current = m_root;

            for(var i = 0; i < keyword.Length; i++)
            {
                var c = keyword[i];
                var index = current.Children.BinarySearch(new Node(c));

                if (index < 0)
                {
                    value = default;
                    return false;
                }

                current = current.Children[index];
            }

            value = current.IsTerminal ? current.Value : default;
            return current.IsTerminal;
        }

        private static Node MakeTrie((string, T)[] keywords)
        {
            var root = new Node('\0', false, default);

            foreach(var pair in keywords)
            {
                var keyword = pair.Item1;
                var value = pair.Item2;
                var current = root;
                // insert
                for(var i = 0; i < keyword.Length; i++)
                {
                    var c = keyword[i];
                    var pseudoNode = new Node(c);
                    var isTerminal = i + 1 == keyword.Length;


                    if (current.Children.Count == 0 || current.Children[current.Children.Count - 1].CompareTo(pseudoNode) <= 0)
                        current.Children.Add(new Node(c, isTerminal, value));

                    else if (current.Children[0].CompareTo(pseudoNode) >= 0)
                        current.Children.Insert(0, new Node(c, isTerminal, value));

                    var index = current.Children.BinarySearch(new Node(c));
                    if (index < 0)
                    {
                        index = ~index;
                        current.Children.Insert(index, new Node(c, isTerminal, value));
                    }

                    current = current.Children[index];
                }
            }

            return root;
        }

        private readonly struct Node  : IComparable<Node>
        {
            public readonly List<Node> Children;
            public readonly char Character;
            public readonly bool IsTerminal;
            public readonly T Value;

            public Node(char character, bool isTerminal, T value)
            {
                Children = new List<Node>();
                Character = character;
                IsTerminal = isTerminal;
                Value = value;
            }

            public Node(char character)
            {
                Children = null;
                Character = character;
                IsTerminal = false;
                Value = default;
            }

            public int CompareTo(Node other)
            {
                return Character.CompareTo(other.Character);
            }
        }
    }
}
