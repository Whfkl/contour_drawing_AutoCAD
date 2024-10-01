using Autodesk.AutoCAD.Runtime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;


namespace CADdrawing
{
    using Autodesk.AutoCAD.DatabaseServices;
    using miniCV;
    using System.Web.ModelBinding;

    public class Class1
    {
        List<Entity> entities = new List<Entity>();
        VideoProcessor videoProcessor = new VideoProcessor(@"xxxxx\Desktop\shaoshuai.mp4");
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;
        int frame_count = 0;
        public void ClearEnts()
        {
            // 获取当前文档和数据库
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 打开模型空间
                BlockTable blockTable = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 遍历模型空间中的所有对象并删除
                foreach (ObjectId id in modelSpace)
                {
                    Entity entity = (Entity)trans.GetObject(id, OpenMode.ForWrite);
                    entity.Erase();
                }

                // 提交事务
                trans.Commit();
            }
        }
        public void drawPolylines(List<List<Tuple<int, int>>> polylines)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord blockTableRecord = (BlockTableRecord)trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var polyline in polylines)
                {
                    // 创建多段线对象
                    Polyline pline = new Polyline();

                    // 添加点
                    for (int i = 0; i < polyline.Count; i++)
                    {
                        var point = polyline[i];
                        pline.AddVertexAt(i, new Autodesk.AutoCAD.Geometry.Point2d(point.Item1, point.Item2), 0, 0, 0);
                    }

                    // 添加到模型空间
                    entities.Add(pline);
                    blockTableRecord.AppendEntity(pline);
                    trans.AddNewlyCreatedDBObject(pline, true);
                }

                // 提交事务
                trans.Commit();

            }
        }


        [CommandMethod("helloworld")]
        public void HelloWorld()
        {

        }

        [CommandMethod("nextframe")]
        //953 frames
        public void nextframe()
        {
            if(frame_count++ != 0)
            {
                ClearEnts();
            }
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var frame = videoProcessor.ReadFrame();
            if (frame == null)
            {
                  return;
            }
            var ip = new ImageProcessor(frame);
            var edges = ip.ExtractEdges(70, 150);
            drawPolylines(edges);
        }
    }
}
namespace miniCV
{
    public class VideoProcessor
    {

        private VideoCapture videoCapture;

        public VideoProcessor(string filePath)
        {
            videoCapture = new VideoCapture(filePath);
            if (!videoCapture.IsOpened())
            {
                throw new System.Exception("无法打开视频文件。");
            }
        }

        public Mat ReadFrame()
        {
            Mat frame = new Mat();
            if (videoCapture.Read(frame))
            {
                return frame; // 返回读取到的帧
            }
            else
            {
                return null; // 返回 null 表示没有更多帧
            }
        }

        public void Release()
        {
            videoCapture.Release(); 
        }
    }
    public class ImageProcessor
    {
        private Mat image;

        public ImageProcessor(string filePath)
        {
            LoadImage(filePath);
        }
        public ImageProcessor(Mat image)
        {
            this.image = image.Flip(FlipMode.X);
        }

        public void LoadImage(string filePath)
        {
            image = Cv2.ImRead(filePath);
        }

        public List<List<Tuple<int, int>>> ExtractEdges(double lowThreshold, double highThreshold)
        {
            // 转换为灰度图像
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // 应用高斯模糊
            Mat blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 1.5);

            // 使用 Canny 边缘检测
            Mat edges = new Mat();
            Cv2.Canny(blurred, edges, lowThreshold, highThreshold);

            // 提取边缘坐标
            return GetEdgeCoordinates(edges);
        }

        private List<List<Tuple<int, int>>> GetEdgeCoordinates(Mat edges)
        {
            List<List<Tuple<int, int>>> contours = new List<List<Tuple<int, int>>>();

            // 查找轮廓
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

        public void ShowImage(Mat img)
        {
            Cv2.ImShow("Edges", img);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();
        }
        public void Dispose()
        {
            image.Dispose();
        }
    }
}