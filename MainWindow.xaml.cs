using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media.Imaging;
using Emgu.CV.Structure;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using Npgsql;
using System.Text;

namespace Сontact_Angle_Meter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage originalImage;
        private BitmapImage processedImage;
        private string originalImagePath;
        private double contactAngle;

        public MainWindow()
        {
            InitializeComponent();
            ImagePreviewTextBlock.Visibility = Visibility.Hidden;
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePathTextBox.Text = openFileDialog.FileName;
                originalImagePath = openFileDialog.FileName;
                originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                imageBox.Source = originalImage;
                StartProcessingButton.IsEnabled = true;
                ImagePreviewTextBlock.Visibility = Visibility.Visible;
                ImagePathTextBox.IsEnabled = false;
            }
        }

        private void StartProcessingButton_Click(object sender, EventArgs e)
        {
            ImageProcessing(originalImagePath);
            ImageProcessingWindow imageWindow = new ImageProcessingWindow(processedImage, contactAngle);
            imageWindow.Show();
        }

        private void OpenDataBaseButton_CLick(object sender, EventArgs e)
        {
            DataBaseWindow DataBaseWindow = new DataBaseWindow();
            DataBaseWindow.Show();
        }

        private void ImageProcessing(string imagePath)
        {
            Mat originalImage = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.Color);
            // Преобразуем изображение в оттенки серого
            Mat grayImage = new Mat();
            CvInvoke.CvtColor(originalImage, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            // Пороговое преобразование для нахождения капли
            CvInvoke.Threshold(grayImage, grayImage, 100, 255, Emgu.CV.CvEnum.ThresholdType.BinaryInv);

            // Поиск контуров
            var contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            CvInvoke.FindContours(grayImage, contours, null, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

            // Выбираем самый крупный контур (предполагается, что это капля)
            Emgu.CV.Util.VectorOfPoint largestContour = null;
            double maxArea = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = contours[i];
                }
            }

            if (largestContour == null)
            {
                MessageBox.Show("Контур капли не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Рисуем контур капли (зеленый цвет)
            CvInvoke.Polylines(originalImage, largestContour, true, new MCvScalar(0, 255, 0), 2);

            // Найти точки касания с поверхностью (крайние левые и правые)
            System.Drawing.Point[] contourPoints = largestContour.ToArray();

            // Найдем крайние точки по горизонтальной линии (нижняя часть капли)
            int minY = contourPoints.Min(p => p.Y); // Минимальная высота капли
            int baselineY = minY + 5; // Линия чуть выше самой нижней точки капли (для устранения шумов)

            var leftMost = contourPoints.Where(p => p.Y >= baselineY).OrderBy(p => p.X).First();
            var rightMost = contourPoints.Where(p => p.Y >= baselineY).OrderByDescending(p => p.X).First();

            // Рисуем касательные линии к капле
            CvInvoke.Line(originalImage, leftMost, new System.Drawing.Point(leftMost.X - 50, leftMost.Y), new MCvScalar(0, 0, 255), 2);
            CvInvoke.Line(originalImage, rightMost, new System.Drawing.Point(rightMost.X + 50, rightMost.Y), new MCvScalar(0, 0, 255), 2);

            // Определение длины отрезков и пропусков
            int segmentLength = 20;
            int gapLength = 10;

            // Расчет общего расстояния между начальной и конечной точкой
            double totalLength = Math.Sqrt(Math.Pow(rightMost.X - leftMost.X, 2) + Math.Pow(rightMost.Y - leftMost.Y, 2));

            // Направляющий вектор
            double dx = (rightMost.X - leftMost.X) / totalLength;
            double dy = (rightMost.Y - leftMost.Y) / totalLength;

            // Цикл рисования прерывистой линии
            for (double dist = 0; dist < totalLength; dist += segmentLength + gapLength)
            {
                System.Drawing.Point p1 = new System.Drawing.Point((int)(leftMost.X + dist * dx), (int)(leftMost.Y + dist * dy));
                System.Drawing.Point p2 = new System.Drawing.Point((int)(leftMost.X + Math.Min(dist + segmentLength, totalLength) * dx), (int)(leftMost.Y + Math.Min(dist + segmentLength, totalLength) * dy));
                CvInvoke.Line(originalImage, p1, p2, new MCvScalar(255, 255, 255), 2, LineType.AntiAlias);
            }

            // Вычисление угла между касательными
            double slopeLeft = (double)(leftMost.Y - baselineY) / (leftMost.X - (leftMost.X - 50));
            double slopeRight = (double)(rightMost.Y - baselineY) / (rightMost.X - (rightMost.X + 50));

            double angleLeft = Math.Atan(slopeLeft) * 180 / Math.PI;
            double angleRight = Math.Atan(slopeRight) * 180 / Math.PI;
            contactAngle = (Math.Abs(angleRight) + Math.Abs(angleLeft)) / 2;
            contactAngle = Math.Round(contactAngle, 1); // Округление до 1 знака после запятой

            // Определение направления касательных
            double leftAngleRadians = Math.PI * angleLeft / 180.0;
            double rightAngleRadians = Math.PI * angleRight / 180.0;

            // Рисуем касательные линии под рассчитанным углом
            int lineLength = 100;

            // Касательная слева
            int leftEndX = leftMost.X + (int)(lineLength * Math.Cos(leftAngleRadians));
            int leftEndY = leftMost.Y - (int)(lineLength * Math.Sin(leftAngleRadians)); // В WPF ось Y направлена вниз, поэтому нужно вычитать

            CvInvoke.Line(originalImage, leftMost, new System.Drawing.Point(leftEndX, leftEndY), new MCvScalar(0, 0, 255), 2);

            // Касательная справа
            int rightEndX = rightMost.X - (int)(lineLength * Math.Cos(rightAngleRadians));
            int rightEndY = rightMost.Y + (int)(lineLength * Math.Sin(rightAngleRadians)); // Складываем, чтобы корректно нарисовать линию вверх

            CvInvoke.Line(originalImage, rightMost, new System.Drawing.Point(rightEndX, rightEndY), new MCvScalar(0, 0, 255), 2);

            // --- Отображение угла по центру капли ---
            // Вычисляем центр капли
            var boundingRect = CvInvoke.BoundingRectangle(largestContour);
            int centerX = boundingRect.X + boundingRect.Width / 2;
            int centerY = boundingRect.Y + boundingRect.Height / 2;

            // Выводим текст с углом по центру капли
            string angleText = $"{contactAngle:F1}";
            CvInvoke.PutText(originalImage, angleText, new System.Drawing.Point(centerX - 30, centerY),
                Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.0, new MCvScalar(255, 255, 255), 2);

            // Отрисовываем вручную значок градуса
            System.Drawing.Point degreePosition = new System.Drawing.Point(centerX + 50, centerY - 20);  //Позиция значка 
            CvInvoke.Circle(originalImage, degreePosition, 5, new MCvScalar(255, 255, 255), 2);

            // Преобразуем изображение для отображения в WPF
            processedImage = ConvertMatToBitmapImage(originalImage);
        }

        // Метод для конвертации Mat в BitmapImage для вывода в окне
        private BitmapImage ConvertMatToBitmapImage(Mat mat)
        {
            MemoryStream ms = new MemoryStream();
            Bitmap bitmap = mat.ToBitmap();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            return bitmapImage;
        }

    }    
}