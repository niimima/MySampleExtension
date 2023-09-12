using Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.ProjectSystem.Query;
using System.Diagnostics;

namespace MySampleExtension
{
    /// <summary>
    /// Command1 handler.
    /// </summary>
    [VisualStudioContribution]
    internal class ForceDebugCommand : Command
    {
        private readonly TraceSource logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForceDebugCommand"/> class.
        /// </summary>
        /// <param name="extensibility">Extensibility object.</param>
        /// <param name="traceSource">Trace source instance to utilize.</param>
        public ForceDebugCommand(VisualStudioExtensibility extensibility, TraceSource traceSource)
            : base(extensibility)
        {
            // This optional TraceSource can be used for logging in the command. You can use dependency injection to access
            // other services here as well.
            this.logger = Requires.NotNull(traceSource, nameof(traceSource));
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%MySampleExtension.ForceDebugCommand.DisplayName%")
        {
            // Use this object initializer to set optional parameters for the command. The required parameter,
            // displayName, is set above. DisplayName is localized and references an entry in .vsextension\string-resources.json.
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
            Placements = new[] { CommandPlacement.KnownPlacements.ExtensionsMenu },
            Shortcuts = new[] { new CommandShortcutConfiguration(ModifierKey.LeftAlt, Key.F5) },
        };

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Use InitializeAsync for any one-time setup or initialization.
            return base.InitializeAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            var workSpace = Extensibility.Workspaces();

            // 取得したいプロパティまで以下のように取ることもできる
            var projects = await workSpace.QueryProjectsAsync(
            project => project.With(project => project.Name)
                            .With(project => project.Path)
                            .With(project => project.Files.With(file => file.FileName))
                            .With(project => project.LaunchProfiles.With(profile => profile.Name))
                            .With(project => project.Configurations.With(config => config.Name)),
            cancellationToken);

            // 取得方法が分からないのでプロジェクト名からプロセスを特定してKill
            // コンソールプログラムの場合はプロセス名が異なるためこれではKillできない
            var projectName = string.Empty;
            var processes = Process.GetProcesses();
            foreach (var project in projects)
            {
                var target = processes.Where(process => IsProjectProcess(process, project.Name)).FirstOrDefault();
                if (target == null) continue;

                // 実行中のプロジェクト名を保持
                projectName = project.Name;

                // プロセスキルした後すぐにソリューションの操作をすると確認ダイアログが出てくるため遅延して待機する
                target.Kill();
                target.WaitForExit();
                await Task.Delay(1000);

                break;
            }

            // ソリューションからも同様にプロパティを取得できる
            var solutions = await workSpace.QuerySolutionAsync(
            solution => solution.With(solution => solution.ActiveConfiguration)
                            .With(solution => solution.ActivePlatform)
                            .With(solution => solution.Projects.With(project => project.Name)),
            cancellationToken);
            // ソリューションをデバッグ実行
            foreach (var solution in solutions)
            {
                // キルしたプロジェクトがないか、キルしたプロジェクトを持っていなければ何もしない
                if (string.IsNullOrEmpty(projectName) == false && solution.Projects.Any(project => project.Name == projectName) == false) continue;

                // TODO 仕組みをよくわかっておらず、なぜQueryableにしないといけないのかわかっていない
                await solution.AsQueryable().BuildAsync(cancellationToken);
                await solution.AsQueryable().DebugLaunchAsync(cancellationToken);

                // 1件のみ実行する
                break;
            }
        }

        /// <summary>
        /// 対象プロジェクトのプロセスかを取得する
        /// </summary>
        /// <param name="process">プロセス</param>
        /// <param name="projectName">プロジェクト名</param>
        /// <returns>対象プロジェクトのプロセスか</returns>
        private bool IsProjectProcess(Process process, string projectName)
        {
            try
            {
                var mainModule = process.MainModule;
                if (mainModule == null) return false;

                var fileName = mainModule.FileName;
                if (string.IsNullOrEmpty(fileName)) return false;

                return fileName.Contains(projectName);
            }
            catch
            {
                // 例外発生時も対象外
                return false;
            }
        }
    }
}