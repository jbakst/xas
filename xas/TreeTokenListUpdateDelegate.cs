/*
 * ITreeTokenListUpdateDelegate.cs
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

namespace com.redwine.xas
{
    public interface ITreeTokenListUpdateDelegate
    {
        void AddedChild(LinkedListTree parent, LinkedListTree child);
        void AddedChild(LinkedListTree parent, int index, LinkedListTree child);
        void AppendToken(LinkedListTree parent, LinkedListToken append);
        void AddToken(LinkedListTree parent, int index, LinkedListToken append);
        void DeletedChild(LinkedListTree parent, int index, LinkedListTree child);
        void ReplacedChild(LinkedListTree tree, int index, LinkedListTree child, LinkedListTree oldChild);
    }
}
