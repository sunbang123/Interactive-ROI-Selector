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

        public MainWindow()
        {
            InitializeComponent();

            TransformGroup group = new TransformGroup();

            group.Children.Add(zoomTransform);
            group.Children.Add(panTransform);

            ROICanvas.RenderTransform = group;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start panning
            isPanning = true;
            panStartPoint = e.GetPosition(ROIGrid);
            this.Cursor = Cursors.Hand;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 사진열기
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                MainImage.Source = bitmap;
            }
        }
    }
}