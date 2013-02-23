/*
 * LinkedListToken.java
 * 
 * Copyright (c) 2006 David Holroyd
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace com.redwine.xas
{

    public class LinkedListToken : ClassicToken
    {
        private LinkedListToken prev = null;
        private LinkedListToken next = null;

        public LinkedListToken(int type, String text)
            : base(type, text)
        {

        }

        public LinkedListToken getNext()
        {
            return next;
        }

        public void setNext(LinkedListToken next)
        {
            if (this == next)
            {
                throw new ArgumentException("Token stream loop detected (" + ToString() + ")");
            }
            this.next = next;
            if (next != null)
            {
                next.prev = this;
            }
        }

        public LinkedListToken getPrev()
        {
            return prev;
        }

        public void setPrev(LinkedListToken prev)
        {
            if (this == prev)
            {
                throw new ArgumentException("Token stream loop detected");
            }
            this.prev = prev;
            if (prev != null)
            {
                prev.next = this;
            }
        }

        public void afterInsert(LinkedListToken insert)
        {
            if (insert.getPrev() != null)
            {
                throw new ArgumentException("afterInsert(" + insert + ") : prev was not null");
            }
            if (insert.getNext() != null)
            {
                throw new ArgumentException("afterInsert(" + insert + ") : next was not null");
            }
            insert.next = next;
            insert.prev = this;
            if (next != null)
            {
                next.prev = insert;
            }
            next = insert;
        }

        public void beforeInsert(LinkedListToken insert)
        {
            if (insert.getPrev() != null)
            {
                throw new ArgumentException("beforeInsert(" + insert + ") : prev was not null");
            }
            if (insert.getNext() != null)
            {
                throw new ArgumentException("beforeInsert(" + insert + ") : next was not null");
            }
            insert.prev = prev;
            insert.next = this;
            if (prev != null)
            {
                prev.next = insert;
            }
            prev = insert;
        }

        public void delete()
        {
            if (prev != null)
            {
                prev.next = next;
            }
            if (next != null)
            {
                next.prev = prev;
            }
            next = prev = null;
        }
    }
}
