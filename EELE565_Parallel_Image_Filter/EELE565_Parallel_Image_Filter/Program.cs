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
        const int numThreads = 4;

        static Thread[] threadArr;

        public static Bitmap bitmap_in,
                      bitmap_out;

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

            Console.WriteLine("Opening image and setting things up...");
            bitmap_in = new Bitmap(@"../../Pluto-Wide-FINAL-9-17-15.jpg");
            //bitmap_in = new Bitmap(@"../../photo3.jpg");
            width = bitmap_in.Width;
            height = bitmap_in.Height;

            /* create output bitmap of same size */
            bitmap_out = new Bitmap(width, height);

            /* create threads */
            threadArr = new Thread[numThreads];
            for(i = 0; i < numThreads; i++)
            {
                int start_x, end_x;

                start_x = i * width / numThreads;
                end_x   = (i +1) * width / numThreads;

                Console.WriteLine("Thread {0}: columns {1} through {2}", i, start_x, end_x);

                threadArr[i] = new Thread(() => filter((Bitmap)bitmap_in.Clone(), start_x, 0, end_x, height));
            }

            /* start threads */            
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (i = 0; i < numThreads; i++)
            {
                threadArr[i].Start();
            }

            /* sync threads */
            for (i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }
            stopwatch.Stop();

            Console.WriteLine("Runtime: {0} seconds for {1} threads", 
                stopwatch.ElapsedMilliseconds / 1000.0, numThreads);

            /* save output bitmap */
            Console.WriteLine("Saving output image");
            bitmap_out.Save(@"../../output.jpg", bitmap_in.RawFormat);
            
        }

        static void filter(Bitmap bitmap_in_clone, int start_x, int start_y, int end_x, int end_y)
        {
            int x, y,   // image coordinates
                fx, fy, // filter image coordinates
                i, j;   // filter coordinates

            byte[]  RGBin  = new byte[3] { 0, 0, 0 },
                    RGBout = new byte[3] { 0, 0, 0 };

            Color pixel;
                    

            /* for each pixel in the image */
            for(x = start_x; x < end_x; x++)
            {
                for(y = start_y; y < end_y; y++)
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
                            if(0.0 == kernel[i, j])
                            {
                                continue;
                            }

                            /* determine pixel coordinate of input image corosponding to filter coordinate */
                            fx = (x -  filterWidth / 2 + i + width ) % width;
                            fy = (y - filterHeight / 2 + j + height) % height;

                            /* get input pixel in RGB array */
                            pixel = bitmap_in_clone.GetPixel(fx, fy);

                            /* crunch the numbers */
                            RGBout[0] += (byte)(pixel.R * kernel[i, j]);
                            RGBout[1] += (byte)(pixel.G * kernel[i, j]);
                            RGBout[2] += (byte)(pixel.B * kernel[i, j]);
                        }
                    }

                    /* write new RGB value to output pixel */
                    pixel = Color.FromArgb(RGBout[0], RGBout[1], RGBout[2]);
                    bitmap_out.SetPixel(x, y, pixel);

                }
            }
        }


    }
}
