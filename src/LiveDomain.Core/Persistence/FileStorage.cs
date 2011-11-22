﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LiveDomain.Core
{
    /// <summary>
    /// Responsible for knowing about file names, formats and directory layout.
    /// </summary>
    internal class FileStorage : Storage
    {


        EngineConfiguration _config;


        internal FileStorage(EngineConfiguration config) : base(config)
        {
            _config = config;
        }

        public override void Initialize()
        {
            EnsureDirectoryExists(_config.TargetLocation);

            if (_config.HasAlternativeSnapshotLocation)
            {
                EnsureDirectoryExists(_config.SnapshotLocation); 
            }
        }


        private void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// List all the files in the snapshot and journal directories
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<string> GetItemIdentifiers()
        {
            foreach (string fileName in Directory.GetFiles(_config.TargetLocation))
            {
                yield return new FileInfo(fileName).Name;
            }

            if (_config.HasAlternativeSnapshotLocation)
            {
                foreach (string fileName in Directory.GetFiles(_config.SnapshotLocation))
                {
                    yield return new FileInfo(fileName).Name;
                }
            }
        }

        protected override void RemoveSnapshot(string id)
        {
            string path = Path.Combine(_config.SnapshotLocation, id);
            File.Delete(path);
        }

        protected override Stream GetReadStream(string id)
        {
            string path = GetFullyQualifiedPath(id);
            Stream stream = File.OpenRead(path);
            if (_config.Compression == CompressionMethod.GZip)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            return stream;
        }

        protected override bool Exists(string id)
        {
            return File.Exists(GetFullyQualifiedPath(id));
        }

        protected override Stream GetWriteStream(string id, bool append)
        {
            string path = GetFullyQualifiedPath(id);

            var filemode = append ? FileMode.Append : FileMode.Create;
            
            Stream stream = new FileStream(path, filemode, FileAccess.Write);
            if (_config.Compression == CompressionMethod.GZip)
            {
                stream = new GZipStream(stream, CompressionMode.Compress);
            }
            return stream;
        }

        private void VerifyDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                if (!DirectoryEx.IsEmpty(directory))
                {
                    throw new ApplicationException("Directory must be empty: " + directory);
                }
            }
        }

        /// <summary>
        /// Use correct directory based on filetype, journal or snapshot.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string GetFullyQualifiedPath(string id)
        {
            string directory = _config.TargetLocation;
            if (id.EndsWith(StorageFragmentIdentifier.SnapshotSuffix))
            {
                directory = _config.SnapshotLocation;
            }
            return Path.Combine(directory,id);
        }

        public override void VerifyCanCreate()
        {
            VerifyDirectory(_config.TargetLocation);
            if (_config.HasAlternativeSnapshotLocation)
                VerifyDirectory(_config.SnapshotLocation);
        }

        public override void VerifyCanLoad()
        {
            string error = String.Empty;
            if (!Directory.Exists(_config.TargetLocation))
            {
                error = "Target directory does not exist\n";
            }

            if (_config.HasAlternativeSnapshotLocation)
            {
                if (!Directory.Exists(_config.SnapshotLocation))
                {
                    error += "Snapshot directory does not exist\n";
                }
            }

            string initialSnapshot = Path.Combine(_config.SnapshotLocation, "000000000.snapshot");
            if (!File.Exists(initialSnapshot))
            {
                error += "Initial snapshot missing\n";
            }

            if (Directory.GetFiles(_config.TargetLocation, "*.journal").Count() == 0)
            {
                error += "No journal files found\n";
            }

            if (error != String.Empty)
            {
                throw new ApplicationException("Error(s) loading: " + error);   
            }
        }

    }
}