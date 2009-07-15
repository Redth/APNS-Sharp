using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace JdSoft.Apple.Apns.Notifications.Collections
{
	class SingleLinkNode<T>
	{
		public SingleLinkNode<T> Next;
		public T Item;
	}

	class SyncMethods
	{
		public static bool CAS<T>(ref T location, T comparand, T newValue) where T : class
		{
			return
				(object)comparand ==
				(object)Interlocked.CompareExchange<T>(ref location, newValue, comparand);
		}
	}
}
