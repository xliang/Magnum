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
namespace Sample.WebActors.Actors.Echo
{
	using System.Threading;
	using Magnum.Actions;
	using Magnum.Channels;
	using Magnum.Logging;

	/// <summary>
	/// A simple actor that echoes the input to the output channel
	/// </summary>
	public class EchoActor
	{
		private static readonly ILogger _log = Logger.GetLogger<EchoActor>();

		private readonly ActionQueue _queue;

		public EchoActor(ActionQueue queue)
		{
			_queue = queue;

			EchoChannel = new ConsumerChannel<EchoInputModel>(_queue, ProcessRequest);
		}

		public Channel<EchoInputModel> EchoChannel { get; private set; }

		private void ProcessRequest(EchoInputModel inputModel)
		{
			_log.Debug(x => x.Write("Echo[{0}]: {1}", Thread.CurrentThread.ManagedThreadId, inputModel.Text));

			inputModel.OutputChannel.Send(new EchoOutputModel
				{
					Text = inputModel.Text,
					UserAgent = inputModel.UserAgent,
				});
		}
	}
}