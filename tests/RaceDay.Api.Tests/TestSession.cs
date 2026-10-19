using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Tests;

// Small in-memory session implementation used only by the unit tests.
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public void Clear() => _sessionStorage.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _sessionStorage.Remove(key);
    public void Set(string key, byte[] value) => _sessionStorage[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value!);
}
