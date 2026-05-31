using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InteractiveROISelector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isPanning = false;

        Point panStartPoint = new Point();

        TranslateTransform panTransform = new TranslateTransform();
        ScaleTransform zoomTransform = new ScaleTransform();

        bool isDrawing = false;
        Point roiStartPoint;
        Rectangle currentROI = new Rectangle();
        List<Rectangle> roiList = new List<Rectangle>();

        public MainWindow()
        {
            InitializeComponent();

            TransformGroup group = new TransformGroup();

            group.Children.Add(zoomTransform);
            group.Children.Add(panTransform);

            ROICanvas.RenderTransform = group;
        }

        private void Canvas_PanningDown(object sender, MouseButtonEventArgs e)
        {
            // Start panning
            isPanning = true;
            panStartPoint = e.GetPosition(ROIGrid);
            this.Cursor = Cursors.Hand;
        }

        private void Canvas_PanningUp(object sender, MouseButtonEventArgs e)
        {
            // Stop panning
            isPanning = false;
            this.Cursor = Cursors.Arrow;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                Point currentPoint = e.GetPosition(ROIGrid);
                Vector delta = currentPoint - panStartPoint;
                panTransform.X += delta.X;
                panTransform.Y += delta.Y;
                panStartPoint = currentPoint;
            }

            // ROI 사각형을 그리는 중일 때
            if (isDrawing && currentROI != null)
            {
                Point currentPoint = e.GetPosition(ROICanvas);

                // [핵심 보정] 역방향 드래그 예외 처리
                // 가로/세로 길이는 무조건 절대값(Abs)으로 양수만 나오게 처리
                double width = Math.Abs(currentPoint.X - roiStartPoint.X);
                double height = Math.Abs(currentPoint.Y - roiStartPoint.Y);

                // 사각형이 시작되는 좌측 상단 꼭지점은 두 점 중 더 작은 값(Min)으로 지정
                double left = Math.Min(currentPoint.X, roiStartPoint.X);
                double top = Math.Min(currentPoint.Y, roiStartPoint.Y);

                // 계산된 크기와 위치를 현재 사각형에 실시간 적용
                currentROI.Width = width;
                currentROI.Height = height;
                Canvas.SetLeft(currentROI, left);
                Canvas.SetTop(currentROI, top);
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom in or out
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            Point mousePosition = e.GetPosition(ROICanvas);
            double oldScale = zoomTransform.ScaleX;
            double newScale = oldScale * zoomFactor;

            panTransform.X -= mousePosition.X * (newScale - oldScale);
            panTransform.Y -= mousePosition.Y * (newScale - oldScale);

            zoomTransform.ScaleX = newScale;
            zoomTransform.ScaleY = newScale;
        }

        private void Button_ImgOpen(object sender, RoutedEventArgs e)
        {
            // 사진열기
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png; *.tif) | *.jpg; *.jpeg; *.png; *.tif; *.tiff ";
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                MainImage.Source = bitmap;

                // ROI 사각형 초기화 (도화지에서 지우고 메모리 비우기)
                // 1. 도화지에서 리스트에 있는 모든 사각형을 찾아 제거
                foreach (Rectangle roi in roiList)
                {
                    ROICanvas.Children.Remove(roi);
                }

                // 2. 리스트 자체도 깨끗하게 비우기
                roiList.Clear();
                currentROI = null;

                // 상태 스위치 초기화
                isDrawing = false;
                isPanning = false;

                // 카메라(줌/팬) 원위치로 초기화 (1배율, X/Y 0)
                panTransform.X = 0;
                panTransform.Y = 0;
                zoomTransform.ScaleX = 1;
                zoomTransform.ScaleY = 1;
            }
        }

        private void Canvas_RoiDown(object sender, MouseButtonEventArgs e)
        {
            // Start drawing ROI
            isDrawing = true;
            roiStartPoint = e.GetPosition(ROICanvas);
            currentROI = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0))
            };
            Canvas.SetLeft(currentROI, roiStartPoint.X);
            Canvas.SetTop(currentROI, roiStartPoint.Y);
            ROICanvas.Children.Add(currentROI);
        }

        private void Canvas_RoiUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing && currentROI != null)
            {
                // Ctrl 키가 눌려있는지 확인 (키보드 상태 감지)
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (currentROI.Width < 10 || currentROI.Height < 10) return; // 너무 작으면 무시

                    // 완벽한 돋보기 배율(Scale) 찾기 (0.9는 여백 10% 확보)
                    double scaleX = ROIGrid.ActualWidth / currentROI.Width;
                    double scaleY = ROIGrid.ActualHeight / currentROI.Height;
                    double targetScale = Math.Min(scaleX, scaleY) * 0.9;

                    // 사각형의 정중앙 좌표 찾기
                    double roiCenterX = Canvas.GetLeft(currentROI) + (currentROI.Width / 2);
                    double roiCenterY = Canvas.GetTop(currentROI) + (currentROI.Height / 2);

                    // 밀대(Pan)와 돋보기(Zoom) 이동
                    panTransform.X = (ROIGrid.ActualWidth / 2) - (roiCenterX * targetScale);
                    panTransform.Y = (ROIGrid.ActualHeight / 2) - (roiCenterY * targetScale);
                    zoomTransform.ScaleX = targetScale;
                    zoomTransform.ScaleY = targetScale;

                    // 줌 기능으로 썼으니 ROI 박스는 캔버스에서 깔끔하게 지워줌
                    ROICanvas.Children.Remove(currentROI);
                }
                else
                {
                    // ==========================================
                    // [A타입] 일반 ROI 지정 모드
                    // ==========================================
                    roiList.Add(currentROI); // 리스트에 기록
                }

                // 다음 그리기를 위해 현재 변수 비우기
                currentROI = null;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 1. Ctrl 키와 Z 키가 동시에 눌렸는지 확인
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Z)
            {
                // 2. 리스트에 지울 ROI 사각형이 하나라도 남아있는지 확인
                if (roiList.Count > 0)
                {
                    // 3. 리스트의 가장 마지막 번호(인덱스) 구하기
                    int lastIndex = roiList.Count - 1;

                    // 4. 장부에서 마지막 사각형 객체 가져오기
                    Rectangle lastROI = roiList[lastIndex];

                    // 5. 캔버스에서 눈에 보이는 ROI 사각형 지우기
                    ROICanvas.Children.Remove(lastROI);

                    // 6. 리스트에서도 완전히 기록 삭제하기
                    roiList.RemoveAt(lastIndex);
                }
            }
        }
    }
}