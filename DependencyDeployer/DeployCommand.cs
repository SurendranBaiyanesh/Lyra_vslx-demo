using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DependencyDeployer
{
    [Command(PackageIds.DeployCommand)]
    internal sealed class DeployCommand : BaseCommand<DeployCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var projects = await GetTargetProjectsAsync();
            var options = await General.GetLiveInstanceAsync();
            var pane = await VS.Windows.CreateOutputWindowPaneAsync("Dependency Deployer");
            await pane.ActivateAsync();

            int ok = 0, failed = 0;
            using (var client = new DependencyClient(options.BaseUrl, options.AccessToken))
            {
                foreach (var project in projects)
                {
                    var projectDir = Path.GetDirectoryName(project.FullPath);
                    var manifest = DependencyManifest.Find(projectDir);
                    if (manifest == null) continue;

                    await pane.WriteLineAsync($"== {project.Name} ({Path.GetFileName(manifest)})");
                    List<Dependency> deps;
                    try { deps = DependencyManifest.Load(manifest); }
                    catch (Exception ex) { failed++; await pane.WriteLineAsync($"   ERROR {ex.Message}"); continue; }

                    var targetDir = Path.Combine(projectDir, options.TargetFolder);
                    foreach (var dep in deps)
                    {
                        await VS.StatusBar.ShowMessageAsync($"Deploying {dep.Name} {dep.Version}...");
                        try
                        {
                            await client.DownloadAsync(dep, targetDir, CancellationToken.None);
                            ok++;
                            await pane.WriteLineAsync($"   OK    {dep.Name} {dep.Version}");
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            await pane.WriteLineAsync($"   ERROR {ex.Message}");
                        }
                    }
                }
            }

            var summary = $"Dependency deploy finished: {ok} succeeded, {failed} failed.";
            await pane.WriteLineAsync(summary);
            await VS.StatusBar.ShowMessageAsync(summary);
        }

        // project node selected -> that project; solution node -> every project in the solution
        private static async System.Threading.Tasks.Task<IEnumerable<Project>> GetTargetProjectsAsync()
        {
            var item = await VS.Solutions.GetActiveItemAsync();
            if (item is Project p) return new[] { p };
            return await VS.Solutions.GetAllProjectsAsync();
        }
    }
}
