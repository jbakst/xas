using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.redwine.xas
{
    class Metadata
    {
        private List<string> _publicMembers = new List<string>();
        private List<string> _protectedMembers = new List<string>();
        private List<string> _privateMembers = new List<string>();
        private List<string> _virtualMembers = new List<string>();
        private List<string> _variables = new List<string>();

        private List<string> _publicFields = new List<string>();
        private List<string> _protectedFields = new List<string>();
        private List<string> _privateFields = new List<string>();
        private List<string> _initializers = new List<string>();
        private List<string> _ctors = new List<string>();
        private List<string> _dtors = new List<string>();

        private List<string> _statics = new List<string>();

        private List<string> _source = new List<string>();
        private List<string> _header = new List<string>();

        private List<string> _includes = new List<string>();

        private string _namespace;

        public Metadata()
        {

        }

        public string Namespace
        {
            set
            {
                _namespace = value;
            }
            get
            {
                return _namespace;
            }
                
        }

        public string Constructor
        {
            set
            {
                _ctors.Add(value);
            }
        }

        public List<string> Constructors
        {
            get
            {
                return _ctors;
            }
        }


        public string Destructor
        {
            set
            {
                _dtors.Add(value);
            }
        }

        public List<string> Destructors
        {
            get
            {
                return _dtors;
            }
        }

        public string Header
        {
            set
            {
                _header.Add(value);
            }
        }

        public List<string> Headers
        {
            get
            {
                return _header;
            }
        }

        public string Static
        {
            set
            {
                _statics.Add(value);
            }
        }

        public List<String> Statics
        {
            get
            {
                return _statics;
            }
        }

        public string Source
        {
            set
            {
                _source.Add(value);
            }
        }

        public List<String> Sources
        {
            get
            {
                return _source;
            }
        }

        public string Include
        {
            set
            {
                _includes.Add(value);
            }
        }

        public List<String> Includes
        {
            get
            {
                return _includes;
            }
        }

        public string Initializer
        {
            set
            {
                _initializers.Add(value);
            }
        }

        public List<string> Initializers
        {
            get
            {
                return _initializers;
            }
        }

        public string PublicMember
        {
            set
            {
                _publicMembers.Add(value);
            }
        }

        public string ProtectedMember
        {
            set
            {
                _protectedMembers.Add(value);
            }
        }

        public string PrivateMember
        {
            set
            {
                _privateMembers.Add(value);
            }
        }

        public string VirtualMember
        {
            set
            {
                _virtualMembers.Add(value);
            }
        }

        //

        public string PublicField
        {
            set
            {
                _publicFields.Add(value);
            }
        }

        public string ProtectedField
        {
            set
            {
                _protectedFields.Add(value);
            }
        }

        public string PrivateField
        {
            set
            {
                _privateFields.Add(value);
            }
        }


        public List<string> PublicMembers
        {
            get
            {
                return _publicMembers;
            }
        }

        public List<string> ProtectedMembers
        {
            get
            {
                return _protectedMembers;
            }
        }

        public List<string> PrivateMembers
        {
            get
            {
                return _privateMembers;
            }
        }

        // 

        public List<string> PublicFields
        {
            get
            {
                return _publicFields;
            }
        }

        public List<string> ProtectedFields
        {
            get
            {
                return _protectedFields;
            }
        }

        public List<string> PrivateFields
        {
            get
            {
                return _privateFields;
            }
        }

    }
}
