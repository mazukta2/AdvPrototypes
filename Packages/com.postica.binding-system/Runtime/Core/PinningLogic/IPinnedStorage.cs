using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.PinningLogic
{
    public interface IPinnedStorage
    {
        string Id { get; }
        IEnumerable<PinnedPath> AllPaths { get; }

        void AddPath(PinnedPath path);
        void RemovePath(PinnedPath path);
        void Clear();

        void StorePinUsage(Object context);
        IEnumerable<Object> GetLastUsedPins();
        
        bool ContainsPath(PinnedPath path);
        IEnumerable<PinnedPath> GetPathsForObject(Object obj);
    }
}
