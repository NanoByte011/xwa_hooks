﻿using JeremyAnsel.IO.Locator;
using JeremyAnsel.Xwa.Opt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace hook_32bpp_net
{
    public static class Main
    {
        private static bool _isD3dRendererHookEnabled = IsD3dRendererHookEnabled();

        private static bool IsD3dRendererHookEnabled()
        {
            IList<string> lines = XwaHooksConfig.GetFileLines("ddraw.cfg");
            bool isD3dRendererHookEnabled = XwaHooksConfig.GetFileKeyValueInt(lines, "D3dRendererHookEnabled", 1) != 0;
            return isD3dRendererHookEnabled;
        }

        private static IList<string> _getCustomFileLines_lines;
        private static string _getCustomFileLines_name;
        private static string _getCustomFileLines_mission;
        private static string _getCustomFileLines_hangar;
        private static byte _getCustomFileLines_hangarIff;

        private static SevenZip.Compression.LZMA.Decoder _decoder;
        private static byte[] _decoderProperties;

        public static IList<string> GetCustomFileLines(string name)
        {
            string xwaMissionFileName = Marshal.PtrToStringAnsi(new IntPtr(0x06002E8));
            int currentGameState = Marshal.ReadInt32(new IntPtr(0x09F60E0 + 0x25FA9));
            int updateCallback = Marshal.ReadInt32(new IntPtr(0x09F60E0 + 0x25FB1 + currentGameState * 0x850 + 0x844));
            bool isTechLibraryGameStateUpdate = updateCallback == 0x00574D70;
            string hangar = Marshal.PtrToStringAnsi(new IntPtr(0x00ABD680));
            byte hangarIff = Marshal.ReadByte(new IntPtr(0x00ABD680) + 255);

            if (isTechLibraryGameStateUpdate)
            {
                _getCustomFileLines_name = name;
                _getCustomFileLines_mission = null;
                _getCustomFileLines_lines = XwaHooksConfig.GetFileLines("FlightModels\\" + name + ".txt");
                _getCustomFileLines_hangar = null;
                _getCustomFileLines_hangarIff = 0;

                if (_getCustomFileLines_lines.Count == 0)
                {
                    _getCustomFileLines_lines = XwaHooksConfig.GetFileLines("FlightModels\\default.ini", name);
                }
            }
            else
            {
                if (_getCustomFileLines_name != name
                    || _getCustomFileLines_mission != xwaMissionFileName
                    || _getCustomFileLines_hangar != hangar
                    || _getCustomFileLines_hangarIff != hangarIff)
                {
                    _getCustomFileLines_name = name;
                    _getCustomFileLines_mission = xwaMissionFileName;
                    _getCustomFileLines_hangar = hangar;
                    _getCustomFileLines_hangarIff = hangarIff;

                    string mission = XwaHooksConfig.GetStringWithoutExtension(xwaMissionFileName);
                    _getCustomFileLines_lines = XwaHooksConfig.GetFileLines(mission + "_" + name + ".txt");

                    if (_getCustomFileLines_lines.Count == 0)
                    {
                        _getCustomFileLines_lines = XwaHooksConfig.GetFileLines(mission + ".ini", name);
                    }

                    if (_getCustomFileLines_hangar != null && !_getCustomFileLines_hangar.EndsWith("\\"))
                    {
                        IList<string> hangarLines = new List<string>();

                        if (hangarLines.Count == 0)
                        {
                            hangarLines = XwaHooksConfig.GetFileLines(_getCustomFileLines_hangar + name + _getCustomFileLines_hangarIff + ".txt");
                        }

                        if (hangarLines.Count == 0)
                        {
                            hangarLines = XwaHooksConfig.GetFileLines(_getCustomFileLines_hangar + ".ini", name + _getCustomFileLines_hangarIff);
                        }

                        if (hangarLines.Count == 0)
                        {
                            hangarLines = XwaHooksConfig.GetFileLines(_getCustomFileLines_hangar + name + ".txt");
                        }

                        if (hangarLines.Count == 0)
                        {
                            hangarLines = XwaHooksConfig.GetFileLines(_getCustomFileLines_hangar + ".ini", name);
                        }

                        foreach (string line in hangarLines)
                        {
                            _getCustomFileLines_lines.Add(line);
                        }
                    }

                    if (_getCustomFileLines_lines.Count == 0)
                    {
                        _getCustomFileLines_lines = XwaHooksConfig.GetFileLines("FlightModels\\" + name + ".txt");
                    }

                    if (_getCustomFileLines_lines.Count == 0)
                    {
                        _getCustomFileLines_lines = XwaHooksConfig.GetFileLines("FlightModels\\default.ini", name);
                    }
                }
            }

            return _getCustomFileLines_lines;
        }

        private static int GetFlightgroupsDefaultCount(string optName)
        {
            int count = 0;

            //for (int index = 255; index >= 0; index--)
            //{
            //    string skinName = "Default_" + index.ToString(CultureInfo.InvariantCulture);

            //    if (GetSkinDirectoryLocatorPath(optName, skinName) != null)
            //    {
            //        count = index + 1;
            //        break;
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => 0,
                (range, _, localValue) =>
                {
                    int localCount = 0;

                    for (int index = range.Item2 - 1; index >= range.Item1; index--)
                    {
                        string skinName = "Default_" + index.ToString(CultureInfo.InvariantCulture);

                        if (GetSkinDirectoryLocatorPath(optName, skinName) != null)
                        {
                            localCount = index + 1;
                            break;
                        }
                    }

                    return Math.Max(localCount, localValue);
                },
                localCount =>
                {
                    lock (locker)
                    {
                        if (localCount > count)
                        {
                            count = localCount;
                        }
                    }
                });

            return count;
        }

        private static int GetFlightgroupsCount(IList<string> objectLines, string optName)
        {
            int count = 0;

            //for (int index = 255; index >= 0; index--)
            //{
            //    string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
            //    string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

            //    if (!string.IsNullOrEmpty(value))
            //    {
            //        count = index + 1;
            //        break;
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => 0,
                (range, _, localValue) =>
                {
                    int localCount = 0;

                    for (int index = range.Item2 - 1; index >= range.Item1; index--)
                    {
                        string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
                        string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

                        if (!string.IsNullOrEmpty(value))
                        {
                            localCount = index + 1;
                            break;
                        }
                    }

                    return Math.Max(localCount, localValue);
                },
                localCount =>
                {
                    lock (locker)
                    {
                        if (localCount > count)
                        {
                            count = localCount;
                        }
                    }
                });

            return count;
        }

        private static List<int> GetFlightgroupsColors(IList<string> objectLines, string optName, int fgCount, bool hasDefaultSkin)
        {
            bool hasBaseSkins = hasDefaultSkin || !string.IsNullOrEmpty(XwaHooksConfig.GetFileKeyValue(objectLines, optName));

            var colors = new List<int>();

            //for (int index = 0; index < 256; index++)
            //{
            //    string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
            //    string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

            //    if (!string.IsNullOrEmpty(value) || (hasBaseSkins && index < fgCount))
            //    {
            //        colors.Add(index);
            //    }
            //}

            var locker = new object();
            var partition = Partitioner.Create(0, 256);

            Parallel.ForEach(
                partition,
                () => new List<int>(),
                (range, _, localValue) =>
                {
                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        string key = optName + "_fgc_" + index.ToString(CultureInfo.InvariantCulture);
                        string value = XwaHooksConfig.GetFileKeyValue(objectLines, key);

                        if (!string.IsNullOrEmpty(value) || (hasBaseSkins && index < fgCount))
                        {
                            localValue.Add(index);
                        }
                    }

                    return localValue;
                },
                localCount =>
                {
                    lock (locker)
                    {
                        colors.AddRange(localCount);
                    }
                });

            return colors;
        }

        private static string GetSkinDirectoryLocatorPath(string optName, string skinName)
        {
            string path = $"FlightModels\\Skins\\{optName}\\{skinName}";

            var baseDirectoryInfo = new DirectoryInfo(path);
            bool baseDirectoryExists = baseDirectoryInfo.Exists && baseDirectoryInfo.EnumerateFiles().Any();

            if (baseDirectoryExists)
            {
                return path;
            }

            if (File.Exists(path + ".zip"))
            {
                return path + ".zip";
            }

            if (File.Exists(path + ".7z"))
            {
                return path + ".7z";
            }

            return null;
        }

        private static OptFile _tempOptFile;
        private static int _tempOptFileSize;

        [DllExport(CallingConvention.Cdecl)]
        public static int ReadOptFunction([MarshalAs(UnmanagedType.LPStr)] string optFilename)
        {
            _tempOptFile = null;
            _tempOptFileSize = 0;

            if (!File.Exists(optFilename))
            {
                return 0;
            }

            string optName = Path.GetFileNameWithoutExtension(optFilename);

            var opt = OptFile.FromFile(optFilename);

            if (Directory.Exists($"FlightModels\\Skins\\{optName}"))
            {
                IList<string> objectLines = GetCustomFileLines("Skins");
                IList<string> baseSkins = XwaHooksConfig.Tokennize(XwaHooksConfig.GetFileKeyValue(objectLines, optName));
                bool hasDefaultSkin = GetSkinDirectoryLocatorPath(optName, "Default") != null || GetFlightgroupsDefaultCount(optName) != 0;
                int fgCount = GetFlightgroupsCount(objectLines, optName);
                bool hasSkins = hasDefaultSkin || baseSkins.Count != 0 || fgCount != 0;

                if (hasSkins)
                {
                    fgCount = Math.Max(fgCount, opt.MaxTextureVersion);
                    fgCount = Math.Max(fgCount, GetFlightgroupsDefaultCount(optName));
                    UpdateOptFile(optName, opt, objectLines, baseSkins, fgCount, hasDefaultSkin);
                }
            }

            if (_isD3dRendererHookEnabled)
            {
                opt.Meshes
                    .AsParallel()
                    .ForAll(mesh =>
                    {
                        foreach (MeshLod lod in mesh.Lods)
                        {
                            GroupFaceGroups(lod);
                        }
                    });
            }

            _tempOptFile = opt;
            _tempOptFileSize = opt.GetSaveRequiredFileSize(false);

            return _tempOptFileSize;
        }

        [DllExport(CallingConvention.Cdecl)]
        public static int GetOptVersionFunction()
        {
            if (_tempOptFile == null)
            {
                return 0;
            }

            return _tempOptFile.Version;
        }

        [DllExport(CallingConvention.Cdecl)]
        public static unsafe void WriteOptFunction(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero || _tempOptFile == null || _tempOptFileSize == 0)
            {
                _tempOptFile = null;
                _tempOptFileSize = 0;
                return;
            }

            using (var stream = new UnmanagedMemoryStream((byte*)ptr, _tempOptFileSize, _tempOptFileSize, FileAccess.Write))
            {
                _tempOptFile.Save(stream, false, false);
            }

            _tempOptFile = null;
            _tempOptFileSize = 0;
        }

        private static void GroupFaceGroups(MeshLod lod)
        {
            var groups = new List<FaceGroup>(lod.FaceGroups.Count);

            foreach (var faceGroup in lod.FaceGroups)
            {
                FaceGroup index = null;

                foreach (var group in groups)
                {
                    if (group.Textures.Count != faceGroup.Textures.Count)
                    {
                        continue;
                    }

                    if (group.Textures.Count == 0)
                    {
                        index = group;
                        break;
                    }

                    int t = 0;
                    for (; t < group.Textures.Count; t++)
                    {
                        if (group.Textures[t] != faceGroup.Textures[t])
                        {
                            break;
                        }
                    }

                    if (t == group.Textures.Count)
                    {
                        index = group;
                        break;
                    }
                }

                if (index == null)
                {
                    groups.Add(faceGroup);
                }
                else
                {
                    foreach (var face in faceGroup.Faces)
                    {
                        index.Faces.Add(face);
                    }
                }
            }

            lod.FaceGroups.Clear();

            foreach (var group in groups)
            {
                lod.FaceGroups.Add(group);
            }
        }

        private static void UpdateOptFile(string optName, OptFile opt, IList<string> objectLines, IList<string> baseSkins, int fgCount, bool hasDefaultSkin)
        {
            List<List<string>> fgSkins = ReadFgSkins(optName, objectLines, baseSkins, fgCount);
            List<string> distinctSkins = fgSkins.SelectMany(t => t).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            ICollection<string> texturesExist = GetTexturesExist(optName, opt, distinctSkins);
            List<int> fgColors = GetFlightgroupsColors(objectLines, optName, fgCount, hasDefaultSkin);
            CreateSwitchTextures(opt, texturesExist, fgSkins, fgColors);
            UpdateSkins(optName, opt, distinctSkins, fgSkins);
        }

        private static List<List<string>> ReadFgSkins(string optName, IList<string> objectLines, IList<string> baseSkins, int fgCount)
        {
            var fgSkins = new List<List<string>>(fgCount);

            for (int i = 0; i < fgCount; i++)
            {
                var skins = new List<string>(baseSkins);
                string fgKey = optName + "_fgc_" + i.ToString(CultureInfo.InvariantCulture);
                skins.AddRange(XwaHooksConfig.Tokennize(XwaHooksConfig.GetFileKeyValue(objectLines, fgKey)));

                if (skins.Count == 0)
                {
                    string skinName = "Default_" + i.ToString(CultureInfo.InvariantCulture);

                    if (GetSkinDirectoryLocatorPath(optName, skinName) != null)
                    {
                        skins.Add(skinName);
                    }
                    else
                    {
                        skins.Add("Default");
                    }
                }

                fgSkins.Add(skins);
            }

            return fgSkins;
        }

        private static ICollection<string> GetTexturesExist(string optName, OptFile opt, List<string> distinctSkins)
        {
            var texturesExist = new SortedSet<string>();

            foreach (string skin in distinctSkins)
            {
                string path = GetSkinDirectoryLocatorPath(optName, skin);

                if (path == null)
                {
                    continue;
                }

                SortedSet<string> filesSet;

                using (IFileLocator locator = FileLocatorFactory.Create(path))
                {
                    if (locator == null)
                    {
                        continue;
                    }

                    var filesEnum = locator.EnumerateFiles()
                        .Select(t => Path.GetFileName(t));

                    filesSet = new SortedSet<string>(filesEnum, StringComparer.OrdinalIgnoreCase);
                }

                foreach (string textureName in opt.Textures.Keys)
                {
                    if (TextureExists(filesSet, textureName, skin) != null)
                    {
                        texturesExist.Add(textureName);
                    }
                }
            }

            return texturesExist;
        }

        //private static void CreateSwitchTextures(OptFile opt, ICollection<string> texturesExist, List<List<string>> fgSkins, List<int> fgColors)
        //{
        //    int fgCount = fgSkins.Count;

        //    if (fgCount == 0)
        //    {
        //        return;
        //    }

        //    var newTextures = new ConcurrentBag<Texture>();

        //    opt.Textures
        //        .Where(texture => texturesExist.Contains(texture.Key))
        //        .AsParallel()
        //        .ForAll(texture =>
        //    {
        //        texture.Value.Convert8To32(false);

        //        foreach (int i in fgColors)
        //        {
        //            Texture newTexture = texture.Value.Clone();
        //            newTexture.Name += "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]);
        //            newTextures.Add(newTexture);
        //        }
        //    });

        //    foreach (var newTexture in newTextures)
        //    {
        //        opt.Textures.Add(newTexture.Name, newTexture);
        //    }

        //    foreach (var mesh in opt.Meshes)
        //    {
        //        foreach (var lod in mesh.Lods)
        //        {
        //            foreach (var faceGroup in lod.FaceGroups)
        //            {
        //                if (faceGroup.Textures.Count == 0)
        //                {
        //                    continue;
        //                }

        //                string name = faceGroup.Textures[0];

        //                if (!texturesExist.Contains(name))
        //                {
        //                    continue;
        //                }

        //                faceGroup.Textures.Clear();

        //                for (int i = 0; i < fgCount; i++)
        //                {
        //                    if (fgColors.Contains(i))
        //                    {
        //                        faceGroup.Textures.Add(name + "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]));
        //                    }
        //                    else
        //                    {
        //                        faceGroup.Textures.Add(name);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        private static void CreateSwitchTextures(OptFile opt, ICollection<string> texturesExist, List<List<string>> fgSkins, List<int> fgColors)
        {
            int fgCount = fgSkins.Count;

            if (fgCount == 0)
            {
                return;
            }

            var newTextures = new ConcurrentBag<Texture>();

            opt.Textures
                .Where(texture => texturesExist.Contains(texture.Key))
                .AsParallel()
                .ForAll(texture =>
                {
                    texture.Value.Convert8To32(false);

                    foreach (int i in fgColors)
                    {
                        Texture newTexture = texture.Value.Clone();
                        newTexture.Name += "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]);
                        newTextures.Add(newTexture);
                    }
                });

            foreach (var newTexture in newTextures)
            {
                opt.Textures.Add(newTexture.Name, newTexture);
            }

            opt.Meshes
                .SelectMany(t => t.Lods)
                .SelectMany(t => t.FaceGroups)
                .AsParallel()
                .ForAll(faceGroup =>
                {
                    if (faceGroup.Textures.Count == 0)
                    {
                        return;
                    }

                    string name = faceGroup.Textures[0];

                    if (!texturesExist.Contains(name))
                    {
                        return;
                    }

                    faceGroup.Textures.Clear();

                    for (int i = 0; i < fgCount; i++)
                    {
                        if (fgColors.Contains(i))
                        {
                            faceGroup.Textures.Add(name + "_fg_" + i.ToString(CultureInfo.InvariantCulture) + "_" + string.Join(",", fgSkins[i]));
                        }
                        else
                        {
                            faceGroup.Textures.Add(name);
                        }
                    }
                });
        }

        private static void UpdateSkins(string optName, OptFile opt, List<string> distinctSkins, List<List<string>> fgSkins)
        {
            var locatorsPath = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var filesSets = new ConcurrentDictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);

            distinctSkins.AsParallel().ForAll(skin =>
            {
                string path = GetSkinDirectoryLocatorPath(optName, skin);
                locatorsPath[skin] = path;

                SortedSet<string> filesSet = null;

                if (path != null)
                {
                    using (IFileLocator locator = FileLocatorFactory.Create(path))
                    {
                        if (locator != null)
                        {
                            var filesEnum = locator.EnumerateFiles()
                                .Select(t => Path.GetFileName(t));

                            filesSet = new SortedSet<string>(filesEnum, StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }

                filesSets[skin] = filesSet ?? new SortedSet<string>();
            });

            opt.Textures
                .AsParallel()
                .Where(texture => texture.Key.IndexOf("_fg_") != -1)
                .ForAll(texture =>
            {
                int position = texture.Key.IndexOf("_fg_");

                if (position == -1)
                {
                    return;
                }

                string textureName = texture.Key.Substring(0, position);
                int fgIndex = int.Parse(texture.Key.Substring(position + 4, texture.Key.IndexOf('_', position + 4) - position - 4), CultureInfo.InvariantCulture);

                foreach (string skin in fgSkins[fgIndex])
                {
                    string path = locatorsPath[skin];

                    if (path == null)
                    {
                        continue;
                    }

                    string filename = TextureExists(filesSets[skin], textureName, skin);

                    if (filename == null)
                    {
                        continue;
                    }

                    using (IFileLocator locator = FileLocatorFactory.Create(path))
                    {
                        if (locator == null)
                        {
                            continue;
                        }

                        CombineTextures(texture.Value, locator, filename);
                    }
                }

                texture.Value.GenerateMipmaps();
            });
        }

        private static void CombineTextures(Texture baseTexture, IFileLocator locator, string filename)
        {
            Texture newTexture;

            using (Stream file = locator.Open(filename))
            {
                newTexture = Texture.FromStream(file);
                newTexture.Name = Path.GetFileNameWithoutExtension(filename);
            }

            if (newTexture.Width != baseTexture.Width || newTexture.Height != baseTexture.Height)
            {
                return;
            }

            newTexture.Convert8To32(false);

            int size = baseTexture.Width * baseTexture.Height;
            byte[] src = newTexture.ImageData;
            byte[] dst = baseTexture.ImageData;

            //for (int i = 0; i < size; i++)
            //{
            //    int a = src[i * 4 + 3];

            //    dst[i * 4 + 0] = (byte)(dst[i * 4 + 0] * (255 - a) / 255 + src[i * 4 + 0] * a / 255);
            //    dst[i * 4 + 1] = (byte)(dst[i * 4 + 1] * (255 - a) / 255 + src[i * 4 + 1] * a / 255);
            //    dst[i * 4 + 2] = (byte)(dst[i * 4 + 2] * (255 - a) / 255 + src[i * 4 + 2] * a / 255);
            //}

            var partition = Partitioner.Create(0, size);

            Parallel.ForEach(partition, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int a = src[i * 4 + 3];

                    dst[i * 4 + 0] = (byte)(dst[i * 4 + 0] * (255 - a) / 255 + src[i * 4 + 0] * a / 255);
                    dst[i * 4 + 1] = (byte)(dst[i * 4 + 1] * (255 - a) / 255 + src[i * 4 + 1] * a / 255);
                    dst[i * 4 + 2] = (byte)(dst[i * 4 + 2] * (255 - a) / 255 + src[i * 4 + 2] * a / 255);
                }
            });
        }

        private static readonly string[] _textureExtensions = new string[] { ".bmp", ".png", ".jpg" };

        private static string TextureExists(ICollection<string> files, string baseFilename, string skin)
        {
            foreach (string ext in _textureExtensions)
            {
                string filename = baseFilename + "_" + skin + ext;

                if (files.Contains(filename))
                {
                    return filename;
                }
            }

            foreach (string ext in _textureExtensions)
            {
                string filename = baseFilename + ext;

                if (files.Contains(filename))
                {
                    return filename;
                }
            }

            return null;
        }

        [DllExport(CallingConvention.Cdecl)]
        unsafe public static void ReadCompressedDatImageFunction(byte* destination, int destinationLength, byte* source, int sourceLength)
        {
            if (_decoder == null)
            {
                _decoder = new SevenZip.Compression.LZMA.Decoder();
            }

            byte[] coderProperties = new byte[5];
            Marshal.Copy(new IntPtr(source), coderProperties, 0, 5);

            if (_decoderProperties == null
                || _decoderProperties[0] != coderProperties[0]
                || _decoderProperties[1] != coderProperties[1]
                || _decoderProperties[2] != coderProperties[2]
                || _decoderProperties[3] != coderProperties[3]
                || _decoderProperties[4] != coderProperties[4])
            {
                _decoderProperties = coderProperties;
                _decoder.SetDecoderProperties(coderProperties);
            }

            using var imageDecompressedStream = new UnmanagedMemoryStream(destination, destinationLength, destinationLength, FileAccess.Write);
            using var imageStream = new UnmanagedMemoryStream(source + 5, sourceLength - 5, sourceLength - 5, FileAccess.Read);
            _decoder.Code(imageStream, imageDecompressedStream, sourceLength - 5, destinationLength, null);
        }
    }
}
