# CAD Drawing with Video and Image Processing
## 项目简介

该项目旨在集成 AutoCAD 与 OpenCVSharp，提供视频帧逐帧绘图以及从图像中提取边缘并绘制多段线的功能。用户可以通过简单的命令在 AutoCAD 中执行视频和图像处理，便于可视化和分析。

## 功能

- **逐帧绘制视频**：用户可以通过"nextframe"命令从视频中读取帧，并在 AutoCAD 中绘制提取到的边缘。
- **图像绘制**：用户可以执行"picdraw"命令选择图像文件，从中提取边缘并绘制到 AutoCAD 中。
- **自定义边缘提取参数**：使用"set_edge_params"命令在绘制前设置 Canny 边缘检测的阈值。

## 使用方法
1. 打开AutoCAD 2022（推荐），运行 netload 命令，加载 CADdrawing.dll

2. 输入 nextframe 命令，根据提示选择视频路径，重复nextframe命令将逐帧绘制。

3. （可选）使用 picdraw 命令可以绘制单张图片。

4. （可选）set_edge_params 命令调整canny算法的阈值。

5. （可选） erase_entities 命令删除已经绘制的图像。
## 环境要求

- AutoCAD
- OpenCVSharp
- .NET Framework


