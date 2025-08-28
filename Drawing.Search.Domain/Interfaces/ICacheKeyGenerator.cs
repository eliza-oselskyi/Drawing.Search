namespace Drawing.Search.Domain.Interfaces;

public interface ICacheKeyGenerator
{
    string GenerateDrawingKey(string drawingId);
    string GenerateDrawingObjectKey(string drawingId, string objectId);
    string GenerateAssemblyKey(string drawingId, string assemblyId);
}