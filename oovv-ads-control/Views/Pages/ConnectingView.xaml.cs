using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace oovv_ads_control.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConnectingView.xaml
    /// </summary>
    public partial class ConnectingView : UserControl
    {
        public ConnectingView()
        {
            InitializeComponent();

            // 资源里的对象不会生成代码隐藏字段，用 FindResource 取出斑马纹画刷的位移变换，驱动持续滚动动画
            Loaded += (_, _) =>
            {
                var stripeBrush = (DrawingBrush)FindResource("StripeBrush");
                var translate = (TranslateTransform)stripeBrush.Transform;

                // 0 -> 16 正好是一个瓷砖(Viewport)的宽度，绝对像素位移，滚动速度不会随进度条宽度变化
                var animation = new DoubleAnimation(0, 16, TimeSpan.FromSeconds(1))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };
                translate.BeginAnimation(TranslateTransform.XProperty, animation);
            };
        }
    }
}
