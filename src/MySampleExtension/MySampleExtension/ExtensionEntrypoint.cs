using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace MySampleExtension
{
    /// <summary>
    /// Extension entrypoint for the VisualStudio.Extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ExtensionEntrypoint()
        {
#if DEBUG
            // F5でデバッグ実行できないため、デバッグ実行時にはデバッガを実行する
            // https://github.com/microsoft/VSExtensibility/issues/240
            System.Diagnostics.Debugger.Launch();
#endif
        }

        /// <inheritdoc />
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection);

            // You can configure dependency injection here by adding services to the serviceCollection.
        }
    }
}