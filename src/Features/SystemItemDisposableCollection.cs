using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Features
{
    public class SystemItemDisposableCollection : IDisposable
    {
        public bool KeepFiles = false;
        private readonly IList<SystemItem> _disposableSystemItems = new List<SystemItem>();
        private string RootDirectory { get; }

        private bool disposedValue = false;

        public SystemItemDisposableCollection(List<SystemItem> items, string rootDirectory)
        {
            this._disposableSystemItems = items;
            this.RootDirectory = rootDirectory;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue && !KeepFiles)
            {
                if (disposing)
                {
                    foreach (var item in _disposableSystemItems)
                    {
                        if (item != null)
                        {
                            item.Dispose();
                        }
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            if (!KeepFiles)
            {
                Directory.Delete(this.RootDirectory, true);
            }
        }
    }


    public class SystemItem : IDisposable
    {
        public Type Type { get; }
        public string Filepath { get; }

        public SystemItem(Image _, string filepath)
        {
            this.Type = typeof(Image);
            this.Filepath = filepath;
        }

        public SystemItem(string filepath)
        {
            this.Type = typeof(FileInfo);
            this.Filepath = filepath;
        }

        public void Dispose()
        {
            File.Delete(this.Filepath);
        }
    }
}