using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class ThreadSafeQueue<T>
	{

		public ThreadSafeQueue()
		{
			queue = new Queue<T>();
			lockObj = new object();
		}

		Queue<T> queue;
		object lockObj;

		public T Dequeue()
		{
			T result = default(T);

			lock (lockObj)
			{
				result = queue.Dequeue();
			}

			return result;
		}

		public void Enqueue(T item)
		{
			lock (lockObj)
			{
				queue.Enqueue(item);
			}
		}

		public int Count
		{
			get { return queue.Count; }
		}

	}
}
