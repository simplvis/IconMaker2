using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace IconMaker2
{
    public static class IconRenderer
    {
        // 컬러 이모지를 완벽하게 렌더링하기 위한 전용 메서드
        public static Bitmap RenderEmojiToBitmap(string emoji, int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // TextRenderer는 GDI+보다 이모지 컬러 표현에 유리함
                TextRenderer.DrawText(g, emoji, new Font("Segoe UI Emoji", size / 2),
                    new Rectangle(0, 0, size, size), Color.Black, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            return bmp;
        }

        // 텍스트를 픽셀 데이터로 변환 (미리보기용)
        public static List<PixelInfo> GenerateTextPixels(string text, Font font, string hexColor, int gridSize, bool keepOriginalColor = false)
        {
            var previewPixels = new List<PixelInfo>();
            if (string.IsNullOrEmpty(text)) return previewPixels;

            int bufferSize = 256;
            using (Bitmap bigBmp = new Bitmap(bufferSize, bufferSize))
            using (Graphics gBig = Graphics.FromImage(bigBmp))
            {
                // 컬러 이모지 지원을 위해 폰트 설정
                string fontFamily = keepOriginalColor ? "Segoe UI Emoji" : font.FontFamily.Name;
                using (Font renderFont = new Font(fontFamily, 40, font.Style))
                {
                    gBig.TextRenderingHint = keepOriginalColor ? 
                        System.Drawing.Text.TextRenderingHint.ClearTypeGridFit : 
                        System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    
                    gBig.Clear(Color.Transparent);
                    
                    StringFormat sf = StringFormat.GenericTypographic;
                    sf.FormatFlags |= StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
                    SizeF sz = gBig.MeasureString(text, renderFont, PointF.Empty, sf);
                    
                    // 컬러 이모지는 Brushes.Black이 아니라 실제 컬러로 그려져야 함
                    gBig.DrawString(text, renderFont, Brushes.Black, (bufferSize - sz.Width) / 2, (bufferSize - sz.Height) / 2, sf);
                }

                int minX = bufferSize, minY = bufferSize, maxX = -1, maxY = -1;
                bool hasPixels = false;
                for (int y = 0; y < bufferSize; y++)
                {
                    for (int x = 0; x < bufferSize; x++)
                    {
                        Color c = bigBmp.GetPixel(x, y);
                        if (c.A > 32) // 투명도 기준 완화
                        {
                            if (x < minX) minX = x; if (x > maxX) maxX = x;
                            if (y < minY) minY = y; if (y > maxY) maxY = y;
                            hasPixels = true;
                        }
                    }
                }

                if (hasPixels)
                {
                    int contentW = maxX - minX + 1;
                    int contentH = maxY - minY + 1;
                    int targetSize = gridSize - 2;

                    using (Bitmap finalBmp = new Bitmap(gridSize, gridSize))
                    using (Graphics gFinal = Graphics.FromImage(finalBmp))
                    {
                        gFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        float ratio = Math.Min((float)targetSize / contentW, (float)targetSize / contentH);
                        int dw = (int)(contentW * ratio);
                        int dh = (int)(contentH * ratio);
                        
                        gFinal.DrawImage(bigBmp, 
                            new Rectangle((gridSize - dw) / 2, (gridSize - dh) / 2, dw, dh), 
                            new Rectangle(minX, minY, contentW, contentH), 
                            GraphicsUnit.Pixel);

                        for (int fy = 0; fy < gridSize; fy++)
                        {
                            for (int fx = 0; fx < gridSize; fx++)
                            {
                                Color c = finalBmp.GetPixel(fx, fy);
                                if (c.A > 64)
                                {
                                    string colorHex = keepOriginalColor ? 
                                        $"#{c.R:X2}{c.G:X2}{c.B:X2}" : hexColor;
                                    previewPixels.Add(new PixelInfo { X = fx, Y = fy, Color = colorHex });
                                }
                            }
                        }
                    }
                }
            }
            return previewPixels;
        }

        // 비트맵 이미지를 픽셀 데이터로 변환
        public static List<PixelInfo> BitmapToPixels(Bitmap sourceBmp, int gridSize, bool useOriginalColor = true, string? fallbackColor = null)
        {
            var pixels = new List<PixelInfo>();
            using (Bitmap resized = new Bitmap(gridSize, gridSize))
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.Clear(Color.Transparent);
                g.DrawImage(sourceBmp, 0, 0, gridSize, gridSize);

                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        Color c = resized.GetPixel(x, y);
                        if (c.A > 64)
                        {
                            string hex = useOriginalColor ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : (fallbackColor ?? "#000000");
                            pixels.Add(new PixelInfo { X = x, Y = y, Color = hex });
                        }
                    }
                }
            }
            return pixels;
        }

        // PNG 기반 고품질 ICO 파일 저장
        public static void SaveAsHighQualityIco(Bitmap bmp, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write((short)0);      // Reserved
                writer.Write((short)1);      // Type (1 = Icon)
                writer.Write((short)1);      // Count
                
                byte width = (byte)(bmp.Width >= 256 ? 0 : bmp.Width);
                byte height = (byte)(bmp.Height >= 256 ? 0 : bmp.Height);
                writer.Write(width);         
                writer.Write(height);        
                writer.Write((byte)0);       
                writer.Write((byte)0);       
                writer.Write((short)1);      
                writer.Write((short)32);     

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();
                    writer.Write((int)pngBytes.Length); 
                    writer.Write(22);                   
                    writer.Write(pngBytes);
                }
                writer.Flush();
            }
        }
    }
}
