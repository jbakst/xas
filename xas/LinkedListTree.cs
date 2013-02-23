/*
 * LinkedListTree.java
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
using Antlr.Runtime.Tree;   
using Antlr.Runtime;

namespace com.redwine.xas
{
    public class LinkedListTree : BaseTree
    {

        public IToken token;

        private LinkedListTree parent = null;

        private LinkedListToken startToken;
        private LinkedListToken stopToken;

        protected LinkedListToken initialInsertionAfter;
        protected LinkedListToken initialInsertionBefore;

        private ITreeTokenListUpdateDelegate tokenListUpdater;

        public LinkedListTree() { }

        public LinkedListTree(LinkedListTree node)
            : base(node)
        {

            this.token = node.token;
        }

        public LinkedListTree(IToken t)
        {
            this.token = t;
        }

        public ITreeTokenListUpdateDelegate getTokenListUpdater()
        {
            return tokenListUpdater;
        }
        public void setTokenListUpdater(ITreeTokenListUpdateDelegate tokenListUpdater)
        {
            this.tokenListUpdater = tokenListUpdater;
        }

        override public ITree Parent
        {
            set
            {
                parent = (LinkedListTree)value;
            }
            get
            {
                return parent;
            }

        }

        public override ITree DupNode()
        {
            return new LinkedListTree(this);
        }

        public override int Type
        {
            set
            {
                token.Type = value;
            }
            get
            {
                if (token == null)
                {
                    return 0;
                }

                return token.Type;
            }

        }

        public int getType()
        {
            if (token == null)
            {
                return 0;
            }
            return token.Type;  //getType();
        }

        public override string Text
        {
            set
            {

            }
            get
            {
                return ToString();
            }

        }

        public String getText()
        {
            return ToString();
        }

        public override bool IsNil
        {
            get
            {
                return token == null;
            }
        }

        public IToken getToken()
        {
            return token;
        }

        public int getLine()
        {
            if (token == null || token.Line == 0)
            {
                if (ChildCount > 0)
                {
                    return GetChild(0).Line;
                }
                return 0;
            }
            return token.Line;
        }

        public int getCharPositionInLine()
        {
            if (token == null || token.CharPositionInLine == -1)
            {
                if (ChildCount > 0)
                {
                    return GetChild(0).CharPositionInLine;
                }
                return 0;
            }
            return token.CharPositionInLine;
        }

        public override String ToString()
        {
            if (IsNil)
            {
                return "nil";
            }
            return token.Text;
        }

        public LinkedListTree getFirstChild()
        {
            if (ChildCount == 0)
            {
                return null;
            }
            return (LinkedListTree)GetChild(0);
        }

        public LinkedListTree getLastChild()
        {
            int c = ChildCount;
            if (c == 0)
            {
                return null;
            }
            return (LinkedListTree)GetChild(c - 1);
        }

        public int getIndexOfChild(LinkedListTree child)
        {
            return Children.IndexOf(child);
        }

        /**
         * Adds the given child node to the end of the list of children
         * maintanined by the given parent node, and inserts the tokens
         * belonging to child into the tokenlist of parent just after the
         * stop-token of the previous last child.
         */
        public void addChildWithTokens(LinkedListTree child)
        {
            addChild(child);
            tokenListUpdater.AddedChild(this, child);
        }

        public void setChildWithTokens(int index, LinkedListTree child)
        {
            LinkedListTree oldChild = (LinkedListTree)GetChild(index);
            setChild(index, child);
            tokenListUpdater.ReplacedChild(this, index, child, oldChild);
        }

        public void addChildWithTokens(int index, LinkedListTree child)
        {
            if (Children == null)
            {
                CreateChildrenList();
            }
            Children.Add(child);
            if (child != null)
            {
                child.Parent = this;
            }
            tokenListUpdater.AddedChild(this, index, child);
        }

        /**
         * @deprecated use #addChildWithTokens(LinkedListTree), damnit
         */
        public void addChild(ITree child)
        {
            base.AddChild(child);
            if (child != null)
            {
                ((LinkedListTree)child).Parent = this;
            }
        }

        /**
         * @deprecated use #setChildWithTokens(int,LinkedListTree), damnit
         */
        public void setChild(int index, BaseTree child)
        {
            base.SetChild(index, child);
            ((LinkedListTree)child).Parent = this;
        }

        public BaseTree deleteChild(int index)
        {
            LinkedListTree result = (LinkedListTree)base.DeleteChild(index);
            tokenListUpdater.DeletedChild(this, index, result);
            result.Parent = null;
            return result;
        }

        public void appendToken(LinkedListToken append)
        {
            tokenListUpdater.AppendToken(this, append);
        }

        public void addToken(int index, LinkedListToken append)
        {
            tokenListUpdater.AddToken(this, index, append);
        }

        public void setInitialInsertionAfter(LinkedListToken insert)
        {
            initialInsertionAfter = insert;
        }

        public void setInitialInsertionBefore(LinkedListToken insert)
        {
            initialInsertionBefore = insert;
        }

        public LinkedListToken getInitialInsertionAfter()
        {
            return initialInsertionAfter;
        }

        public LinkedListToken getInitialInsertionBefore()
        {
            return initialInsertionBefore;
        }

        public void setStartToken(LinkedListToken startToken)
        {
            if (parent != null)
            {
                parent.notifyChildStartTokenChange(this, startToken);
            }
            this.startToken = startToken;
        }

        public LinkedListToken getStartToken()
        {
            return startToken;
        }

        public LinkedListToken setStopToken(LinkedListToken stopToken)
        {
            if (parent != null)
            {
                parent.notifyChildStopTokenChange(this, stopToken);
            }
            return this.stopToken = stopToken;
        }

        public LinkedListToken getStopToken()
        {
            return stopToken;
        }

        /**
         * called when one of this node's children updates it's start-token,
         * so that this node can potentially take action; maybe by setting
         * the same start-token IF the child was the very-first in this node's
         * list of children.
         */
        private void notifyChildStartTokenChange(LinkedListTree child, LinkedListToken newStart)
        {
            // TODO: maybe move to delegates
            if (isFirst(child) && isSameStartToken(child))
            {
                setStartToken(newStart);
            }
        }

        private bool isSameStartToken(LinkedListTree child)
        {
            return child.getStartToken() == getStartToken();
        }

        private bool isFirst(LinkedListTree child)
        {
            return child == getFirstChild();
        }

        /**
         * called when one of this node's children updates it's stop-token,
         * so that this node can potentially take action; maybe by setting
         * the same stop-token IF the child was the very-last in this node's
         * list of children.
         */
        private void notifyChildStopTokenChange(LinkedListTree child, LinkedListToken newStop)
        {
            // TODO: maybe move to delegates
            if (isLast(child) && (isSameStopToken(child) || isNoStopToken(child)))
            {
                setStopToken(newStop);
            }
        }

        private bool isNoStopToken(LinkedListTree child)
        {
            return child.getStopToken() == null;
        }

        private bool isSameStopToken(LinkedListTree child)
        {
            return child.getStopToken() == getStopToken();
        }

        private bool isLast(LinkedListTree child)
        {
            return child == getLastChild();
        }

        public int getTokenStartIndex()
        {
            throw new NotImplementedException("unimplemented");
        }

        public int getTokenStopIndex()
        {
            throw new NotImplementedException("unimplemented");
        }

        public override int TokenStartIndex
        {
            set
            {
            }
            get
            {
                return 0;
            }
        }

        public override int TokenStopIndex
        {
            set
            {
            }
            get
            {
                return 0;
            }
        }
    }
}
