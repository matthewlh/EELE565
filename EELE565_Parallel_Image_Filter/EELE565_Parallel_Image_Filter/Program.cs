using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace EELE565_Parallel_Image_Filter
{
    class Program
    {
        static Bitmap   bitmap_in,
                        bitmap_out;

        static int  width, height,
                    filterWidth, filterHeight;

        static void Main(string[] args)
        {
            /* Open image and set things up */ 
            bitmap_in = new Bitmap("");
            width = bitmap_in.Width;
            height = bitmap_in.Height;

            /* create output bitmap of same size */
            bitmap_out = new Bitmap(width, height);

            
        }

        static void filter(int start_x, int start_y, int end_x, int end_y)
        {
            int x, y, //image coordinates
                i, j; //filter coordinates

            byte[]  RGBin  = new byte[3] { 0.0, 0.0, 0.0 },
                    RGBout = new byte[3] { 0.0, 0.0, 0.0 };

            Color pixel;
                    

            /* for each pixel in the image */
            for(x = start_x; x < end_x; x++)
            {
                for(y = start_y; y < end_y; y++)
                {
                    /* get input pixel in RGB array */
                    pixel = bitmap_in.GetPixel(x, y);
                    RGBin[0] = pixel.R;
                    RGBin[1] = pixel.G;
                    RGBin[2] = pixel.B;

                    /* reset output RGB array */
                    RGBout[0] = 0;
                    RGBout[1] = 0;
                    RGBout[2] = 0;

                    /* for each filter element */
                    for (i = 0; i < filterWidth; i++)
                    {
                        for(j = 0; j < filterHeight; j++)
                        {

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
