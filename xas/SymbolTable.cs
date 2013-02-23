using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    public class SymbolTable : IEnumerable
    {
        private List<Symbol> _symbols = new List<Symbol>();

        public void Add(Symbol item)
        {
            _symbols.Add(item);
        }

        public bool Find(string item)
        {
            bool result = false;

            foreach (Symbol s in _symbols)
            {
                if (s.Name == item)
                {
                    result = true;
                }
            }

            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator(); 
        }

        IEnumerator<Symbol> GetEnumerator()
        {
            return _symbols.GetEnumerator();
        }

        public Symbol Lookup(string item)
        {
            Symbol result = null;
            foreach (Symbol s in _symbols)
            {
                if (s.Name == item)
                {
                    result = s;
                    break;
                }
            }

            return result;
        }
    }
}
