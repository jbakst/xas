using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    public class Symbol
    {
        private string _type;
        private int _storage = -1;
        private int _modifier = -1;
        private int _scope = -1;
        private string _name;
        private string _value;
        private string _argList;

        private Dictionary<string, string> _args;

        private int _accessor = -1;
        private string _returnType;
        private int _heap = -1;
        private int _kind = -1;
        private string _enclosingName;
        private Object _defaultValue;
        private bool _virtual = false;
        private bool _ctor = false;
        private string _extendedtype;
        private string _implementedtype;

        //private List<String> _code;

        public const int STATIC = 0;
        public const int CONST = 1;
        public const int PUBLIC = 2;
        public const int PROTECTED = 3;
        public const int PRIVATE = 4;
        public const int ALLOCATED = 5;
        public const int FUNCTION = 6;
        public const int CLASS = 7;
        public const int SETTER = 8;
        public const int GETTER = 9;
        public const int OVERRIDE = 10;
        public const int FINAL = 11;
        public const int VARIABLE = 12;
        public const int INTERNAL = 13;

        public Symbol()
        {

        }

        public string ExtendedType
        {
            set
            {
                _extendedtype = value;
            }
            get
            {
                return _extendedtype;
            }
        }

        public string ImplementedType
        {
            set
            {
                _implementedtype = value;
            }
            get
            {
                return _implementedtype;
            }
        }

        public bool Constructor
        {
            set
            {
                _ctor = value;
            }
            get
            {
                return _ctor;
            }
        }

        public Object DefaultValue
        {
            set
            {
                _defaultValue = value;
                if ("null" == (string)value)
                {
                    Allocator = ALLOCATED;
                }
            }
            get
            {
                return _defaultValue;
            }
        }

        public string EnclosingType
        {
            set
            {
                _enclosingName = value;
            }
            get
            {
                return _enclosingName;
            }
        }

        public string EnclosingName
        {
            set
            {
                _enclosingName = value;
            }
            get
            {
                return _enclosingName;
            }
        }

        public bool Virtual
        {
            set
            {
                _virtual = value;
            }
            get
            {
                return _virtual;
            }
        }

        public int Kind
        {
            set
            {
                _kind = value;
            }
            get
            {
                return _kind;
            }
        }

        public int Allocator
        {
            set
            {
                _heap = value;
            }
            get
            {
                return _heap;
            }
        }

        public string ReturnType
        {
            set
            {
                if (null != value)
                {
                    _returnType = value.Trim();
                }
                else
                {
                    _returnType = "void";
                }
            }
            get
            {
                return _returnType;
            }
        }

        public string ArgList
        {
            set
            {
                _argList = value;
            }
            get
            {
                return _argList;
            }
        }

        public Dictionary<string, string> Arguments
        {
            set
            {
                _args = value;
            }
            get
            {
                return _args;
            }
        }

        public int Accessor
        {
            set
            {
                _accessor = value;
            }
            get
            {
                return _accessor;
            }
        }

        public string Type
        {
            set
            {
                _type = value;
            }
            get
            {
                return _type;
            }
        }

        public int Storage
        {
            set
            {
                _storage = value;
            }
            get
            {
                return _storage;
            }
        }

        public int Modifier
        {
            set
            {
                _modifier = value;
            }
            get
            {
                return _modifier;
            }
        }

        public int Scope
        {
            set
            {
                _scope = value;
            }
            get
            {
                return _scope;
            }
        }

        public String Name
        {
            set
            {
                _name = value.Trim();
            }
            get
            {
                return _name;
            }
        }

        public string Value
        {
            set
            {
                _value = value.Trim();
            }
            get
            {
                return _value;
            }
        }
    }
}
