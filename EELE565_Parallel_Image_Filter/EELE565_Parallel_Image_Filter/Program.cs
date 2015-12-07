using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace EELE565_Parallel_Image_Filter
{
    class Program
    {
        const int numThreads = 8;

        static Thread[] threadArr;

        static Bitmap bitmapIn, bitmapOut;
        static byte[] byteArrIn,
                      byteArrOut;


        static IntPtr ptr;
        static int bytes;

        static int width, height;

        const int filterWidth = 16,
                    filterHeight = 16;

        const double k = 1.0 / (16 + 15);
        
        static double[,] kernel =
        {
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},

            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {  k,   k,   k,    k,   k,   k,   k,   k,    k,   k,   k,   k,    k,   k,   k,   k},

            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},

            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0,   k,  0.0, 0.0, 0.0, 0.0,  0.0, 0.0, 0.0, 0.0},
        };

        static void Main(string[] args)
        {
            /* start timing */
            Stopwatch stopwatch = Stopwatch.StartNew();

            /* local vars */
            int i;

            /* open image */
            Console.WriteLine("Opening image and setting things up...");
            //bitmapIn = new Bitmap(@"C: \Users\ssel\Google Drive\MLH_Ultra Docs\EELE 565 Parallel Processing\Project\IMG_8882.TIF");
            bitmapIn = new Bitmap(@"../../Pluto-Wide-FINAL-9-17-15.jpg");
            //bitmapIn = new Bitmap(@"../../photo3.jpg");
            bitmapOut = (Bitmap)bitmapIn.Clone();

            width = bitmapIn.Width;
            height = bitmapIn.Height;
            
            /* convert image to byte[] */
            bitmapToByte();

            /* create output byte array of same size as input */
            byteArrOut = new byte[byteArrIn.Count()];

            /* create threads */
            threadArr = new Thread[numThreads];
            for(i = 0; i < numThreads; i++)
            {
                int start_x, end_x;

                start_x = i * width / numThreads;
                end_x   = (i +1) * width / numThreads;

                Console.WriteLine("Thread {0}: columns {1} through {2}", i, start_x, end_x);

                threadArr[i] = new Thread(() => filter(start_x, 0, end_x, height));

                /* start thread */
                threadArr[i].Start();
            }

            /* wait for all worker threads to complete */
            for (i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }

            /* save output bitmap */
            Console.WriteLine("Saving output image");
            arrayToBitmap();
            bitmapOut.Save(@"../../output.jpg");

            /* stop timing */
            stopwatch.Stop();
            Console.WriteLine("Runtime: {0} seconds for {1} threads", 
                stopwatch.ElapsedMilliseconds / 1000.0, numThreads);

        }

        static void filter(int start_x, int start_y, int end_x, int end_y)
        {
            int x, y,   // image coordinates
                fx, fy, // filter image coordinates
                i, j;   // filter coordinates

            byte[]  RGBout = new byte[3] { 0, 0, 0 };

            int index;
            byte R, G, B;


            /* for each pixel in the image */
            for (x = start_x; x < end_x; x++)
            {
                for (y = start_y; y < end_y; y++)
                {
                    /* reset output RGB array */
                    RGBout[0] = 0;
                    RGBout[1] = 0;
                    RGBout[2] = 0;

                    /* for each filter element */
                    for (i = 0; i < filterWidth; i++)
                    {
                        for (j = 0; j < filterHeight; j++)
                        {
                            /* check for zero in kernal */
                            if (0.0 == kernel[i, j])
                            {
                                continue;
                            }

                            /* determine pixel coordinate of input image corosponding to filter coordinate */
                            fx = (x - filterWidth / 2 + i + width) % width;
                            fy = (y - filterHeight / 2 + j + height) % height;

                            /* get index of fx,fy in array */
                            index = (fx + fy*width)*3;

                            /* crunch the numbers */
                            RGBout[0] += (byte)(byteArrIn[index    ] * kernel[i, j]);
                            RGBout[1] += (byte)(byteArrIn[index + 1] * kernel[i, j]);
                            RGBout[2] += (byte)(byteArrIn[index + 2] * kernel[i, j]);
                        }
                    }

                    /* write new RGB value to output array */
                    index = (x + y*width)*3;
                    byteArrOut[index    ] = RGBout[0];
                    byteArrOut[index + 1] = RGBout[1];
                    byteArrOut[index + 2] = RGBout[2];

                }
            }
        }

        static void bitmapToByte()
        {
            Rectangle rect;
            System.Drawing.Imaging.BitmapData bmpData;

            /* lock bits to speedup access */
            rect = new Rectangle(0, 0, width, height);
            bmpData = bitmapIn.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmapIn.PixelFormat);

            /* Get the address of the first line. */
            ptr = bmpData.Scan0;

            /* Declare an array to hold the bytes of the bitmap. */
            bytes = Math.Abs(bmpData.Stride) * bitmapIn.Height;
            byteArrIn = new byte[bytes];

            /* Copy the RGB values into the array. */
            System.Runtime.InteropServices.Marshal.Copy(ptr, byteArrIn, 0, bytes);

            /* unlock bits */
            bitmapIn.UnlockBits(bmpData);
        }

        static void arrayToBitmap()
        {
            Rectangle rect;
            System.Drawing.Imaging.BitmapData bmpData;

            /* lock bits to speedup access */
            rect = new Rectangle(0, 0, width, height);
            bmpData = bitmapOut.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmapOut.PixelFormat);

            /* Get the address of the first line. */
            ptr = bmpData.Scan0;

            /* Copy the RGB values back to the bitmap */
            System.Runtime.InteropServices.Marshal.Copy(byteArrOut, 0, ptr, bytes);

            /* Unlock the bits. */
            bitmapOut.UnlockBits(bmpData);
        }

    }
}
