using System.Collections.Generic;

namespace Didimo.Animation
{
    public class StreamBuffer : CircularBuffer<char>
    {

        public readonly char newlineCharacter = '\n';
        public readonly char spaceCharacter = ' ';

        public StreamBuffer(int size) : base(size)
        {
        }

        /// <summary>
        /// Scan the buffer until a newline is found
        /// </summary>
        /// <returns>Accumulated buffer before the newline (not included)</returns>
        public List<char> ScanForNewline()
        {
            List<char> data = new List<char>();

            char nextCharacter = Dequeue();
            while (nextCharacter != newlineCharacter)
            {
                data.Add(nextCharacter);
                nextCharacter = Dequeue();
            }

            return data;
        }

        /// <summary>
        /// Scan the buffer until a space is found
        /// </summary>
        /// <returns>Accumulated buffer before the space (not included)</returns>
        public List<char> ScanForSpace()
        {
            List<char> data = new List<char>();

            char nextCharacter = Dequeue();
            while (nextCharacter != spaceCharacter)
            {
                data.Add(nextCharacter);
                nextCharacter = Dequeue();
            }

            return data;
        }

        /// <summary>
        /// Scan the buffer until either a newline or a space is found
        /// </summary>
        /// <param name="stopCharacter">The character that was found (newline or space)</param>
        /// <returns>Accumulated buffer before the stopCharacter was found (not included)</returns>
        public List<char> ScanForNewlineOrSpace(out char stopCharacter)
        {
            List<char> data = new List<char>();

            char nextCharacter = Dequeue();
            while (nextCharacter != newlineCharacter && nextCharacter != spaceCharacter)
            {
                data.Add(nextCharacter);
                nextCharacter = Dequeue();
            }

            stopCharacter = nextCharacter;
            return data;
        }

    }
}
