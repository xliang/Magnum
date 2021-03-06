// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Channels.Internal
{
	using System;
	using System.Collections.Generic;

	public class MessageDictionary<TKey, TValue> :
		IMessageDictionary<TKey, TValue>
	{
		private readonly Func<TValue, TKey> _getKey;
		private Dictionary<TKey, TValue> _messages = new Dictionary<TKey, TValue>();

		public MessageDictionary(Func<TValue, TKey> getKey)
		{
			_getKey = getKey;
		}

		public void Add(TValue message)
		{
			TKey key = _getKey(message);

			_messages[key] = message;
		}

		public IDictionary<TKey, TValue> RemoveAll()
		{
			Dictionary<TKey, TValue> result = _messages;

			_messages = new Dictionary<TKey, TValue>();

			return result;
		}
	}
}