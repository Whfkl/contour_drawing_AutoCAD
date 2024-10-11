
using Autodesk.AutoCAD.EditorInput;
using OpenCvSharp;
using System.Collections.Generic;
using System;

namespace miniCV
    /// <summary>
/// The <c>miniCV</c> namespace provides functionality for processing images and videos
/// using OpenCVSharp for integration with AutoCAD. It contains classes for handling 
/// video capture, image edge detection, and processing using the Canny algorithm.
/// </summary>
/// <remarks>
/// This namespace is designed to work within the AutoCAD environment, allowing developers
/// to capture video frames or load images, extract edges, and use them in AutoCAD for drawing
/// polylines. The following key classes are provided:
/// <list type="bullet">
/// <item>
/// <term><see cref="VideoProcessor"/></term>
/// <description>Handles video capture and frame retrieval using OpenCVSharp.</description>
/// </item>
/// <item>
/// <term><see cref="ImageProcessor"/></term>
/// <description>Processes images for edge detection, converting to grayscale, blurring, and 
/// extracting edges using the Canny algorithm.</description>
/// </item>
/// </list>
/// </remarks>

{
    public class VideoProcessor : IDisposable
    {
        private VideoCapture videoCapture;
        private Editor editor;

        public VideoProcessor(string filePath, Editor editor)
        {
            this.editor = editor;
            try
            {
                videoCapture = new VideoCapture(filePath);
                if (!videoCapture.IsOpened())
                {
                    editor.WriteMessage("\n无法打开视频文件。\n");
                    videoCapture = null;
                }
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"\n系统异常: {ex.Message}\n");
            }
        }

        public Mat ReadFrame()
        {
            if (videoCapture == null) return null;

            Mat frame = new Mat();
            if (videoCapture.Read(frame))
            {
                return frame;
            }
            else
            {
                return null;
            }
        }

        public void Release()
        {
            videoCapture?.Release();
        }

        public void Dispose()
        {
            Release();
        }
    }

    public class ImageProcessor : IDisposable
    {
        private Mat image;
        private Editor editor;

        public ImageProcessor(string filePath, Editor editor)
        {
            this.editor = editor;
            try
            {
                LoadImage(filePath);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"\n系统异常: {ex.Message}\n");
            }
        }

        public ImageProcessor(Mat image)
        {
            this.image = image.Flip(FlipMode.X);
        }

        public void LoadImage(string filePath)
        {
            try
            {
                image = Cv2.ImRead(filePath);
                image = image.Flip(FlipMode.X);
                if (image.Empty())
                {
                    editor.WriteMessage("\n无法加载图片文件。\n");
                }
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"\n系统异常: {ex.Message}\n");
            }
        }

        public List<List<Tuple<int, int>>> ExtractEdges(double lowThreshold, double highThreshold)
        {
            if (image == null || image.Empty()) return new List<List<Tuple<int, int>>>();

            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 1.5);

            Mat edges = new Mat();
            Cv2.Canny(blurred, edges, lowThreshold, highThreshold);

            return GetEdgeCoordinates(edges);
        }

        private List<List<Tuple<int, int>>> GetEdgeCoordinates(Mat edges)
        {
            List<List<Tuple<int, int>>> contours = new List<List<Tuple<int, int>>>();
            Cv2.FindContours(edges, out Point[][] contoursArray, out _, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contoursArray)
            {
                List<Tuple<int, int>> contourPoints = new List<Tuple<int, int>>();
                foreach (var point in contour)
                {
                    contourPoints.Add(Tuple.Create(point.X, point.Y));
                }
                contours.Add(contourPoints);
            }

            return contours;
        }

        public void Dispose()
        {
            image?.Dispose();
        }
    }
}