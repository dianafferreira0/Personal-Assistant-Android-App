using System.IO;
using System.Text;

namespace Didimo.Animation
{
    public static class StreamReaderExtensions
    {
        public static string ReadUntilDelimiter(this StreamReader self, char delimiter)
        {
            StringBuilder currentLine = new StringBuilder();
            int i;
            char c;
            while ((i = self.Read()) >= 0)
            {
                c = (char)i;
                if (c == delimiter)
                {
                    break;
                }

                currentLine.Append(c);
            }

            return currentLine.ToString();
        }
    }
}
