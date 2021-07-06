using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Didimo.Animation
{
    public class TCPClient : MonoBehaviour
    {
        private TcpClient socketConnection;
        private Thread clientReceiveThread;
        private Thread clientParseThread;
        private StreamBuffer streamBuffer;
        private Dictionary<string, string> streamBufferVars = new Dictionary<string, string>();
        private Encoding iso = Encoding.GetEncoding("ISO-8859-1");
        //ulong messageId = 0;

        public int port = 8585;
        public string ipAddress = "10.10.180.26";
        public int readSizeFromStream = 1024;
        public int streamCircularBufferSize = 64 * 1024;
        public Text reconnectButtonText;

        [Serializable]
        public class StringListEvent : UnityEvent<string[]> { }
        [Serializable]
        public class FloatListEvent : UnityEvent<float[], ulong> { }
        [Serializable]
        public class ShortListEvent : UnityEvent<short[], ulong> { }
        public StringListEvent onFacsListReceiveEvent;
        public FloatListEvent onFacsReceiveEvent;
        public ShortListEvent onAudioReceiveEvent;

        public IEnumerator MainThreadOnFacsListReceiveEvent(string[] facsList)
        {
            onFacsListReceiveEvent.Invoke(facsList);
            yield return null;
        }

        public IEnumerator MainThreadOnFacsReceiveEvent(float[] facs, ulong timeStamp)
        {
            onFacsReceiveEvent.Invoke(facs, timeStamp);

            yield return null;
        }

        public IEnumerator MainThreadOnAudioReceiveEvent(short[] audioSample, ulong timeStamp)
        {
            onAudioReceiveEvent.Invoke(audioSample, timeStamp);

            yield return null;
        }

        /// <summary>
        /// Setup TCP socket connection.
        /// </summary>
        private void ConnectToTcpServer()
        {
            if (IsConnected)
            {
                return;
            }

            try
            {
                streamBuffer = new StreamBuffer(streamCircularBufferSize);

                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();

                clientParseThread = new Thread(new ThreadStart(ParseData));
                clientParseThread.IsBackground = true;
                clientParseThread.Start();

            }
            catch (Exception e)
            {
                Debug.Log("On client connect exception " + e);
            }
        }

        string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private float ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return BitConverter.ToSingle(data, offset);
        }

        private short ReadShort(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (2 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 1];
                data[offset + 1] = tmp;
            }
            return BitConverter.ToInt16(data, offset);
        }



        /// <summary>
        /// Runs in the background clientParseThread. Parses the incoming data
        /// </summary>
        private void ParseData()
        {
            while (true)
            {
                FlushParserVars();

                char messageID_stopCharacter;
                // Parse messageID
                List<char> messageID = streamBuffer.ScanForNewlineOrSpace(out messageID_stopCharacter);
                if (messageID_stopCharacter == streamBuffer.newlineCharacter)
                {
                    // invalid message, so discard
                    continue;
                }

                streamBufferVars["messageID"] = string.Concat(messageID).Trim();

                int res;
                // Parse memo
                if (int.TryParse(streamBufferVars["messageID"], out res))
                {
                    char stopCharacter;
                    // Message ID is an actual integer, so look for the memo after
                    List<char> memo = streamBuffer.ScanForNewlineOrSpace(out stopCharacter);
                    streamBufferVars["memo"] = string.Concat(memo).Trim();
                }
                else
                {
                    // MessageID wasn't an integer. We received just the memo or it's junk
                    streamBufferVars["memo"] = string.Copy(streamBufferVars["messageID"]);
                    streamBufferVars["messageID"] = null;
                }

                switch (streamBufferVars["memo"])
                {
                    case "FACLIST":
                        ParseMemoFACLIST();
                        break;

                    case "FACS":
                        ParseMemoFACS();
                        break;

                    case "AUDIO":
                        ParseMemoAUDIO();
                        break;

                    default:
                        Debug.Log("Invalid memo received in message: " + streamBufferVars["memo"]);
                        break;
                }
            }
        }


        private void ParseMemoFACLIST()
        {
            // Find sourceID
            List<char> sourceID = streamBuffer.ScanForSpace();
            streamBufferVars["sourceID"] = string.Concat(sourceID);

            // Find dataset name
            List<char> dataset = streamBuffer.ScanForSpace();
            streamBufferVars["dataset"] = string.Concat(dataset);

            // Find and parse blendshapes
            List<char> blendshapeNames = streamBuffer.ScanForNewline();
            streamBufferVars["blendshapeNames"] = string.Concat(blendshapeNames).Trim();

            //streamBuff
            string[] facNames = streamBufferVars["blendshapeNames"].Split(' ');
            UnityMainThreadDispatcher.Instance().Enqueue(MainThreadOnFacsListReceiveEvent(facNames));
        }

        //44100hz 1 channel, 16 bit integer
        private void ParseMemoAUDIO()
        {
            // Find sourceID
            List<char> sourceID = streamBuffer.ScanForSpace();
            streamBufferVars["sourceID"] = string.Concat(sourceID);

            // Find send time
            List<char> sendTime = streamBuffer.ScanForSpace();
            streamBufferVars["sendTime"] = string.Concat(sendTime);
            ulong timeStamp = ulong.Parse(streamBufferVars["sendTime"]);

            // Find byte length
            List<char> byteLength = streamBuffer.ScanForSpace();
            streamBufferVars["byteLength"] = string.Concat(byteLength);

            int byteLength_int;
            // Parse Byte length
            if (int.TryParse(streamBufferVars["byteLength"], out byteLength_int))
            {
                // Read all values
                List<char> audioValues = streamBuffer.DequeueMultiple(byteLength_int);
                streamBufferVars["audioValues"] = string.Concat(audioValues);

                int numAudioSamples = byteLength_int / 2;
                short[] audioSample = new short[numAudioSamples];

                // encode the BS values back to byte array
                List<byte> bsValuesBytes = new List<byte>(iso.GetBytes(streamBufferVars["audioValues"]));
                byte[] bsValuesBytesArray = bsValuesBytes.ToArray();

                // Decode String and add to array
                for (int i = 0; i < numAudioSamples; i++)
                {
                    audioSample[i] = ReadShort(bsValuesBytesArray, i * 2, true);
                }

                UnityMainThreadDispatcher.Instance().Enqueue(MainThreadOnAudioReceiveEvent(audioSample, timeStamp));
            }
            else
            {
                Debug.Log("Invalid AUDIO message received!");
            }
        }
        private void ParseMemoFACS()
        {
            // Find sourceID
            List<char> sourceID = streamBuffer.ScanForSpace();
            streamBufferVars["sourceID"] = string.Concat(sourceID);

            // Find send time
            List<char> sendTime = streamBuffer.ScanForSpace();
            streamBufferVars["sendTime"] = string.Concat(sendTime);
            ulong timeStamp = ulong.Parse(streamBufferVars["sendTime"]);

            // Find byte length
            List<char> byteLength = streamBuffer.ScanForSpace();
            streamBufferVars["byteLength"] = string.Concat(byteLength);

            int byteLength_int;
            // Parse Byte length
            if (int.TryParse(streamBufferVars["byteLength"], out byteLength_int))
            {
                // Read all values
                List<char> blendshapeValues = streamBuffer.DequeueMultiple(byteLength_int);
                streamBufferVars["blendshapeValues"] = string.Concat(blendshapeValues);

                int numFacs = byteLength_int / 4;
                float[] bsValues = new float[numFacs];


                // encode the BS values back to byte array
                List<byte> bsValuesBytes = new List<byte>(iso.GetBytes(streamBufferVars["blendshapeValues"]));
                byte[] bsValuesBytesArray = bsValuesBytes.ToArray();

                // Decode String and add to array
                for (int i = 0; i < numFacs; i++)
                {
                    bsValues[i] = ReadSingle(bsValuesBytesArray, i * 4, false);
                }

                UnityMainThreadDispatcher.Instance().Enqueue(MainThreadOnFacsReceiveEvent(bsValues, timeStamp));
            }
            else
            {
                Debug.Log("Invalid FACS message received!");
            }
        }



        /// <summary> 	
        /// Runs in background clientReceiveThread; Listens for incomming data. 	
        /// </summary>     
        private void ListenForData()
        {
            try
            {
                Debug.Log("Connecting to " + ipAddress);
                socketConnection = new TcpClient(ipAddress, port);
                NetworkStream stream = socketConnection.GetStream();
                StreamReader reader = new StreamReader(stream, iso);


                while (true)
                {
                    char[] messageBuffer = new char[readSizeFromStream];
                    if (stream.CanRead)
                    {
                        int charsRead = reader.Read(messageBuffer, 0, readSizeFromStream);
                        streamBuffer.EnqueueMultiple(messageBuffer, charsRead);
                    }
                    else
                    {
                        Console.WriteLine("Sorry. You cannot read from this NetworkStream.");
                    }
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }

            Debug.Log("Thread ended (disconnect)");
        }


        /// <summary>
        /// Runs in the background clientParseThread. Flushes the dictionary with vars captured from the parsing
        /// </summary>
        private void FlushParserVars()
        {
            streamBufferVars.Clear();
        }




        public bool IsConnected
        {
            get
            {
                try
                {
                    if (clientReceiveThread == null || !clientReceiveThread.IsAlive || socketConnection == null)
                    {
                        return false;
                    }
                    if (socketConnection != null && socketConnection.Client != null && socketConnection.Client.Connected)
                    {
                        /* per the documentation on Poll:
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if (socketConnection.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (socketConnection.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary> 	
        /// Send message to server using socket connection. 	
        /// </summary> 	
        private void SendMessage()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Cannot send message: not connected");
                return;
            }
            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    string clientMessage = "This is a message from one of your clients.";
                    // Convert string message to byte array.                 
                    byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                    // Write byte array to socketConnection stream.                 
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                    Debug.Log("Client sent his message - should be received by server");
                    //messageId++;
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
#if UNITY_EDITOR
            // register an event handler when the class is initialized
            UnityEditor.EditorApplication.playModeStateChanged += LogPlayModeState;
#endif
        }

        // Update is called once per frame
        void Update()
        {
            if (IsConnected)
            {
                reconnectButtonText.text = "Disconnect";
            }
            else
            {
                reconnectButtonText.text = "Connect";
            }

            //ConnectToTcpServer();
        }

        public void ToggleConnection()
        {
            if (IsConnected)
            {
                if (socketConnection != null)
                {
                    socketConnection.GetStream().Close();
                    socketConnection.Close();
                }
                if (clientReceiveThread != null)
                {
                    clientReceiveThread.Abort();
                }
            }
            else
            {
                ConnectToTcpServer();
            }
        }

#if UNITY_EDITOR
        private void LogPlayModeState(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                if (clientReceiveThread != null)
                {
                    clientReceiveThread.Abort();
                }
            }
        }
#endif
    }
}
