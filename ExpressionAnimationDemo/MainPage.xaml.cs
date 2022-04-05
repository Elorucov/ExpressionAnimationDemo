using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace ExpressionAnimationDemo {
    class Test {
        public Test(int num) {
            Number = num;
        }

        public int Number { get; private set; }
    }

    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 480));
        }

        ScrollViewer ListSV = null;
        ScrollViewer currentScrollViewer;
        double AvatarHeight { get { return Avatar.ActualHeight + Avatar.Margin.Top + Avatar.Margin.Bottom; } }
        const double UserNamePanelScale = 4;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            List<Test> samples = new List<Test>();
            foreach (int i in Enumerable.Range(1, 100)) {
                samples.Add(new Test(i));
            }
            SampleListView.ItemsSource = samples;
            SampleGridView.ItemsSource = samples;

            ListSV = GetScrollViewer(SampleListView);
            ListSV.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            // Disable focus on flip view (IsTabStop is not working for unknown purposes ¯\_(ツ)_/¯)
            MainFlipView.GotFocus += (a, b) => {
                (ActionButtons.Children[0] as Control).Focus(FocusState.Programmatic);
            };

            SetupMargins();
            Header.SizeChanged += Header_SizeChanged;

            ChangeScrollViewerForAnimation();
            MainFlipView.SelectionChanged += FlipViewSelectionChanged;

            MainFlipView.SizeChanged += MainFlipView_SizeChanged;
            PointerEntered += MainPage_PointerEntered; // need to change IndicatorMode for scroll bar.
            scrollBar.Scroll += ScrollBar_Scroll;
            InfoSV.ViewChanging += SVViewChanging;
            ListSV.ViewChanging += SVViewChanging;
            GridSV.ViewChanging += SVViewChanging;
        }

        private void FlipViewSelectionChanged(object sender, SelectionChangedEventArgs e) {
            ChangeScrollViewerForAnimation();
        }

        private void Header_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetupMargins();
        }

        private void MainFlipView_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetupScrollBar();
        }

        private void MainPage_PointerEntered(object sender, PointerRoutedEventArgs e) {
            var pointer = e.GetCurrentPoint(sender as UIElement);
            if (pointer.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse) {
                scrollBar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;
            } else {
                scrollBar.IndicatorMode = ScrollingIndicatorMode.TouchIndicator;
            }
        }

        private void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
            currentScrollViewer.ChangeView(null, e.NewValue, null);
        }

        private void ChangeScrollViewerForAnimation() {
            switch (MainFlipView.SelectedIndex) {
                case 0:
                    currentScrollViewer = InfoSV;
                    break;
                case 1:
                    currentScrollViewer = ListSV;
                    break;
                case 2:
                    currentScrollViewer = GridSV;
                    break;
            }
            SetupExpressionAnimations(currentScrollViewer);
            ChangeActionButtonsAvailability(currentScrollViewer.VerticalOffset);
            SetupScrollBar();
            UpdateScrollBarPosition();
        }

        private void SetupMargins() {
            InfoFVIMargin.Height = Header.ActualHeight;
            ListFVIMargin.Height = Header.ActualHeight;
            SampleGridView.Margin = new Thickness(SampleGridView.Margin.Left, Header.ActualHeight, SampleGridView.Margin.Right, SampleGridView.Margin.Bottom);

            Visual hv = ElementCompositionPreview.GetElementVisual(UserName);
            hv.CenterPoint = new Vector3((int)UserName.ActualWidth / 2, 0, 0);
        }

        private async void SetupScrollBar() {
            await Task.Delay(50);
            scrollBar.ViewportSize = currentScrollViewer.ViewportHeight;
            scrollBar.Maximum = currentScrollViewer.ScrollableHeight;
        }

        private void UpdateScrollBarPosition() {
            scrollBar.Value = currentScrollViewer.VerticalOffset;

            string debug = $"SVEH: {currentScrollViewer.ExtentHeight}\n";
            debug += $"SVSH: {currentScrollViewer.ScrollableHeight}\n";
            debug += $"SVVO: {currentScrollViewer.VerticalOffset}\n";
            debug += $"SVVH: {currentScrollViewer.ViewportHeight}\n";
            debug += $"SBM:  {scrollBar.Maximum}\n";
            debug += $"SBV:  {scrollBar.Value}\n";
            debug += $"SBVP: {scrollBar.ViewportSize}";
            dbg.Text = debug;
        }

        private void SVViewChanging(object sender, ScrollViewerViewChangingEventArgs e) {
            ScrollViewer sv = sender as ScrollViewer;

            // Autoscroll to fix header in full or compact state.
            double voff = e.FinalView.VerticalOffset;
            double header = Header.ActualHeight;
            double compactHeader = header - AvatarHeight - ActionButtons.ActualHeight - (UserName.ActualHeight / UserNamePanelScale);
            double hh = header - compactHeader;

            if (e.IsInertial && e.NextView.VerticalOffset == voff && voff < hh) {
                sv.CancelDirectManipulations();
                double off = voff < hh / 2 ? 0 : hh;
                sv.ChangeView(null, off, null, false);
            }

            // Disable hit testing for action buttons if offset != 0.
            ChangeActionButtonsAvailability(e.NextView.VerticalOffset);

            // Change scroll bar position
            UpdateScrollBarPosition();
        }

        private void ChangeActionButtonsAvailability(double scrollVerticalOffset) {
            bool isEnabled = scrollVerticalOffset == 0;
            ActionButtons.IsHitTestVisible = isEnabled;
            foreach (Control button in ActionButtons.Children) {
                button.IsTabStop = isEnabled;
            }
        }

        private ScrollViewer GetScrollViewer(ListViewBase listview) {
            try {
                return VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(listview, 0), 0) as ScrollViewer;
            } catch {
                return null;
            }
        }

        private void SetupExpressionAnimations(ScrollViewer scrollViewer) {
            // Expression animations
            ElementCompositionPreview.SetIsTranslationEnabled(Avatar, true);
            ElementCompositionPreview.SetIsTranslationEnabled(UserName, true);
            ElementCompositionPreview.SetIsTranslationEnabled(ActionButtons, true);
            ElementCompositionPreview.SetIsTranslationEnabled(FakePivot, true);

            float avaHeight = (float)(Math.Round(AvatarHeight));
            float usernameHeight = (float)Math.Round(UserName.ActualHeight); // user name & online info stackpanel's height
            float actionButtonsHeight = (float)Math.Round(ActionButtons.ActualHeight);

            float a = 1.0f / avaHeight * (avaHeight + actionButtonsHeight); // for header animation
            float b = (float)UserNamePanelScale;
            float c = usernameHeight / b;

            // Avatar offset & opacity
            SetupExpressionAnimation(scrollViewer, Avatar, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight}, 0)", "Translation.Y");
            SetupExpressionAnimation(scrollViewer, Avatar, $"(Clamp(1 / -{avaHeight + actionButtonsHeight} * Scroll.Translation.Y, 0, 1) - 1) * -1", "Opacity");

            // Username translation & scale
            SetupExpressionAnimation(scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight}, 0)", "Translation.Y");
            SetupExpressionAnimation(scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (-{avaHeight + actionButtonsHeight + c} * -{b}), 1 / -{b}, 0) + 1", "Scale.X");
            SetupExpressionAnimation(scrollViewer, UserName, $"Clamp(Scroll.Translation.Y / (-{avaHeight + actionButtonsHeight + c} * -{b}), 1 / -{b}, 0) + 1", "Scale.Y");

            // Header background offset
            SetupExpressionAnimation(scrollViewer, HeaderBackground, $"Clamp(Scroll.Translation.Y, -{avaHeight + actionButtonsHeight + c}, 0)", "Offset.Y");
            
            // Action buttons scale & opacity
            foreach (FrameworkElement button in ActionButtons.Children) {
                ElementCompositionPreview.SetIsTranslationEnabled(button, true);
                Visual abv = ElementCompositionPreview.GetElementVisual(button);
                abv.CenterPoint = new Vector3((int)button.ActualWidth / 2, 0, 0);

                SetupExpressionAnimation(scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Scale.X");
                SetupExpressionAnimation(scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Scale.Y");
                SetupExpressionAnimation(scrollViewer, button, $"(Clamp(Scroll.Translation.Y / -{avaHeight + actionButtonsHeight}, 0, 1) - 1) * -1", "Opacity");
            }

            // Action buttons panel translation
            SetupExpressionAnimation(scrollViewer, ActionButtons, $"Clamp(Scroll.Translation.Y / (1 / {avaHeight} * ({avaHeight + actionButtonsHeight})), -{avaHeight + usernameHeight + c}, 0)", "Translation.Y");

            // Pivot tabs translation
            SetupExpressionAnimation(scrollViewer, FakePivot, $"Clamp(Scroll.Translation.Y, -{avaHeight + actionButtonsHeight + c}, 0)", "Translation.Y");
        }

        private void SetupExpressionAnimation(ScrollViewer scrollViewer, UIElement target, string expression, string property) {
            var compositor = Window.Current.Compositor;
            var sprops = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);

            var exp = compositor.CreateExpressionAnimation();
            exp.SetReferenceParameter("Scroll", sprops);
            exp.Expression = expression;

            Visual visual = ElementCompositionPreview.GetElementVisual(target);
            visual.StartAnimation(property, exp);
        }
    }
}