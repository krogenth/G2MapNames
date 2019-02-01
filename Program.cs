using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;

namespace mapFilename
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(dir, "*.mdt", System.IO.SearchOption.AllDirectories);

            BinaryReader fileIn;
            BinaryWriter fileOut;

            fileOut = new BinaryWriter(File.Open(dir + "maps.csv", FileMode.Create));

            byte[] inBuffer = new byte[UInt16.MaxValue * 4];
            foreach (string var in files)
            {
                UInt32 sectionLength = 0, sectionPointer = 0, headerLength = 0;

                fileIn = new BinaryReader(File.Open(var, FileMode.Open));
                inBuffer = fileIn.ReadBytes(0x80);

                sectionLength = BitConverter.ToUInt32(inBuffer, 0x78);
                sectionPointer = BitConverter.ToUInt32(inBuffer, 0x7C);

                if (!((sectionLength == 0) || (sectionPointer == 0)))
                {
                    Array.Clear(inBuffer, 0, 0x80);
                    fileIn.BaseStream.Position = sectionPointer;
                    inBuffer = fileIn.ReadBytes(0x04);

                    headerLength = BitConverter.ToUInt32(inBuffer, 0);

                    UInt32 numPointers = (headerLength - 12) / 4;
                    UInt32[] pointers = new UInt32[numPointers];

                    Array.Clear(inBuffer, 0, 0x04);
                    inBuffer = fileIn.ReadBytes((int)headerLength);

                    for (int i = 0; i < numPointers; i++)
                        pointers[i] = BitConverter.ToUInt16(inBuffer, (i * 4) + 6);

                    for (UInt32 i = 0; i < numPointers; i++)
                        pointers[i] *= 8;

                    Array.Clear(inBuffer, 0, (int)headerLength);
                    inBuffer = fileIn.ReadBytes((int)sectionLength - (int)headerLength);

                    for (UInt32 pointerVar = numPointers; pointerVar > 0; pointerVar--)
                    {
                        if (((inBuffer[pointers[pointerVar] + 1] == 0x80) || (inBuffer[pointers[pointerVar] + 1] == 0x01)) && (inBuffer[pointers[pointerVar]] == 0x17))
                        {
                            UInt32 mapNameSize;
                            if (pointerVar == numPointers)
                                mapNameSize = (sectionLength - headerLength) - pointers[pointerVar];
                            else
                                mapNameSize = pointers[pointerVar + 1] - pointers[pointerVar];

                            string mapName = System.Text.Encoding.UTF8.GetString(inBuffer, (int)pointers[pointerVar], (int)mapNameSize);

                            if (!mapName.Contains("to "))
                            {
                                fileOut.Write(var);
                                fileOut.Write(",");
                                fileOut.Write(mapName);
                                fileOut.Write(0x0D0A);

                                break;
                            }
                        }
                    }
                    Array.Clear(inBuffer, 0, (int)sectionLength - (int)headerLength);
                }
                fileIn.Close();
            }

            fileOut.Close();
            Console.ReadLine();

        }
    }
}
