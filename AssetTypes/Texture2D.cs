using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using AssetStudio;
using AssetStudioExporter.AssetTypes.Feature;
using AssetStudioExporter.Export;
using AssetStudioExporter.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudioExporter.AssetTypes
{
    public class Texture2D : TextureFile, IAssetType, IAssetTypeReader<Texture2D>, IAssetTypeExporter
    {
        public static AssetClassID AssetClassID { get; } = AssetClassID.Texture2D;

        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        new TextureFormat m_TextureFormat;

        public Texture2D() { }
        public Texture2D(TextureFile textureFile)
        {
            // 自动生成，复制构造函数
            m_Name = textureFile.m_Name;
            m_ForcedFallbackFormat = textureFile.m_ForcedFallbackFormat;
            m_DownscaleFallback = textureFile.m_DownscaleFallback;
            m_Width = textureFile.m_Width;
            m_Height = textureFile.m_Height;
            m_CompleteImageSize = textureFile.m_CompleteImageSize;
            m_TextureFormat = (TextureFormat)textureFile.m_TextureFormat;
            m_MipCount = textureFile.m_MipCount;
            m_MipMap = textureFile.m_MipMap;
            m_IsReadable = textureFile.m_IsReadable;
            m_ReadAllowed = textureFile.m_ReadAllowed;
            m_StreamingMipmaps = textureFile.m_StreamingMipmaps;
            m_StreamingMipmapsPriority = textureFile.m_StreamingMipmapsPriority;
            m_ImageCount = textureFile.m_ImageCount;
            m_TextureDimension = textureFile.m_TextureDimension;
            m_TextureSettings = textureFile.m_TextureSettings;
            m_LightmapFormat = textureFile.m_LightmapFormat;
            m_ColorSpace = textureFile.m_ColorSpace;
            m_StreamData = textureFile.m_StreamData;

            pictureData = textureFile.pictureData;
        }

        private UnityVersion version;
        public static Texture2D Read(AssetTypeValueField value, UnityVersion version)
        {
            var t = new Texture2D(ReadTextureFile(value))
            {
                version = version
            };
            return t;
        }

        public bool Export(AssetsFileInstance assetsFile, Stream stream)
        {
            return Export(assetsFile, stream, ExporterSetting.Default.ImageExportFormat);
        }        
        
        public byte[]? GetAssetData(AssetsFileInstance assetsFile)
        {
            byte[] rawdata;
            if (pictureData?.Length > 0)
            {
                rawdata = pictureData;
            }
            else
            {
                var path = m_StreamData.path;
                var size = m_StreamData.size;
                var offset = (long)m_StreamData.offset;

                if (size == 0 || string.IsNullOrEmpty(path))
                {
                    Console.WriteLine($"[WARN] Texture2D '{m_Name}' doesn't have any data, skipped");
                    return null;
                }

                rawdata = assetsFile.GetAssetData(path, size, offset);
            }
            return rawdata;
        }

        public bool Export(AssetsFileInstance assetsFile, Stream stream, ImageFormat format)
        {
            var image = GetImage(assetsFile);
            if (image is null)
            {
                return false;
            }

            using(image)
            {
                switch (format)
                {
                    case ImageFormat.Jpeg:
                        image.SaveAsJpeg(stream);
                        break;
                    case ImageFormat.Webp:
                        image.SaveAsWebp(stream);
                        break;
                    case ImageFormat.Tga: 
                        image.SaveAsTga(stream);
                        break;
                    case ImageFormat.Tiff: 
                        image.SaveAsTiff(stream);
                        break;
                    case ImageFormat.Bmp: 
                        image.SaveAsBmp(stream); 
                        break;                
                    case ImageFormat.Gif: 
                        image.SaveAsGif(stream); 
                        break;
                    default:
                        image.SaveAsPng(stream);
                        break;
                }
            }
            return true;
        }


        public Image<Bgra32>? GetImage(AssetsFileInstance assetsFile, bool flip = true)
        {
            var rawdata = GetAssetData(assetsFile);
            if (rawdata is null || rawdata.Length == 0)
            {
                return null;
            }

            if (ExporterSetting.Default.TextureDecoder == TextureDecoderType.AssetStudio)
            {
                return ConvertToImage(rawdata, flip);
            }
            else
            {
                var decoded = DecodeManaged(rawdata, m_TextureFormat, m_Width, m_Height);
                return ConvertToImageFromDecoded(decoded, flip);
            }
        }

        Image<Bgra32>? ConvertToImage(byte[] data, bool flip = true)
        {
            var converter = new Texture2DConverter(this, version);
            var buff = BigArrayPool<byte>.Shared.Rent(m_Width * m_Height * 4);
            try
            {
                if (converter.DecodeTexture2D(data, buff))
                {
                    var image = Image.LoadPixelData<Bgra32>(buff, m_Width, m_Height);
                    if (flip)
                    {
                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                    }
                    return image;
                }
                return null;
            }
            finally
            {
                BigArrayPool<byte>.Shared.Return(buff);
            }
        }
        
        Image<Bgra32> ConvertToImageFromDecoded(byte[] decoded, bool flip = true)
        {
            var image = Image.LoadPixelData<Bgra32>(decoded, m_Width, m_Height);
            if (flip)
            {
                image.Mutate(x => x.Flip(FlipMode.Vertical));
            }
            return image;
        }

        public string GetFileExtension(string fileName)
        {
            if (ExporterSetting.Default.ImageExportFormat == ImageFormat.Auto)
                return ".png";
            return "." + ExporterSetting.Default.ImageExportFormat.ToString().ToLower();
        }
    }
}
