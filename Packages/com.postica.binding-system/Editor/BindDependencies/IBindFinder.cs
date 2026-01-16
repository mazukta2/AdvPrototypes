using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Dependencies
{
    internal interface IBindFinder
    {
        string Name { get; }
        Texture2D Icon { get; }
        Task<IEnumerable<BindDependencyGroup>> FindDependencies(Object root, string path, Action<float> progress, CancellationToken cancellationToken);
    }
    
    internal interface IBatchBindFinder : IBindFinder
    {
        void AddToBatch(Object root, string path, Action<float> progress);
        void PauseBatch();
        void ResumeBatch();
        void ClearBatch();
        void StopBatch() 
        {
            PauseBatch();
            ClearBatch();
        }
        Task<IEnumerable<BindDependencyGroup>> BatchFind(Action<float> progressCallback, CancellationToken cancellationToken);
    }

    internal class BindDependencyGroup
    {
        public string Name { get; }
        public List<BindDependencyGroup> SubGroups { get; } = new();
        public List<BindDependency> Dependencies { get; }
        public Texture Icon { get; internal set; }

        public BindDependencyGroup(string name, Texture icon = null)
        {
            Name = name;
            Icon = icon;
            Dependencies = new List<BindDependency>();
        }
    }
    
    internal class BindDependency
    {
        public Object Source { get; }
        public Object Target { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }

        public BindDependency(Object source, Object target, string sourcePath, string targetPath)
        {
            Source = source;
            Target = target;
            SourcePath = sourcePath.Replace('/', '.');
            TargetPath = targetPath;
        }
    }
}