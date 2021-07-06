using System.Collections.Generic;
using System.Threading;

namespace Didimo.Animation
{
    /// <summary>
    /// Circular buffer that can be written and saved to.
    /// </summary>
    /// <typeparam name="T">type of the items stored inside the buffer</typeparam>
    public class CircularBuffer<T>
    {

        private T[] itemBuffer;

        private int readIndex = 0;
        private int writeIndex = 0;
        private readonly int maxsize;


        public CircularBuffer(int size)
        {
            maxsize = size;
            itemBuffer = new T[maxsize];
        }

        /// <summary>
        /// Remaining size of unread elements
        /// </summary>
        public int Size
        {
            get
            {
                return (writeIndex - readIndex + maxsize) % maxsize;
            }
        }



        /// <summary>
        /// Add the item to the queue. This will wait if there is no space in the queue.
        /// </summary>
        /// <param name="item">item to add into the queue</param>
        public void Enqueue(T item)
        {
            int nextWriteIndex = (writeIndex + 1) % maxsize;
            while (nextWriteIndex == readIndex)
            {
                Thread.SpinWait(1);
            }

            itemBuffer[writeIndex] = item;
            IncreaseWriterIndex();
        }

        /// <summary>
        /// Add multiple items to the queue. This will wait if there is no space in the queue.
        /// </summary>
        /// <param name="items"></param>
        public void EnqueueMultiple(T[] items, int count = 0)
        {

            if (count <= 0)
            {
                count = items.Length;
            }

            for (int i = 0; i < count; i++)
            {  // Can't be length here! cause length will be the entire array (including null)
                Enqueue(items[i]);
            }
        }


        public void EnqueueMultiple(List<T> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Enqueue(items[i]);
            }
        }





        /// <summary>
        /// Get the next item from the queue. This will wait until an item is ready if needed.
        /// </summary>
        /// <returns>next item in the queue</returns>
        public T Dequeue()
        {
            while (readIndex == writeIndex)
            {
                Thread.SpinWait(1);
            }

            T readItem = itemBuffer[readIndex];
            IncreaseReaderIndex();
            return readItem;
        }


        public List<T> DequeueMultiple(int count)
        {   // return list to be easier to manage! (like AddRange and etc)
            List<T> data = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                data.Add(Dequeue());
            }
            return data;
        }

        public List<T> DequeueMultipleOrLess(int count)
        {   // return list to be easier to manage! (like AddRange and etc)
            List<T> data = new List<T>();
            for (int i = 0; i < count; i++)
            {
                data.Add(Dequeue());
                if (readIndex == writeIndex)
                {
                    break;
                }
            }
            return data;
        }


        /// <summary>
        /// Clear the buffer, removing all items from the queue.
        /// </summary>
        public void ClearBuffer()
        {
            readIndex = writeIndex = 0;
        }


        /// <summary>
        /// Atomicly increase and apply modulo to the writer index
        /// </summary>
        private void IncreaseWriterIndex()
        {
            Interlocked.Exchange(ref writeIndex, (writeIndex + 1) % maxsize);
        }

        /// <summary>
        /// Atomicly increase and apply modulo to the reader index
        /// </summary>
        private void IncreaseReaderIndex()
        {
            Interlocked.Exchange(ref readIndex, (readIndex + 1) % maxsize);
        }

        private void ResetIndices()
        {
            Interlocked.Exchange(ref writeIndex, 0);
            Interlocked.Exchange(ref readIndex, 0);
        }
    }
}
