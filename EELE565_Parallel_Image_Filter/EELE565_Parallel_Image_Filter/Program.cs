using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace EELE565_Parallel_Image_Filter
{
    class Program
    {
        const int numThreads = 8;

        static Thread[] threadArr;

        public static Bitmap    bitmapIn,
                                bitmapOut;

        static byte[,,] pixelArrIn,
                        pixelArrOut;

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
            /* local vars */
            int i;
            
            /* start timing */
            Stopwatch stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Opening image...");
            bitmapIn = new Bitmap(@"../../Pluto-Wide-FINAL-9-17-15.jpg");
            //bitmapIn = new Bitmap(@"../../photo3.jpg");

            Console.WriteLine("Setting parameters...");
            width = bitmapIn.Width;
            height = bitmapIn.Height;

            pixelArrIn  = new byte[width, height, 3];
            pixelArrOut = new byte[width, height, 3];
            
            bitmapOut = new Bitmap(width, height);

            Console.WriteLine("Converting bitmap to array...");
            loadBitmap();

            /* create threads */
            Console.WriteLine("Spawning threads...");
            threadArr = new Thread[numThreads];
            for(i = 0; i < numThreads; i++)
            {
                int start_x, end_x;

                start_x = i * width / numThreads;
                end_x   = (i +1) * width / numThreads;

                Console.WriteLine("Thread {0}: columns {1} through {2}", i, start_x, end_x);

                threadArr[i] = new Thread(() => filter(start_x, 0, end_x, height));

                threadArr[i].Start();
            }

            /* sync threads */
            for (i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }

            /* save output bitmap */
            Console.WriteLine("Converting array to bitmap...");
            saveBitmap();
            Console.WriteLine("Saving output image...");
            bitmapOut.Save(@"../../output.jpg", bitmapIn.RawFormat);

            /* done timing things */
            stopwatch.Stop();
            Console.WriteLine("Runtime: {0} seconds for {1} threads",
                stopwatch.ElapsedMilliseconds / 1000.0, numThreads);

        }

        static void filter(int start_x, int start_y, int end_x, int end_y)
        {
            int x, y,   // image coordinates
                fx, fy, // filter image coordinates
                i, j;   // filter coordinates                    

            /* for each pixel in the image */
            for(x = start_x; x < end_x; x++)
            {
                for(y = start_y; y < end_y; y++)
                {
                    /* init output RGB array */
                    pixelArrOut[x, y, 0] = 0;
                    pixelArrOut[x, y, 1] = 0;
                    pixelArrOut[x, y, 2] = 0;

                    /* for each filter element */
                    for (i = 0; i < filterWidth; i++)
                    {
                        for (j = 0; j < filterHeight; j++)
                        {
                            ///* check for zero in kernal */
                            //if(kernel[i, j] == 0.0)
                            //{
                            //    continue;
                            //}

                            /* determine pixel coordinate of input image corosponding to filter coordinate */
                            fx = (x -  filterWidth / 2 + i + width ) % width;
                            fy = (y - filterHeight / 2 + j + height) % height;

                            /* crunch the numbers */
                            pixelArrOut[x, y, 0] += (byte)(pixelArrIn[fx, fy, 0] * kernel[i, j]);
                            pixelArrOut[x, y, 1] += (byte)(pixelArrIn[fx, fy, 1] * kernel[i, j]);
                            pixelArrOut[x, y, 2] += (byte)(pixelArrIn[fx, fy, 2] * kernel[i, j]);
                        }
                    }

                    /* write new RGB value to output pixel */
                    

                }
            }
        }

        static void loadBitmap()
        {
            int x, y;

            /* for each pixel in the image */
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    /* save RGB data from Bitmap */
                    pixelArrIn[x, y, 0] = bitmapIn.GetPixel(x, y).R;
                    pixelArrIn[x, y, 1] = bitmapIn.GetPixel(x, y).G;
                    pixelArrIn[x, y, 2] = bitmapIn.GetPixel(x, y).B;
                }
            }
        }

        static void saveBitmap()
        {
            int x, y;
            Color pixel;

            /* for each pixel in the image */
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    pixel = Color.FromArgb(pixelArrOut[x, y, 0], pixelArrOut[x, y, 1], pixelArrOut[x, y, 2]);
                    bitmapOut.SetPixel(x, y, pixel);
                }
            }
        }

    }
}
