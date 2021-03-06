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
namespace Magnum.Actors.Schedulers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading;

	public class ThreadPoolScheduler :
		Scheduler
	{
		private static readonly long _freq = Stopwatch.Frequency;
		private static readonly double MsMultiplier = 1000.00/_freq;

		private readonly object _lock = new object();
		private readonly SortedList<long, List<ScheduledEvent>> _pending = new SortedList<long, List<ScheduledEvent>>();
		private readonly long _startTimeInTicks = Stopwatch.GetTimestamp();

		private bool _enabled = true;
		private ManualResetEvent _waiter;

		private long Now
		{
			get { return (long) ((Stopwatch.GetTimestamp() - _startTimeInTicks)*MsMultiplier); }
		}

		public Unschedule Schedule(int interval, Action action)
		{
			var pending = new SingleEvent(interval, action, Now);

			QueueEvent(pending);
			return pending.Cancel;
		}

		public Unschedule Schedule(int initialInterval, int periodicInterval, Action action)
		{
			var pending = new RecurringEvent(initialInterval, periodicInterval, action, Now);

			QueueEvent(pending);
			return pending.Cancel;
		}

		public void Dispose()
		{
			_enabled = false;
			if (_waiter != null)
			{
				_waiter.Set();
			}
		}

		public void QueueEvent(ScheduledEvent pending)
		{
			lock (_lock)
			{
				AddScheduledEvent(pending);
				if (_waiter != null)
				{
					_waiter.Set();
				}
				else
				{
					WaitExpired(null, false);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nextScheduledTime"></param>
		/// <param name="now"></param>
		/// <returns>True if there is a future time to schedule, 
		/// otherwise false if nothing is available or execution is immediately required</returns>
		public bool GetNextScheduledTime(ref long nextScheduledTime, long now)
		{
			nextScheduledTime = 0;

			if (_pending.Count <= 0)
				return false;

			foreach (KeyValuePair<long, List<ScheduledEvent>> pair in _pending)
			{
				if (now >= pair.Key)
					return false;

				nextScheduledTime = (pair.Key - now);
				return true;
			}

			return false;
		}

		private void AddScheduledEvent(ScheduledEvent pending)
		{
			List<ScheduledEvent> list;
			if (!_pending.TryGetValue(pending.ScheduledTime, out list))
			{
				list = new List<ScheduledEvent>(2);
				_pending[pending.ScheduledTime] = list;
			}
			list.Add(pending);
		}

		private void WaitExpired(object sender, bool timeout)
		{
			if (!_enabled) return;

			lock (_lock)
			{
				do
				{
					List<ScheduledEvent> rescheduled = ExecuteExpired();
					Queue(rescheduled);
				} while (!ScheduleTimerCallback());
			}
		}

		private bool ScheduleTimerCallback()
		{
			if (_waiter != null)
			{
				_waiter.Close();
				_waiter = null;
			}

			if (_pending.Count <= 0)
				return true;

			long interval = 0;
			if (GetNextScheduledTime(ref interval, Now))
			{
				_waiter = new ManualResetEvent(false);

				ThreadPool.RegisterWaitForSingleObject(_waiter, WaitExpired, interval, interval, true);
				return true;
			}
			return false;
		}

		private void Queue(IEnumerable<ScheduledEvent> rescheduled)
		{
			if (rescheduled == null) return;

			foreach (ScheduledEvent pendingEvent in rescheduled)
			{
				QueueEvent(pendingEvent);
			}
		}

		private List<ScheduledEvent> ExecuteExpired()
		{
			SortedList<long, List<ScheduledEvent>> expired = RemoveExpired();

			List<ScheduledEvent> rescheduled = null;
			if (expired.Count <= 0)
				return rescheduled;

			foreach (KeyValuePair<long, List<ScheduledEvent>> pair in expired)
			{
				foreach (ScheduledEvent pendingEvent in pair.Value)
				{
					ScheduledEvent next = pendingEvent.Execute(Now);
					if (next == null) continue;

					if (rescheduled == null)
						rescheduled = new List<ScheduledEvent>(2);

					rescheduled.Add(next);
				}
			}
			return rescheduled;
		}

		private SortedList<long, List<ScheduledEvent>> RemoveExpired()
		{
			lock (_lock)
			{
				var expired = new SortedList<long, List<ScheduledEvent>>();
				foreach (KeyValuePair<long, List<ScheduledEvent>> item in _pending)
				{
					if (Now < item.Key)
						break;

					expired.Add(item.Key, item.Value);
				}

				foreach (KeyValuePair<long, List<ScheduledEvent>> item in expired)
				{
					_pending.Remove(item.Key);
				}

				return expired;
			}
		}
	}
}