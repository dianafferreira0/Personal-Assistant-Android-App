using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Didimo.Utils
{
    public class ExifUtils
    {
        static bool ArraysAreEqual(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Add Exif data to a jpeg. If not a jpeg, or jpeg already has Exif data, returns the original jpegRawData.
        /// </summary>
        /// <param name="jpegRawData">The jpeg we want to modify. Must not have Exif data.</param>
        /// <param name="exifData">The jpeg with the added exif data. If jpegRawData isn't a jpeg, or already contains Exif data, returns jpegRawData.</param>
        /// <returns></returns>
        public static byte[] AddExifData(byte[] jpegRawData, byte[] exifData)
        {
            if (jpegRawData[2] == 0xFF && jpegRawData[3] == 0xD8)
            {
                Debug.LogWarning("Not a jpeg. Skipping...");
                return jpegRawData;
            }

            if (jpegRawData[2] == 0xFF && jpegRawData[3] == 0xE1)
            {
                Debug.LogWarning("Image already has Exif data. Skipping...");
                return jpegRawData;
            }

            byte[] result = new byte[jpegRawData.Length + exifData.Length];

            Array.Copy(jpegRawData, 0, result, 0, 2);
            Array.Copy(exifData, 0, result, 2, exifData.Length);
            Array.Copy(jpegRawData, 2, result, exifData.Length + 2, jpegRawData.Length - 2);

            return result;
        }

        /// <summary>
        /// Try to get the Exif data of a jpeg image. Returns null if it fails.
        /// </summary>
        /// <param name="imageFilePath">Path to the image file.</param>
        /// <returns>The bytes of the Exif data. Null if we fail to get Exif data.</returns>
        public static byte[] GetExifData(string imageFilePath)
        {
            List<byte> result = new List<byte>();

            try
            {
                using (FileStream fsSource = new FileStream(imageFilePath,
                    FileMode.Open, FileAccess.Read))
                {

                    // FFD8	FFE1 SSSS 457869660000 TTTT
                    // FFD8 - Start of Input marker
                    // FFE1 - APP1 Marker 
                    // SSSS - Size of APP1 Marker
                    // 457869660000 - ASCII character "Exif" and 2bytes of 0x00

                    // Read the source file into a byte array.
                    int numBytesToRead = 2;
                    int numBytesRead = 0;
                    byte[] bytes = new byte[numBytesToRead];
                    int n = fsSource.Read(bytes, 0, numBytesToRead);
                    numBytesRead += n;
                    byte[] cenas = new byte[] { 0xFF, 0xD8 };
                    if (n != numBytesToRead || !ArraysAreEqual(bytes, new byte[] { 0xFF, 0xD8 })) // 0xFFD8 is the Start Of Input (SOI) marker
                    {
                        Debug.LogWarning("This is not a jpeg. Start of Input Marker isn't present at start of file.");
                        return null;
                    }

                    n = fsSource.Read(bytes, 0, numBytesToRead);
                    numBytesRead += n;
                    if (n != numBytesToRead || !ArraysAreEqual(bytes, new byte[] { 0xFF, 0xE1 })) // 0xFFD8 is the APP1 Marker, used to avoid confluct with JFIF  format.
                    {
                        Debug.LogWarning("This jpeg doesn't have Exif data. App1 Marker was not found.");
                        return null;
                    }
                    result.AddRange(bytes);

                    // SSSS - Size of APP1 Marker
                    n = fsSource.Read(bytes, 0, numBytesToRead);
                    numBytesRead += n;
                    if (n != numBytesToRead) // Sanity check
                    {
                        Debug.LogWarning("Unexpected end of file.");
                        return null;
                    }
                    result.AddRange(bytes);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }

                    int app1DataSize = BitConverter.ToInt16(bytes, 0);

                    numBytesToRead = 6;
                    bytes = new byte[numBytesToRead];

                    n = fsSource.Read(bytes, 0, numBytesToRead);
                    numBytesRead += n;
                    // 457869660000 - ASCII character "Exif" and 2bytes of 0x00
                    if (n != numBytesToRead || !ArraysAreEqual(bytes, new byte[] { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 }))
                    {
                        Debug.LogWarning("This jpeg doesn't have Exif data. 'Exif' not specified in APP1 Data.");
                        return null;
                    }
                    result.AddRange(bytes);

                    numBytesToRead = app1DataSize - 6 - 2;
                    bytes = new byte[numBytesToRead];

                    n = fsSource.Read(bytes, 0, numBytesToRead);
                    numBytesRead += n;
                    if (n != numBytesToRead)
                    {
                        Debug.LogWarning("Unexpected end of file.");
                        return null;
                    }
                    result.AddRange(bytes);
                }
            }
            catch (FileNotFoundException ioEx)
            {
                Debug.LogError(ioEx);
                return null;
            }
            catch (OverflowException ovEx)
            {
                Debug.LogError(ovEx);
                result = null;
            }

            return result?.ToArray();
        }
    }
}