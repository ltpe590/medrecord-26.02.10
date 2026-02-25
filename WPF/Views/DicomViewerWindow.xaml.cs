using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace WPF.Views
{
    public partial class DicomViewerWindow : Window
    {
        private DicomFile?    _dicomFile;
        private DicomDataset? _dataset;
        private string        _filePath = string.Empty;
        private int    _totalFrames  = 1;
        private int    _currentFrame = 0;
        private double _zoom   = 1.0;
        private double _wc     = 40;
        private double _ww     = 400;
        private bool   _invert = false;
        private bool   _isPanning = false;
        private Point  _panStart;
        private double _offsetX = 0, _offsetY = 0;
        private short[]? _pixels;
        private int _imgWidth, _imgHeight;

        public DicomViewerWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            Loaded += (_, _) => LoadDicomFile(filePath);
        }

        private void LoadDicomFile(string path)
        {
            try
            {
                _dicomFile   = DicomFile.Open(path);
                _dataset     = _dicomFile.Dataset;
                _totalFrames = _dataset.Contains(DicomTag.NumberOfFrames)
                    ? _dataset.GetSingleValueOrDefault(DicomTag.NumberOfFrames, 1) : 1;
                _imgWidth  = _dataset.GetSingleValueOrDefault(DicomTag.Columns, 512);
                _imgHeight = _dataset.GetSingleValueOrDefault(DicomTag.Rows,    512);
                if (_dataset.Contains(DicomTag.WindowCenter))
                    _wc = _dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 40.0);
                if (_dataset.Contains(DicomTag.WindowWidth))
                    _ww = _dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 400.0);
                SliderWC.Value = _wc;
                SliderWW.Value = Math.Max(1, _ww);
                TxtFileName.Text     = Path.GetFileName(path);
                TxtPatientMeta.Text  = "Patient: "  + GetTag(DicomTag.PatientName);
                TxtModalityMeta.Text = "Modality: " + GetTag(DicomTag.Modality);
                var d = GetTag(DicomTag.StudyDate);
                TxtDateMeta.Text = d.Length == 8 ? "Date: " + d[6..8] + "/" + d[4..6] + "/" + d[0..4] : string.Empty;
                TxtSizeMeta.Text = _imgWidth + " x " + _imgHeight + " px";
                TxtPixelSpacing.Text = _dataset.Contains(DicomTag.PixelSpacing)
                    ? "Spacing: " + _dataset.GetValue<decimal>(DicomTag.PixelSpacing, 0).ToString("F3") + " mm"
                    : string.Empty;
                _currentFrame = 0;
                RenderFrame();
                FitToView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open DICOM file:\n" + ex.Message, "DICOM Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }

        private short[] ExtractPixels(int frame)
        {
            var pixelData     = DicomPixelData.Create(_dataset!);
            var frameData     = pixelData.GetFrame(frame);
            var bytes         = frameData.Data;
            int bitsAllocated = _dataset!.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16);
            int pixelCount    = _imgWidth * _imgHeight;
            var result        = new short[pixelCount];
            if (bitsAllocated == 8)
            {
                for (int i = 0; i < pixelCount && i < bytes.Length; i++)
                    result[i] = bytes[i];
            }
            else
            {
                bool isSigned = _dataset!.GetSingleValueOrDefault(DicomTag.PixelRepresentation, (ushort)0) == 1;
                for (int i = 0; i < pixelCount && (i * 2 + 1) < bytes.Length; i++)
                {
                    ushort raw = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                    result[i]  = isSigned ? (short)raw : (short)Math.Min(raw, (int)short.MaxValue);
                }
            }
            double slope     = _dataset!.GetSingleValueOrDefault(DicomTag.RescaleSlope,     1.0);
            double intercept = _dataset!.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
            if (slope != 1.0 || intercept != 0.0)
                for (int i = 0; i < pixelCount; i++)
                    result[i] = (short)Math.Clamp(result[i] * slope + intercept,
                        (double)short.MinValue, (double)short.MaxValue);
            return result;
        }

        private void RenderFrame()
        {
            if (_dataset == null) return;
            try { _pixels = ExtractPixels(_currentFrame); TxtFrameInfo.Text = (_currentFrame + 1) + " / " + _totalFrames; RenderWithCurrentWL(); }
            catch (Exception ex) { TxtFrameInfo.Text = "Error: " + ex.Message; }
        }

        private void RenderWithCurrentWL()
        {
            if (_pixels == null) return;
            int w = _imgWidth, h = _imgHeight;
            var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Gray8, null);
            bmp.Lock();
            unsafe
            {
                byte* ptr = (byte*)bmp.BackBuffer;
                double lo = _wc - _ww / 2.0; double rng = _ww;
                for (int i = 0; i < w * h; i++)
                {
                    double v = (_pixels[i] - lo) / rng;
                    v = v < 0 ? 0 : v > 1 ? 1 : v;
                    byte b = (byte)(v * 255);
                    ptr[i] = _invert ? (byte)(255 - b) : b;
                }
            }
            bmp.AddDirtyRect(new Int32Rect(0, 0, w, h)); bmp.Unlock(); bmp.Freeze();
            DicomImage.Source = bmp; DicomImage.Width = w * _zoom; DicomImage.Height = h * _zoom;
            Canvas.SetLeft(DicomImage, _offsetX); Canvas.SetTop(DicomImage, _offsetY);
        }

        private void FitToView()
        {
            if (_imgWidth == 0 || _imgHeight == 0) return;
            double vw = ImageViewport.ActualWidth  > 10 ? ImageViewport.ActualWidth  : 800;
            double vh = ImageViewport.ActualHeight > 10 ? ImageViewport.ActualHeight : 560;
            _zoom = Math.Min(vw / _imgWidth, vh / _imgHeight) * 0.95;
            SliderZoom.Value = Math.Clamp(_zoom, 0.1, 8.0);
            _offsetX = (vw - _imgWidth * _zoom) / 2; _offsetY = (vh - _imgHeight * _zoom) / 2;
            RenderWithCurrentWL();
        }

        private void BtnPrevFrame_Click(object sender, RoutedEventArgs e)
        { if (_currentFrame > 0) { _currentFrame--; RenderFrame(); } }
        private void BtnNextFrame_Click(object sender, RoutedEventArgs e)
        { if (_currentFrame < _totalFrames - 1) { _currentFrame++; RenderFrame(); } }
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _invert = false;
            if (_dataset != null)
            {
                _wc = _dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 40.0);
                _ww = _dataset.GetSingleValueOrDefault(DicomTag.WindowWidth,  400.0);
                SliderWC.Value = _wc; SliderWW.Value = Math.Max(1, _ww);
            }
            FitToView();
        }
        private void BtnInvert_Click(object sender, RoutedEventArgs e)
        { _invert = !_invert; RenderWithCurrentWL(); }

        private void BtnSaveJpg_Click(object sender, RoutedEventArgs e)
        {
            if (_pixels == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save DICOM frame as JPEG", Filter = "JPEG Image|*.jpg",
                FileName = Path.GetFileNameWithoutExtension(_filePath)
                           + (_totalFrames > 1 ? "_f" + (_currentFrame + 1) : "") + ".jpg"
            };
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                int w = _imgWidth, h = _imgHeight;
                var full = new WriteableBitmap(w, h, 96, 96, PixelFormats.Gray8, null);
                full.Lock();
                unsafe
                {
                    byte* ptr = (byte*)full.BackBuffer;
                    double lo = _wc - _ww / 2.0; double rng = _ww;
                    for (int i = 0; i < w * h; i++)
                    {
                        double v = (_pixels[i] - lo) / rng;
                        v = v < 0 ? 0 : v > 1 ? 1 : v;
                        byte b = (byte)(v * 255);
                        ptr[i] = _invert ? (byte)(255 - b) : b;
                    }
                }
                full.AddDirtyRect(new Int32Rect(0, 0, w, h)); full.Unlock();
                var converted = new FormatConvertedBitmap(full, PixelFormats.Rgb24, null, 0);
                var encoder   = new JpegBitmapEncoder { QualityLevel = 92 };
                encoder.Frames.Add(BitmapFrame.Create(converted));
                using var fs = File.OpenWrite(dlg.FileName);
                encoder.Save(fs);
                MessageBox.Show("Saved: " + Path.GetFileName(dlg.FileName), "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void SliderWC_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        { if (!IsLoaded) return; _wc = e.NewValue; TxtWC.Text = ((int)_wc).ToString(); RenderWithCurrentWL(); }
        private void SliderWW_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        { if (!IsLoaded) return; _ww = Math.Max(1, e.NewValue); TxtWW.Text = ((int)_ww).ToString(); RenderWithCurrentWL(); }
        private void SliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _zoom = e.NewValue; TxtZoom.Text = ((int)(_zoom * 100)) + "%";
            if (DicomImage.Source != null)
            {
                DicomImage.Width = _imgWidth * _zoom; DicomImage.Height = _imgHeight * _zoom;
                Canvas.SetLeft(DicomImage, _offsetX); Canvas.SetTop(DicomImage, _offsetY);
            }
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor  = e.Delta > 0 ? 1.15 : 0.87;
            double newZoom = Math.Clamp(_zoom * factor, 0.1, 8.0);
            Point  cursor  = e.GetPosition(ImageViewport);
            _offsetX = cursor.X - (cursor.X - _offsetX) * (newZoom / _zoom);
            _offsetY = cursor.Y - (cursor.Y - _offsetY) * (newZoom / _zoom);
            _zoom = newZoom; SliderZoom.Value = _zoom;
        }
        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        { _isPanning = true; _panStart = e.GetPosition(ImageViewport); ImageViewport.CaptureMouse(); ImageViewport.Cursor = Cursors.SizeAll; }
        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        { _isPanning = false; ImageViewport.ReleaseMouseCapture(); ImageViewport.Cursor = Cursors.Cross; }
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var pos = e.GetPosition(ImageViewport);
                _offsetX += pos.X - _panStart.X; _offsetY += pos.Y - _panStart.Y; _panStart = pos;
                Canvas.SetLeft(DicomImage, _offsetX); Canvas.SetTop(DicomImage, _offsetY);
            }
            else if (_pixels != null)
            {
                var pos = e.GetPosition(DicomImage);
                int px = (int)(pos.X / _zoom); int py = (int)(pos.Y / _zoom);
                if (px >= 0 && px < _imgWidth && py >= 0 && py < _imgHeight)
                    TxtPixelInfo.Text = "HU: " + _pixels[py * _imgWidth + px] + "  (" + px + "," + py + ")";
            }
        }

        private string GetTag(DicomTag tag)
        {
            try { return _dataset?.GetSingleValueOrDefault(tag, string.Empty) ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
