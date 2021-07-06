using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Didimo.Animation
{
    public class CircularTester : MonoBehaviour
    {

        private Thread readerThread;
        private Thread writerThread;
        private CircularBuffer<int> circularBuffer;
        private StreamBuffer streamBuffer;

        void Start()
        {
            TestBuffer();
        }


        public void TestBuffer()
        {
            circularBuffer = new CircularBuffer<int>(8);
            streamBuffer = new StreamBuffer(8);

            readerThread = new Thread(new ThreadStart(ReaderStreamBufferThreadFunction));
            readerThread.IsBackground = true;

            writerThread = new Thread(new ThreadStart(WriterStreamBufferThreadFunction));
            writerThread.IsBackground = true;

            readerThread.Start();
            writerThread.Start();
        }


        void ReaderCircularBufferThreadFunction()
        {
            for (int i = 0; i < 10; i++)
            {
                int j = circularBuffer.Dequeue();
                Debug.Log(string.Format("Reader: Dequeued {0}", j));
            }
        }

        void WriterCircularBufferThreadFunction()
        {
            for (int i = 0; i < 10; i++)
            {
                circularBuffer.Enqueue(i);
                Debug.Log(string.Format("Writer: Enqueued {0}", i));
            }
        }



        void ReaderStreamBufferThreadFunction()
        {
            List<char> data;

            char stopCharacter;
            data = streamBuffer.ScanForNewlineOrSpace(out stopCharacter);
            Debug.Log(string.Concat(data));
            Debug.Log(data.Count);

            data = streamBuffer.ScanForNewlineOrSpace(out stopCharacter);
            Debug.Log(string.Concat(data));
            Debug.Log(data.Count);
        }

        void WriterStreamBufferThreadFunction()
        {
            streamBuffer.Enqueue('a');
            streamBuffer.Enqueue('b');
            streamBuffer.Enqueue('c');
            streamBuffer.Enqueue('d');
            Thread.Sleep(1000);
            streamBuffer.Enqueue(' ');
            streamBuffer.Enqueue('f');
            streamBuffer.Enqueue('g');
            streamBuffer.Enqueue('h');
            streamBuffer.Enqueue('i');
            streamBuffer.Enqueue('j');
            Thread.Sleep(1000);
            streamBuffer.Enqueue('\n');
        }
    }
}