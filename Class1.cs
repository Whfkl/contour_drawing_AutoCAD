using Autodesk.AutoCAD.Runtime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.Windows.Forms;

namespace CADdrawing
{

    using Autodesk.AutoCAD.DatabaseServices;
    using miniCV;
    using Application = Autodesk.AutoCAD.ApplicationServices.Application;

    public class CADManager
    {
        private List<Entity> entities = new List<Entity>();
        private VideoProcessor videoProcessor;
        private Document doc = Application.DocumentManager.MdiActiveDocument;
        private Database db = Application.DocumentManager.MdiActiveDocument.Database;
        private int frameCount = 0;
        private Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;

        // 添加保存边缘提取参数的成员变量
        private double lowThreshold = 70.0;
        private double highThreshold = 150.0;

        // 清除本程序绘制的实体
        [CommandMethod("erase_entities")]
        public void ClearEntities()
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (Entity entity in entities)
                {
                    if (entity != null && !entity.IsErased && !entity.ObjectId.IsNull)
                    {
                        Entity ent = trans.GetObject(entity.ObjectId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                }

                entities.Clear();
                trans.Commit();
            }
        }

        // 在CAD中绘制多段线
        public void DrawPolylines(List<List<Tuple<int, int>>> polylines)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord blockTableRecord = (BlockTableRecord)trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var polyline in polylines)
                {
                    Polyline pline = new Polyline();
                    for (int i = 0; i < polyline.Count; i++)
                    {
                        var point = polyline[i];
                        pline.AddVertexAt(i, new Autodesk.AutoCAD.Geometry.Point2d(point.Item1, point.Item2), 0, 0, 0);
                    }
                    entities.Add(pline);
                    blockTableRecord.AppendEntity(pline);
                    trans.AddNewlyCreatedDBObject(pline, true);
                }
                trans.Commit();
            }
        }

        // 设置边缘提取参数
        [CommandMethod("set_edge_params")]
        public void SetEdgeParameters()
        {
            // 提示用户输入低阈值
            var lowThresholdPrompt = new PromptDoubleOptions("\n请输入Canny边缘检测的低阈值: ");
            lowThresholdPrompt.DefaultValue = lowThreshold;
            var lowThresholdResult = editor.GetDouble(lowThresholdPrompt);

            // 如果用户输入有效，设置新的低阈值
            if (lowThresholdResult.Status == PromptStatus.OK)
            {
                lowThreshold = lowThresholdResult.Value;
            }

            // 提示用户输入高阈值
            var highThresholdPrompt = new PromptDoubleOptions("\n请输入Canny边缘检测的高阈值: ");
            highThresholdPrompt.DefaultValue = highThreshold;
            var highThresholdResult = editor.GetDouble(highThresholdPrompt);

            // 如果用户输入有效，设置新的高阈值
            if (highThresholdResult.Status == PromptStatus.OK)
            {
                highThreshold = highThresholdResult.Value;
            }

            editor.WriteMessage($"\n边缘提取参数已更新: 低阈值 = {lowThreshold}, 高阈值 = {highThreshold}\n");
        }

        [CommandMethod("nextframe")]
        public void NextFrame()
        {
            if (videoProcessor == null)
            {
                string filePath = ShowFileDialog("选择视频文件", "视频文件|*.mp4");
                if (string.IsNullOrEmpty(filePath)) return;

                videoProcessor = new VideoProcessor(filePath, editor);
            }

            if (frameCount++ != 0) ClearEntities();

            var frame = videoProcessor.ReadFrame();
            if (frame == null)
            {
                editor.WriteMessage("\n视频播放完毕。\n");
                videoProcessor.Release();
                return;
            }

            var imageProcessor = new ImageProcessor(frame);
            var edges = imageProcessor.ExtractEdges(lowThreshold, highThreshold);  // 使用保存的阈值
            DrawPolylines(edges);
        }

        [CommandMethod("picDraw")]
        // 从文件中读取图片并绘制边缘
        public void DrawPicture()
        {
            string filePath = ShowFileDialog("选择图片文件", "图片文件|*.jpg;*.png");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var imageProcessor = new ImageProcessor(filePath, editor);
                var edges = imageProcessor.ExtractEdges(lowThreshold, highThreshold);  // 使用保存的阈值
                DrawPolylines(edges);
                imageProcessor.Dispose();
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"\n系统异常: {ex.Message}\n");
            }
        }

        // 显示文件选择对话框
        private string ShowFileDialog(string title, string filter)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = filter;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
                return null;
            }
        }
    }
}

