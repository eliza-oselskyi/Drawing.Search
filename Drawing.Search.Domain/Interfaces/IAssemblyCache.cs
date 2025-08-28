using System.Collections.Generic;

namespace Drawing.Search.Domain.Interfaces;

public interface IAssemblyCache
{
    void CacheAssemblyPosition(string identifier, string assemblyPosition);
    IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPosition);
    IEnumerable<object> GetAssemblyObjects(string assemblyPosition);
    IEnumerable<string> GetAllAssemblyPositions();
}