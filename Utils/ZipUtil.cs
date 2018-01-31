using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;

namespace incadea.WsCrm.DeploymentTool.Utils
{
    /// <summary>
    /// util to zip and unzip
    /// </summary>
    public class ZipUtil
    {
        /// <summary>
        /// compile folder content to zip file
        /// </summary>
        /// <param name="fromPath">path to folder</param>
        /// <param name="toPath">path to result zip file</param>
        /// <param name="compression">type of compression (NoCompressionByDefault)</param>
        public void ZipFolder(string fromPath, string toPath, CompressionLevel compression = CompressionLevel.NoCompression)
        {
            var rootDir = new DirectoryInfo(fromPath);
            if (File.Exists(toPath))
            {
                File.Delete(toPath);
            }
            using (var toStream = File.OpenWrite(toPath))
            using (var archive = new ZipArchive(toStream, ZipArchiveMode.Create))
            {
                ZipFolder(rootDir, string.Empty, archive, compression);
            }
        }

        /// <summary>
        /// unzip file to folder
        /// </summary>
        /// <param name="fromPath">path to result zip file</param>
        /// <param name="toPath">path to folder</param>
        public void UnzipFolder(string fromPath, string toPath)
        {
            if (Directory.Exists(toPath))
            {
                Directory.Delete(toPath, true);
            }

            var rootDir = Directory.CreateDirectory(toPath);

            using (var fromStream = File.OpenRead(fromPath))
            using (var archive = new ZipArchive(fromStream, ZipArchiveMode.Read))
            {
                foreach (var archiveEntry in archive.Entries)
                {
                    if (!archiveEntry.FullName.EndsWith("/"))
                    {
                        var fileinfo = new FileInfo(Path.Combine(rootDir.FullName, archiveEntry.FullName));
                        Directory.CreateDirectory(fileinfo.DirectoryName);
                        using (var entry = archiveEntry.Open())
                        using (var file = fileinfo.OpenWrite())
                        {
                            entry.CopyTo(file);
                        }
                    }
                }
            }
        }

        private void ZipFolder(DirectoryInfo folder, string path, ZipArchive archive,
            CompressionLevel compression = CompressionLevel.NoCompression)
        {
            folder.EnumerateFiles().ToList()
                .ForEach(file =>
                {
                    using (var zipFile = archive.CreateEntry($"{path}{file.Name}", compression).Open())
                    using (var stream = file.OpenRead())
                    {
                        stream.CopyTo(zipFile);
                    }
                });
            folder.EnumerateDirectories().ToList()
                .ForEach(dir => ZipFolder(dir, $"{path}{dir.Name}/", archive, compression));
        }


        /// <summary>
        /// opens azure package and updates config files
        /// </summary>
        /// <param name="archivePath">path to package</param>
        /// <param name="changeConfig">update action</param>
        public void Update(string archivePath, Action<Stream> changeConfig)
        {
            using (var mainarchive = new ZipArchive(File.Open(archivePath, FileMode.Open), ZipArchiveMode.Update))
            {
                foreach (var contentEntry in mainarchive.Entries.Where(e => e.FullName.EndsWith(".cssx")))
                {
                    string hash;
                    using (var contentStream = contentEntry.Open())
                    using (var archive = new ZipArchive(contentStream, ZipArchiveMode.Update))
                    {
                        var appEntry = archive.GetEntry("approot/Web.config");
                        if (appEntry != null)
                        {
                            using (var config = appEntry.Open())
                            {
                                changeConfig(config);
                            }
                        }
                        var entry = archive.GetEntry("sitesroot/0/Web.config");
                        if (entry != null)
                        {
                            using (var config = entry.Open())
                            {
                                changeConfig(config);
                            }
                        }
                    }
                    using (var contentStream = contentEntry.Open())
                    {
                        var hashBytes = new SHA256Managed().ComputeHash(contentStream);
                        hash = BitConverter.ToString(hashBytes).Replace("-", "");
                    }
                    var manifest = mainarchive.Entries.Single(entry => entry.FullName.EndsWith(".csman"));
                    using (var manStream = manifest.Open())
                    {
                        var doc = new XmlDocument();
                        doc.Load(manStream);
                        var item = doc.SelectSingleNode($"//Item[@name='{contentEntry.Name}']");
                        item.Attributes["hash"].Value = hash;
                        manStream.SetLength(0);
                        doc.Save(manStream);
                    }
                }
            }
        }
    }
}
