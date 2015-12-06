using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace EELE565_Parallel_Image_Filter
{
    class Program
    {
        static Bitmap bitmap_in,
                        bitmap_out;

        static int width, height;

        const int filterWidth = 5,
                    filterHeight = 5;

        const double k = 1.0 / 9.0;

        static double[,] kernel =
            {
                {0.0, 0.0,   k, 0.0, 0.0},
                {0.0, 0.0,   k, 0.0, 0.0},
                {  k,   k,   k,   k,   k},
                {0.0, 0.0,   k, 0.0, 0.0},
                {0.0, 0.0,   k, 0.0, 0.0}
            };

        static void Main(string[] args)
        {
            Console.WriteLine("Opening image and setting things up...");
            bitmap_in = new Bitmap(@"../../Pluto-Wide-FINAL-9-17-15.jpg");
            bitmap_in = new Bitmap(@"../../photo3.jpg");
            width = bitmap_in.Width;
            height = bitmap_in.Height;

            /* create output bitmap of same size */
            bitmap_out = new Bitmap(width, height);

            /* test by filtering whole image sequentially */
            Console.WriteLine("Sequentially filtering image...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            filter(0, 0, width, height);
            stopwatch.Stop();

            Console.WriteLine("Seqential Runtime: {0} seconds.", stopwatch.ElapsedMilliseconds / 1000.0);

            /* save output bitmap */
            Console.WriteLine("Saving output image");
            bitmap_out.Save(@"../../output.jpg", bitmap_in.RawFormat);
            
        }

        static void filter(int start_x, int start_y, int end_x, int end_y)
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
                            /* determine pixel coordinate of input image corosponding to filter coordinate */
                            fx = (x -  filterWidth / 2 + i + width ) % width;
                            fy = (y - filterHeight / 2 + j + height) % height;

                            /* get input pixel in RGB array */
                            pixel = bitmap_in.GetPixel(fx, fy);

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
