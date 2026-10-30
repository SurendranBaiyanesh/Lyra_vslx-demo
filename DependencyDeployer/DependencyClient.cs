using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyDeployer
{
    public sealed class DependencyClient : IDisposable
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;

        public DependencyClient(string baseUrl, string accessToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            if (!string.IsNullOrEmpty(accessToken))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>Downloads one DLL into targetDir via a temp file, so a failed download never leaves a broken DLL.</summary>
        public async Task DownloadAsync(Dependency dep, string targetDir, CancellationToken ct)
        {
            var url = $"{_baseUrl}/dependencies/{Uri.EscapeDataString(dep.Name)}/{Uri.EscapeDataString(dep.Version)}";
            using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"{dep.Name} {dep.Version}: {(int)response.StatusCode} {response.ReasonPhrase} ({url})");

                Directory.CreateDirectory(targetDir);
                var target = Path.Combine(targetDir, dep.Name);
                var temp = target + ".download";
                using (var file = File.Create(temp))
                    await response.Content.CopyToAsync(file);
                if (File.Exists(target)) File.Delete(target);
                File.Move(temp, target);
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
